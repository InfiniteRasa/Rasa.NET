using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // Spawned creatures have their family's CR_* abilities (Family_abilities).
    [TestClass]
    public class FamilyAbilitiesMigrationTests
    {
        private const string Migration = "20261221000000_Family_abilities";
        private const string Previous = "20261220000000_Maw_boss_bite";

        private const string Spawned =
            "SELECT creature_1_Id FROM spawnpool UNION SELECT creature_2_Id FROM spawnpool UNION SELECT creature_3_Id FROM spawnpool " +
            "UNION SELECT creature_4_Id FROM spawnpool UNION SELECT creature_5_Id FROM spawnpool UNION SELECT creature_6_Id FROM spawnpool " +
            "UNION SELECT creature_id FROM mission_spawn";

        // (classes, CR_* action every spawned one of them has)
        [TestMethod]
        [DataRow("6031, 10319, 10606", 182, DisplayName = "Boargar: stun")]
        [DataRow("6031, 10319, 10606", 235, DisplayName = "Boargar: knockback")]
        [DataRow("4313, 10323", 405, DisplayName = "Winged Fithik: lunge")]
        [DataRow("4313, 10323", 201, DisplayName = "Winged Fithik: acid spray")]
        [DataRow("6032, 10310", 431, DisplayName = "Amoeboid V1: melee")]
        [DataRow("6032, 10310", 274, DisplayName = "Amoeboid V1: vomit")]
        [DataRow("6170", 208, DisplayName = "Miasma: gas cloud")]
        [DataRow("6441, 10317", 248, DisplayName = "Beam Manta: lightning")]
        [DataRow("7233, 24084", 245, DisplayName = "Shield Drone: heal")]
        [DataRow("9244, 10321", 395, DisplayName = "Caretaker: attack")]
        [DataRow("9751, 10335", 410, DisplayName = "Linker: channel")]
        [DataRow("6038", 190, DisplayName = "Treeback: stomp")]
        [DataRow("6038", 191, DisplayName = "Treeback: swarm")]
        [DataRow("6038", 192, DisplayName = "Treeback: howl")]
        [DataRow("3868, 6236, 21474", 429, DisplayName = "Machina: revive")]
        public void EverySpawnedCreatureOfTheFamilyHasIt(string classes, int actionId)
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var without = Query(database,
                $"SELECT c.id AS Value FROM creature c WHERE c.class_id IN ({classes}) AND c.id IN ({Spawned}) " +
                "AND NOT EXISTS (SELECT 1 FROM creature_action a WHERE a.action_id = " + actionId +
                " AND a.id IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8)) ORDER BY c.id");

            Assert.AreEqual(0, without.Length, string.Join(", ", without));
        }

        [TestMethod]
        public void TheThraxBossesHaveTheirKits()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            foreach (var overseer in new long[] { 80, 82, 83, 84, 90 })
                CollectionAssert.IsSubsetOf(new long[] { 278, 449, 451, 454, 455 }, Actions(database, overseer), $"creature {overseer}");

            CollectionAssert.IsSubsetOf(new long[] { 457, 458, 488 }, Actions(database, 75));
            CollectionAssert.Contains(Actions(database, 3), 397L);
            CollectionAssert.Contains(Actions(database, 530005), 397L);
        }

        [TestMethod]
        public void EveryRowIsWhereItWasPutAndDamageFollowsTheLevel()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            foreach (var slot in FamilyAbilities.Slots)
                Assert.AreEqual((long)slot.Row, Slot(database, slot.Creature, slot.Index), $"creature {slot.Creature} slot {slot.Index}");
            foreach (var swap in FamilyAbilities.Swaps)
                Assert.AreEqual((long)swap.Row, Slot(database, swap.Creature, swap.Index), $"creature {swap.Creature} slot {swap.Index}");

            Assert.AreEqual(FamilyAbilities.Rows.Length, (int)(FamilyAbilities.IdMax - FamilyAbilities.IdMin + 1));

            // Goriam (level 35) bites at 2_BOSS, 100-125 x 2^(34/8) x 0.25.
            Assert.AreEqual(476L, Query(database, "SELECT a.min_damage AS Value FROM creature c JOIN creature_action a ON a.id = c.action1 WHERE c.id = 520056").Single());
            Assert.AreEqual(595L, Query(database, "SELECT a.max_damage AS Value FROM creature c JOIN creature_action a ON a.id = c.action1 WHERE c.id = 520056").Single());
        }

        [TestMethod]
        public void DownPutsEveryCreatureBackAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);
            database.Context.Database.ExecuteSqlRaw("UPDATE creature SET action2 = 3782 WHERE id = 44");

            migrator.Migrate(Migration);
            Assert.AreEqual(3782L, Slot(database, 44, 2), "a slot taken by hand is left");
            Assert.AreEqual(76002L, Slot(database, 44, 3));

            foreach (var sql in FamilyAbilities.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(FamilyAbilities.Rows.Length, (int)Query(database,
                $"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {FamilyAbilities.IdMin} AND {FamilyAbilities.IdMax}").Single(), "once");

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE creature SET action2 = 0 WHERE id = 44");

            CollectionAssert.AreEqual(before, Snapshot(database));
            Assert.AreEqual(0L, Query(database,
                $"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {FamilyAbilities.IdMin} AND {FamilyAbilities.IdMax}").Single());
        }

        private static long[] Snapshot(OrdinaryLootTestDatabase database) =>
            FamilyAbilities.Slots.Select(s => s.Creature).Concat(FamilyAbilities.Swaps.Select(s => s.Creature)).Distinct().OrderBy(id => id)
                .SelectMany(id => Enumerable.Range(1, 8).Select(index => Slot(database, id, index))).ToArray();

        private static long Slot(OrdinaryLootTestDatabase database, uint creature, int index) =>
            Query(database, $"SELECT action{index} AS Value FROM creature WHERE id = {creature}").Single();

        private static long[] Actions(OrdinaryLootTestDatabase database, long creature) =>
            Query(database, "SELECT a.action_id AS Value FROM creature c JOIN creature_action a ON a.id IN " +
                $"(c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8) WHERE c.id = {creature}");

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
