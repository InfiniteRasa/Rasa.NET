using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.TestSupport;

    /// <summary>
    /// Title_bosses: the named bosses the boss titles (BossTitles) ask for and the world did not have. Up is nothing but
    /// inserts of new rows, so the rows that were there are the rows that are there; Down takes out exactly those; and
    /// with the migration in, every boss of every title's list has a creature, the new ones standing on a map of their
    /// own title, on the server's walkable ground and away from the safe ground the server's startup check guards.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TitleBossesTests
    {
        private const string Migration = "20270303000001_Title_bosses";

        /// <summary>How far a pool may stand over or under the walkable ground its boss is put on.</summary>
        private const float GroundTolerance = 2.5f;

        private static List<string[]> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string[]>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(Enumerable.Range(0, reader.FieldCount)
                    .Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)).ToArray());

            return rows;
        }

        private static readonly string[] Tables =
        {
            "creature", "creature_stat", "creature_appearance", "creature_action", "spawnpool", "teleporter", "npc_package", "vendor", "entityclass", "map_info"
        };

        private static Dictionary<string, long> Counts(RasaDbContextBase context) =>
            Tables.ToDictionary(t => t, t => long.Parse(Rows(context, $"select count(*) from {t}")[0][0], CultureInfo.InvariantCulture));

        private static IEnumerable<uint> BossIds => Enumerable.Range(0, TitleBossesSeed.BossCount).Select(i => TitleBossesSeed.FirstCreatureId + (uint)i);

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }

        [TestMethod]
        public void UpIsOnlyInsertsOfNewRowsAndDownTakesOutExactlyThem()
        {
            var up = TitleBossesSeed.UpStatements;
            var down = TitleBossesSeed.DownStatements;

            var creatures = up.Where(sql => sql.StartsWith("insert into creature (")).ToList();
            Assert.HasCount(32, creatures, "the 32 bosses");
            Assert.HasCount(TitleBossesSeed.BossCount, creatures);
            Assert.IsTrue(up.All(sql => sql.StartsWith("insert into ")), "Up inserts and does nothing else");
            Assert.IsTrue(down.All(sql => sql.StartsWith("delete from ") && sql.Contains(" where id = ")), "Down deletes rows by id and nothing else");
            Assert.HasCount(up.Length, down, "a delete for every insert");

            var tables = up.Select(sql => sql.Split(' ')[2]).Distinct().OrderBy(t => t).ToArray();
            CollectionAssert.AreEqual(new[] { "creature", "creature_action", "creature_appearance", "creature_stat", "spawnpool" }, tables);

            // The ids are the seed's own band (93xxxxx), clear of upstream's rows (all below 650000).
            foreach (var sql in up)
            {
                var id = long.Parse(sql.Substring(sql.IndexOf("values (", StringComparison.Ordinal) + 8).Split(',')[0], CultureInfo.InvariantCulture);
                Assert.IsTrue(id >= 9300001 && id <= 9399999, sql);
            }

            // One pool per boss: automatic, upstream's boss respawn (500), one creature, no radius.
            var pools = up.Where(sql => sql.StartsWith("insert into spawnpool")).ToList();
            Assert.HasCount(TitleBossesSeed.BossCount, pools);
            Assert.IsTrue(pools.All(sql => sql.Contains(", 0, 0, 500, ")), "mode 0, animation 0, respown_time 500");
            var emptyTail = ", 1, 1" + string.Concat(Enumerable.Repeat(", 0", 16)) + ");";
            Assert.IsTrue(pools.All(sql => sql.EndsWith(emptyTail)), "one boss, the five other slots and the radius empty");
        }

        /// <summary>
        /// The server's own startup: creatures and pools loaded from the migrated world database (CreatureInit,
        /// SpawnPoolInit), then ValidatePools' check against the hospitals and waypoint pads.
        /// </summary>
        [TestMethod]
        public void TheNewBossesLoadAsHostileCreaturesAndTheirPoolsPassTheServersOwnSafeGroundCheck()
        {
            using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create();
            var creatures = CreatureManager.Instance.LoadedCreatures;
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools;
            var ids = BossIds.ToList();

            foreach (var id in ids)
            {
                Assert.IsTrue(creatures.TryGetValue(id, out var creature), $"creature {id} is loaded");
                Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory, $"creature {id} is hostile");
                Assert.IsGreaterThan(0u, creature.NameId, $"creature {id} has the client's name for it");
                Assert.IsNotEmpty(creature.Actions, $"creature {id} attacks");
                Assert.IsTrue(creature.Level >= 32 && creature.Level <= 47, $"creature {id} level {creature.Level}");

                Assert.IsTrue(pools.TryGetValue(id, out var pool), $"pool {id} is loaded");
                Assert.AreEqual(SpawnPoolManager.ModeAutomatic, pool.Mode);
                Assert.AreEqual(1, pool.SpawnSlot.Count(slot => slot.CreatureId == id && slot.CountMax == 1), $"pool {id} holds its boss");
            }

            // ValidatePools: the pools against every hospital and waypoint pad, the friendly NPCs' pools and the AFS turrets.
            // The harness holds the teleporters of the Wilderness only, so the pads of every map are read from the table (as the
            // server's startup has them: type 2 a waypoint, type 5 a hospital).
            var places = Rows(harness.World, $"select map_context_id, pos_x, pos_y, pos_z from teleporter where type in ({(int)WaypointType.Waypoint}, {(int)WaypointType.Hospital}) and map_context_id <> 0")
                .Select(r => (Map: uint.Parse(r[0], CultureInfo.InvariantCulture),
                    Position: new Vector3(float.Parse(r[1], CultureInfo.InvariantCulture), float.Parse(r[2], CultureInfo.InvariantCulture), float.Parse(r[3], CultureInfo.InvariantCulture)),
                    What: "a hospital or waypoint pad"))
                .ToList();
            Assert.IsGreaterThan(100, places.Count, "the world's hospitals and waypoint pads are loaded");

            var found = SpawnPoolManager.Instance.PoolsTooClose(places);
            var ours = found.Where(item => ids.Any(id => item.Message.StartsWith($"spawnpool {id}:", StringComparison.Ordinal))).ToList();
            Assert.IsEmpty(ours, string.Join("; ", ours.Select(item => item.Message)));
        }

        /// <summary>
        /// Every new boss stands on the server's own walkable ground for its map (the navmesh its chase and its
        /// return home are walked on), at the ground's height.
        /// </summary>
        [TestMethod]
        public void EveryNewBossStandsOnItsMapsWalkableGround()
        {
            var navmeshes = Path.Combine(RepositoryRoot(), "navmesh");
            var maps = new Dictionary<uint, string>();
            var placed = new List<(uint Id, uint Map, Vector3 At)>();

            foreach (var sql in TitleBossesSeed.UpStatements.Where(sql => sql.StartsWith("insert into spawnpool")))
            {
                var values = sql.Substring(sql.IndexOf("values (", StringComparison.Ordinal) + 8).TrimEnd(';', ')').Split(',')
                    .Select(v => v.Trim()).ToArray();
                placed.Add((uint.Parse(values[0], CultureInfo.InvariantCulture), uint.Parse(values[8], CultureInfo.InvariantCulture),
                    new Vector3(float.Parse(values[4], CultureInfo.InvariantCulture), float.Parse(values[5], CultureInfo.InvariantCulture), float.Parse(values[6], CultureInfo.InvariantCulture))));
            }

            // The maps' file names, as map_info has them.
            maps[2028] = "adv_arieki_torden_abyss";
            maps[2190] = "adv_arieki_torden_abyss_dybukkar";
            maps[1911] = "adv_arieki_ligo_thunderhead";
            maps[1304] = "adv_foreas_valverde_pools";
            maps[2047] = "adv_foreas_valverde_descent";
            maps[1734] = "adv_arieki_ligo_ashendesert";
            maps[1993] = "adv_arieki_ligo_burningsteps";
            maps[1497] = "adv_foreas_valverde_plateau";

            var off = new List<string>();

            foreach (var (id, map, at) in placed)
            {
                Assert.IsTrue(maps.TryGetValue(map, out var name), $"pool {id}: map {map} is one of the bosses' maps");
                var nav = TestNavMeshes.Query(NavMeshFile.PathFor(navmeshes, name));
                var ground = nav.GroundHeight(at);
                var nearest = nav.Nearest(at);

                if (ground == null || nearest == null)
                    off.Add($"pool {id} on {name} at {at}: no walkable ground");
                else if (Math.Abs(ground.Value - at.Y) > GroundTolerance || Vector2.Distance(new Vector2(nearest.Value.X, nearest.Value.Z), new Vector2(at.X, at.Z)) > GroundTolerance)
                    off.Add($"pool {id} on {name} at {at}: ground {ground.Value:0.00}, nearest walkable {nearest.Value}");
            }

            Assert.IsEmpty(off, string.Join("; ", off));
        }

        [TestMethod]
        public void TheMigrationAddsTheBossesTheTitlesNeedAndNothingElseAndComesBackOut()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                // The world as it is the migration before this one.
                var all = context.Database.GetMigrations().ToList();
                CollectionAssert.Contains(all, Migration, "the migration is registered");
                var previous = all[all.IndexOf(Migration) - 1];
                migrator.Migrate(previous);
                var before = Counts(context);
                var originalPools = Rows(context, "select * from spawnpool order by id").Select(r => string.Join("|", r)).ToHashSet();
                var originalCreatures = Rows(context, "select * from creature order by id").Select(r => string.Join("|", r)).ToHashSet();

                // Not one of the ids is taken before.
                Assert.AreEqual("0", Rows(context, "select count(*) from creature where id between 9300001 and 9399999")[0][0]);
                Assert.AreEqual("0", Rows(context, "select count(*) from spawnpool where id between 9300001 and 9399999")[0][0]);
                Assert.AreEqual("0", Rows(context, "select count(*) from creature_action where id between 9300001 and 9399999")[0][0]);

                migrator.Migrate(Migration);
                var after = Counts(context);

                Assert.AreEqual(before["creature"] + TitleBossesSeed.BossCount, after["creature"]);
                Assert.AreEqual(before["creature_stat"] + TitleBossesSeed.BossCount, after["creature_stat"]);
                Assert.AreEqual(before["spawnpool"] + TitleBossesSeed.BossCount, after["spawnpool"]);
                Assert.IsGreaterThan(before["creature_action"], after["creature_action"], "attack rows of their own for the new bosses");

                foreach (var table in new[] { "teleporter", "npc_package", "vendor", "entityclass", "map_info" })
                    Assert.AreEqual(before[table], after[table], $"{table} is not touched");

                // Every row that was there is still there, as it was.
                var nowPools = Rows(context, "select * from spawnpool order by id").Select(r => string.Join("|", r)).ToHashSet();
                var nowCreatures = Rows(context, "select * from creature order by id").Select(r => string.Join("|", r)).ToHashSet();
                Assert.IsTrue(originalPools.IsSubsetOf(nowPools), "no spawn pool changed or went");
                Assert.IsTrue(originalCreatures.IsSubsetOf(nowCreatures), "no creature changed or went");

                // The new bosses: each is on a map of a BossTitles list that names it by its name id, and its pool holds it.
                var bosses = Rows(context, "select c.id, c.name_id, c.class_id, c.level, c.max_hp, p.map_context_id, p.pos_x, p.pos_z, p.creature_1_Id from creature c join spawnpool p on p.id = c.id where c.id between 9300001 and 9399999 order by c.id");
                Assert.HasCount(TitleBossesSeed.BossCount, bosses);

                foreach (var row in bosses)
                {
                    var nameId = uint.Parse(row[1], CultureInfo.InvariantCulture);
                    var map = uint.Parse(row[5], CultureInfo.InvariantCulture);
                    var zone = BossTitles.ZoneOf(map, nameId);
                    Assert.IsNotNull(zone, $"creature {row[0]} (name {nameId}) counts for a title on map {map}");
                    Assert.AreEqual(row[0], row[8], "the pool holds its boss");
                    Assert.IsGreaterThanOrEqualTo(3000, int.Parse(row[4], CultureInfo.InvariantCulture), "a boss's health");

                    // A retail class (ours start at 21000000), and one the world has.
                    var classId = uint.Parse(row[2], CultureInfo.InvariantCulture);
                    Assert.IsLessThan(21000000u, classId, $"creature {row[0]} is of a retail class");
                    Assert.AreEqual("1", Rows(context, $"select count(*) from entityclass where id = {row[2]}")[0][0], "its class exists");
                    Assert.AreEqual("1", Rows(context, $"select count(*) from map_info where map_context_id = {map}")[0][0], "its map exists");

                    // Every attack it has is a row.
                    foreach (var a in Rows(context, $"select action1, action2, action3, action4, action5, action6, action7, action8 from creature where id = {row[0]}")[0].Where(v => v != "0"))
                        Assert.AreEqual("1", Rows(context, $"select count(*) from creature_action where id = {a}")[0][0], $"creature {row[0]} attack {a}");

                    // Before the migration nothing of this name stood on the title's maps: a boss is not doubled.
                    Assert.AreEqual(1, Rows(context, $"select count(*) from creature c join spawnpool p on p.creature_1_Id = c.id where c.name_id = {nameId} and p.map_context_id in ({string.Join(",", zone.Maps)})")
                        .Select(r => int.Parse(r[0], CultureInfo.InvariantCulture)).Single(), $"creature {row[0]} is the only {nameId} on {zone.Name}'s maps");
                }

                // No title lacks a boss any more: every boss of every list has a creature row in the world.
                foreach (var title in BossTitles.Zones)
                    foreach (var boss in title.Bosses)
                    {
                        var ids = string.Join(",", boss.NameIds);
                        var count = int.Parse(Rows(context, $"select count(*) from creature where name_id in ({ids})")[0][0], CultureInfo.InvariantCulture);
                        Assert.IsGreaterThan(0, count, $"{title.Name}: {boss.Name} has no creature");
                    }

                // Down takes out exactly what Up put in.
                migrator.Migrate(previous);
                var back = Counts(context);

                foreach (var table in Tables)
                    Assert.AreEqual(before[table], back[table], $"{table} after Down");

                CollectionAssert.AreEquivalent(originalPools.ToList(), Rows(context, "select * from spawnpool order by id").Select(r => string.Join("|", r)).ToList());
                CollectionAssert.AreEquivalent(originalCreatures.ToList(), Rows(context, "select * from creature order by id").Select(r => string.Join("|", r)).ToList());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }
    }
}
