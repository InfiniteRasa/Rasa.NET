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
    /// Alia Das and three other Wilderness posts as a GM placed them from gameplay video
    /// (Alia_das_npcs, AliaDasNpcs).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AliaDasNpcsTests
    {
        private const string Migration = "20261231000000_Alia_das_npcs";
        private const string Previous = "20261230000000_Weapon_template_rows";

        [TestMethod]
        public void EveryoneStandsWhereTheGmPutThem()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var pool in AliaDasNpcs.Pools)
                Assert.AreEqual((pool.Creature, R(pool.X), R(pool.Y), R(pool.Z), R(pool.Rotation)), Pool(database, pool.Id), $"pool {pool.Id}");

            foreach (var move in AliaDasNpcs.Moves)
                Assert.AreEqual((move.Creature, R(move.ToX), R(move.ToY), R(move.ToZ), R(move.ToRotation)), Pool(database, move.Pool), move.Who);

            foreach (var removed in AliaDasNpcs.Removed)
                Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {removed.Id}"), $"pool {removed.Id} taken away");

            // The six class trainers are gone from Alia Das; Kincaid, who trains every class, stands where they were pinned.
            Assert.AreEqual(0L, Count(database, "SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 501001 AND 501006"));
            Assert.IsTrue(ClassTrainers.TryGet(AliaDasNpcs.Kincaid, out var kincaid) && kincaid.Trains == CharacterClass.None);
            Assert.IsTrue(Vector2.Distance(new Vector2(765f, 385.8047f), new Vector2(766f, 386.1f)) < 1.1f, "the client's Class Trainer: Alia Das pin");

            foreach (var (pool, steps) in AliaDasNpcs.Beats())
                Assert.AreEqual((long)steps.Length, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool_patrol WHERE pool_id = {pool}"), $"pool {pool}'s beat");

            Assert.AreEqual(1L, Count(database, "SELECT pose AS Value FROM spawnpool_pose WHERE id = 100"), "Rogers stands at his post");
            Assert.AreEqual(2L, Count(database, "SELECT pose AS Value FROM spawnpool_pose WHERE id = 193"), "Salter holds his rifle ready");
            Assert.AreEqual(1L, Count(database, "SELECT pose AS Value FROM spawnpool_pose WHERE id = 219"), "Apirka at ease with his spear");
            Assert.AreEqual(1L, Count(database, $"SELECT pose AS Value FROM spawnpool_pose WHERE id = {AliaDasNpcs.GuardNorthPool}"));

            Assert.AreEqual(147L, Count(database, "SELECT greeting_id AS Value FROM npc_greeting WHERE id = 100"));
            Assert.AreEqual(1307L, Count(database, "SELECT greeting_id AS Value FROM npc_greeting WHERE id = 133"));
            Assert.AreEqual(1199L, Count(database, "SELECT greeting_id AS Value FROM npc_greeting WHERE id = 118"));
            Assert.AreEqual(488L, Count(database, "SELECT greeting_id AS Value FROM npc_greeting WHERE id = 510002"), "Beacham keeps his line");
            Assert.AreEqual(859L, Count(database, "SELECT greeting_id AS Value FROM npc_greeting WHERE id = 30"), "the registrar keeps the clan line");

            // Maddrey sells what the "Human" sold; the two by the crafting tent the Kraftwerks' components.
            Assert.AreEqual(Count(database, "SELECT COUNT(*) AS Value FROM vendor_item WHERE id = 25"),
                Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {AliaDasNpcs.Maddrey}"));
            Assert.AreEqual(36L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {AliaDasNpcs.Maddrey}"));
            Assert.AreEqual(10L, Count(database, $"SELECT package_id AS Value FROM vendor WHERE id = {AliaDasNpcs.Maddrey}"));
            Assert.AreEqual(18L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {AliaDasNpcs.Harrington}"));
            Assert.AreEqual(6L, Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {AliaDasNpcs.North}"));
            var components = database.Context.Database.SqlQueryRaw<long>("SELECT DISTINCT x.itemTemplateId AS Value FROM recipe_input i " +
                "JOIN itemtemplate_itemclass x ON x.itemClassId = i.input_class_id").ToList();
            CollectionAssert.AreEquivalent(components, AliaDasNpcs.HarringtonStock.Concat(AliaDasNpcs.NorthStock).Select(t => (long)t).ToList(),
                "every component a recipe takes, once");

            Assert.AreEqual(8997L, Count(database, "SELECT name_id AS Value FROM creature WHERE id = 30"), "Clan Registrar Kolchak");
            Assert.AreEqual((long)AliaDasNpcs.SwapsetFemale, Count(database, "SELECT class_id AS Value FROM creature WHERE id = 30"));
            Assert.AreEqual(8509L, Count(database, "SELECT name_id AS Value FROM creature WHERE id = 73"), "Supply Sergeant Caruso");
            Assert.AreEqual(6339L, Count(database, "SELECT class_id AS Value FROM creature WHERE id = 118"), "Cimoch on his swapset body: a mission giver needs the NPC augmentation");
            Assert.AreEqual((long)AliaDasNpcs.ForeanSpearman, Count(database, "SELECT class_id AS Value FROM creature WHERE id = 43"), "Apirka a spearman");

            foreach (var look in AliaDasNpcs.Looks)
                CollectionAssert.AreEqual(look.Is.OrderBy(s => s.Id).Select(s => (long)s.Id * 1000003 + s.ClassId).ToList(),
                    Appearance(database, look.Creature).Select(row => row.Slot * 1000003 + row.ClassId).ToList(), look.Who);

            Assert.AreEqual(2L, Count(database, "SELECT COUNT(*) AS Value FROM ambient_npc WHERE id IN (22, 23) AND map_context_id = 1220"));

            // The objectives that send the player to them point where they stand now.
            foreach (var indicator in AliaDasNpcs.Indicators)
                Assert.AreEqual((R(indicator.ToX), R(indicator.ToY), R(indicator.ToZ)), database.Context.Database.SqlQueryRaw<PointRow>(
                        $"SELECT pos_x AS X, pos_y AS Y, pos_z AS Z FROM mission_indicator WHERE mission_id = {indicator.Mission} " +
                        $"AND objective_id = {indicator.Objective} AND indicator_id = {indicator.IndicatorId}")
                    .AsEnumerable().Select(row => (R(row.X), R(row.Y), R(row.Z))).Single(), indicator.Who);

            // And the escort to Solis ends in front of him, facing him.
            var bindings = database.Context.Database.SqlQueryRaw<string>(
                "SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1407 AND script_key = 'wilderness.solis-escort'").AsEnumerable().Single();
            StringAssert.Contains(bindings, AliaDasNpcs.SolisRouteEndIs);
            Assert.IsFalse(bindings.Contains(AliaDasNpcs.SolisRouteEndWas));
            var end = new Vector2(788.39f, 583.29f);
            Assert.AreEqual(1.5f, Vector2.Distance(end, new Vector2(789.8906f, 583.3438f)), 0.01f);
            Assert.AreEqual(1.5708, database.Context.Database.SqlQueryRaw<double>("SELECT rotation AS Value FROM kraftwerks WHERE id = 4").Single(), 1e-6,
                "the crafting station a quarter turn clockwise");
        }

        [TestMethod]
        public void TheCraftingStationsBackMeetsTheTentWallItStandsAgainst()
        {
            // prop_hum_craftstation_v01.geo: x -1.884 to 0.490 (its depth, the back at +x),
            // z -2.099 to 1.899 (its 4 m length). Yaw turns the model as a creature's facing does:
            // local (x, z) to world (x cos + z sin, -x sin + z cos).
            var yaw = AliaDasNpcs.CraftingStationIs;
            var corners = new[] { (-1.884, -2.099), (-1.884, 1.899), (0.490, -2.099), (0.490, 1.899) }
                .Select(c => (X: AliaDasNpcs.CraftingStationX + c.Item1 * Math.Cos(yaw) + c.Item2 * Math.Sin(yaw),
                              Z: AliaDasNpcs.CraftingStationZ - c.Item1 * Math.Sin(yaw) + c.Item2 * Math.Cos(yaw)))
                .ToList();

            // The tent (ArchHumTentGeneric01V02): inner faces from the navmesh's inside edges, less its erosion.
            const double south = 359.2, north = 368.1, west = 794.15, east = 801.86;

            Assert.AreEqual(south, corners.Min(c => c.Z), 0.05, "its back against the south wall");
            Assert.IsTrue(corners.All(c => c.Z <= north && c.X >= west && c.X <= east), "and wholly inside the tent");

            // Unturned, it went through the wall by 1.6 m.
            Assert.IsTrue(AliaDasNpcs.CraftingStationZ - 2.099 < south - 1.5);
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedAndTheDownPutsBackWhatItChanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM moved Moawi and recoloured Beacham's tunic by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x = 10 WHERE id = 64");
            database.Context.Database.ExecuteSqlRaw("UPDATE creature_appearance SET color = 123 WHERE id = 510002 AND slot_id = 15");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(10.0, Pool(database, 64).X, "Moawi left where the GM put him");
            var beacham = Appearance(database, 510002);
            Assert.AreEqual(123L, beacham.Single(row => row.Slot == 15).Color, "the GM's tunic colour kept");
            Assert.AreEqual((long)AliaDasNpcs.Brown, beacham.Single(row => row.Slot == 2).Color, "his boots brown");

            // Twice changes nothing.
            var once = Snapshot(database);
            // Raw, not formatted: the escort's JSON has braces.
            foreach (var sql in AliaDasNpcs.Up())
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

            Assert.IsTrue(creatures[AliaDasNpcs.Kolchak].Npc.NpcIsClanMaster, "Clan Registrar Kolchak keeps the clan window");
            Assert.IsTrue(CreatureManager.IsClanMaster(8838) && CreatureManager.IsClanMaster(8993) && CreatureManager.IsClanMaster(9000) && !CreatureManager.IsClanMaster(9001));
            Assert.AreEqual(36, creatures[AliaDasNpcs.Maddrey].Npc.Vendor.VendorItems.Count, "Maddrey's weapons and ammunition");
            Assert.AreEqual(18, creatures[AliaDasNpcs.Harrington].Npc.Vendor.VendorItems.Count);
            Assert.AreEqual(6, creatures[AliaDasNpcs.North].Npc.Vendor.VendorItems.Count);
            Assert.IsTrue(creatures[AliaDasNpcs.Kincaid].Npc.NpcIsTrainer);
            Assert.AreEqual(147u, creatures[AliaDasNpcs.Rogers].Npc.GreetingId);
            Assert.AreEqual(1199u, creatures[AliaDasNpcs.Cimoch].Npc.GreetingId);

            var pools = harness.Map.SpawnPools.ToDictionary(pool => pool.DbId);
            foreach (var (pool, steps) in AliaDasNpcs.Beats())
                Assert.AreEqual(steps.Length, pools[pool].Patrol?.Count ?? 0, $"pool {pool} walks its beat");
            Assert.AreEqual(NpcPose.WeaponOut, pools[193].Pose, "Salter");
            Assert.AreEqual(NpcPose.Standing, pools[100].Pose, "Rogers");
            Assert.IsFalse(pools.ContainsKey(36) || Enumerable.Range(501001, 6).Any(id => pools.ContainsKey((uint)id)));

            // The posted ones stand exactly on their spots.
            harness.SpawnWorld(AliaDasNpcs.GuardNorthPool, AliaDasNpcs.SpauldingPool, 100);
            foreach (var pool in new[] { AliaDasNpcs.GuardNorthPool, AliaDasNpcs.SpauldingPool, 100u })
            {
                var npc = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Single(c => c.SpawnPool?.DbId == pool);
                var spot = AliaDasNpcs.Pools.Where(p => p.Id == pool).Select(p => new Vector3((float)p.X, (float)p.Y, (float)p.Z))
                    .Concat(AliaDasNpcs.Moves.Where(m => m.Pool == pool).Select(m => new Vector3((float)m.ToX, (float)m.ToY, (float)m.ToZ))).Single();
                Assert.IsTrue(Vector3.Distance(spot, npc.Position) < 0.01f, $"pool {pool} at {npc.Position}, not {spot}");
            }
        }

        [TestMethod]
        public void EveryBeatIsOnWalkableGroundAndNoLegCutsThroughAWall()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = harness.Map.NavMesh;

            foreach (var (pool, steps) in AliaDasNpcs.Beats())
            {
                for (var i = 0; i < steps.Length; i++)
                {
                    var here = new Vector3((float)steps[i].X, (float)steps[i].Y, (float)steps[i].Z);
                    var ground = nav.Nearest(here + new Vector3(0, 0.2f, 0));
                    Assert.IsTrue(ground.HasValue, $"pool {pool} step {i} off the navmesh");
                    Assert.IsTrue(new Vector2(ground.Value.X - here.X, ground.Value.Z - here.Z).Length() < 0.6f, $"pool {pool} step {i} {here} near {ground}");
                    Assert.AreEqual(ground.Value.Y, here.Y, 0.5f, $"pool {pool} step {i}: its floor");

                    // The leg to the next step (the last back to the first) is a walk the navmesh allows,
                    // not one round a wall: its path is no longer than the straight line.
                    var next = steps[(i + 1) % steps.Length];
                    var there = new Vector3((float)next.X, (float)next.Y, (float)next.Z);
                    var path = nav.FindPath(ground.Value, nav.Nearest(there + new Vector3(0, 0.2f, 0)).Value, out var complete);
                    Assert.IsTrue(complete, $"pool {pool} leg {i} cannot be walked");
                    var walked = Length(ground.Value, path);
                    var straight = new Vector2(there.X - here.X, there.Z - here.Z).Length();
                    Assert.IsTrue(walked <= straight + 0.75f, $"pool {pool} leg {i}: {walked:F2} m of path for {straight:F2} m straight");
                }
            }

            // The trainee keeps clear of the flagpole's sandbags, and the road party of the road.
            foreach (var step in AliaDasNpcs.CircleBeat)
                Assert.AreEqual(AliaDasNpcs.CircleRadius, Vector2.Distance(new Vector2((float)step.X, (float)step.Z), new Vector2((float)AliaDasNpcs.FlagX, (float)AliaDasNpcs.FlagZ)), 0.01);
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in AliaDasNpcs.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in AliaDasNpcs.Down())
                StringAssert.Contains(down, sql);
        }

        private static float Length(Vector3 start, List<Vector3> path)
        {
            var total = 0f;
            var at = start;
            foreach (var point in path)
            {
                total += new Vector2(point.X - at.X, point.Z - at.Z).Length();
                at = point;
            }
            return total;
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
            var creatures = AliaDasNpcs.Creatures.Select(c => c.Id).Concat(AliaDasNpcs.Recasts.Select(r => r.Creature))
                .Concat(AliaDasNpcs.Looks.Select(l => l.Creature)).Concat(AliaDasNpcs.Removed.Select(r => r.Creature)).Distinct().ToList();
            var pools = AliaDasNpcs.Pools.Select(p => p.Id).Concat(AliaDasNpcs.Moves.Select(m => m.Pool))
                .Concat(AliaDasNpcs.Removed.Select(r => r.Id)).Distinct().ToList();
            var c = string.Join(", ", creatures);
            var p = string.Join(", ", pools);

            string[] queries =
            {
                $"SELECT 'creature ' || id || ' ' || comment || ' ' || class_id || ' ' || name_id || ' ' || run_speed || ' ' || walk_speed || ' ' || level || ' ' || action1 AS Value FROM creature WHERE id IN ({c})",
                $"SELECT 'look ' || id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance WHERE id IN ({c})",
                $"SELECT 'pool ' || id || ' ' || creature_1_Id || ' ' || respown_time || ' ' || printf('%.3f %.3f %.3f %.3f', pos_x, pos_y, pos_z, rotation) AS Value FROM spawnpool WHERE id IN ({p})",
                $"SELECT 'pose ' || id || ' ' || pose AS Value FROM spawnpool_pose WHERE id IN ({p})",
                $"SELECT 'step ' || pool_id || ' ' || step AS Value FROM spawnpool_patrol WHERE pool_id IN ({p})",
                $"SELECT 'greeting ' || id || ' ' || greeting_id || ' ' || important AS Value FROM npc_greeting WHERE id IN ({c})",
                $"SELECT 'vendor ' || id || ' ' || package_id AS Value FROM vendor WHERE id IN ({c})",
                $"SELECT 'stock ' || id || ' ' || item_template_id AS Value FROM vendor_item WHERE id IN ({c})",
                "SELECT 'ambient ' || id || ' ' || class_id AS Value FROM ambient_npc WHERE id IN (22, 23)",
                "SELECT 'station ' || printf('%.4f', rotation) AS Value FROM kraftwerks WHERE id = 4",
                "SELECT 'indicator ' || mission_id || ' ' || objective_id || ' ' || indicator_id || ' ' || printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM mission_indicator WHERE mission_id IN (508, 701, 623, 1407, 1069)",
                "SELECT 'escort ' || bindings AS Value FROM mission_scene_binding WHERE mission_id = 1407",
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
