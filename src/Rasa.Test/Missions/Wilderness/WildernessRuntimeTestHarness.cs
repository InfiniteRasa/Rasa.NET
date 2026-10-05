using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Test.Database;

namespace Rasa.Test.Missions.Wilderness
{
    internal sealed class WildernessRuntimeTestHarness : IGameUnitOfWorkFactory, IDisposable
    {
        private readonly string _directory;
        private readonly List<Action> _restoreServices = new();
        private readonly HashSet<ulong> _originalCreatures = EntityManager.Instance.Creatures.Keys.ToHashSet();
        private readonly HashSet<ulong> _originalObjects = EntityManager.Instance.DynamicObjects.Keys.ToHashSet();
        private bool _disposed;

        private WildernessRuntimeTestHarness()
        {
            _directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        private void Initialize(Action<MigrationBuilder> additionalContent, string targetWorldMigration)
        {
            Context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());
            World = OpenWorld();
            if (targetWorldMigration == null)
                MigratedDatabaseTemplates.Migrate(World, World.Initialize);
            else
                World.GetService<IMigrator>().Migrate(targetWorldMigration);
            if (additionalContent != null)
            {
                var migration = new MigrationBuilder(World.Database.ProviderName);
                additionalContent(migration);
                var commands = World.GetService<IMigrationsSqlGenerator>().Generate(migration.Operations, World.Model);
                using var transaction = World.Database.BeginTransaction();
                foreach (var generated in commands)
                {
                    using var command = World.Database.GetDbConnection().CreateCommand();
                    command.Transaction = transaction.GetDbTransaction();
                    command.CommandText = generated.CommandText;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (System.Data.Common.DbException error)
                    {
                        var sql = command.CommandText.Length <= 1800
                            ? command.CommandText : command.CommandText.Substring(0, 1800);
                        throw new InvalidOperationException($"Additional mission data failed for generated SQL: {sql}", error);
                    }
                }
                transaction.Commit();
                World.ChangeTracker.Clear();
            }
            Map = Context.Map;
            Map.NavMesh = TestNavMeshes.Query(
                NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_foreas_concordia_wilderness"));

            var manifestations = new ManifestationManager(this);
            Maps = new MapChannelManager(this, refreshStats: (_, _) => { }, assignPlayer: _ => { },
                enterMapChannels: _ => { });
            Maps.MapChannelArray.Add(1220, Map);
            Manager = new MissionApplication(this, new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>(), manifestations, utcNow: () => UtcNow);
            Creatures = new CreatureManager(this, manifestations, Manager);
            Objects = new DynamicObjectManager(this, Maps, missionManager: Manager);
            Install(Maps);
            Install(Manager);
            Install(Creatures);
            Install(Objects);
            Install(manifestations);
            Install(new CharacterManager(this));
            Install(new NpcManager(this, Manager));
            Install(CreateService<LogosManager>());
            Install(CreateService<ToolActionManager>());
            Install(new InventoryManager(this, Manager));
            Install(new SpawnPoolManager(this));
            Install(new LootDispenserManager(this, missionManager: Manager));
            Install(CreateService<ItemManager>());
            Install(CreateService<EntityClassManager>());
            Install(CreateService<ClanManager>());
            Install(CreateService<SocialManager>());
            Install(new AuctionHouseManager(this, Manager));

            EntityClassManager.Instance.LoadEntityClasses();
            var report = Manager.LoadMissions();
            if (report.BlocksReadiness)
                throw new InvalidOperationException(string.Join("; ", report.Diagnostics.Select(item => item.Message)));
            Creatures.CreatureInit();
            SpawnPoolManager.Instance.SpawnPoolInit();
            Objects.InitTeleporters();
            LogosManager.Instance.LogosInit();
            Objects.DynamicObjectWorker(Map, 0);
            ConfigurePlayer();
        }

        internal static WildernessRuntimeTestHarness Create(Action<MigrationBuilder> additionalContent = null,
            string targetWorldMigration = null)
        {
            var harness = new WildernessRuntimeTestHarness();
            try
            {
                harness.Initialize(additionalContent, targetWorldMigration);
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        internal MissionTestContext Context { get; private set; }
        internal SqliteWorldContext World { get; private set; }
        internal MapChannel Map { get; private set; }
        internal MapChannelManager Maps { get; private set; }
        internal MissionApplication Manager { get; private set; }
        internal CreatureManager Creatures { get; private set; }
        internal DynamicObjectManager Objects { get; private set; }
        internal Client Client => Context.Client;
        internal DateTime UtcNow { get; set; } = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

        internal void SpawnWorld(params uint[] spawnIds) => SpawnWorldAfter(0, spawnIds);

        internal void SpawnWorldAfter(long milliseconds, params uint[] spawnIds)
        {
            var pools = Map.SpawnPools.ToArray();
            try
            {
                if (spawnIds.Length != 0)
                {
                    var selected = pools.Where(pool => spawnIds.Contains(pool.DbId)).ToArray();
                    if (selected.Length != spawnIds.Distinct().Count())
                        throw new InvalidOperationException("A requested migrated spawn pool is absent.");
                    Map.SpawnPools.Clear();
                    Map.SpawnPools.AddRange(selected);
                }
                SpawnPoolManager.Instance.SpawnPoolWorker(Map, milliseconds);
            }
            finally
            {
                Map.SpawnPools.Clear();
                Map.SpawnPools.AddRange(pools);
            }
        }

        internal void Tick(long milliseconds = 250)
        {
            UtcNow = UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(Map, milliseconds);
            Manager.Scenes.Tick(Map);
        }

        internal Creature Npc(uint spawnId) => Map.MapCellInfo.Cells.Values
            .SelectMany(cell => cell.CreatureList)
            .Distinct()
            .SingleOrDefault(creature => creature.SpawnPool?.DbId == spawnId && creature.Npc != null &&
                CreatureManager.IsLivingOnMap(Map, creature));

        internal void MoveTo(Vector3 position)
        {
            Client.SetWorldPosition(position, Client.Player.Rotation);
            CellManager.Instance.UpdateVisibility(Client);
        }

        internal IReadOnlyList<PythonPacket> Drain() => MissionTestContext.Drain(Client);

        internal AbilityManager LoadAbilities()
        {
            var abilities = CreateService<AbilityManager>();
            Install(abilities);
            abilities.AbilityInit();
            return abilities;
        }

        public ICharUnitOfWork CreateChar() => Context.CreateChar();

        public IWorldUnitOfWork CreateWorld()
        {
            var context = OpenWorld();
            return new WorldUnitOfWork(context,
                new ActionRepository(context), new EquipmentRepository(context), new CreaturesOfThisWorld(context),
                new EntityClassRepository(context), new FootlockerRepository(context), new LogosRepository(context),
                new MapInfoRepository(context), new MapLinkRepository(context), new KraftwerksRepository(context),
                new MapRegionRepository(context), new MapMarkerRepository(context),
                new MapEmitterRepository(context), new SpawnPoolArrivalRepository(context), new RecipeRepository(context),
                new NpcMissionRepository(context), new NpcMissionRewardRepository(context),
                new MissionContentRepository(context), new NpcPackageRepository(context),
                new PlayerRandomNameRepository(context), new SpawnpoolRepository(context), new TeleporterRepository(context));
        }

        /// <summary>
        /// The creature rows of this harness's World, which a test can stop at an earlier
        /// migration (targetWorldMigration). CreatureInit reads every NPC's greeting, and the
        /// table of them is Add_npc_greetings': a World stopped before that migration has no
        /// such table, where a server's World, migrated to the end, always has. A World without
        /// the table has no greetings, as one with the table and no rows has none.
        ///
        /// A table a later migration adds, read as the harness starts, needs the same.
        /// </summary>
        private sealed class CreaturesOfThisWorld : CreatureRepository, ICreatureRepository
        {
            private readonly SqliteWorldContext _context;

            internal CreaturesOfThisWorld(SqliteWorldContext context) : base(context) => _context = context;

            List<Rasa.Structures.World.NpcGreetingEntry> ICreatureRepository.GetNpcGreetings() =>
                HasTable(Rasa.Structures.World.NpcGreetingEntry.TableName)
                    ? GetNpcGreetings()
                    : new List<Rasa.Structures.World.NpcGreetingEntry>();

            private bool HasTable(string name) => _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = {0}", name)
                .AsEnumerable().Single() > 0;
        }

        private SqliteWorldContext OpenWorld() =>
            (SqliteWorldContext)PersistenceIntegrationTests.CreateContext(
                typeof(SqliteWorldContext), Path.Combine(_directory, "world"));

        private T CreateService<T>() where T : class =>
            (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null, args: new object[] { this }, culture: null);

        private void Install<T>(T service)
        {
            var field = typeof(T).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{typeof(T).Name} has no test-replaceable singleton.");
            var previous = field.GetValue(null);
            _restoreServices.Add(() => field.SetValue(null, previous));
            field.SetValue(null, service);
        }

        private void ConfigurePlayer()
        {
            Client.Player.RuntimeMapChannel = Map;
            Client.MissionAreaService = new MissionAreaService(() => Manager);
            Client.Player.Race = Race.Human;
            Client.Player.Class = (uint)CharacterClass.Recruit;
            foreach (var (kind, value) in new[]
            {
                (Attributes.Body, 10), (Attributes.Mind, 10), (Attributes.Spirit, 10),
                (Attributes.Health, 100), (Attributes.Chi, 100), (Attributes.Power, 100),
                (Attributes.Regen, 0), (Attributes.Armor, 0)
            })
                Client.Player.Attributes[kind] = new ActorAttributes(kind, value, value, value, 0, 0);
            MoveTo(new Vector3(884.11f, 305.8f, 347.81f));
            Drain();
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var creature in EntityManager.Instance.Creatures.Values
                         .Where(creature => !_originalCreatures.Contains(creature.EntityId) &&
                             Map != null && ReferenceEquals(creature.RuntimeMapChannel, Map)).ToArray())
                CellManager.Instance.RemoveCreatureFromWorld(Map, creature);
            // The map's objects too: EntityManager outlives the harness, and its registered
            // logos, teleporters and other objects each hold this map and so its whole world.
            foreach (var dynamicObject in EntityManager.Instance.DynamicObjects.Values
                         .Where(dynamicObject => !_originalObjects.Contains(dynamicObject.EntityId) &&
                             Map != null && ReferenceEquals(dynamicObject.RuntimeMapChannel, Map)).ToArray())
                CellManager.Instance.RemoveFromWorld(Map, dynamicObject);
            foreach (var restore in _restoreServices.AsEnumerable().Reverse())
                restore();
            World?.Dispose();
            Context?.Dispose();
            SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, true);
        }
    }
}
