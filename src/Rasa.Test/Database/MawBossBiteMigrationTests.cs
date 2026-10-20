using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // Seymour, the Maw boss, bites with CR_MAW_MELEE for a boss's damage (Maw_boss_bite), not with a
    // Maw weapon swing for 5-12.
    [TestClass]
    public class MawBossBiteMigrationTests
    {
        private const string Migration = "20261220000000_Maw_boss_bite";
        private const string Previous = "20261219000000_Hunter_shields";

        [TestMethod]
        public void SeymourBitesAsAMawForMoreThanHisKin()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            Assert.AreEqual((long)MawBossBite.Row, Action1(database));
            foreach (var (column, value) in new[] { ("action_id", 422L), ("action_arg_id", 1L), ("min_damage", 691L), ("max_damage", 1394L), ("damage_type", 1L) })
                Assert.AreEqual(value, Query(database, $"SELECT {column} AS Value FROM creature_action WHERE id = {MawBossBite.Row}").Single(), column);

            // Twice a Maw's at his level: a level 34 Maw bites for 244-493.
            var kin = Query(database, "SELECT a.max_damage AS Value FROM creature c JOIN creature_action a ON a.id = c.action1 " +
                "WHERE c.class_id = 6332 AND c.level = 34");
            Assert.IsTrue(kin.Length > 0 && kin.All(max => max < MawBossBite.MinDamage));
        }

        [TestMethod]
        public void DownPutsTheSwingBackAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual((long)MawBossBite.OldRow, Action1(database));

            migrator.Migrate(Migration);
            Assert.AreEqual((long)MawBossBite.Row, Action1(database));

            foreach (var sql in MawBossBite.Up)
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(1, Query(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id = {MawBossBite.Row}").Single(), "once");

            migrator.Migrate(Previous);
            Assert.AreEqual((long)MawBossBite.OldRow, Action1(database));
            Assert.AreEqual(0, Query(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id = {MawBossBite.Row}").Single());
            Assert.AreEqual(1, Query(database, $"SELECT COUNT(*) AS Value FROM creature_action WHERE id = {MawBossBite.OldRow}").Single(), "the swing's row is kept");
        }

        [TestMethod]
        public void ASlotChangedByHandIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature SET action1 = 55019 WHERE id = {MawBossBite.Seymour}");

            migrator.Migrate(Migration);

            Assert.AreEqual(55019L, Action1(database));
        }

        private static long Action1(OrdinaryLootTestDatabase database) =>
            Query(database, $"SELECT action1 AS Value FROM creature WHERE id = {MawBossBite.Seymour}").Single();

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
