extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Context.Char;
    using Context.World;
    using Data;
    using Game;
    using Game.Handlers;
    using Managers;
    using Repositories.Char;
    using Repositories.Char.Character;
    using Repositories.Char.CharacterAbilityDrawer;
    using Repositories.Char.CharacterAppearance;
    using Repositories.Char.CharacterInventory;
    using Repositories.Char.CharacterLockbox;
    using Repositories.Char.CharacterLogos;
    using Repositories.Char.CharacterMission;
    using Repositories.Char.CharacterMissionDeadline;
    using Repositories.Char.CharacterMissionProgress;
    using Repositories.Char.CharacterMissionScenario;
    using Repositories.Char.CharacterFlag;
    using Repositories.Char.CharacterSkills;
    using Repositories.Char.CharacterStartingExperience;
    using Repositories.Char.CharacterTeleporter;
    using Repositories.Char.CharacterTitle;
    using Repositories.Char.Clan;
    using Repositories.Char.ClanInventory;
    using Repositories.Char.ClanLockboxLog;
    using Repositories.Char.ClanMember;
    using Repositories.Char.GameAccount;
    using Repositories.Char.Items;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Services.DbContext;
    using Structures;
    using Structures.Char;
    using World;

    [TestClass]
    [DoNotParallelize]
    public class BootcampCharacterEntryTests
    {
        private const uint BootcampMapContextId = 1985;
        private const uint WildernessMapContextId = 1220;
        private const uint MissionInitiation = 1990;

        [TestMethod]
        public void PendingFirstSelectionWithoutSkipEntersPrivate1985WithoutAcceptingInitiation()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(41);
            var characterId = context.SeedCharacter(41, 1, "Pending");
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Pending);
            var client = context.CreateSelectionClient(41);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Loading, client.State);
            Assert.AreEqual(BootcampMapContextId, client.Player.MapContextId);
            Assert.IsNotNull(client.Player.MapChannel);
            Assert.IsTrue(client.Player.MapChannel.IsPrivateInstance);
            Assert.AreEqual(characterId, client.Player.MapChannel.OwnerCharacterId);
            Assert.AreSame(
                client.Player.MapChannel,
                context.Maps.FindOwnedPrivateInstance(BootcampMapContextId, characterId));

            using var verify = context.OpenChar();
            var durableCharacter = new CharacterRepository(verify).Get(characterId);
            var durableStart = new CharacterStartingExperienceRepository(verify).Get(characterId);
            var durableMission = new CharacterMissionRepository(verify)
                .GetByCharacterAndMission(characterId, MissionInitiation);

            Assert.IsNotNull(durableStart);
            Assert.AreEqual(CharacterStartingExperienceState.Bootcamp, durableStart.State);
            Assert.AreEqual(BootcampMapContextId, durableCharacter.MapContextId);
            Assert.AreEqual(389.8046875d, durableCharacter.CoordX, 0.0001d);
            Assert.AreEqual(136.78515625d, durableCharacter.CoordY, 0.0001d);
            Assert.AreEqual(-80.6640625d, durableCharacter.CoordZ, 0.0001d);
            Assert.IsNull(durableMission);
            Assert.IsFalse(client.Player.Missions.ContainsKey(MissionInitiation));
        }

        [TestMethod]
        public void FirstBootcampSelectionFacesTheOppositeDirectionWithoutRotatingAgainOnReconnect()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(45);
            var characterId = context.SeedCharacter(45, 1, "Facing");
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Pending);
            var client = context.CreateSelectionClient(45);
            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });

            using (var verify = context.OpenChar())
            {
                var character = new CharacterRepository(verify).Get(characterId);
                Assert.AreEqual(3.11637806892395d - Math.PI, character.Rotation, 0.000001d);
            }

            using (var unit = context.CreateChar())
            {
                unit.Characters.UpdateCharacterPosition(characterId, 390, 137, -75, 1.25, BootcampMapContextId);
                unit.Complete();
            }
            context.Maps.ReleaseOwnedPrivateInstances(characterId);
            var reconnect = context.CreateSelectionClient(45);
            context.Characters.RequestSwitchToCharacterInSlot(
                reconnect,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            Assert.AreEqual(1.25f, reconnect.Player.Rotation, 0.000001f);
        }

        [TestMethod]
        public void FirstBootcampMapLoadedStartsWithEmptyAdrenaline()
        {
            using var harness = Missions.BootcampRuntimeTestHarness.CreateFromPendingSelection();
            Assert.AreEqual(0, harness.Client.Player.Attributes[Attributes.Chi].Current);
            Assert.IsGreaterThan(0, harness.Client.Player.Attributes[Attributes.Chi].CurrentMax);
        }

        [TestMethod]
        public void ConcurrentPendingSelectionsEnterBootcampOnceWithoutAcceptingAndReuseOneOwnedRuntime()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(42);
            var characterId = context.SeedCharacter(42, 1, "Concurrent");
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Pending);
            var first = context.CreateSelectionClient(42);
            var second = context.CreateSelectionClient(42);

            context.Characters.RequestSwitchToCharacterInSlot(
                first,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });
            context.Characters.RequestSwitchToCharacterInSlot(
                second,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });

            using var verify = context.OpenChar();
            Assert.AreEqual(
                1,
                verify.CharacterStartingExperienceEntries.Count(entry =>
                    entry.CharacterId == characterId &&
                    entry.State == CharacterStartingExperienceState.Bootcamp));
            Assert.AreEqual(
                0,
                verify.CharacterMissionEntries.Count(entry =>
                    entry.CharacterId == characterId &&
                    entry.MissionId == MissionInitiation));
            Assert.AreSame(first.Player.MapChannel, second.Player.MapChannel);
            Assert.AreSame(
                first.Player.MapChannel,
                context.Maps.FindOwnedPrivateInstance(BootcampMapContextId, characterId));
        }

        [TestMethod]
        public void LegacyCharacterIgnoresSkipPromptAndUsesStoredMap()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(43);
            var characterId = context.SeedCharacter(
                43,
                1,
                "Legacy",
                mapContextId: WildernessMapContextId,
                x: 12.5,
                y: 34.5,
                z: 56.5,
                rotation: 1.25,
                experience: 4000,
                level: 3);
            context.SeedStartingExperience(
                characterId,
                CharacterStartingExperienceState.Legacy,
                revision: "legacy");
            var client = context.CreateSelectionClient(43);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = true
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Loading, client.State);
            Assert.AreEqual(WildernessMapContextId, client.Player.MapContextId);
            Assert.IsFalse(client.Player.MapChannel.IsPrivateInstance);
            using var verify = context.OpenChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Legacy,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.IsNull(
                new CharacterMissionRepository(verify)
                    .GetByCharacterAndMission(characterId, MissionInitiation));
        }

        [TestMethod]
        [DataRow(CharacterStartingExperienceState.Completed)]
        [DataRow(CharacterStartingExperienceState.Skipped)]
        public void StartedBootcampGraduateIgnoresSkipPromptAndUsesStoredAliaState(
            CharacterStartingExperienceState state)
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(431, canSkipBootcamp: true);
            var characterId = context.SeedCharacter(
                431,
                1,
                "Graduate",
                mapContextId: WildernessMapContextId,
                x: 884.11,
                y: 305.8,
                z: 347.81,
                rotation: 1.5613,
                experience: 24000,
                level: 4);
            context.SeedStartingExperience(characterId, state);
            var client = context.CreateSelectionClient(431);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = true
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Loading, client.State);
            Assert.AreEqual(WildernessMapContextId, client.Player.MapContextId);
            Assert.IsFalse(client.Player.MapChannel.IsPrivateInstance);
            Assert.IsNull(context.Maps.FindOwnedPrivateInstance(BootcampMapContextId, characterId));
            using var verify = context.OpenChar();
            var durableCharacter = new CharacterRepository(verify).Get(characterId);
            Assert.AreEqual(WildernessMapContextId, durableCharacter.MapContextId);
            Assert.AreEqual(884.11d, durableCharacter.CoordX, 0.001d);
            Assert.AreEqual(305.8d, durableCharacter.CoordY, 0.001d);
            Assert.AreEqual(347.81d, durableCharacter.CoordZ, 0.001d);
            Assert.AreEqual(
                state,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.IsNull(
                new CharacterMissionRepository(verify)
                    .GetByCharacterAndMission(characterId, MissionInitiation));
        }

        [TestMethod]
        [DataRow(CharacterStartingExperienceState.Completed)]
        [DataRow(CharacterStartingExperienceState.Skipped)]
        public void TamperedBootcampReturnForStartedGraduateIsRepairedToAliaInsteadOfReEnteringBootcamp(
            CharacterStartingExperienceState state)
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(432, canSkipBootcamp: true);
            var characterId = context.SeedCharacter(
                432,
                1,
                "TamperedReturn",
                mapContextId: BootcampMapContextId,
                x: 357.90054,
                y: 120.32544,
                z: 156.5188,
                rotation: 0,
                experience: 24000,
                level: 4);
            context.SeedStartingExperience(characterId, state);
            var client = context.CreateSelectionClient(432);

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = true
                });

            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Loading, client.State);
            Assert.AreEqual(WildernessMapContextId, client.Player.MapContextId);
            Assert.IsFalse(client.Player.MapChannel.IsPrivateInstance);
            Assert.IsNull(context.Maps.FindOwnedPrivateInstance(BootcampMapContextId, characterId));
            using var verify = context.OpenChar();
            var durableCharacter = new CharacterRepository(verify).Get(characterId);
            Assert.AreEqual(WildernessMapContextId, durableCharacter.MapContextId);
            Assert.AreEqual(884.11d, durableCharacter.CoordX, 0.001d);
            Assert.AreEqual(305.8d, durableCharacter.CoordY, 0.001d);
            Assert.AreEqual(347.81d, durableCharacter.CoordZ, 0.001d);
            Assert.AreEqual(
                state,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.AreEqual(
                0,
                verify.CharacterMissionEntries.Count(entry =>
                    entry.CharacterId == characterId &&
                    entry.MissionId == MissionInitiation));
        }

        [TestMethod]
        public void FailedBootcampEntryTransactionLeavesSelectionStatePending()
        {
            using var context = new BootcampSelectionTestContext();
            context.SeedAccount(44);
            var characterId = context.SeedCharacter(44, 1, "Rollback");
            context.SeedStartingExperience(characterId, CharacterStartingExperienceState.Pending);
            var client = context.CreateSelectionClient(44);
            var saveAttempts = 0;
            context.BeforeSave = db =>
            {
                saveAttempts++;
                if (db.CharacterStartingExperienceEntries.Any(entry =>
                        entry.CharacterId == characterId &&
                        entry.State == CharacterStartingExperienceState.Bootcamp))
                    throw new DbUpdateException("boom", new Exception("boom"));
            };

            context.Characters.RequestSwitchToCharacterInSlot(
                client,
                new Packets.Game.Client.RequestSwitchToCharacterInSlotPacket
                {
                    SlotNum = 1,
                    SkipBootcamp = false
                });

            Assert.IsTrue(saveAttempts > 0);
            Assert.AreEqual(RasaGame::Rasa.Data.ClientState.CharacterSelection, client.State);
            Assert.AreEqual(0U, client.Player.Id);
            Assert.IsNull(client.Player.MapChannel);
            using var verify = context.OpenChar();
            Assert.AreEqual(
                CharacterStartingExperienceState.Pending,
                new CharacterStartingExperienceRepository(verify).Get(characterId).State);
            Assert.IsNull(
                new CharacterMissionRepository(verify)
                    .GetByCharacterAndMission(characterId, MissionInitiation));
        }
    }

    internal sealed class BootcampSelectionTestContext : IGameUnitOfWorkFactory, IDisposable
    {
        internal const uint BootcampMapContextId = 1985;
        internal const uint WildernessMapContextId = 1220;
        internal const uint ExitPadWaypointId = 60;
        internal const uint AliaDasWaypointId = 57;
        internal const uint AliaDasHospitalId = 103;
        internal const uint MissionInitiation = 1990;
        internal const uint MissionFinale = 1995;
        internal const uint MissionRetryFinale = 2005;

        private readonly string _directory = Path.Combine(
            AppContext.BaseDirectory,
            "TestDatabases",
            Guid.NewGuid().ToString("N"));

        private readonly MapChannelManagerScope _mapsScope;
        private readonly SqliteWorldContext _worldContext;

        internal Action<SqliteCharContext> BeforeSave { get; set; }
        internal Action<SqliteCharContext> AfterSave { get; set; }
        internal CharacterManager Characters { get; }
        internal MapChannelManager Maps { get; }
        internal DynamicObjectManager Objects { get; }
        internal MissionApplication Missions { get; }

        private string CharDatabase => Path.Combine(_directory, "characters");
        private string WorldDatabase => Path.Combine(_directory, "world");

        internal BootcampSelectionTestContext()
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());

            Directory.CreateDirectory(_directory);

            using (var charContext = OpenChar())
                charContext.Database.Migrate();

            _worldContext = OpenWorld();
            _worldContext.Database.Migrate();


            Missions = new MissionApplication(
                this,
                new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>(),
                new ManifestationManager(this));
            var report = Missions.LoadMissions();
            if (report.BlocksReadiness)
                throw new AssertFailedException(
                    string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));

            Characters = new CharacterManager(this, Missions);
            Maps = new MapChannelManager(
                this,
                updateCharacter: (client, update, value) =>
                    Characters.UpdateCharacter(client, update, value),
                refreshStats: (_, _) => { },
                assignPlayer: _ => { },
                enterMapChannels: _ => { },
                privateInstances: new PrivateMapInstanceService());
            Maps.MapChannelArray.Add(BootcampMapContextId, CreatePublicMap(BootcampMapContextId, "bootcamp_fixture"));
            Maps.MapChannelArray.Add(WildernessMapContextId, CreatePublicMap(WildernessMapContextId, "alia_fixture"));
            _mapsScope = new MapChannelManagerScope(Maps);

            Objects = new DynamicObjectManager(
                this,
                Maps,
                updateCharacter: (client, update, value) =>
                    Characters.UpdateCharacter(client, update, value),
                missionManager: Missions,
                characterManager: Characters);
            Objects.InitTeleporters();
        }

        internal SqliteCharContext OpenChar() =>
            new(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new DatabaseConnectionConfiguration { Database = CharDatabase }
                }),
                new QueryConfiguration(this),
                new SqliteDbContextPropertyModifier());

        internal void SeedAccount(
            uint accountId,
            bool canSkipBootcamp = false,
            string familyName = "Fixture",
            byte selectedSlot = 0)
        {
            using var context = OpenChar();
            context.GameAccountEntries.Add(new GameAccountEntry
            {
                Id = accountId,
                Email = $"bootcamp-{accountId}@example.invalid",
                Name = $"Bootcamp {accountId}",
                FamilyName = familyName,
                SelectedSlot = selectedSlot,
                CanSkipBootcamp = canSkipBootcamp,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                LastIp = "127.0.0.1",
                Level = 0
            });
            context.SaveChanges();
        }

        internal uint SeedCharacter(
            uint accountId,
            byte slot,
            string name,
            uint mapContextId = WildernessMapContextId,
            double x = 1,
            double y = 2,
            double z = 3,
            double rotation = 0,
            uint experience = 0,
            byte level = 1,
            uint characterClass = (uint)CharacterClass.Recruit)
        {
            using var context = OpenChar();
            var entry = new CharacterEntry
            {
                AccountId = accountId,
                Slot = slot,
                Name = name,
                Race = 1,
                Class = characterClass,
                Gender = 0,
                Scale = 1,
                Experience = experience,
                Level = level,
                Credit = 100,
                Prestige = 50,
                ActiveWeapon = 0,
                CurrentAbilitySlot = 0,
                Body = 0,
                Mind = 0,
                Spirit = 0,
                CloneCredits = 0,
                MapContextId = mapContextId,
                CoordX = x,
                CoordY = y,
                CoordZ = z,
                Rotation = rotation,
                RunState = 1,
                CrouchState = 0,
                NumLogins = 0,
                LastLogin = DateTime.UtcNow,
                TotalTimePlayed = 0,
                CreatedAt = DateTime.UtcNow,
                LastPvPClan = DateTime.UtcNow
            };
            context.CharacterEntries.Add(entry);
            context.SaveChanges();
            return entry.Id;
        }

        internal void SeedStartingExperience(
            uint characterId,
            CharacterStartingExperienceState state,
            string revision = "deployment_11")
        {
            using var context = OpenChar();
            context.CharacterStartingExperienceEntries.Add(
                new CharacterStartingExperienceEntry(characterId, revision, state));
            context.SaveChanges();
        }

        internal void SeedWaypoint(uint characterId, uint waypointId, WaypointType waypointType)
        {
            using var context = OpenChar();
            context.CharacterTeleporterEntries.Add(
                new CharacterTeleporterEntry(characterId, waypointId, (byte)waypointType));
            context.SaveChanges();
        }

        internal void SeedMission(uint characterId, uint missionId, MissionState state, bool completeable)
        {
            using var context = OpenChar();
            context.CharacterMissionEntries.Add(
                new CharacterMissionEntry(characterId, missionId, (uint)state)
                {
                    Completeable = completeable
                });

            if (Missions.LoadedMissions.TryGetValue(missionId, out var definition) &&
                definition.IsOperational)
                foreach (var objective in definition.Objectives.Values)
                {
                    var objectiveState = completeable && objective.IsRequired.Value
                        ? MissionObjectiveState.Completed
                        : objective.InitialState.Value;
                    var row = new CharacterMissionObjectiveEntry(
                        characterId,
                        missionId,
                        objective.ObjectiveId,
                        (byte)objectiveState);
                    foreach (var counter in objective.Counters)
                        row.Counters.Add(
                            new CharacterMissionObjectiveCounterEntry(
                                characterId,
                                missionId,
                                objective.ObjectiveId,
                                counter.Key,
                                counter.Value.InitialValue));
                    foreach (var counter in objective.ItemCounters)
                        row.ItemCounters.Add(
                            new CharacterMissionObjectiveItemCounterEntry(
                                characterId,
                                missionId,
                                objective.ObjectiveId,
                                counter.Key,
                                counter.Value.InitialValue));
                    context.CharacterMissionObjectiveEntries.Add(row);
                }

            context.SaveChanges();
        }

        internal Client CreateSelectionClient(uint accountId)
        {
            var client = new Client(this, new ClientPacketHandler())
            {
                State = RasaGame::Rasa.Data.ClientState.CharacterSelection
            };
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!
                .SetValue(client, LoadAccount(accountId));
            return client;
        }

        internal void MaterializeLoadedClient(Client client)
        {
            var map = client.Player.MapChannel;
            map.QueuedClients.Clear();
            EnsureEntityClass(client.Player.EntityClass);
            EnsureEntityClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            if (client.Player.Inventory.EquippedInventory.Count == 0)
                for (var index = 0; index < 22; index++)
                    client.Player.Inventory.EquippedInventory.Add(0);
            if (client.Player.Inventory.HomeInventory.Count == 0)
                for (var index = 0; index < LockboxTab.TotalSlots; index++)
                    client.Player.Inventory.HomeInventory.Add(0);
            if (client.Player.Inventory.PersonalInventory.Count == 0)
                for (var index = 0; index < 250; index++)
                    client.Player.Inventory.PersonalInventory.Add(0);
            if (client.Player.Inventory.WeaponDrawer.Count == 0)
                for (var index = 0; index < 5; index++)
                    client.Player.Inventory.WeaponDrawer.Add(0);
            client.Player.RuntimeMapChannel = map;
            client.State = RasaGame::Rasa.Data.ClientState.Ingame;
            client.AwaitingMapLoaded = false;
            if (!map.ClientList.Contains(client))
                map.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        internal void CompletePendingDeparture(Client client)
        {
            if (client.PendingTransfer?.IsDropship == true)
            {
                var origin = client.PendingTransfer.OriginMap;
                var ship = Objects.Dropships[client.PendingTransfer.DropshipId];
                for (var phase = 0; phase < 6; phase++)
                    Objects.DropshipsWorker(origin, Math.Max(0, ship.PhaseTimeleft));
                Assert.IsTrue(client.PendingTransfer.HasDeparted);
                Assert.AreEqual(RasaGame::Rasa.Data.ClientState.Teleporting, client.State);
                var destination = client.PendingTransfer.DestinationMap;
                if (!Objects.CompleteMapLoadTransfer(client))
                    return;
                if (!destination.ClientList.Contains(client))
                    destination.ClientList.Add(client);
                CellManager.Instance.AddToWorld(client);
                var arrival = new Dropship(Factions.AFS, DropshipType.Teleporter, client, DropshipRole.Arrival);
                CellManager.Instance.AddToWorld(destination, arrival);
                Objects.Dropships.Add(arrival.EntityId, arrival);
                for (var phase = 0; phase < 6; phase++)
                    Objects.DropshipsWorker(destination, Math.Max(0, arrival.PhaseTimeleft));
                return;
            }
            typeof(MapChannelManager)
                .GetMethod(
                    "CompleteMapLinkTransfer",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Maps, new object[] { client });
        }

        internal uint[] ReadInventoryTemplates(uint accountId, uint characterId)
        {
            using var context = OpenChar();
            return (from inventory in context.CharacterInventoryEntries
                    join item in context.ItemEntries on inventory.ItemId equals item.ItemId
                    where inventory.AccountId == accountId &&
                          inventory.CharacterId == characterId &&
                          inventory.InventoryType == (uint)InventoryType.Personal
                    orderby inventory.SlotId
                    select item.ItemTemplateId).ToArray();
        }

        internal long ReadExperienceForLevel(uint level) =>
            _worldContext.ExperienceForLevelEntries
                .Single(entry => entry.Level == level)
                .Experience;

        internal void SeedLightningGrant(uint characterId)
        {
            using var context = OpenChar();
            context.CharacterSkillsEntries.Add(
                new CharacterSkillsEntry(
                    characterId,
                    (uint)SkillId.Lightning,
                    (int)ActionId.AaRecruitLightning,
                    1));
            context.CharacterAbilityDrawerEntries.Add(
                new CharacterAbilityDrawerEntry(
                    characterId,
                    0,
                    (int)ActionId.AaRecruitLightning,
                    1));
            context.SaveChanges();
        }

        internal void SeedPersonalInventory(uint accountId, uint characterId, params uint[] templateIds)
        {
            using var context = OpenChar();
            var nextItemId = context.ItemEntries.Any()
                ? context.ItemEntries.Max(entry => entry.ItemId) + 1
                : 1U;
            for (var index = 0; index < templateIds.Length; index++)
            {
                var templateId = templateIds[index];
                var itemId = nextItemId++;
                context.ItemEntries.Add(new ItemEntry
                {
                    ItemId = itemId,
                    ItemTemplateId = templateId,
                    StackSize = templateId == 28 ? 20U : 1U,
                    CurrentHitPoints = 100,
                    AmmoCount = templateId == 28 ? 20U : 0U,
                    Color = 0,
                    CrafterName = string.Empty,
                    CreatedAt = DateTime.UtcNow
                });
                context.CharacterInventoryEntries.Add(
                    new CharacterInventoryEntry(
                        accountId,
                        characterId,
                        (uint)InventoryType.Personal,
                        (uint)index,
                        itemId));
            }

            context.SaveChanges();
        }

        public void Dispose()
        {
            _mapsScope.Dispose();
            _worldContext.Dispose();
            SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, true);
        }

        public ICharUnitOfWork CreateChar()
        {
            var context = OpenChar();
            context.SavingChanges += (_, _) => BeforeSave?.Invoke(context);
            context.SavedChanges += (_, _) => AfterSave?.Invoke(context);
            return new CharUnitOfWork(
                context,
                gameAccounts: new GameAccountRepository(context),
                censoredWords: null,
                characters: new CharacterRepository(context),
                characterAbilityDrawers: new CharacterAbilityDrawerRepository(context),
                characterAppearances: new CharacterAppearanceRepository(context),
                characterInventories: new CharacterInventoryRepository(context),
                characterLockboxes: new CharacterLockboxRepository(context),
                characterLogoses: new CharacterLogosRepository(context),
                characterMissions: new CharacterMissionRepository(context),
                characterMissionDeadlines: new CharacterMissionDeadlineRepository(context),
                characterMissionProgress: new CharacterMissionProgressRepository(context),
                characterMissionScenario: new CharacterMissionScenarioRepository(context),
                characterOptions: null,
                characterFlags: new CharacterFlagRepository(context),
                characterSkills: new CharacterSkillsRepository(context),
                characterStartingExperience: new CharacterStartingExperienceRepository(context),
                characterTeleporters: new CharacterTeleporterRepository(context),
                characterTitles: new CharacterTitleRepository(context),
                auctions: null,
                clans: new ClanRepository(context),
                clanInventories: new ClanInventoryRepository(context),
                clanMembers: new ClanMemberRepository(context),
                clanLockboxLogs: new ClanLockboxLogRepository(context),
                friends: null,
                ignoreds: null,
                items: new ItemRepository(context),
                petitions: null,
                userOptions: null);
        }

        public IWorldUnitOfWork CreateWorld() =>
            new RepositoryBackedWorldUnitOfWork(_worldContext);

        private SqliteWorldContext OpenWorld() =>
            new(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    World = new DatabaseConnectionConfiguration { Database = WorldDatabase }
                }),
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());

        private GameAccountEntry LoadAccount(uint accountId)
        {
            using var context = OpenChar();
            return new GameAccountRepository(context).Get(accountId);
        }

        private static MapChannel CreatePublicMap(uint contextId, string name) => new()
        {
            MapInfo = new MapInfo(contextId, name, 1556, 0),
            ClientList = new List<Client>(),
            PlayerLimit = 128
        };

        private static void EnsureEntityClass(EntityClasses entityClass)
        {
            if (EntityClassManager.Instance.LoadedEntityClasses.ContainsKey(entityClass))
                return;

            EntityClassManager.Instance.LoadedEntityClasses.Add(
                entityClass,
                new EntityClass(
                    (uint)entityClass,
                    entityClass.ToString(),
                    0,
                    0,
                    new List<AugmentationType>(),
                    true));
        }

        private sealed class RepositoryBackedWorldUnitOfWork : IWorldUnitOfWork
        {
            internal RepositoryBackedWorldUnitOfWork(SqliteWorldContext context)
            {
                Actions = null;
                Equipment = new EquipmentRepository(context);
                Creatures = new CreatureRepository(context);
                EntityClasses = new EntityClassRepository(context);
                Footlockers = null;
                Logoses = null;
                MapInfos = new MapInfoRepository(context);
                MapLinks = null;
                Kraftwerks = null;
                MapRegions = null;
                MapMarkers = new MapMarkerRepository(context);
                Recipes = null;
                NpcMissions = null;
                NpcMissionRewards = null;
                MissionContent = new MissionContentRepository(context);
                NpcPackages = new NpcPackageRepository(context);
                RandomNames = null;
                Spawnpools = new SpawnpoolRepository(context);
                Teleporters = new TeleporterRepository(context);
            }

            public IActionRepository Actions { get; }
            public IEquipmentRepository Equipment { get; }
            public ICreatureRepository Creatures { get; }
            public IEntityClassRepository EntityClasses { get; }
            public IFootlockerRepository Footlockers { get; }
            public ILogosRepository Logoses { get; }
            public IMapInfoRepository MapInfos { get; }
            public IMapLinkRepository MapLinks { get; }
            public IKraftwerksRepository Kraftwerks { get; }
            public IMapRegionRepository MapRegions { get; }
            public IMapMarkerRepository MapMarkers { get; }
            public IRecipeRepository Recipes { get; }
            public INpcMissionRepository NpcMissions { get; }
            public INpcMissionRewardRepository NpcMissionRewards { get; }
            public IMissionContentRepository MissionContent { get; }
            public INpcPackageRepository NpcPackages { get; }
            public IPlayerRandomNameRepository RandomNames { get; }
            public ISpawnpoolRepository Spawnpools { get; }
            public ITeleporterRepository Teleporters { get; }
            public void Complete() { }
            public void Reject() { }
            public Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction BeginTransaction() =>
                throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class QueryConfiguration(BootcampSelectionTestContext owner)
            : IDbContextConfigurationService
        {
            public void Configure(
                DbContextOptionsBuilder builder,
                DatabaseConnectionConfiguration configuration)
            {
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory())
                    .Configure(builder, configuration);
                builder.AddInterceptors(new QueryInterceptor(owner));
            }
        }

        private sealed class QueryInterceptor(BootcampSelectionTestContext owner)
            : DbCommandInterceptor
        {
        }

        private sealed class MapChannelManagerScope : IDisposable
        {
            private readonly FieldInfo _singleton = typeof(MapChannelManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly object _previous;

            internal MapChannelManagerScope(MapChannelManager current)
            {
                _previous = _singleton.GetValue(null);
                _singleton.SetValue(null, current);
            }

            public void Dispose()
            {
                _singleton.SetValue(null, _previous);
            }
        }
    }
}
