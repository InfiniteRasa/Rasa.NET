using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Data;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// The reconstructed Wilderness hostile pools respawn in tenths of a second as every other pool
    /// does, not in two (Wilderness_respawn_in_tenths, WildernessRespawns).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessRespawnTests
    {
        private const string Migration = "20270129000000_Wilderness_respawn_in_tenths";
        private const string Previous = "20270128000000_Tent_floor_npcs";

        /// <summary>The friendly reconstructed pools - escort ranger, Milpas, the Treeback herd, Pierre - which stay at 20.</summary>
        private static readonly uint[] Friendly = { 630001, 630010, 630043, 630044, 630045, 630070 };

        [TestMethod]
        public void EveryHostilePoolTakesItsTimeAndNothingElseChanges()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // Every reconstructed pool on the map was written 20; the hostile ones are this list.
            var hostileAtTwenty = Strings(database,
                "SELECT CAST(s.id AS TEXT) AS Value FROM spawnpool s JOIN creature c ON c.id = s.creature_1_Id " +
                "WHERE s.id BETWEEN 630000 AND 639999 AND s.respown_time = 20 AND c.faction = 0 ORDER BY s.id");
            CollectionAssert.AreEquivalent(WildernessRespawns.Pools.Select(p => p.Pool.ToString()).ToList(), hostileAtTwenty);
            foreach (var pool in Friendly)
                Assert.AreEqual(20u, Respawn(database, pool), $"{pool} before");

            // A GM set one egg-layer to 45 s by hand: it stays.
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET respown_time = 450 WHERE id = 630042");
            var before = Snapshot(database);

            migrator.Migrate(Migration);

            foreach (var (pool, respawn, what) in WildernessRespawns.Pools)
                Assert.AreEqual(pool == 630042 ? 450u : respawn, Respawn(database, pool), $"{pool} {what}");

            Assert.AreEqual(200u, Respawn(database, 630040), "an egg-layer: 20 s");
            Assert.AreEqual(3000u, Respawn(database, 630130), "Skeev: 300 s");
            Assert.AreEqual(BaneMortars.Respawn, Respawn(database, 630076), "a mortar as every other Bane mortar");

            foreach (var pool in Friendly)
                Assert.AreEqual(20u, Respawn(database, pool), $"{pool} after");

            var changed = WildernessRespawns.Pools.Select(p => $"{p.Pool} ").ToArray();
            CollectionAssert.AreEqual(before.Where(r => !changed.Any(r.StartsWith)).ToList(),
                Snapshot(database).Where(r => !changed.Any(r.StartsWith)).ToList(), "every other pool as it was");

            // Twice is once.
            var once = Snapshot(database);
            foreach (var sql in WildernessRespawns.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(once, Snapshot(database));

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back 20, and leaves the GM's 45 s");
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in WildernessRespawns.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in WildernessRespawns.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void AKilledEggLayerIsBackInTwentySecondsAndNotInTwo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(630040);

            var pool = harness.Map.SpawnPools.Single(candidate => candidate.DbId == 630040);
            Assert.AreEqual(20_000L, pool.RespawnTime);

            var eggLayer = Living(harness).Single();
            harness.MoveTo(eggLayer.Position);
            eggLayer.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(harness.Map, eggLayer, harness.Client.Player);
            Assert.IsEmpty(Living(harness));

            harness.SpawnWorldAfter(2_000, 630040);
            Assert.IsEmpty(Living(harness), "not after two seconds");

            harness.SpawnWorldAfter(17_000, 630040);
            Assert.IsEmpty(Living(harness), "nor after 19");

            harness.SpawnWorldAfter(1_000, 630040);
            Assert.HasCount(1, Living(harness), "back at 20");
        }

        private static List<Creature> Living(WildernessRuntimeTestHarness harness) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.SpawnPool?.DbId == 630040 && creature.State != CharacterState.Dead)
                .ToList();

        private static uint Respawn(OrdinaryLootTestDatabase database, uint pool) =>
            uint.Parse(Strings(database, $"SELECT CAST(respown_time AS TEXT) AS Value FROM spawnpool WHERE id = {pool}").Single());

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Strings(database, "SELECT id || ' ' || respown_time || ' ' || mode || ' ' || creature_1_Id AS Value FROM spawnpool ORDER BY id");

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();
    }
}
