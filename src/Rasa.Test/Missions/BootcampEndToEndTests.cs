extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char.CharacterFlag;
    using Rasa.Repositories.Char.CharacterStartingExperience;
    using Rasa.Repositories.Char.CharacterTeleporter;
    using Rasa.Repositories.Char.GameAccount;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Gameplay;

    [TestClass]
    [DoNotParallelize]
    public class BootcampEndToEndTests
    {
        private const uint MissionCaptureTheFlag = 1994;
        private const uint MissionCallingForReinforcements = 1995;
        private const uint MissionBombRetry = 2005;
        private const uint InitiationFirstAreaId = 430;
        private const uint InitiationSecondAreaId = 431;
        private const uint CaveInAreaId = 439;
        private const uint MissingScoutAreaId = 435;
        private static readonly TimeSpan BombDeadline = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan FuseDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ArrivalDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan YoungbloodDelay = TimeSpan.FromSeconds(7);

        [TestMethod]
        public void FreshCharacterCompletesTheFull1990To1995BootcampChainAndDepartsToAliaDas()
        {
            using var harness = CreateFreshBootcampHarness();
            var youngblood = AdvanceFreshCharacterToMission1995(harness);
            CompleteCallingForReinforcements(harness, youngblood);

            AssertMissionChainThroughFinale(harness);

            DepartToAliaDas(harness);
            AssertDurableBootcampDeparture(
                harness,
                expectedMissionId: MissionCallingForReinforcements);
        }

        [TestMethod]
        public void ClearingRewardedCaptureTheFlagPreservesYoungbloodAcrossFreshReconnectForMission1995()
        {
            using var harness = CreateFreshBootcampHarness();
            AdvanceFreshCharacterToMission1995(harness);
            var handler = new ClientPacketHandler();
            handler.RegisterClient(harness.Client);
            new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(handler,
                new AbandonMissionPacket { MissionId = MissionCaptureTheFlag });
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(MissionCaptureTheFlag));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.MissionHistory[MissionCaptureTheFlag]);

            harness.ReconnectFresh();

            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsNotNull(youngblood, "The next mission's Bootcamp NPC must outlive the previous journal entry.");
            Assert.IsTrue(youngblood.IsInteractable);
            harness.MovePlayerTo(youngblood);
            harness.Drain();
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = youngblood.EntityId });
            var conversation = harness.Drain().OfType<ConversePacket>().Single();
            Assert.IsTrue(conversation.ConvoDataDict.TryGetValue(ConversationType.MissionDispense, out var offers));
            Assert.IsTrue(((Dictionary<uint, MissionInfo>)offers).ContainsKey(MissionCallingForReinforcements));
            npcs.AssignNPCMission(harness.Client, new AssignNPCMissionPacket
            {
                NpcEntityId = youngblood.EntityId, MissionId = MissionCallingForReinforcements
            });
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[MissionCallingForReinforcements].State);
            using var verify = harness.Context.CreateChar();
            Assert.IsNull(verify.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, MissionCaptureTheFlag));
            Assert.IsNotNull(verify.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, MissionCallingForReinforcements));
            Assert.AreEqual(0U, verify.CharacterMissions.Runtime.Scene(youngblood.SpawnPool.SceneRunId).MissionId);
        }

        [TestMethod]
        public void EntitledAccountCanChooseNormalBootcampForANewPendingCharacterWithoutCopiedProgress()
        {
            using var harness = CreateFreshBootcampHarness(accountEntitled: true);
            Assert.IsTrue(harness.Client.AccountEntry.CanSkipBootcamp);
            Assert.IsFalse(harness.Client.Player.StartingExperienceCompleted);
            Assert.AreEqual(0, harness.Client.Player.MissionHistory.Count);
            CollectionAssert.AreEquivalent(
                new[] { BootcampRuntimeTestHarness.MissionInitiation },
                harness.Client.Player.Missions.Keys.ToArray());
            using (var unit = harness.Context.CreateChar())
            {
                Assert.AreEqual(0, unit.CharacterMissions.Runtime.History(harness.Client.Player.Id).Count);
                Assert.IsFalse(unit.CharacterFlags.HasValue(
                    harness.Client.Player.Id, CharacterFlagIds.BootcampComplete));
            }

            var youngblood = AdvanceFreshCharacterToMission1995(harness);
            CompleteCallingForReinforcements(harness, youngblood);
            AssertMissionChainThroughFinale(harness);
            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(Rasa.Game.Missions.Persistence.MissionRequirementFactsAdapter
                    .HasCompletedStartingExperience(unit, harness.Client.Player.Id),
                    harness.Client.Player.StartingExperienceCompleted);

            DepartToAliaDas(harness);
            AssertDurableBootcampDeparture(harness, MissionCallingForReinforcements);
            Assert.IsTrue(harness.Client.Player.StartingExperienceCompleted);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FreshCharacterTimesOutMission1995ThenCompletes2005AndDepartsWithoutDuplicate1995CompletionOutputs(bool accountEntitled)
        {
            using var harness = CreateFreshBootcampHarness(accountEntitled);
            var youngblood = StartTimedFinale(harness);
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.Drain();

            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));
            var timeoutPackets = harness.Drain();
            Assert.AreEqual(
                MissionState.Failed,
                harness.Client.Player.Missions[MissionCallingForReinforcements].State);
            Assert.AreEqual(1, timeoutPackets.OfType<ObjectiveFailedPacket>().Count());
            Assert.AreEqual(1, timeoutPackets.OfType<MissionFailedPacket>().Count());
            Assert.AreEqual(0, timeoutPackets.OfType<MissionCompleteablePacket>().Count());
            Assert.AreEqual(0, timeoutPackets.OfType<MissionCompletedPacket>().Count());
            Assert.AreEqual(0, timeoutPackets.OfType<MissionRewardedPacket>().Count());

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                MissionBombRetry));
            harness.Drain();

            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            harness.UtcNow += FuseDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            var van = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId);
            Assert.IsNotNull(van);
            BootcampExtractionAssaultTests.DefeatAll(harness);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                van.EntityId,
                MissionBombRetry,
                4,
                1));

            AssertMissionChainThroughRetryDeparture(harness);
            Assert.IsTrue(harness.Client.Player.Missions[MissionBombRetry].Completeable);

            DepartToAliaDas(harness);
            AssertDurableBootcampDeparture(
                harness,
                expectedMissionId: MissionBombRetry);
        }

        [TestMethod]
        public void BootcampCompletionUnlocksAccountSkipAndSecondPendingCharacterUsesSkipParity()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(713);
            var firstCharacterId = context.SeedCharacter(
                713,
                1,
                "Graduate",
                mapContextId: BootcampSelectionTestContext.BootcampMapContextId,
                x: -225,
                y: 101.12099,
                z: -71,
                experience: 49250,
                level: 5);
            var secondCharacterId = context.SeedCharacter(713, 2, "SecondPass");
            context.SeedStartingExperience(firstCharacterId, CharacterStartingExperienceState.Bootcamp);
            context.SeedStartingExperience(secondCharacterId, CharacterStartingExperienceState.Pending);
            context.SeedMission(
                firstCharacterId,
                BootcampSelectionTestContext.MissionFinale,
                MissionState.Active,
                completeable: true);
            context.SeedWaypoint(
                firstCharacterId,
                BootcampSelectionTestContext.ExitPadWaypointId,
                WaypointType.Dropship);
            SeedDepartureParityState(context, 713, firstCharacterId);

            var departingClient = context.CreateSelectionClient(713);
            context.Characters.RequestSwitchToCharacterInSlot(
                departingClient,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });
            context.MaterializeLoadedClient(departingClient);

            var privateMap = context.Maps.FindOwnedPrivateInstance(
                BootcampSelectionTestContext.BootcampMapContextId,
                firstCharacterId);
            Assert.IsNotNull(privateMap);

            context.Objects.SelectWaypoint(
                departingClient,
                new Rasa.Packets.MapChannel.Client.SelectWaypointPacket
                {
                    WaypointId = BootcampSelectionTestContext.ExitPadWaypointId,
                    MapInstanceId = privateMap.InstanceId
                });
            context.CompletePendingDeparture(departingClient);

            using (var verify = context.OpenChar())
            {
                Assert.AreEqual(
                    CharacterStartingExperienceState.Completed,
                    new CharacterStartingExperienceRepository(verify).Get(firstCharacterId).State);
                Assert.IsTrue(new GameAccountRepository(verify).Get(713).CanSkipBootcamp);
            }

            var secondClient = context.CreateSelectionClient(713);
            context.Characters.RequestSwitchToCharacterInSlot(
                secondClient,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 2,
                    SkipBootcamp = true
                });

            Assert.AreEqual(
                BootcampSelectionTestContext.WildernessMapContextId,
                secondClient.Player.MapContextId);
            using var secondVerify = context.OpenChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Skipped,
                new CharacterStartingExperienceRepository(secondVerify).Get(secondCharacterId).State);
            Assert.IsTrue(new GameAccountRepository(secondVerify).Get(713).CanSkipBootcamp);
        }

        [TestMethod]
        public void FreshNormalDepartureMatchesSkipParityForTravelLoadoutAndWaypoints()
        {
            using var normal = CreateFreshBootcampHarness();
            var youngblood = AdvanceFreshCharacterToMission1995(normal);
            CompleteCallingForReinforcements(normal, youngblood);
            DepartToAliaDas(normal);

            using var normalStorage = normal.Context.Open();
            using var normalVerify = normal.Context.CreateChar();
            var normalTemplates =
                (from inventory in normalStorage.CharacterInventoryEntries
                 join item in normalStorage.ItemEntries on inventory.ItemId equals item.ItemId
                 where inventory.AccountId == normal.Client.AccountEntry.Id &&
                       inventory.CharacterId == normal.Client.Player.Id &&
                       inventory.InventoryType == (uint)InventoryType.Personal
                 orderby inventory.SlotId
                 select item.ItemTemplateId).ToArray();
            var normalWaypoints = normalVerify.CharacterTeleporters.Get(normal.Client.Player.Id)
                .Select(entry => entry.WaypointId)
                .OrderBy(id => id)
                .ToArray();

            using var skip = new BootcampSelectionTestContext();
            skip.SeedAccount(1901, canSkipBootcamp: true);
            var skippedCharacterId = skip.SeedCharacter(1901, 1, "Skipped");
            skip.SeedStartingExperience(skippedCharacterId, CharacterStartingExperienceState.Pending);
            var skippedClient = skip.CreateSelectionClient(1901);
            skip.Characters.RequestSwitchToCharacterInSlot(
                skippedClient,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = true
                });

            using var skipVerify = skip.OpenChar();
            var skippedTemplates = skip.ReadInventoryTemplates(1901, skippedCharacterId);
            var skippedWaypoints = new CharacterTeleporterRepository(skipVerify).Get(skippedCharacterId)
                .Select(entry => entry.WaypointId)
                .OrderBy(id => id)
                .ToArray();
            var normalRuntimeWaypoints = normal.Client.Player.GainedWaypoints
                .Where(entry =>
                    entry.WaypointId == BootcampSelectionTestContext.AliaDasWaypointId ||
                    entry.WaypointId == BootcampSelectionTestContext.AliaDasHospitalId)
                .Select(entry => entry.WaypointId)
                .OrderBy(id => id)
                .ToArray();

            CollectionAssert.AreEqual(skippedTemplates, normalTemplates);
            CollectionAssert.AreEqual(skippedWaypoints, normalWaypoints);
            CollectionAssert.AreEqual(skippedWaypoints, normalRuntimeWaypoints);
            CollectionAssert.AreEqual(
                new[]
                {
                    BootcampSelectionTestContext.AliaDasWaypointId,
                    BootcampSelectionTestContext.AliaDasHospitalId
                },
                skippedWaypoints);
            CollectionAssert.Contains(
                DynamicObjectManager.Instance.CreateListOfWaypoints(normal.Client, WaypointType.Waypoint)
                    [BootcampSelectionTestContext.WildernessMapContextId]
                    .Waypoints
                    .Select(waypoint => waypoint.WaypointId)
                    .ToArray(),
                BootcampSelectionTestContext.AliaDasWaypointId);
            Assert.AreEqual(BootcampSelectionTestContext.WildernessMapContextId, normal.Client.Player.MapContextId);
            Assert.AreEqual(BootcampSelectionTestContext.WildernessMapContextId, skippedClient.Player.MapContextId);
        }

        [TestMethod]
        public void TwoSimultaneousBootcampCharactersReceiveDistinctPrivateInstancesAndDurableMissionState()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(714);
            context.SeedAccount(715);
            var firstCharacterId = context.SeedCharacter(714, 1, "FirstPending");
            var secondCharacterId = context.SeedCharacter(715, 1, "SecondPending");
            context.SeedStartingExperience(firstCharacterId, CharacterStartingExperienceState.Pending);
            context.SeedStartingExperience(secondCharacterId, CharacterStartingExperienceState.Pending);

            var firstClient = context.CreateSelectionClient(714);
            var secondClient = context.CreateSelectionClient(715);

            context.Characters.RequestSwitchToCharacterInSlot(
                firstClient,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });
            context.Characters.RequestSwitchToCharacterInSlot(
                secondClient,
                new Rasa.Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });

            Assert.AreNotSame(firstClient.Player.MapChannel, secondClient.Player.MapChannel);
            Assert.IsTrue(firstClient.Player.MapChannel.IsPrivateInstance);
            Assert.IsTrue(secondClient.Player.MapChannel.IsPrivateInstance);
            Assert.AreEqual(firstCharacterId, firstClient.Player.MapChannel.OwnerCharacterId);
            Assert.AreEqual(secondCharacterId, secondClient.Player.MapChannel.OwnerCharacterId);
            using var verify = context.OpenChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Bootcamp,
                new CharacterStartingExperienceRepository(verify).Get(firstCharacterId).State);
            Assert.AreEqual(
                CharacterStartingExperienceState.Bootcamp,
                new CharacterStartingExperienceRepository(verify).Get(secondCharacterId).State);
        }

        private static BootcampRuntimeTestHarness.Harness CreateFreshBootcampHarness(bool accountEntitled = false)
        {
            var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection(accountEntitled: accountEntitled);
            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Ingame, harness.Client.State);
            Assert.AreEqual(BootcampRuntimeTestHarness.BootcampMapContextId, harness.Client.Player.MapContextId);
            Assert.IsNotNull(harness.Client.Player.MapChannel);
            Assert.IsTrue(harness.Client.Player.MapChannel.IsPrivateInstance);
            Assert.AreEqual(harness.Client.Player.Id, harness.Client.Player.MapChannel.OwnerCharacterId);
            Assert.AreSame(
                harness.Client.Player.MapChannel,
                harness.Maps.FindOwnedPrivateInstance(
                    BootcampRuntimeTestHarness.BootcampMapContextId,
                    harness.Client.Player.Id));

            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation));
            Assert.AreEqual(1, harness.Drain().OfType<DispenseRadioMissionPacket>().Count());
            var handler = new ClientPacketHandler();
            handler.RegisterClient(harness.Client);
            new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(
                handler,
                new AssignRadioMissionPacket { MissionId = BootcampRuntimeTestHarness.MissionInitiation });
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count());

            using var verify = harness.Context.CreateChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Bootcamp,
                verify.CharacterStartingExperience.Get(harness.Client.Player.Id).State);
            var durableMission = verify.CharacterMissions.GetByCharacterAndMission(
                harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionInitiation);
            Assert.IsNotNull(durableMission);
            Assert.AreEqual((uint)MissionState.Active, durableMission.MissionState);
            Assert.IsFalse(durableMission.Completeable);
            return harness;
        }

        private static Creature AdvanceFreshCharacterToMission1995(BootcampRuntimeTestHarness.Harness harness)
        {
            var actors = SeedBootcampActors(harness);
            harness.Drain();
            CompleteInitiation(harness, actors.McAllister);
            CompleteGearingUp(harness, actors);
            return CompleteCaptureTheFlag(harness, actors);
        }

        private static BootcampActors SeedBootcampActors(BootcampRuntimeTestHarness.Harness harness) =>
            new(
                harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId),
                harness.AddNpc(
                    BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                    BootcampRuntimeTestHarness.CaptainDelessioPackageId),
                harness.AddNpc(
                    BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                    BootcampRuntimeTestHarness.CorporalHartmannPackageId),
                harness.AddNpc(
                    BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId,
                    BootcampRuntimeTestHarness.CorporalDeSimonePackageId));

        private static void CompleteInitiation(
            BootcampRuntimeTestHarness.Harness harness,
            Creature mcAllister)
        {
            if (!harness.Client.Player.Missions.ContainsKey(BootcampRuntimeTestHarness.MissionInitiation))
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                    harness.Client,
                    mcAllister.EntityId,
                    BootcampRuntimeTestHarness.MissionInitiation));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(BootcampRuntimeTestHarness.MissionInitiation, InitiationFirstAreaId)));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(BootcampRuntimeTestHarness.MissionInitiation, InitiationSecondAreaId)));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client,
                mcAllister.EntityId,
                BootcampRuntimeTestHarness.MissionInitiation,
                selectionIndex: null,
                rating: null));
        }

        private static void CompleteGearingUp(
            BootcampRuntimeTestHarness.Harness harness,
            BootcampActors actors)
        {
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                actors.McAllister.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp));
            harness.Drain();

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                4,
                1));
            BootcampRuntimeTestHarness.LootEquipmentCrate(harness);

            PrepareEquipping(harness);
            Assert.IsTrue(RecordTemplateEquipProgress(harness, 13066));

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Delessio.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                5,
                1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                6,
                1));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.WeaponAttack)));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                9,
                1));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.ObjectHit(
                    PracticeTargetManager.EntityClassId,
                    (uint)ActionId.AaRecruitLightning)));

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.Hartmann.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                7,
                1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client,
                actors.DeSimone.EntityId,
                BootcampRuntimeTestHarness.MissionGearingUp,
                selectionIndex: null,
                rating: null));
        }

        private static Creature CompleteCaptureTheFlag(
            BootcampRuntimeTestHarness.Harness harness,
            BootcampActors actors)
        {
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                actors.DeSimone.EntityId,
                MissionCaptureTheFlag));
            harness.Drain();

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                actors.DeSimone.EntityId,
                MissionCaptureTheFlag,
                4,
                1));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(MissionCaptureTheFlag, CaveInAreaId)));

            var tizzik = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.TizzikGiCreatureId);
            Assert.IsNotNull(tizzik);
            new CreatureManager(null, new ManifestationManager(harness.Context), harness.Manager)
                .HandleCreatureKill(harness.BootcampMap, tizzik, harness.Client.Player);

            harness.UtcNow += YoungbloodDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsNotNull(youngblood);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                youngblood.EntityId,
                MissionCaptureTheFlag,
                3,
                1));
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client,
                youngblood.EntityId,
                MissionCaptureTheFlag,
                selectionIndex: null,
                rating: null));
            return youngblood;
        }

        private static void CompleteCallingForReinforcements(
            BootcampRuntimeTestHarness.Harness harness,
            Creature youngblood)
        {
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                MissionCallingForReinforcements));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(MissionCallingForReinforcements, MissingScoutAreaId)));

            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[MissionCallingForReinforcements].Objectives[2].State);

            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-conrad-corpse"));
            harness.UseObjectAndRecover(FindScenarioObject(harness, "bootcamp-dropship-debris"));
            harness.UtcNow += FuseDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            var van = BootcampRuntimeTestHarness.FindNpcByPackage(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CorporalVanValkenbergPackageId);
            Assert.IsNotNull(van);
            BootcampExtractionAssaultTests.DefeatAll(harness);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                van.EntityId,
                MissionCallingForReinforcements,
                4,
                1));
        }

        private static Creature StartTimedFinale(BootcampRuntimeTestHarness.Harness harness)
        {
            var youngblood = AdvanceFreshCharacterToMission1995(harness);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                youngblood.EntityId,
                MissionCallingForReinforcements));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(MissionCallingForReinforcements, MissingScoutAreaId)));

            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[MissionCallingForReinforcements].Objectives[2].State);
            return youngblood;
        }

        private static void AssertMissionChainThroughFinale(BootcampRuntimeTestHarness.Harness harness)
        {
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation].State);
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp].State);
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[MissionCaptureTheFlag].State);
            Assert.AreEqual(
                MissionState.Active,
                harness.Client.Player.Missions[MissionCallingForReinforcements].State);
            Assert.IsTrue(harness.Client.Player.Missions[MissionCallingForReinforcements].Completeable);
        }

        private static void AssertMissionChainThroughRetryDeparture(BootcampRuntimeTestHarness.Harness harness)
        {
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionInitiation].State);
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[BootcampRuntimeTestHarness.MissionGearingUp].State);
            Assert.AreEqual(
                MissionState.Completed,
                harness.Client.Player.Missions[MissionCaptureTheFlag].State);
            Assert.AreEqual(
                MissionState.Failed,
                harness.Client.Player.Missions[MissionCallingForReinforcements].State);
            Assert.AreEqual(
                MissionState.Active,
                harness.Client.Player.Missions[MissionBombRetry].State);
        }

        private static DynamicObject FindScenarioObject(
            BootcampRuntimeTestHarness.Harness harness,
            string key) =>
            BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, key)
            ?? throw new AssertFailedException($"Missing scenario object {key}.");

        private static void PrepareEquipping(BootcampRuntimeTestHarness.Harness harness)
        {
            harness.Client.Player.AppearanceData ??= new Dictionary<EquipmentData, AppearanceData>();
            typeof(ManifestationManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!
                .SetValue(null, new ManifestationManager(harness.Context));
        }

        private static bool RecordTemplateEquipProgress(
            BootcampRuntimeTestHarness.Harness harness,
            uint templateId)
        {
            var slot = FindPersonalSlotByTemplate(harness.Client, templateId);
            var item = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.PersonalInventory[(int)slot]);
            return item?.ItemTemplate != null &&
                harness.Manager.RecordProgress(
                    harness.Client,
                    MissionProgressEvent.ItemEquipped(
                        (uint)item.ItemTemplate.Class,
                        item.ItemTemplate.ItemTemplateId));
        }

        private static uint FindPersonalSlotByTemplate(Client client, uint templateId)
        {
            for (uint slot = 0; slot < client.Player.Inventory.PersonalInventory.Count; slot++)
            {
                var entityId = client.Player.Inventory.PersonalInventory[(int)slot];
                if (entityId == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(entityId);
                if (item?.ItemTemplate?.ItemTemplateId == templateId)
                    return slot;
            }

            throw new AssertFailedException($"Could not find personal inventory item template {templateId}.");
        }

        private static void DepartToAliaDas(BootcampRuntimeTestHarness.Harness harness)
        {
            var exitPad = harness.BootcampMap.Teleporters.Values
                .Single(teleporter =>
                    teleporter.ObjectData is WaypointInfo waypoint &&
                    waypoint.WaypointId == BootcampSelectionTestContext.ExitPadWaypointId);
            harness.MovePlayerTo(exitPad);
            Assert.IsTrue(harness.BootcampMap.ClientList.Contains(harness.Client));
            Assert.IsTrue(DynamicObjectManager.Instance.IsStationAvailable(harness.Client,
                BootcampSelectionTestContext.BootcampMapContextId, BootcampSelectionTestContext.ExitPadWaypointId));
            new MapTriggerManager().TriggersProximityWorker(harness.BootcampMap);
            Assert.IsNotNull(harness.Client.PendingTransfer);
            var objects = DynamicObjectManager.Instance;
            var departure = objects.Dropships[harness.Client.PendingTransfer.DropshipId];
            for (var phase = 0; phase < 6; phase++)
                objects.DropshipsWorker(harness.BootcampMap, Math.Max(0, departure.PhaseTimeleft));
            harness.RouteMapLoaded();
            var arrival = objects.Dropships.Values.Single(ship =>
                ship.Client == harness.Client && ship.Role == DropshipRole.Arrival);
            for (var phase = 0; phase < 6; phase++)
                objects.DropshipsWorker(harness.Client.Player.MapChannel, Math.Max(0, arrival.PhaseTimeleft));
        }

        private static void AssertDurableBootcampDeparture(
            BootcampRuntimeTestHarness.Harness harness,
            uint expectedMissionId)
        {
            Assert.AreEqual(BootcampSelectionTestContext.WildernessMapContextId, harness.Client.Player.MapContextId);
            Assert.IsFalse(harness.Client.Player.MapChannel.IsPrivateInstance);
            Assert.IsNull(harness.Client.PendingTransfer);
            Assert.IsNull(harness.Maps.FindOwnedPrivateInstance(
                BootcampRuntimeTestHarness.BootcampMapContextId,
                harness.Client.Player.Id));

            using var verify = harness.Context.CreateChar();
            var levelFourExperience = harness.WorldContext.ExperienceForLevelEntries
                .Single(entry => entry.Level == 4)
                .Experience;
            var durableCharacter = verify.Characters.Get(harness.Client.Player.Id);
            Assert.AreEqual(24000L, levelFourExperience);
            Assert.AreEqual((uint)levelFourExperience, durableCharacter.Experience);
            Assert.AreEqual((byte)4, durableCharacter.Level);
            Assert.AreEqual(
                CharacterStartingExperienceState.Completed,
                verify.CharacterStartingExperience.Get(harness.Client.Player.Id).State);
            Assert.IsTrue(verify.CharacterFlags.HasValue(
                harness.Client.Player.Id,
                CharacterFlagIds.BootcampComplete));
            Assert.IsTrue(verify.GameAccounts.Get(harness.Client.AccountEntry.Id).CanSkipBootcamp);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    BootcampSelectionTestContext.AliaDasWaypointId,
                    BootcampSelectionTestContext.AliaDasHospitalId
                },
                verify.CharacterTeleporters.Get(harness.Client.Player.Id)
                    .Select(entry => entry.WaypointId)
                    .ToArray());

            var departureMission = verify.CharacterMissions.GetByCharacterAndMission(
                harness.Client.Player.Id,
                expectedMissionId);
            Assert.IsNotNull(departureMission);
            Assert.AreEqual((uint)MissionState.Active, departureMission.MissionState);
            Assert.IsTrue(departureMission.Completeable);
        }

        private static void SeedDepartureParityState(
            BootcampSelectionTestContext context,
            uint accountId,
            uint characterId)
        {
            context.SeedLightningGrant(characterId);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.AliaDasWaypointId, WaypointType.Waypoint);
            context.SeedWaypoint(characterId, BootcampSelectionTestContext.AliaDasHospitalId, WaypointType.Hospital);
            context.SeedPersonalInventory(accountId, characterId, 13066, 13096, 13156, 13186, 13713, 28);
        }

        private sealed record BootcampActors(
            Creature McAllister,
            Creature Delessio,
            Creature Hartmann,
            Creature DeSimone);
    }
}
