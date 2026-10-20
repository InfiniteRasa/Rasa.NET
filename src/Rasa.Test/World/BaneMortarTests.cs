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
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A Bane Mortar on every empty mortar base of the maps with a zone (BaneMortars,
    /// Bane_mortars): a lieutenant of its zone on its pad, facing as the pad does, in the Bane
    /// garrison of a control point it stands by.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BaneMortarTests
    {
        private const string Before = "20270120000000_Lightning_fields_strikes";
        private const string Migration = "20270122000000_Bane_mortars";

        /// <summary>The Wilderness pads that already had their mortar (WildernessWaveAPopulationV1).</summary>
        private static readonly uint[] WildernessMortars = { 630076, 630077, 630078, 630079 };

        [TestMethod]
        public void EachZonesMortarIsALieutenantByTheOpenCountryRulesAsTheBaneMortarIsAtLevel42()
        {
            Assert.HasCount(15, BaneMortars.Zones);

            foreach (var zone in BaneMortars.Zones)
            {
                var health = BaneMortars.BaseHealth(zone.Level);
                Assert.AreEqual((uint)Math.Round(1.75 * health), zone.Health, zone.Name);
                Assert.AreEqual(10 * zone.Level - 20, zone.Armor, zone.Name);
                Assert.AreEqual(((uint)Math.Round(0.064 * health), (uint)Math.Round(0.096 * health)), (zone.MinDamage, zone.MaxDamage), zone.Name);
                Assert.IsTrue(zone.Creature is >= BaneMortars.CreatureIdMin and <= BaneMortars.CreatureIdMax);
                Assert.IsTrue(zone.Action is >= BaneMortars.ActionIdMin and <= BaneMortars.ActionIdMax);
            }

            // Ashen Desert's is the Bane Mortar's level, and the Bane Mortar to the point.
            var ashen = BaneMortars.Zones.Single(zone => zone.Name == "Ashen Desert");
            Assert.AreEqual((42u, 17500u, 400u, 640u, 960u), (ashen.Level, ashen.Health, ashen.Armor, ashen.MinDamage, ashen.MaxDamage));
        }

        [TestMethod]
        public void EveryPadHasOnePoolOfItsOwn()
        {
            Assert.HasCount(237, BaneMortars.Mortars);
            Assert.HasCount(237, BaneMortars.Mortars.Select(mortar => mortar.Pool).Distinct().ToList());
            Assert.HasCount(237, BaneMortars.Mortars.Select(mortar => mortar.Pad).Distinct().ToList(), "one mortar a pad");
            Assert.IsTrue(BaneMortars.Mortars.All(mortar => mortar.Pool is >= BaneMortars.PoolIdMin and <= BaneMortars.PoolIdMax));
            Assert.IsTrue(BaneMortars.Mortars.All(mortar => Math.Abs(mortar.Rotation) <= 3.1416), "a facing, to four places");
            Assert.HasCount(94, BaneMortars.Mortars.Where(mortar => mortar.ControlPoint != 0).ToList(), "in a control point's garrison");
            CollectionAssert.AreEquivalent(BaneMortars.Zones.Select(zone => zone.Name).ToList(),
                BaneMortars.Mortars.Select(mortar => mortar.Zone).Distinct().ToList(), "every zone's row used, no zone missing");
        }

        [TestMethod]
        public void TheMigrationPutsAMortarOnEachPadAndDownTakesThemAway()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                AssertMortars(world);

                world.GetService<IMigrator>().Migrate(Before);

                Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id >= BaneMortars.PoolIdMin && row.Id <= BaneMortars.PoolIdMax));
                Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => row.Id >= BaneMortars.CreatureIdMin && row.Id <= BaneMortars.CreatureIdMax));
                Assert.IsFalse(world.CreatureStatEntries.AsNoTracking().Any(row => row.Id >= BaneMortars.CreatureIdMin && row.Id <= BaneMortars.CreatureIdMax));
                Assert.IsFalse(world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id >= BaneMortars.CreatureIdMin && row.Id <= BaneMortars.CreatureIdMax));
                Assert.IsFalse(world.CreatureActionEntries.AsNoTracking().Any(row => row.Id >= BaneMortars.ActionIdMin && row.Id <= BaneMortars.ActionIdMax));
                Assert.IsFalse(world.ControlPointLinkEntries.AsNoTracking().Any(row => row.ObjectId >= BaneMortars.PoolIdMin && row.ObjectId <= BaneMortars.PoolIdMax));
                Assert.IsFalse(world.Set<CreatureLootBindingEntry>().AsNoTracking().Any(row => row.CreatureId >= BaneMortars.CreatureIdMin && row.CreatureId <= BaneMortars.CreatureIdMax));

                world.GetService<IMigrator>().Migrate(Migration);

                AssertMortars(world);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static void AssertMortars(WorldContext world)
        {
            var pools = world.SpawnPoolEntries.AsNoTracking().ToList();
            var ours = pools.Where(row => row.Id >= BaneMortars.PoolIdMin && row.Id <= BaneMortars.PoolIdMax).ToDictionary(row => row.Id);
            var links = world.ControlPointLinkEntries.AsNoTracking()
                .Where(row => row.ObjectId >= BaneMortars.PoolIdMin && row.ObjectId <= BaneMortars.PoolIdMax).ToList();
            var points = world.ControlPointEntries.AsNoTracking().ToList();
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
            var maps = world.MapInfoEntries.AsNoTracking().Select(row => row.Id).ToHashSet();
            var spawnPoints = world.TeleporterEntries.AsNoTracking().ToList()
                .Where(row => row.Description != null && (row.Description.StartsWith("Hospital:") || row.Description.StartsWith("Waypoint:"))).ToList();
            var friendly = pools.Where(row => creatures.TryGetValue(row.Creature1Id, out var creature) && creature.Faction == 1).ToList();
            var wilderness = pools.Where(row => WildernessMortars.Contains(row.Id)).ToList();

            Assert.HasCount(BaneMortars.Mortars.Length, ours);
            Assert.HasCount(BaneMortars.Mortars.Count(mortar => mortar.ControlPoint != 0), links);
            Assert.HasCount(4, wilderness);

            foreach (var mortar in BaneMortars.Mortars)
            {
                var pool = ours[mortar.Pool];
                var zone = BaneMortars.ZoneOf(mortar);

                Assert.IsTrue(maps.Contains(pool.MapContextId), $"pool {pool.Id} on a map of the world");
                Assert.AreEqual((mortar.Map, mortar.X, mortar.Y, mortar.Z, mortar.Rotation), (pool.MapContextId, (double)pool.PosX, (double)pool.PosY, (double)pool.PosZ, (double)pool.Rotation), $"pool {pool.Id}");
                Assert.AreEqual((zone.Creature, (byte)1, (byte)1, 0u), (pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Creature2Id), $"pool {pool.Id}: one mortar");
                Assert.AreEqual(0.0, pool.Radius, $"pool {pool.Id}: where its pad is");
                Assert.AreEqual(BaneMortars.Respawn, pool.RespawnTime);

                // Not on a pad that has one already.
                Assert.IsFalse(wilderness.Any(row => row.MapContextId == pool.MapContextId && Math.Sqrt(Math.Pow(row.PosX - pool.PosX, 2) + Math.Pow(row.PosZ - pool.PosZ, 2)) < 4),
                    $"pool {pool.Id} on a Wilderness mortar's pad");

                double Off(double x, double z) => Math.Sqrt(Math.Pow(x - pool.PosX, 2) + Math.Pow(z - pool.PosZ, 2));

                // In the garrison of the point whose ground it is on, and only then: within reach
                // of the nearest point, or of a hospital, waypoint or NPC of a point's post.
                var onMap = points.Where(point => point.MapContextId == pool.MapContextId).ToList();
                var nearest = onMap.Select(point => (point.Id, Distance: Off(point.PosX, point.PosZ))).OrderBy(point => point.Distance).FirstOrDefault();
                var facilities = onMap.Where(point =>
                        spawnPoints.Any(row => row.MapContextId == pool.MapContextId && row.Description.Contains($"{point.Name} (Control Point)") && Off(row.PosX, row.PosZ) <= BaneMortars.FacilityReach) ||
                        friendly.Any(row => row.MapContextId == pool.MapContextId && (creatures[row.Creature1Id].Comment ?? "").Contains($"{point.Name} (Control Point)") && Off(row.PosX, row.PosZ) <= BaneMortars.FacilityReach))
                    .Select(point => point.Id).ToList();
                var byMarker = nearest.Id != 0 && nearest.Distance <= BaneMortars.ControlPointReach;

                if (mortar.ControlPoint != 0)
                {
                    Assert.IsTrue(byMarker ? nearest.Id == mortar.ControlPoint : facilities.Contains(mortar.ControlPoint), $"pool {pool.Id}: on point {mortar.ControlPoint}'s ground");
                    Assert.AreEqual((byte)SpawnPoolManager.ModeControlPoint, pool.Mode);
                    Assert.AreEqual(1, links.Count(link => link.ObjectId == pool.Id && link.ControlPointId == mortar.ControlPoint && link.Kind == ControlPointLinkEntry.KindBanePool));
                }
                else
                {
                    Assert.IsFalse(byMarker, $"pool {pool.Id} by a point and not in its garrison");
                    Assert.IsEmpty(facilities, $"pool {pool.Id} by a point's post and not in its garrison");
                    Assert.AreEqual((byte)SpawnPoolManager.ModeAutomatic, pool.Mode);

                    // Nowhere a player arrives within its notice, nor by friendly NPCs it would fight.
                    Assert.IsFalse(spawnPoints.Any(row => row.MapContextId == pool.MapContextId && Off(row.PosX, row.PosZ) <= BaneMortars.SpawnPointReach), $"pool {pool.Id} by a hospital or waypoint");
                    Assert.IsFalse(friendly.Any(row => row.MapContextId == pool.MapContextId && Off(row.PosX, row.PosZ) <= BaneMortars.FriendlyReach), $"pool {pool.Id} by a friendly post");
                }
            }

            foreach (var zone in BaneMortars.Zones)
            {
                var creature = creatures[zone.Creature];
                var attack = actions[zone.Action];
                var weapon = world.CreatureAppearanceEntries.AsNoTracking().Single(row => row.Id == zone.Creature);

                Assert.AreEqual((BaneMortars.MortarClass, 0u, zone.Level, zone.Health, 0u), (creature.ClassId, creature.Faction, creature.Level, creature.MaxHitPoints, creature.NameId), zone.Name);
                Assert.AreEqual((0u, 0u), (creature.RunSpeed, creature.WalkSpeed), "an emplacement never moves");
                Assert.AreEqual((zone.Action, 0u), (creature.Action1, creature.Action2));
                Assert.AreEqual(((int)zone.Health, (int)zone.Armor), (stats[zone.Creature].Health, stats[zone.Creature].Armor));
                Assert.AreEqual((411u, 1u, 60.0, 1000u, zone.MinDamage, zone.MaxDamage, 1u),
                    (attack.ActionId, attack.ActionArgId, attack.RangeMax, attack.Cooldown, attack.MinDamage, attack.MaxDamage, attack.DamageType), zone.Name);
                Assert.AreEqual((BaneMortars.WeaponSlot, BaneMortars.Launcher), (weapon.SlotId, weapon.ClassId), "the launcher the shell comes from");
            }

            // On a map whose loot policy requires a profile, every mortar has one: the Game server
            // does not start without (OrdinaryLootCoverage).
            var requiring = world.Set<LootMapPolicyEntry>().AsNoTracking().Where(row => row.RequireProfiles).Select(row => row.MapContextId).ToList();
            var bindings = world.Set<CreatureLootBindingEntry>().AsNoTracking().ToList();
            foreach (var pool in ours.Values.Where(pool => requiring.Contains(pool.MapContextId)))
                Assert.AreEqual(1, bindings.Count(row => row.MapContextId == pool.MapContextId && row.CreatureId == pool.Creature1Id), $"pool {pool.Id}: a loot profile");
            Assert.IsTrue(ours.Values.Any(pool => requiring.Contains(pool.MapContextId)), "the Wilderness's two are on such a map");

            // Ashen Desert's matches the Bane Mortar row.
            var bane = creatures[WreckCreatures.BaneMortarId];
            var ashen = creatures[BaneMortars.Zones.Single(zone => zone.Name == "Ashen Desert").Creature];
            Assert.AreEqual((bane.ClassId, bane.Level, bane.MaxHitPoints), (ashen.ClassId, ashen.Level, ashen.MaxHitPoints));
            Assert.AreEqual((actions[bane.Action1].MinDamage, actions[bane.Action1].MaxDamage), (actions[ashen.Action1].MinDamage, actions[ashen.Action1].MaxDamage));
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "values (592011, 'Bane Mortar - Ashen Desert', 7482, 0, 42, 17500, 0, 0, 0, 75211, 0, 0, 0, 0, 0, 0, 0);");
            StringAssert.Contains(up, "values (75211, 'Bane Mortar Ashen Desert weapon 411/1', 411, 1, 0.0, 60.0, 1000, 0, 640, 960, 1);");
            StringAssert.Contains(up, "insert into creature_appearance (id, slot_id, Class_id, color) values (592011, 13, 10604, 1);");
            StringAssert.Contains(up, "insert into control_point_link (control_point_id, kind, object_id) values (1, 1, 592103);");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "delete from spawnpool where id between 592101 and 592999;");
            StringAssert.Contains(down, "delete from control_point_link where kind = 1 and object_id between 592101 and 592999;");
        }

        /// <summary>
        /// A zone's mortar, put down from its pool, stands where the pool is with its launcher
        /// drawn, its zone's level and health, and shells a player who comes within its notice (Creature.AggroRange, as any emplacement).
        /// </summary>
        [TestMethod]
        public void AMortarStandsOnItsPadArmedAndShellsAPlayerInReach()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var map = harness.Map;
            var zone = BaneMortars.Zones.Single(z => z.Name == "Divide");
            var spot = harness.Client.Player.Position + new Vector3(12, 0, 0);

            map.SpawnPools.Clear();
            map.SpawnPools.Add(new SpawnPool
            {
                DbId = BaneMortars.PoolIdMax,
                Position = spot,
                Rotation = 1.5,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Mode = SpawnPoolManager.ModeAutomatic,
                RespawnTime = 1000,
                UpdateTimer = 1000,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(zone.Creature, 1, 1) }
            });

            SpawnPoolManager.Instance.SpawnPoolWorker(map, 0);

            var mortar = map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Single(creature => creature.DbId == zone.Creature);
            Assert.AreEqual((EntityClasses)BaneMortars.MortarClass, mortar.EntityClass);
            Assert.IsTrue(Emplacements.Is(mortar));
            Assert.AreEqual(zone.Level, mortar.Level);
            Assert.AreEqual((double)zone.Health, mortar.Attributes[Attributes.Health].NormalMax, 0.5);
            Assert.AreEqual(BaneMortars.Launcher, mortar.AppearanceData[EquipmentData.Weapon].Class, "its launcher in the weapon slot");
            Assert.AreEqual(spot.X, mortar.Position.X, 0.01);
            Assert.AreEqual(spot.Z, mortar.Position.Z, 0.01);

            var stood = mortar.Position;
            var shelled = false;
            for (var tick = 0; tick < 40 && !shelled; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(map, 250);
                shelled = mortar.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                          mortar.Controller.ActionFighting.TargetEntityId == harness.Client.Player.EntityId;
            }

            Assert.IsTrue(shelled, "the player 12 m off, within its notice, taken on");
            Assert.AreEqual(stood, mortar.Position, "and from where it stands");
        }
    }
}
