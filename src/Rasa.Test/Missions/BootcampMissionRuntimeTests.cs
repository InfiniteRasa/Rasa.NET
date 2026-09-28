extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using GameClientState = RasaGame::Rasa.Data.ClientState;

namespace Rasa.Test.Missions
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Context;
    using Context.World;
    using Data;
    using Game;
    using Managers;
    using Repositories.World;
    using Repositories.UnitOfWork;
    using Services.DbContext;
    using Structures;
    using Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class BootcampMissionRuntimeTests
    {
        private const uint MissingScoutAreaId = 435;
        private const uint BootcampMapContextId = 1985;
        private const uint WildernessMapContextId = 1220;
        private static readonly TimeSpan FuseDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ArrivalDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan BombDeadline = TimeSpan.FromMinutes(10);

        [TestMethod]
        public void CallingForReinforcementsSuccessPathDelaysTheHandoffUntilAfterTheCharge()
        {
            using var harness = CreateHarness();
            var youngblood = harness.AddNpc(510207, 2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, true);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Context.Client,
                youngblood.EntityId,
                1995));
            harness.Context.Drain();

            AssertMissionObjectives(
                harness.Context.Client.Player.Missions[1995],
                (2U, MissionObjectiveState.Incomplete),
                (3U, MissionObjectiveState.Inactive),
                (1U, MissionObjectiveState.Inactive),
                (4U, MissionObjectiveState.Inactive));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Area(1995, MissingScoutAreaId)));
            Assert.AreEqual(
                MissionObjectiveState.Completed,
                harness.Context.Client.Player.Missions[1995].Objectives[2].State);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Context.Client.Player.Missions[1995].Objectives[3].State);
            Assert.IsNull(FindNpcByPackage(harness.BootcampMap, 2584));
            Assert.IsNotNull(FindScenarioObject(harness.BootcampMap, "bootcamp-conrad-corpse"));
            Assert.IsNotNull(FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Interaction(21081)));
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Context.Client.Player.Missions[1995].Objectives[1].State);
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Context.Client.Player.Missions[1995].Objectives[4].State);
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Context.Client));
            Assert.IsNull(FindNpcByPackage(harness.BootcampMap, 2564));
            using (var unit = harness.Context.CreateChar())
            {
                var deadline = unit.CharacterMissionDeadlines.Get(harness.Context.Client.Player.Id, 1995);
                Assert.IsNotNull(deadline);
                Assert.AreEqual(CharacterMissionDeadlineState.Active, deadline.State);
                Assert.AreEqual(harness.UtcNow + BombDeadline, deadline.DueAtUtc);
            }

            harness.UtcNow += FuseDelay;
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Context.Client));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Interaction(24586)));
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Context.Client.Player.Missions[1995].Objectives[4].State);
            Assert.IsNull(FindNpcByPackage(harness.BootcampMap, 2564));
            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(
                    CharacterMissionDeadlineState.Satisfied,
                    unit.CharacterMissionDeadlines.Get(harness.Context.Client.Player.Id, 1995).State);

            Assert.IsFalse(harness.Manager.TickScenarios(harness.Context.Client));
            harness.UtcNow += FuseDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Context.Client));
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                harness.Context.Client.Player.Missions[1995].Objectives[4].State);
            Assert.IsNull(FindNpcByPackage(harness.BootcampMap, 2564));

            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Context.Client));
            Assert.AreEqual(MissionObjectiveState.Inactive, harness.Context.Client.Player.Missions[1995].Objectives[4].State);
            DefeatAssault(harness);

            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Context.Client.Player.Missions[1995].Objectives[4].State);
            var vanValkenberg = FindNpcByPackage(harness.BootcampMap, 2564);
            Assert.IsNotNull(vanValkenberg);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Context.Client,
                vanValkenberg.EntityId,
                1995,
                4,
                1));

            Assert.AreEqual(GameClientState.Ingame, harness.Context.Client.State);
            Assert.IsNull(harness.Context.Client.PendingTransfer);
            Assert.IsTrue(harness.Context.Client.Player.Missions[1995].Completeable);
            using var completedUnit = harness.Context.CreateChar();
            Assert.IsFalse(completedUnit.CharacterFlags.HasValue(
                harness.Context.Client.Player.Id,
                CharacterFlagIds.BootcampComplete));
            Assert.IsFalse(harness.Context.Client.AccountEntry.CanSkipBootcamp);
        }

        [TestMethod]
        public void CallingForReinforcementsFailureMakesRetryAvailableButNotBeforeExpiry()
        {
            using var harness = CreateHarness();
            var youngblood = harness.AddNpc(510207, 2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, true);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Context.Client,
                youngblood.EntityId,
                1995));
            AssertMissionNotAdvertised(harness.Manager, harness.Context.Client.Player, youngblood, 2005);

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Area(1995, MissingScoutAreaId)));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Interaction(21081)));
            AssertMissionNotAdvertised(harness.Manager, harness.Context.Client.Player, youngblood, 2005);

            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Context.Client));

            var failedMission = harness.Context.Client.Player.Missions[1995];
            Assert.AreEqual(MissionState.Failed, failedMission.State);
            Assert.AreEqual(MissionObjectiveState.Failed, failedMission.Objectives[1].State);
            AssertMissionAdvertised(harness.Manager, harness.Context.Client.Player, youngblood, 2005);
        }

        [TestMethod]
        public void CallingForReinforcementsRetryStartsItsDeadlineOnAcceptAndCompletesAfterTheFuse()
        {
            using var harness = CreateHarness();
            var youngblood = harness.AddNpc(510207, 2561);

            harness.SeedMission(1, 1994, (uint)MissionState.Completed, true);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Context.Client,
                youngblood.EntityId,
                1995));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Area(1995, MissingScoutAreaId)));
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Interaction(21081)));
            harness.UtcNow += BombDeadline + TimeSpan.FromSeconds(1);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Context.Client));

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Context.Client,
                youngblood.EntityId,
                2005));
            harness.Context.Drain();

            var retryMission = harness.Context.Client.Player.Missions[2005];
            AssertMissionObjectives(
                retryMission,
                (1U, MissionObjectiveState.Incomplete),
                (4U, MissionObjectiveState.Inactive));
            using (var unit = harness.Context.CreateChar())
            {
                var deadline = unit.CharacterMissionDeadlines.Get(harness.Context.Client.Player.Id, 2005);
                Assert.IsNotNull(deadline);
                Assert.AreEqual(CharacterMissionDeadlineState.Active, deadline.State);
                Assert.AreEqual(harness.UtcNow + BombDeadline, deadline.DueAtUtc);
            }

            harness.UtcNow += FuseDelay;
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Context.Client));
            Assert.IsNull(FindNpcByPackage(harness.BootcampMap, 2564, missionId: 2005));

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Context.Client,
                MissionProgressEvent.Interaction(24586)));
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                retryMission.Objectives[4].State);

            harness.UtcNow += FuseDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Context.Client));
            Assert.AreEqual(
                MissionObjectiveState.Inactive,
                retryMission.Objectives[4].State);
            Assert.IsNull(FindNpcByPackage(harness.BootcampMap, 2564, missionId: 2005));

            harness.UtcNow += ArrivalDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Context.Client));
            DefeatAssault(harness);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                retryMission.Objectives[4].State);
            var vanValkenberg = FindNpcByPackage(harness.BootcampMap, 2564, missionId: 2005);
            Assert.IsNotNull(vanValkenberg);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Context.Client,
                vanValkenberg.EntityId,
                2005,
                4,
                1));

            Assert.AreEqual(GameClientState.Ingame, harness.Context.Client.State);
            Assert.IsNull(harness.Context.Client.PendingTransfer);
            Assert.IsTrue(harness.Context.Client.Player.Missions[2005].Completeable);
            Assert.IsFalse(harness.Context.Client.AccountEntry.CanSkipBootcamp);
        }

        private static void AssertMissionObjectives(
            MissionLog mission,
            params (uint ObjectiveId, MissionObjectiveState State)[] expected)
        {
            CollectionAssert.AreEqual(
                expected,
                expected
                    .Select(entry => (entry.ObjectiveId, mission.Objectives[entry.ObjectiveId].State))
                    .ToArray());
        }

        private static void AssertMissionAdvertised(
            MissionApplication manager,
            Manifestation player,
            Creature npc,
            uint missionId)
        {
            var classification = manager.ClassifyNpcConversation(player, npc);
            Assert.IsTrue(classification.TryGetStatus(out var status, out var missionIds));
            Assert.AreEqual(ConversationStatus.Available, status);
            CollectionAssert.Contains(missionIds, missionId);
        }

        private static void AssertMissionNotAdvertised(
            MissionApplication manager,
            Manifestation player,
            Creature npc,
            uint missionId)
        {
            var classification = manager.ClassifyNpcConversation(player, npc);
            if (!classification.TryGetStatus(out _, out var missionIds))
                return;

            CollectionAssert.DoesNotContain(missionIds, missionId);
        }

        private static void DefeatAssault(BootcampRuntimeHarness harness)
        {
            foreach (var creature in harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.DbId == 510228).ToArray())
            {
                creature.State = CharacterState.Dead;
                harness.Manager.Scenes.RecordDefeat(harness.BootcampMap, creature, null);
            }
        }

        private static Creature FindNpcByPackage(MapChannel map, uint npcPackageId, uint? missionId = null) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .SingleOrDefault(creature =>
                    creature.Npc?.NpcPackageId == npcPackageId &&
                    (missionId == null ||
                     creature.SpawnPool?.ScenarioMissionId == missionId));

        private static DynamicObject FindScenarioObject(MapChannel map, string key) =>
            map.DynamicObjects.SingleOrDefault(dynamicObject =>
                dynamicObject.SceneActorRole == key ||
                string.Equals(dynamicObject.ScenarioKey, key, StringComparison.Ordinal) ||
                dynamicObject.ScenarioKey?.EndsWith($":object:{key}", StringComparison.Ordinal) == true);

        private static void PrepareBootcampScenarioClasses()
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            foreach (var entityClassId in new uint[] { 24586, 24990, 21081, 10516 })
                if (!classes.ContainsKey((EntityClasses)entityClassId))
                    classes.Add((EntityClasses)entityClassId, new EntityClass(
                        entityClassId,
                        $"scenario_object_{entityClassId}",
                        0,
                        0,
                        entityClassId == 21081 ? new List<AugmentationType> { AugmentationType.NPC } : new List<AugmentationType>(),
                        true));

            if (!classes.TryGetValue((EntityClasses)4001, out var creatureClass))
            {
                creatureClass = new EntityClass(4001, "scenario_creature", 0, 0, new List<AugmentationType>(), true);
                classes.Add((EntityClasses)4001, creatureClass);
            }

            if (!creatureClass.Augmentations.Contains(AugmentationType.Creature))
                creatureClass.Augmentations.Add(AugmentationType.Creature);
        }

        private static void MoveClientToMap(Client client, MapChannel origin, MapChannel destination)
        {
            CellManager.Instance.RemoveFromWorld(client);
            origin.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        private static BootcampRuntimeHarness CreateHarness()
        {
            var databaseDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "TestDatabases",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(databaseDirectory);
            var worldDatabase = Path.Combine(databaseDirectory, "world");
            var worldContext = (SqliteWorldContext)CreateContext(typeof(SqliteWorldContext), worldDatabase);
            worldContext.Database.Migrate();


            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());
            context.AddRewardTemplate(11519, 20000064);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)20000064]
                .ItemTemplates[11519].InventoryCategory = InventoryCategory.Mission;
            context.Map.MapInfo = new MapInfo(BootcampMapContextId, "bootcamp_runtime", 1556, 0);
            context.Client.Player.MapContextId = BootcampMapContextId;

            PrepareBootcampScenarioClasses();

            RuntimeLoadingFactory factory = null;
            MissionApplication manager = null;
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            foreach (var creatureId in new[] { 39U, 50U, 510208U, 510209U, 510227U, 510228U })
            {
                creatures.LoadedCreatures[creatureId] = new Creature
                {
                    DbId = creatureId,
                    EntityClass = (EntityClasses)4001,
                    Npc = new Npc
                    {
                        NpcPackageId = creatureId == 510208U
                            ? 2584U
                            : creatureId == 510209U
                                ? 2564U
                                : creatureId
                    },
                    AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
                };
            }

            MapChannelManager maps = null;
            var objects = new DynamicObjectManager(null, maps);
            var manifestation = new ManifestationManager(context);
            var deadlineService = new MissionDeadlineService(
                () => factory,
                () => manager,
                () => now);
            var scenarioService = new MissionSceneHost(
                () => factory,
                () => manager,
                manifestation,
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService(),
                scenarioService: scenarioService);
            maps.MapChannelArray.Add(BootcampMapContextId, context.Map);
            maps.MapChannelArray.Add(WildernessMapContextId, new MapChannel
            {
                MapInfo = new MapInfo(WildernessMapContextId, "alia_das_fixture", 1556, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128
            });
            objects = new DynamicObjectManager(null, maps);
            factory = new RuntimeLoadingFactory(context, worldContext);
            manager = new MissionApplication(
                factory,
                new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>(),
                manifestation,
                deadlineService: deadlineService,
                scenarioService: scenarioService,
                utcNow: () => now);
            var report = manager.LoadMissions();
            Assert.IsFalse(report.BlocksReadiness, string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var bootcampMap = maps.GetOrCreatePrivateInstance(BootcampMapContextId, context.Client.Player.Id);
            MoveClientToMap(context.Client, context.Map, bootcampMap);

            return new BootcampRuntimeHarness(
                context,
                worldContext,
                manager,
                bootcampMap,
                singletons,
                () => now,
                value => now = value);
        }

        private static RasaDbContextBase CreateContext(Type contextType, string database)
        {
            var connection = new DatabaseConnectionConfiguration { Database = database };
            var options = Options.Create(new DatabaseConfiguration
            {
                Provider = "Sqlite",
                Auth = connection,
                Char = connection,
                World = connection
            });
            return (RasaDbContextBase)Activator.CreateInstance(
                contextType,
                options,
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier())!;
        }

        private sealed class BootcampRuntimeHarness : IDisposable
        {
            private readonly Func<DateTime> _getUtcNow;
            private readonly Action<DateTime> _setUtcNow;
            private readonly IDisposable _singletons;

            internal BootcampRuntimeHarness(
                MissionTestContext context,
                SqliteWorldContext worldContext,
                MissionApplication manager,
                MapChannel bootcampMap,
                IDisposable singletons,
                Func<DateTime> getUtcNow,
                Action<DateTime> setUtcNow)
            {
                Context = context;
                WorldContext = worldContext;
                Manager = manager;
                BootcampMap = bootcampMap;
                _singletons = singletons;
                _getUtcNow = getUtcNow;
                _setUtcNow = setUtcNow;
            }

            internal MissionTestContext Context { get; }
            internal SqliteWorldContext WorldContext { get; }
            internal MissionApplication Manager { get; }
            internal MapChannel BootcampMap { get; }

            internal DateTime UtcNow
            {
                get => _getUtcNow();
                set => _setUtcNow(value);
            }

            internal void SeedMission(uint characterId, uint missionId, uint state, bool completeable)
            {
                using (var unit = Context.CreateChar())
                {
                    unit.ExecuteTransaction(() =>
                    {
                        unit.CharacterMissions.Add(new CharacterMissionEntry(characterId, missionId, state)
                        {
                            Completeable = completeable
                        });
                        if (Manager.LoadedMissions.TryGetValue(missionId, out var definition) &&
                            definition.IsOperational)
                        {
                            unit.CharacterMissionProgress.AddObjectives(
                                definition.Objectives.Values.Select(objective =>
                                {
                                    var row = new CharacterMissionObjectiveEntry(
                                        characterId,
                                        missionId,
                                        objective.ObjectiveId,
                                        (byte)(completeable && objective.IsRequired.Value
                                            ? MissionObjectiveState.Completed
                                            : objective.InitialState.Value));
                                    foreach (var counter in objective.Counters)
                                        row.Counters.Add(new CharacterMissionObjectiveCounterEntry(
                                            characterId,
                                            missionId,
                                            objective.ObjectiveId,
                                            counter.Key,
                                            counter.Value.InitialValue));
                                    foreach (var counter in objective.ItemCounters)
                                        row.ItemCounters.Add(new CharacterMissionObjectiveItemCounterEntry(
                                            characterId,
                                            missionId,
                                            objective.ObjectiveId,
                                            counter.Key,
                                            counter.Value.InitialValue));
                                    return row;
                                }));
                        }
                    });
                }

                using var reload = Context.CreateChar();
                Manager.Hydrate(
                    Context.Client.Player,
                    reload.CharacterMissions.Get(Context.Client.Player.Id),
                    reload.CharacterMissionProgress.Get(Context.Client.Player.Id));
            }

            internal Creature AddNpc(uint dbId, uint npcPackageId) =>
                Context.AddNpc(dbId, BootcampMap, npcPackageId);

            public void Dispose()
            {
                _singletons.Dispose();
                var directory = Path.GetDirectoryName(WorldContext.Database.GetDbConnection().DataSource);
                WorldContext.Dispose();
                SqliteConnection.ClearAllPools();
                Context.Dispose();
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private sealed class RuntimeLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly SqliteWorldContext _worldContext;

            internal RuntimeLoadingFactory(
                MissionTestContext charFactory,
                SqliteWorldContext worldContext)
            {
                _charFactory = charFactory;
                _worldContext = worldContext;
            }

            public Repositories.Char.ICharUnitOfWork CreateChar() => _charFactory.CreateChar();

            public IWorldUnitOfWork CreateWorld() =>
                new RepositoryBackedWorldUnitOfWork(_worldContext);
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
                MapMarkers = null;
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

        private sealed class ManagerInstances : IDisposable
        {
            private readonly FieldInfo _mapsField = typeof(MapChannelManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _objectsField = typeof(DynamicObjectManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _creaturesField = typeof(CreatureManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _missionsField = typeof(MissionApplication)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly object _previousMaps;
            private readonly object _previousObjects;
            private readonly object _previousCreatures;
            private readonly object _previousMissions;

            internal ManagerInstances(
                MapChannelManager maps,
                DynamicObjectManager objects,
                CreatureManager creatures,
                MissionApplication missions)
            {
                _previousMaps = _mapsField.GetValue(null);
                _previousObjects = _objectsField.GetValue(null);
                _previousCreatures = _creaturesField.GetValue(null);
                _previousMissions = _missionsField.GetValue(null);
                _mapsField.SetValue(null, maps);
                _objectsField.SetValue(null, objects);
                _creaturesField.SetValue(null, creatures);
                _missionsField.SetValue(null, missions);
            }

            public void Dispose()
            {
                _mapsField.SetValue(null, _previousMaps);
                _objectsField.SetValue(null, _previousObjects);
                _creaturesField.SetValue(null, _previousCreatures);
                _missionsField.SetValue(null, _previousMissions);
            }
        }
    }
}
