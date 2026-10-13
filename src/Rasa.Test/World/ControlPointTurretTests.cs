using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The turrets on the AFS control points' walls (Add_control_point_turrets): an AFS turret in
    /// the AFS's garrison and a Bane turret in the Bane's on each spot, and what is left of the
    /// side that loses a point taken off its spots.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ControlPointTurretTests
    {
        private const string Before = "20261206000000_Add_edmund_range_upper_floor";
        private const string Migration = "20261207000000_Add_control_point_turrets";

        /// <summary>The spots each point has: its fence's outer corners and its turret platforms.</summary>
        private static readonly Dictionary<uint, int> SpotsByPoint = new()
        {
            [2] = 10, [4] = 6, [6] = 8, [7] = 1, [10] = 6, [12] = 4, [13] = 6, [17] = 8,
            [19] = 4, [21] = 9, [22] = 6, [23] = 6, [28] = 7, [31] = 6, [33] = 4, [40] = 5
        };

        [TestMethod]
        public void EachSpotHasTheAfsTurretOfItsZoneInTheAfsGarrisonAndABaneTurretInTheBanesAndDownTakesThemAway()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                AssertTurrets(world);

                world.GetService<IMigrator>().Migrate(Before);

                Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id >= ControlPointTurrets.PoolIdMin && row.Id <= ControlPointTurrets.PoolIdMax));
                Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => row.Id >= ControlPointTurrets.CreatureIdMin && row.Id <= ControlPointTurrets.CreatureIdMax));
                Assert.IsFalse(world.CreatureActionEntries.AsNoTracking().Any(row => row.Id >= ControlPointTurrets.ActionIdMin && row.Id <= ControlPointTurrets.ActionIdMax));
                Assert.IsFalse(world.ControlPointLinkEntries.AsNoTracking().Any(row => row.ObjectId >= ControlPointTurrets.PoolIdMin && row.ObjectId <= ControlPointTurrets.PoolIdMax));

                world.GetService<IMigrator>().Migrate(Migration);

                AssertTurrets(world);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static void AssertTurrets(WorldContext world)
        {
            var pools = world.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id >= ControlPointTurrets.PoolIdMin && row.Id <= ControlPointTurrets.PoolIdMax).ToDictionary(row => row.Id);
            var links = world.ControlPointLinkEntries.AsNoTracking()
                .Where(row => row.ObjectId >= ControlPointTurrets.PoolIdMin && row.ObjectId <= ControlPointTurrets.PoolIdMax).ToList();
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
            var points = world.ControlPointEntries.AsNoTracking().ToDictionary(row => row.Id);

            Assert.HasCount(ControlPointTurrets.Spots.Length * 2, pools);
            Assert.HasCount(ControlPointTurrets.Spots.Length * 2, links);
            CollectionAssert.AreEquivalent(SpotsByPoint.ToList(),
                ControlPointTurrets.Spots.GroupBy(spot => spot.Point).ToDictionary(group => group.Key, group => group.Count()).ToList());

            foreach (var spot in ControlPointTurrets.Spots)
            {
                var afs = pools[spot.AfsPool];
                var bane = pools[spot.BanePool];
                var point = points[spot.Point];

                Assert.AreEqual(point.MapContextId, afs.MapContextId, $"pool {afs.Id} on its point's map");
                Assert.AreEqual((afs.PosX, afs.PosY, afs.PosZ, afs.Rotation, afs.MapContextId), (bane.PosX, bane.PosY, bane.PosZ, bane.Rotation, bane.MapContextId), $"pools {afs.Id} and {bane.Id} on one spot");
                Assert.IsLessThan(200.0, Math.Sqrt(Math.Pow(afs.PosX - point.PosX, 2) + Math.Pow(afs.PosZ - point.PosZ, 2)), $"pool {afs.Id} at its point");

                Assert.AreEqual((byte)SpawnPoolManager.ModeAutomatic, afs.Mode);
                Assert.AreEqual((byte)SpawnPoolManager.ModeControlPoint, bane.Mode);
                Assert.AreEqual((spot.AfsTurret, (byte)1, (byte)1), (afs.Creature1Id, afs.Creature1MinCount, afs.Creature1MaxCount));
                Assert.AreEqual((spot.BaneTurret, (byte)1, (byte)1), (bane.Creature1Id, bane.Creature1MinCount, bane.Creature1MaxCount));
                Assert.AreEqual(0u, afs.Creature2Id);
                Assert.AreEqual(0u, bane.Creature2Id);

                Assert.AreEqual(1, links.Count(link => link.ControlPointId == spot.Point && link.Kind == ControlPointLinkEntry.KindAfsPool && link.ObjectId == afs.Id));
                Assert.AreEqual(1, links.Count(link => link.ControlPointId == spot.Point && link.Kind == ControlPointLinkEntry.KindBanePool && link.ObjectId == bane.Id));

                Assert.AreEqual(4064u, creatures[spot.AfsTurret].ClassId, "Emplacement_AFS_Turret_Standard");
                Assert.AreEqual(ControlPointTurrets.BaneTurretClass, creatures[spot.BaneTurret].ClassId);
            }

            foreach (var zone in ControlPointTurrets.Zones)
            {
                var afs = creatures[zone.AfsTurret];
                var bane = creatures[zone.BaneTurret];
                var weapon = world.CreatureAppearanceEntries.AsNoTracking().Single(row => row.Id == zone.BaneTurret);
                var attack = actions[bane.Action1];

                Assert.AreEqual(0u, bane.Faction, "the Bane's");
                Assert.AreEqual((afs.Level, afs.MaxHitPoints), (bane.Level, bane.MaxHitPoints), $"{zone.Name}: the zone's AFS turret's level and health");
                Assert.AreEqual((stats[afs.Id].Body, stats[afs.Id].Mind, stats[afs.Id].Spirit, stats[afs.Id].Health, stats[afs.Id].Armor),
                    (stats[bane.Id].Body, stats[bane.Id].Mind, stats[bane.Id].Spirit, stats[bane.Id].Health, stats[bane.Id].Armor));
                Assert.AreEqual((actions[afs.Action1].MinDamage, actions[afs.Action1].MaxDamage), (attack.MinDamage, attack.MaxDamage));
                Assert.AreEqual((411u, 1u), (attack.ActionId, attack.ActionArgId), "Weapon_Creature_Bane_Mortar_Launcher: WEAPON_GROUNDTARGET 1");
                Assert.AreEqual((13u, ControlPointTurrets.BaneMortarLauncher), (weapon.SlotId, weapon.ClassId));
                Assert.AreEqual(0u, bane.Action2);
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "values (551001, 'Bane Turret - Wilderness', 7482, 0, 15, 2892, 0, 0, 0, 73001, 0, 0, 0, 0, 0, 0, 0);");
            StringAssert.Contains(up, "values (73001, 'Bane Turret Wilderness weapon 411/1', 411, 1, 0.0, 60.0, 1000, 0, 62, 93, 1);");
            StringAssert.Contains(up, "insert into control_point_link (control_point_id, kind, object_id) values (4, 1, ");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "delete from spawnpool where id between 551101 and 551999;");
            StringAssert.Contains(down, "delete from control_point_link where kind in (1, 2) and object_id between 551101 and 551999;");
        }

        #region The side that loses a point

        private const uint PointId = 7001;
        private const uint AfsPoolId = 997001;
        private const uint BanePoolId = 997002;
        private const uint AfsTurret = 550013;      // AFS Light Turret - Bootcamp, which keeps its wreck
        private const uint BaneTurret = 590001;     // Bane Mortar
        private const long RespawnMs = 300_000;
        private const EntityClasses LightTurretClass = (EntityClasses)11302;   // Emplacement_AFS_Turret_Mini

        private Func<uint, bool> _known;
        private Func<long> _now;
        private Func<long> _utcNow;

        [TestInitialize]
        public void Start()
        {
            _known = ControlPoints.Instance.KnownCreature;
            _now = ControlPoints.Instance.Now;
            _utcNow = ControlPoints.Instance.UtcNow;
            ControlPoints.Instance.KnownCreature = id => true;
            ControlPoints.Instance.Now = () => 1_000_000;
            ControlPoints.Instance.UtcNow = () => 1_700_000_000_000;
        }

        [TestCleanup]
        public void Restore()
        {
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            ControlPoints.Instance.KnownCreature = _known;
            ControlPoints.Instance.Now = _now;
            ControlPoints.Instance.UtcNow = _utcNow;
        }

        [TestMethod]
        public void TheLosersWreckIsTakenOffTheSpotAndTheWinnersTurretIsSetDownThere()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var map = harness.Map;
            var spot = harness.Client.Player.Position + new Vector3(6, 0, 0);

            Load(harness, AfsTurret);
            Load(harness, BaneTurret);
            map.SpawnPools.Clear();
            map.SpawnPools.Add(Pool(harness, AfsPoolId, AfsTurret, SpawnPoolManager.ModeAutomatic, spot));
            map.SpawnPools.Add(Pool(harness, BanePoolId, BaneTurret, SpawnPoolManager.ModeControlPoint, spot));

            ControlPoints.Instance.Load(new[] { Entry(map.MapInfo.MapContextId, spot) }, new[]
            {
                new ControlPointLinkEntry { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindAfsPool, ObjectId = AfsPoolId },
                new ControlPointLinkEntry { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = BanePoolId }
            }, null);
            ControlPoints.Instance.Place(map);
            var point = ControlPoints.Instance.ById(PointId);

            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);

            var afs = Emplacements(map).Single();
            Assert.AreEqual(LightTurretClass, afs.EntityClass, "the AFS's turret, with the AFS holding the point");
            Assert.AreEqual(1, Emplacements(map).Count());

            // Destroyed, it stays on the spot as its wreck for its pool to put back.
            Kill(map, afs);
            Assert.IsTrue(AlternateMesh.KeepsWreck(afs));

            // The Bane take the point: the wreck goes, and their turret is set down on the spot.
            Assert.IsTrue(ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null));
            Assert.IsFalse(Emplacements(map).Contains(afs), "the AFS turret's wreck is taken off");
            Assert.IsFalse(AlternateMesh.KeepsWreck(afs));

            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);

            var bane = Emplacements(map).Single();
            Assert.AreEqual((EntityClasses)ControlPointTurrets.BaneTurretClass, bane.EntityClass);
            Assert.AreEqual(spot.X, bane.Position.X, 0.01);
            Assert.AreEqual(spot.Z, bane.Position.Z, 0.01);

            // And back: the Bane turret's remains go and a fresh AFS turret stands there.
            Kill(map, bane);
            Assert.IsTrue(ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null));
            Assert.IsFalse(Emplacements(map).Contains(bane), "the Bane turret's remains are taken off");

            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);

            var fresh = Emplacements(map).Single();
            Assert.AreNotSame(afs, fresh);
            Assert.AreEqual(LightTurretClass, fresh.EntityClass);
            Assert.AreNotEqual(CharacterState.Dead, fresh.State);
        }

        private static ControlPointEntry Entry(uint map, Vector3 spot) => new ControlPointEntry
        {
            Id = PointId,
            MapContextId = map,
            Name = "Test Point",
            ClassId = 3814,
            PosX = spot.X + 20,
            PosY = spot.Y,
            PosZ = spot.Z,
            Rotation = 0,
            MarkerEntityId = 0,
            DefaultOwner = ControlPointEntry.OwnerAfs
        };

        /// <summary>The creature row and its class, with its flags, from the migrated world, as AlternateMeshTests reads them.</summary>
        private static void Load(WildernessRuntimeTestHarness harness, uint creatureId)
        {
            var entry = harness.World.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == creatureId);
            var classEntry = harness.World.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
            var entityClass = new EntityClass(classEntry.Id, classEntry.ClassName, classEntry.MeshId, classEntry.ClassCollisionRole,
                classEntry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), classEntry.TargetFlag != 0);

            entityClass.CreatureFlags.AddRange(harness.World.Set<CreatureClassFlagEntry>().AsNoTracking()
                .Where(row => row.ClassId == entry.ClassId).Select(row => (CreatureFlag)row.FlagId));
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.ClassId] = entityClass;
            CreatureManager.Instance.LoadedCreatures[entry.Id] = new Creature(entry) { AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
        }

        private static SpawnPool Pool(WildernessRuntimeTestHarness harness, uint id, uint creatureId, short mode, Vector3 spot) => new SpawnPool
        {
            DbId = id,
            Position = spot,
            MapContextId = harness.Map.MapInfo.MapContextId,
            RuntimeMapChannel = harness.Map,
            Mode = mode,
            AnimType = 0,
            RespawnTime = RespawnMs,
            UpdateTimer = RespawnMs,
            SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(creatureId, 1, 1) }
        };

        private static void Kill(MapChannel map, Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(map, creature, null);
        }

        private static IEnumerable<Creature> Emplacements(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Where(Rasa.Managers.Emplacements.Is).Distinct();

        #endregion
    }
}
