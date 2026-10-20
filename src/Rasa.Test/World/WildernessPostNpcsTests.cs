using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The second batch of Wilderness NPCs and objects a GM placed from gameplay video
    /// (Wilderness_post_npcs, WildernessPostNpcs).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessPostNpcsTests
    {
        private const string Migration = "20270102000000_Wilderness_post_npcs";
        private const string Previous = "20270101000000_Cp_prestige_stock";

        [TestMethod]
        public void EveryoneAndEverythingIsWhereTheGmPutIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var pool in WildernessPostNpcs.Pools)
                Assert.AreEqual((pool.Creature, R(pool.X), R(pool.Y), R(pool.Z), R(pool.Rotation)), Pool(database, pool.Id), $"pool {pool.Id}");

            Assert.AreEqual((WildernessPostNpcs.LightTurret, R(WildernessPostNpcs.TurretX), R(WildernessPostNpcs.TurretY), R(WildernessPostNpcs.TurretZ),
                R(WildernessPostNpcs.TurretRotation)), Pool(database, WildernessPostNpcs.TurretPool), "the turret on the tripod by the command tent");
            Assert.AreEqual(3000L, Count(database, $"SELECT respown_time AS Value FROM spawnpool WHERE id = {WildernessPostNpcs.TurretPool}"));

            foreach (var move in WildernessPostNpcs.Moves)
                Assert.AreEqual((move.Creature, R(move.ToX), R(move.ToY), R(move.ToZ), R(move.ToRotation)), Pool(database, move.Pool), move.Who);

            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {WildernessPostNpcs.HumanVendor3Pool}"),
                "the unnamed vendor at the Landing Zone taken away");
            Assert.AreEqual(4L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN (31, 32, 34, 35) AND creature_1_Id = {WildernessPostNpcs.HumanVendor3}"),
                "his other four pools left");

            // The Landing Zone's AFS garrison: the new ones and the banker in, the vendor's pool out.
            foreach (var pool in WildernessPostNpcs.GarrisonJoins)
                Assert.AreEqual(1L, Count(database, $"SELECT COUNT(*) AS Value FROM control_point_link WHERE control_point_id = 4 AND kind = 2 AND object_id = {pool}"), $"pool {pool}");
            Assert.AreEqual(0L, Count(database, "SELECT COUNT(*) AS Value FROM control_point_link WHERE object_id = 33"));

            Assert.AreEqual(2L, Count(database, $"SELECT pose AS Value FROM spawnpool_pose WHERE id = {WildernessPostNpcs.TokenBanker}"), "the banker's pistol held up");
            Assert.AreEqual(2L, Count(database, $"SELECT pose AS Value FROM spawnpool_pose WHERE id = {WildernessPostNpcs.PrestigeVendorPool}"));
            Assert.AreEqual(7L, Count(database, "SELECT pose AS Value FROM spawnpool_pose WHERE id = 198"), "Lt. Saviours lying wounded");
            Assert.AreEqual(1L, Count(database, $"SELECT pose AS Value FROM spawnpool_pose WHERE id = {WildernessPostNpcs.BenosPool}"));

            foreach (var recast in WildernessPostNpcs.Recasts)
                Assert.AreEqual((long)recast.IsClass * 100000 + recast.IsName,
                    Count(database, $"SELECT class_id * 100000 + name_id AS Value FROM creature WHERE id = {recast.Creature}"), recast.IsComment);

            foreach (var look in WildernessPostNpcs.Looks)
                CollectionAssert.AreEqual(look.Is.OrderBy(s => s.Id).Select(s => ((long)s.Id, (long)s.ClassId, (long)s.Color)).ToList(),
                    Appearance(database, look.Creature), look.Who);

            // Sawyer and Solomon keep the stock they had; the two new sellers are stocked from their likes.
            Assert.AreEqual(11L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {WildernessPostNpcs.Sawyer}"));
            Assert.AreEqual(10L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {WildernessPostNpcs.Solomon}"));
            Assert.AreEqual(87L, Count(database, $"SELECT package_id AS Value FROM vendor WHERE id = {WildernessPostNpcs.Savage}"));
            Assert.AreEqual(10L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {WildernessPostNpcs.Savage}"));
            Assert.AreEqual(139L, Count(database, $"SELECT package_id AS Value FROM vendor WHERE id = {WildernessPostNpcs.PrestigeVendor}"));
            Assert.AreEqual(Count(database, "SELECT COUNT(*) AS Value FROM vendor_item WHERE id = 500100"),
                Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {WildernessPostNpcs.PrestigeVendor}"));

            foreach (var indicator in WildernessPostNpcs.Indicators)
                Assert.AreEqual((R(indicator.ToX), R(indicator.ToY), R(indicator.ToZ)), database.Context.Database.SqlQueryRaw<PointRow>(
                        $"SELECT pos_x AS X, pos_y AS Y, pos_z AS Z FROM mission_indicator WHERE mission_id = {indicator.Mission} " +
                        $"AND objective_id = {indicator.Objective} AND indicator_id = {indicator.IndicatorId}")
                    .AsEnumerable().Select(row => (R(row.X), R(row.Y), R(row.Z))).Single(), indicator.Who);

            for (var i = 0; i < WildernessPostNpcs.Usables.Length; i++)
                Assert.AreEqual((long)WildernessPostNpcs.Usables[i].ClassId,
                    Count(database, $"SELECT class_id AS Value FROM world_usable WHERE id = {WildernessPostNpcs.UsableId(i)} AND map_context_id = 1220 AND usable = 0"));

            Assert.AreEqual((long)WildernessPostNpcs.Footlockers.Length,
                Count(database, $"SELECT COUNT(*) AS Value FROM footlocker WHERE id >= {WildernessPostNpcs.FirstFootlocker} AND class_id = {WildernessPostNpcs.Footlocker} AND map_context_id = 1220"));
            Assert.AreEqual(1L, Count(database, "SELECT COUNT(*) AS Value FROM footlocker WHERE id = 1"), "Alia Das's own footlocker kept");

            Assert.AreEqual(15L, Count(database, $"SELECT COUNT(*) AS Value FROM world_destructible WHERE class_id = {WildernessPostNpcs.BaneBarrel} AND map_context_id = 1220"));
            Assert.AreEqual((long)WildernessPostNpcs.SideAfs, Count(database,
                $"SELECT side AS Value FROM world_destructible WHERE id = {WildernessPostNpcs.GateFieldId} AND class_id = {WildernessPostNpcs.HumanGateField}"));
        }

        [TestMethod]
        public void TheBarrelsAreInTheYardAndClearOfThePathBetweenItsGates()
        {
            foreach (var barrel in WildernessPostNpcs.Barrels)
            {
                Assert.IsTrue(barrel.X > -272 && barrel.X < -254 && barrel.Z > 72 && barrel.Z < 102, $"{barrel} in the yard");
                Assert.IsTrue(barrel.Z < 82 || barrel.Z > 92, $"{barrel} clear of the gates' path");
            }

            for (var i = 0; i < WildernessPostNpcs.Barrels.Length; i++)
                for (var j = i + 1; j < WildernessPostNpcs.Barrels.Length; j++)
                {
                    var a = WildernessPostNpcs.Barrels[i];
                    var b = WildernessPostNpcs.Barrels[j];
                    Assert.IsTrue(Vector2.Distance(new Vector2((float)a.X, (float)a.Z), new Vector2((float)b.X, (float)b.Z)) >= 2.2f, $"{a} and {b} apart");
                }
        }

        [TestMethod]
        public void TheGateFieldFillsTheGateAndEachFootlockerIsAtItsCotsFoot()
        {
            // ArchHumOutpostFenceGateV02 at (206, 163, -35), facing pi: its local x 0..16 runs to world x 206..190.
            // The field's sheet is local x -6.48..6.48 about its origin: so its origin is 8 m along, at 198.
            var field = ForceFields.ClassOf(WildernessPostNpcs.HumanGateField.ToString());
            Assert.IsNotNull(field);
            var turn = Quaternion.CreateFromYawPitchRoll((float)WildernessPostNpcs.GateFieldRotation, 0, 0);
            var gateEnd = new Vector3(206, 163, -35) + Vector3.Transform(new Vector3(16, 0, 0), turn);
            Assert.AreEqual(190f, gateEnd.X, 0.01f, "the gate runs west from its origin");
            var west = new Vector3((float)WildernessPostNpcs.GateFieldX, 163, (float)WildernessPostNpcs.GateFieldZ) + Vector3.Transform(new Vector3(-6.48f, 0, 0), turn);
            var east = new Vector3((float)WildernessPostNpcs.GateFieldX, 163, (float)WildernessPostNpcs.GateFieldZ) + Vector3.Transform(new Vector3(6.48f, 0, 0), turn);
            Assert.IsTrue(Math.Min(west.X, east.X) > 190f && Math.Max(west.X, east.X) < 206f, "the sheet inside the gate");
            Assert.AreEqual(198f, (west.X + east.X) / 2, 0.01f, "and centred in it");

            // The nine cots (ArchHumGenObjCotV01, local x -1.22..1.22): a footlocker 0.76 m deep stands
            // beyond each one's foot, the north row's toward the tent's middle, the south row's away from the dressers.
            var cots = new[] { (862.5, 405.01), (864.8, 404.99), (867.48, 405.02), (869.71, 405.09), (873.18, 405.09), (875.48, 405.07) };
            foreach (var (x, z) in cots)
                Assert.IsTrue(WildernessPostNpcs.Footlockers.Any(f => Math.Abs(f.X - x) < 0.01 && Math.Abs(f.Z - (z - 1.22 - 0.76)) < 0.01 && f.Rotation == 0.0), $"cot {x}, {z}");
            foreach (var (x, z) in new[] { (863.79, 398.96), (869.0, 399.0), (874.71, 399.01) })
                Assert.IsTrue(WildernessPostNpcs.Footlockers.Any(f => Math.Abs(f.X - (x - 1.22 - 0.76)) < 0.01 && Math.Abs(f.Z - z) < 0.01), $"cot {x}, {z}");
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedAndTheDownPutsBackWhatItChanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM moved Quillas and recoloured Rodriguez's tunic by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x = 10 WHERE id = 192");
            database.Context.Database.ExecuteSqlRaw("UPDATE creature_appearance SET color = 123 WHERE id = 97 AND slot_id = 15");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(10.0, Pool(database, 192).X, "Quillas left where the GM put him");
            var rodriguez = Appearance(database, 97);
            Assert.AreEqual((15L, (long)AliaDasNpcs.OfficerTunic, 123L), rodriguez.Single(row => row.Slot == 15), "the GM's tunic kept");
            Assert.AreEqual((2L, (long)WildernessPostNpcs.HeavyBoots, (long)WildernessPostNpcs.Orange), rodriguez.Single(row => row.Slot == 2), "his boots orange");

            // Twice changes nothing.
            var once = Snapshot(database);
            foreach (var sql in WildernessPostNpcs.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        [TestMethod]
        public void TheServerLoadsThemAsTheyAreMeant()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var creatures = harness.Creatures.LoadedCreatures;

            Assert.AreEqual(11, creatures[WildernessPostNpcs.Sawyer].Npc.Vendor.VendorItems.Count, "Sawyer's weapons");
            Assert.AreEqual(10, creatures[WildernessPostNpcs.Savage].Npc.Vendor.VendorItems.Count, "Savage's hospital stock");
            Assert.AreEqual(10, creatures[WildernessPostNpcs.Solomon].Npc.Vendor.VendorItems.Count, "Solomon's");
            Assert.IsNotNull(creatures[WildernessPostNpcs.PrestigeVendor].Npc.Vendor);

            var pools = harness.Map.SpawnPools.ToDictionary(pool => pool.DbId);
            Assert.AreEqual(NpcPose.WeaponOut, pools[WildernessPostNpcs.TokenBanker].Pose, "the CP Token Banker");
            Assert.AreEqual(NpcPose.LyingDown, pools[198].Pose, "Lt. Saviours");
            Assert.AreEqual(NpcPose.Standing, pools[WildernessPostNpcs.FieldGunnerPool].Pose, "the Field Gunner");
            Assert.IsTrue(pools.ContainsKey(WildernessPostNpcs.TurretPool));
            Assert.IsFalse(pools.ContainsKey(WildernessPostNpcs.HumanVendor3Pool));

            foreach (var classId in WildernessPostNpcs.Usables.Select(u => u.ClassId).Distinct())
            {
                Assert.IsNull(PlacedUsables.WhyNot((EntityClasses)classId, out _, out var kind), $"usable {classId}");
                Assert.AreEqual(PlacedUsables.Kind.TwoState, kind, $"usable {classId}");
            }

            Assert.IsNull(PlacedDestructibles.WhyNot((EntityClasses)WildernessPostNpcs.BaneBarrel, out var barrel));
            Assert.AreEqual(PlacedDestructibles.Kind.Inert, barrel);
            Assert.IsNull(PlacedDestructibles.WhyNot((EntityClasses)WildernessPostNpcs.HumanGateField, out var field));
            Assert.AreEqual(PlacedDestructibles.Kind.ForceField, field);

            // The posted ones stand exactly on their spots.
            harness.SpawnWorld(WildernessPostNpcs.FieldGunnerPool, WildernessPostNpcs.TokenBanker, WildernessPostNpcs.BenosPool);
            foreach (var pool in new[] { WildernessPostNpcs.FieldGunnerPool, WildernessPostNpcs.TokenBanker, WildernessPostNpcs.BenosPool })
            {
                var npc = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Single(c => c.SpawnPool?.DbId == pool);
                var spot = WildernessPostNpcs.Pools.Where(p => p.Id == pool).Select(p => new Vector3((float)p.X, (float)p.Y, (float)p.Z))
                    .Concat(WildernessPostNpcs.Moves.Where(m => m.Pool == pool).Select(m => new Vector3((float)m.ToX, (float)m.ToY, (float)m.ToZ))).Single();
                Assert.IsTrue(Vector3.Distance(spot, npc.Position) < 0.01f, $"pool {pool} at {npc.Position}, not {spot}");
            }
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in WildernessPostNpcs.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in WildernessPostNpcs.Down())
                StringAssert.Contains(down, sql);
        }

        private static double R(double value) => Math.Round(value, 3);

        private static (uint Creature, double X, double Y, double Z, double Rotation) Pool(OrdinaryLootTestDatabase database, uint pool) =>
            database.Context.Database.SqlQueryRaw<PoolRow>(
                    $"SELECT creature_1_Id AS Creature, pos_x AS X, pos_y AS Y, pos_z AS Z, rotation AS Rotation FROM spawnpool WHERE id = {pool}")
                .AsEnumerable().Select(row => ((uint)row.Creature, R(row.X), R(row.Y), R(row.Z), R(row.Rotation))).Single();

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static List<(long Slot, long ClassId, long Color)> Appearance(OrdinaryLootTestDatabase database, uint creature) =>
            database.Context.Database.SqlQueryRaw<AppearanceRow>(
                    $"SELECT slot_id AS Slot, Class_id AS ClassId, color AS Color FROM creature_appearance WHERE id = {creature} ORDER BY slot_id")
                .AsEnumerable().Select(row => (row.Slot, row.ClassId, row.Color)).ToList();

        /// <summary>Every row the migration touches, as text, positions to the millimetre.</summary>
        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var creatures = WildernessPostNpcs.Creatures.Select(c => c.Id).Concat(WildernessPostNpcs.Recasts.Select(r => r.Creature))
                .Concat(WildernessPostNpcs.Looks.Select(l => l.Creature)).Concat(WildernessPostNpcs.Removed.Select(r => r.Creature)).Distinct().ToList();
            var pools = WildernessPostNpcs.Pools.Select(p => p.Id).Concat(WildernessPostNpcs.Moves.Select(m => m.Pool))
                .Concat(WildernessPostNpcs.Removed.Select(r => r.Id)).Append(WildernessPostNpcs.TurretPool).Distinct().ToList();
            var c = string.Join(", ", creatures);
            var p = string.Join(", ", pools);

            string[] queries =
            {
                $"SELECT 'creature ' || id || ' ' || comment || ' ' || class_id || ' ' || name_id || ' ' || run_speed || ' ' || walk_speed || ' ' || level || ' ' || action1 AS Value FROM creature WHERE id IN ({c})",
                $"SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id IN ({c})",
                $"SELECT 'pool ' || id || ' ' || creature_1_Id || ' ' || respown_time || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM spawnpool WHERE id IN ({p})",
                $"SELECT 'pose ' || id || ' ' || pose AS Value FROM spawnpool_pose WHERE id IN ({p})",
                $"SELECT 'vendor ' || id || ' ' || package_id AS Value FROM vendor WHERE id IN ({c})",
                $"SELECT 'stock ' || id || ' ' || item_template_id AS Value FROM vendor_item WHERE id IN ({c})",
                "SELECT 'indicator ' || mission_id || ' ' || objective_id || ' ' || indicator_id || ' ' || printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM mission_indicator WHERE mission_id IN (429, 700, 820)",
                "SELECT 'usable ' || id || ' ' || class_id || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM world_usable",
                "SELECT 'footlocker ' || id || ' ' || class_id || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM footlocker",
                "SELECT 'link ' || control_point_id || ' ' || kind || ' ' || object_id AS Value FROM control_point_link",
                "SELECT 'destructible ' || id || ' ' || class_id || ' ' || side || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM world_destructible",
            };

            return queries.SelectMany(sql => database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable()).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        private sealed class PointRow
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }

        private sealed class PoolRow
        {
            public long Creature { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Rotation { get; set; }
        }

        private sealed class AppearanceRow
        {
            public long Slot { get; set; }
            public long ClassId { get; set; }
            public long Color { get; set; }
        }
    }
}
