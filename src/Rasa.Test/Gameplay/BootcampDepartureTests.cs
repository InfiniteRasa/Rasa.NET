extern alias RasaGame;

using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Data;
    using Game;
    using Managers;
    using Missions;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.Char.Character;
    using Repositories.Char.CharacterAbilityDrawer;
    using Repositories.Char.CharacterMission;
    using Repositories.Char.CharacterFlag;
    using Repositories.Char.CharacterSkills;
    using Repositories.Char.CharacterStartingExperience;
    using Repositories.Char.CharacterTeleporter;
    using Repositories.Char.GameAccount;
    using Structures;
    using Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class BootcampDepartureTests
    {
        [TestMethod]
        public void StandingInALockedExtractionBeamDoesNotAutoBoardWhenCheckInUnlocksIt()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(6192);
            var characterId = context.SeedCharacter(6192, 1, "WaitForEntry",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225, y: 101.12099, z: -71);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale,
                MissionState.Active, completeable: true);
            var client = context.CreateSelectionClient(6192);
            context.Characters.RequestSwitchToCharacterInSlot(client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);
            var triggers = new MapTriggerManager(context.Objects);
            var mission = client.Player.Missions[BootcampSelectionTestContext.MissionFinale];
            mission.Completeable = false;
            triggers.TriggersProximityWorker(client.Player.MapChannel);
            mission.Completeable = true;

            triggers.TriggersProximityWorker(client.Player.MapChannel);

            Assert.IsNull(client.PendingTransfer,
                "An availability change must not count as the player's decision to enter the beam.");
            client.Player.Position += new System.Numerics.Vector3(10, 0, 0);
            triggers.TriggersProximityWorker(client.Player.MapChannel);
            client.Player.Position -= new System.Numerics.Vector3(10, 0, 0);
            triggers.TriggersProximityWorker(client.Player.MapChannel);
            Assert.IsNotNull(client.PendingTransfer);
        }

        [TestMethod]
        public void EnteringReadyBootcampBeamBoardsDirectlyWithoutDiscoveringWaypoint()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(6190);
            var characterId = context.SeedCharacter(6190, 1, "DirectExtraction",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225, y: 101.12099, z: -71);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale,
                MissionState.Active, completeable: true);
            var client = context.CreateSelectionClient(6190);
            context.Characters.RequestSwitchToCharacterInSlot(client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);
            MissionTestContext.Drain(client);

            new MapTriggerManager(context.Objects).TriggersProximityWorker(client.Player.MapChannel);

            Assert.IsNotNull(client.PendingTransfer,
                "Entering the ready extraction beam must start departure without a waypoint selection.");
            Assert.IsTrue(client.PendingTransfer.IsDropship);
            Assert.AreEqual(BootcampSelectionTestContext.WildernessMapContextId,
                client.PendingTransfer.DestinationMap.MapInfo.MapContextId);
            Assert.IsFalse(client.Player.GainedWaypoints.Any(waypoint =>
                waypoint.WaypointId == BootcampSelectionTestContext.ExitPadWaypointId));
            var packets = MissionTestContext.Drain(client);
            Assert.IsFalse(packets.OfType<EnteredWaypointPacket>().Any());
            Assert.IsFalse(packets.OfType<WaypointGainedPacket>().Any(packet =>
                packet.WaypointId == BootcampSelectionTestContext.ExitPadWaypointId));
            using var verify = context.OpenChar();
            Assert.IsFalse(verify.CharacterTeleporterEntries.Any(waypoint =>
                waypoint.CharacterId == characterId &&
                waypoint.WaypointId == BootcampSelectionTestContext.ExitPadWaypointId));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PreviouslyDiscoveredBootcampPadCannotBeOfferedOrSelectedForReturn(bool select)
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(6191);
            var source = context.Objects.Teleporters.Values.First(station =>
                station.MapContextId == BootcampSelectionTestContext.WildernessMapContextId &&
                station.ObjectData is WaypointInfo { WaypointType: WaypointType.Dropship });
            var characterId = context.SeedCharacter(6191, 1, "NoReturn",
                x: source.Position.X, y: source.Position.Y, z: source.Position.Z);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Completed);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            var client = context.CreateSelectionClient(6191);
            context.Characters.RequestSwitchToCharacterInSlot(client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);

            if (select)
            {
                context.Objects.SelectWaypoint(client, new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = 0
                });
                Assert.IsNull(client.PendingTransfer,
                    "An old discovery row or forged selection must not admit travel back to Bootcamp.");
            }
            else
            {
                var destinations = context.Objects.CreateListOfDropships(client);
                Assert.IsFalse(destinations.Values.SelectMany(map => map.Waypoints).Any(waypoint =>
                    waypoint.WaypointId == BootcampSelectionTestContext.ExitPadWaypointId),
                    "Bootcamp extraction must never be advertised as a return destination.");
            }
        }

        [TestMethod]
        public void VanHandoffNoLongerTransfersOrGrantsDepartureUntilTheExitPadIsUsed()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var youngblood = harness.AddNpc(510207, 2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, true);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Context.Client,
                youngblood.EntityId,
                1995));
            harness.Context.Drain();
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Area(1995, 435)));
            harness.UseObjectAndRecover(ConradCorpseDialogueTests.Corpse(harness));
            harness.UtcNow += TimeSpan.FromSeconds(5);
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Context.Client));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Interaction(24586)));
            harness.UtcNow += TimeSpan.FromSeconds(5);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Context.Client));
            harness.UtcNow += TimeSpan.FromSeconds(2);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Context.Client));

            var van = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId);
            Assert.IsNotNull(van);
            BootcampExtractionAssaultTests.DefeatAll(harness);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Context.Client,
                van.EntityId,
                1995,
                4,
                1));

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, harness.Context.Client.State);
            Assert.IsNull(harness.Context.Client.PendingTransfer);
            using var verify = harness.Context.CreateChar();
            Assert.IsFalse(verify.CharacterFlags.HasValue(
                harness.Context.Client.Player.Id,
                CharacterFlagIds.BootcampComplete));
            Assert.IsFalse(verify.GameAccounts.Get(harness.Context.Client.AccountEntry.Id).CanSkipBootcamp);
        }

        [TestMethod]
        public void ExitPadDepartureCommitsParityTransfersToAliaAndReleasesThePrivateRuntime()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(61);
            var characterId = context.SeedCharacter(
                61,
                1,
                "Departure",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                rotation: 0,
                experience: 49250,
                level: 5);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            var client = context.CreateSelectionClient(61);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });
            context.MaterializeLoadedClient(client);

            var privateMap = context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId);
            Assert.IsNotNull(privateMap);

            context.Objects.SelectWaypoint(
                client,
                new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = privateMap.InstanceId
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, client.State);
            Assert.IsTrue(client.PendingTransfer.IsDropship);
            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(
                BootcampSelectionTestContext.WildernessMapContextId,
                client.PendingTransfer.DestinationMap.MapInfo.MapContextId);

            context.CompletePendingDeparture(client);

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, client.State);
            Assert.AreEqual(
                BootcampSelectionTestContext.WildernessMapContextId,
                client.Player.MapContextId);
            Assert.IsNull(context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId));

            using var verify = context.OpenChar();
            var durableCharacter = new CharacterRepository(verify).Get(characterId);
            var levelFourExperience = context.ReadExperienceForLevel(4);
            Assert.AreEqual(24000L, levelFourExperience);
            Assert.AreEqual(24000U, (uint)levelFourExperience);
            Assert.AreEqual(24000U, durableCharacter.Experience);
            Assert.AreEqual((byte)4, durableCharacter.Level);
            Assert.AreEqual(
                CharacterStartingExperienceState.Completed,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.IsTrue(new CharacterFlagRepository(verify).HasValue(
                characterId,
                CharacterFlagIds.BootcampComplete));
            Assert.IsTrue(new GameAccountRepository(verify).Get(61).CanSkipBootcamp);

            var rogers = new Creature
            {
                DbId = 100,
                Npc = new Npc { NpcPackageId = 100 }
            };
            var status = context.Missions.ClassifyNpcConversation(client.Player, rogers);
            Assert.IsTrue(status.TryGetStatus(out var conversationStatus, out var missionIds));
            Assert.AreEqual(ConversationStatus.MissionComplete, conversationStatus);
            CollectionAssert.Contains(missionIds, BootcampSelectionTestContext.MissionFinale);
            Assert.AreEqual(
                MissionState.Active,
                client.Player.Missions[BootcampSelectionTestContext.MissionFinale].State);
            Assert.IsTrue(
                client.Player.Missions[BootcampSelectionTestContext.MissionFinale].Completeable);
        }

        [TestMethod]
        public void ExitPadDeparturePublishesWaypointUnlocksAndDestinationMarkersWithoutReconnect()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(613);
            var characterId = context.SeedCharacter(
                613,
                1,
                "WaypointUnlocks",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                experience: 49250,
                level: 5);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            context.SeedLightningGrant(characterId);
            context.SeedPersonalInventory(613, characterId, 13066, 13096, 13156, 13186, 13713, 28);
            Server.GameUnitOfWorkFactory = context;
            MapMarkerManager.Instance.MapMarkerInit();

            var client = context.CreateSelectionClient(613);
            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);
            MissionTestContext.Drain(client);

            var privateMap = context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId);
            Assert.IsNotNull(privateMap);

            context.Objects.SelectWaypoint(
                client,
                new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = privateMap.InstanceId
                });

            CollectionAssert.IsSubsetOf(
                new[] { BootcampSelectionTestContext.AliaDasWaypointId, BootcampSelectionTestContext.AliaDasHospitalId },
                client.Player.GainedWaypoints.Select(entry => entry.WaypointId).ToArray());

            var departurePackets = MissionTestContext.Drain(client);
            CollectionAssert.AreEquivalent(
                new[] { BootcampSelectionTestContext.AliaDasWaypointId, BootcampSelectionTestContext.AliaDasHospitalId },
                departurePackets.OfType<WaypointGainedPacket>()
                    .Select(packet => packet.WaypointId)
                    .ToArray());
            Assert.AreEqual(
                1,
                departurePackets.OfType<WaypointGainedPacket>()
                    .Count(packet => packet.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId));
            Assert.AreEqual(
                1,
                departurePackets.OfType<WaypointGainedPacket>()
                    .Count(packet => packet.WaypointId == BootcampSelectionTestContext.AliaDasHospitalId));
            Assert.AreEqual(0, departurePackets.OfType<UpdateMapMarkerPacket>().Count());

            context.CompletePendingDeparture(client);
            MissionTestContext.Drain(client);
            MapMarkerManager.Instance.PlayerEnteredMap(client);

            using (var verify = context.OpenChar())
            {
                CollectionAssert.AreEquivalent(
                    new[] { BootcampSelectionTestContext.AliaDasWaypointId, BootcampSelectionTestContext.AliaDasHospitalId },
                    new CharacterTeleporterRepository(verify).Get(characterId)
                        .Where(entry =>
                            entry.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId ||
                            entry.WaypointId == BootcampSelectionTestContext.AliaDasHospitalId)
                        .Select(entry => entry.WaypointId)
                        .ToArray());
            }

            var arrivalPackets = MissionTestContext.Drain(client);
            var markerInfo = arrivalPackets.OfType<MapMarkerInfoPacket>().Single();
            using (var world = context.CreateWorld())
            {
                var markers = world.MapMarkers.GetMapMarkers()
                    .Where(entry =>
                        entry.MapContextId == BootcampSelectionTestContext.WildernessMapContextId &&
                        (entry.ObjectId == BootcampSelectionTestContext.AliaDasWaypointId ||
                         entry.ObjectId == BootcampSelectionTestContext.AliaDasHospitalId))
                    .ToArray();
                Assert.IsTrue(markers.Length > 0);
                foreach (var marker in markers)
                {
                    Assert.IsTrue(markerInfo.Markers.ContainsKey(marker.MarkerEntityId));
                    Assert.IsTrue(markerInfo.Markers[marker.MarkerEntityId].IsKnown);
                }
            }

            var waypointMenu = context.Objects.CreateListOfWaypoints(client, WaypointType.Waypoint)
                [BootcampSelectionTestContext.WildernessMapContextId];
            CollectionAssert.Contains(
                waypointMenu.Waypoints.Select(waypoint => waypoint.WaypointId).ToArray(),
                BootcampSelectionTestContext.AliaDasWaypointId);

            var aliaWaypoint = client.Player.MapChannel.Teleporters.Values.Single(teleporter =>
                teleporter.ObjectData is WaypointInfo waypoint &&
                waypoint.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId);
            client.Player.Position = aliaWaypoint.Position;
            context.Objects.SelectWaypoint(
                client,
                new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.AliaDasWaypointId,
                    MapInstanceId = client.Player.MapChannel.InstanceId
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Teleporting, client.State);
            Assert.IsNotNull(client.PendingTransfer);
        }

        [TestMethod]
        public void ExitPadDepartureMatchesSkipParityAtLevelFourUsingTheAuthoritativeThreshold()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(611);
            var departedCharacterId = context.SeedCharacter(
                611,
                1,
                "Departed",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                rotation: 0,
                experience: 49250,
                level: 5);
            context.SeedStartingExperience(departedCharacterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(departedCharacterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(departedCharacterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            SeedNormalDepartureParityState(context, 611, departedCharacterId);

            var departedClient = context.CreateSelectionClient(611);
            context.Characters.RequestSwitchToCharacterInSlot(
                departedClient,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(departedClient);
            context.Objects.SelectWaypoint(
                departedClient,
                new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = departedClient.Player.MapChannel.InstanceId
                });
            context.CompletePendingDeparture(departedClient);

            context.SeedAccount(612, canSkipBootcamp: true);
            var skippedCharacterId = context.SeedCharacter(612, 1, "Skipped");
            context.SeedStartingExperience(skippedCharacterId, CharacterStartingExperienceState.Pending);
            var skippedClient = context.CreateSelectionClient(612);
            context.Characters.RequestSwitchToCharacterInSlot(
                skippedClient,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = true
                });

            using var verify = context.OpenChar();
            var characters = new CharacterRepository(verify);
            var starts = new CharacterStartingExperienceRepository(verify);
            var teleports = new CharacterTeleporterRepository(verify);
            var skills = new CharacterSkillsRepository(verify);
            var trays = new CharacterAbilityDrawerRepository(verify);
            var departed = characters.Get(departedCharacterId);
            var skipped = characters.Get(skippedCharacterId);
            var levelFourExperience = context.ReadExperienceForLevel(4);

            Assert.AreEqual(24000L, levelFourExperience);
            Assert.AreEqual(ExpPerLevel.ExpRequred[3], levelFourExperience);
            Assert.AreEqual((uint)levelFourExperience, departed.Experience);
            Assert.AreEqual((uint)levelFourExperience, skipped.Experience);
            Assert.AreEqual((byte)4, departed.Level);
            Assert.AreEqual((byte)4, skipped.Level);
            Assert.AreEqual((uint)CharacterClass.Recruit, departed.Class);
            Assert.AreEqual((uint)CharacterClass.Recruit, skipped.Class);
            Assert.AreEqual(
                CharacterStartingExperienceState.Completed,
                starts.Get(departedCharacterId).State);
            Assert.AreEqual(
                CharacterStartingExperienceState.Skipped,
                starts.Get(skippedCharacterId).State);
            CollectionAssert.AreEqual(
                context.ReadInventoryTemplates(611, departedCharacterId),
                context.ReadInventoryTemplates(612, skippedCharacterId));
            var departedWaypoints = teleports.Get(departedCharacterId).Select(entry => entry.WaypointId).ToArray();
            var skippedWaypoints = teleports.Get(skippedCharacterId).Select(entry => entry.WaypointId).ToArray();
            CollectionAssert.IsSubsetOf(skippedWaypoints, departedWaypoints);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    BootcampSelectionTestContext.AliaDasWaypointId,
                    BootcampSelectionTestContext.AliaDasHospitalId
                },
                skippedWaypoints);
            Assert.AreEqual(
                1,
                skills.GetCharacterSkills(departedCharacterId).Count(entry =>
                    entry.SkillId == (uint)SkillId.Lightning &&
                    entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                    entry.SkillLevel == 1));
            Assert.AreEqual(
                1,
                skills.GetCharacterSkills(skippedCharacterId).Count(entry =>
                    entry.SkillId == (uint)SkillId.Lightning &&
                    entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                    entry.SkillLevel == 1));
            Assert.AreEqual(
                1,
                trays.GetCharacterAbilities(departedCharacterId).Count(entry =>
                    entry.AbilitySlot == 0 &&
                    entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                    entry.AbilityLevel == 1));
            Assert.AreEqual(
                1,
                trays.GetCharacterAbilities(skippedCharacterId).Count(entry =>
                    entry.AbilitySlot == 0 &&
                    entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                    entry.AbilityLevel == 1));

            var manifestation = new ManifestationManager(context);
            Assert.AreEqual(9, manifestation.GetAvailableAttributePoints(departedClient.Player));
            Assert.AreEqual(9, manifestation.GetAvailableAttributePoints(skippedClient.Player));
            Assert.AreEqual(10, manifestation.GetSkillPointsAvailable(departedClient.Player));
            Assert.AreEqual(10, manifestation.GetSkillPointsAvailable(skippedClient.Player));

            CollectionAssert.AreEqual(
                skippedClient.Player.GainedWaypoints.Select(entry => entry.WaypointId).OrderBy(id => id).ToArray(),
                departedClient.Player.GainedWaypoints
                    .Where(entry =>
                        entry.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId ||
                        entry.WaypointId == BootcampSelectionTestContext.AliaDasHospitalId)
                    .Select(entry => entry.WaypointId)
                    .OrderBy(id => id)
                    .ToArray());
        }

        [TestMethod]
        public void ExitPadRejectsTravelBeforeTheFinalHandoffBecomesCompleteable()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(62);
            var characterId = context.SeedCharacter(
                62,
                1,
                "Locked",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -70.4375);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: false);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            var client = context.CreateSelectionClient(62);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);

            context.Objects.SelectWaypoint(
                client,
                new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = client.Player.MapChannel.InstanceId
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            using var verify = context.OpenChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Bootcamp,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.IsFalse(new GameAccountRepository(verify).Get(62).CanSkipBootcamp);
            Assert.IsFalse(new CharacterFlagRepository(verify).HasValue(
                characterId,
                CharacterFlagIds.BootcampComplete));
        }

        [TestMethod]
        public void DuplicateExitPadSelectionsDuringTransferCommitDepartureAndReleaseOnlyOnce()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(631);
            var characterId = context.SeedCharacter(
                631,
                1,
                "DuplicateExit",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                experience: 49250,
                level: 5);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            SeedNormalDepartureParityState(context, 631, characterId);
            var client = context.CreateSelectionClient(631);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);
            var privateMap = context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId);
            Assert.IsNotNull(privateMap);

            var request = new SelectWaypointPacket
            {
                WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                MapInstanceId = privateMap.InstanceId
            };
            context.Objects.SelectWaypoint(client, request);
            var pendingTransfer = client.PendingTransfer;
            context.Objects.SelectWaypoint(client, request);
            var departurePackets = MissionTestContext.Drain(client);

            Assert.AreSame(pendingTransfer, client.PendingTransfer);
            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, client.State);
            Assert.IsTrue(client.PendingTransfer.IsDropship);
            CollectionAssert.AreEquivalent(
                new[] { BootcampSelectionTestContext.AliaDasWaypointId, BootcampSelectionTestContext.AliaDasHospitalId },
                departurePackets.OfType<WaypointGainedPacket>()
                    .Select(packet => packet.WaypointId)
                    .ToArray());
            Assert.AreEqual(
                1,
                departurePackets.OfType<WaypointGainedPacket>()
                    .Count(packet => packet.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId));
            Assert.AreEqual(
                1,
                departurePackets.OfType<WaypointGainedPacket>()
                    .Count(packet => packet.WaypointId == BootcampSelectionTestContext.AliaDasHospitalId));
            using (var verify = context.OpenChar())
            {
                Assert.AreEqual(
                    1,
                    verify.CharacterFlagEntries.Count(entry =>
                        entry.CharacterId == characterId &&
                        entry.FlagId == CharacterFlagIds.BootcampComplete && entry.Value == 1));
                Assert.AreEqual(
                    1,
                    verify.CharacterStartingExperienceEntries.Count(entry =>
                        entry.CharacterId == characterId &&
                        entry.State == CharacterStartingExperienceState.Completed));
                Assert.AreEqual(
                    24000U,
                    new CharacterRepository(verify).Get(characterId).Experience);
                Assert.IsTrue(new GameAccountRepository(verify).Get(631).CanSkipBootcamp);
                Assert.AreEqual(
                    1,
                    new CharacterTeleporterRepository(verify).Get(characterId)
                        .Count(entry => entry.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId));
                Assert.AreEqual(
                    1,
                    new CharacterTeleporterRepository(verify).Get(characterId)
                        .Count(entry => entry.WaypointId == BootcampSelectionTestContext.AliaDasHospitalId));
                CollectionAssert.AreEqual(
                    new uint[] { 13066, 13096, 13156, 13186, 13713, 28 },
                    context.ReadInventoryTemplates(631, characterId));
            }

            context.CompletePendingDeparture(client);
            context.CompletePendingDeparture(client);

            Assert.IsNull(context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId));
        }

        [TestMethod]
        public void ConcurrentExitPadSelectionsOnlyStartOneTransferAndApplyDepartureOnce()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(632);
            var characterId = context.SeedCharacter(
                632,
                1,
                "ConcurrentExit",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                experience: 49250,
                level: 5);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            SeedNormalDepartureParityState(context, 632, characterId);
            var firstClient = context.CreateSelectionClient(632);
            var secondClient = context.CreateSelectionClient(632);

            context.Characters.RequestSwitchToCharacterInSlot(
                firstClient,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.Characters.RequestSwitchToCharacterInSlot(
                secondClient,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(firstClient);
            context.MaterializeLoadedClient(secondClient);

            var request = new SelectWaypointPacket
            {
                WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                MapInstanceId = firstClient.Player.MapChannel.InstanceId
            };
            context.Objects.SelectWaypoint(firstClient, request);
            context.Objects.SelectWaypoint(secondClient, request);

            Assert.IsNotNull(firstClient.PendingTransfer);
            Assert.IsNull(secondClient.PendingTransfer);
            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, firstClient.State);
            Assert.IsTrue(firstClient.PendingTransfer.IsDropship);
            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, secondClient.State);

            using (var verify = context.OpenChar())
            {
                Assert.AreEqual(
                    1,
                    verify.CharacterFlagEntries.Count(entry =>
                        entry.CharacterId == characterId &&
                        entry.FlagId == CharacterFlagIds.BootcampComplete && entry.Value == 1));
                Assert.AreEqual(
                    1,
                    verify.CharacterSkillsEntries.Count(entry =>
                        entry.CharacterId == characterId &&
                        entry.SkillId == (uint)SkillId.Lightning &&
                        entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                        entry.SkillLevel == 1));
                Assert.AreEqual(
                    1,
                    verify.CharacterAbilityDrawerEntries.Count(entry =>
                        entry.CharacterId == characterId &&
                        entry.AbilityId == (int)ActionId.AaRecruitLightning &&
                        entry.AbilityLevel == 1));
                Assert.AreEqual(
                    24000U,
                    new CharacterRepository(verify).Get(characterId).Experience);
            }

            context.CompletePendingDeparture(firstClient);
            Assert.IsNull(context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId));

            context.Objects.SelectWaypoint(secondClient, request);
            Assert.IsNull(secondClient.PendingTransfer);
            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, secondClient.State);
        }

        [TestMethod]
        public void FailedDepartureTransactionKeepsTheCharacterInBootcampAndPreservesTheRuntime()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(63);
            var characterId = context.SeedCharacter(
                63,
                1,
                "DepartureRollback",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                level: 5,
                experience: 49250);
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedMission(characterId, BootcampSelectionTestContext.MissionRetryFinale, MissionState.Active, completeable: true);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.ExitPadWaypointId, WaypointType.Dropship);
            var client = context.CreateSelectionClient(63);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            context.MaterializeLoadedClient(client);
            var privateMap = context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId);
            Assert.IsNotNull(privateMap);

            context.BeforeSave = db =>
            {
                if (db.CharacterStartingExperienceEntries.Any(entry =>
                        entry.CharacterId == characterId &&
                        entry.State == CharacterStartingExperienceState.Completed))
                    throw new DbUpdateException("boom", new Exception("boom"));
            };

            context.Objects.SelectWaypoint(
                client,
                new SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = privateMap.InstanceId
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreSame(privateMap, client.Player.MapChannel);
            Assert.IsNotNull(context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                characterId));
            using var verify = context.OpenChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Bootcamp,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.IsFalse(new GameAccountRepository(verify).Get(63).CanSkipBootcamp);
            Assert.IsFalse(new CharacterFlagRepository(verify).HasValue(
                characterId,
                CharacterFlagIds.BootcampComplete));
            Assert.AreEqual(49250U, new CharacterRepository(verify).Get(characterId).Experience);
            Assert.AreEqual(
                (uint)MissionState.Active,
                new CharacterMissionRepository(verify)
                    .GetByCharacterAndMission(characterId, BootcampSelectionTestContext.MissionRetryFinale)
                    .MissionState);
        }

        private static void SeedNormalDepartureParityState(
            BootcampSelectionTestContext context,
            uint accountId,
            uint characterId)
        {
            context.SeedLightningGrant(characterId);
            context.SeedPersonalInventory(accountId, characterId, 13066, 13096, 13156, 13186, 13713, 28);
        }
    }
}
