using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    /// <summary>The Caves of Donn's Fithik with its kin's lunge and acid spray (Donn_fithik_abilities, DonnFithikAbilities).</summary>
    [TestClass]
    public class DonnFithikAbilitiesTests
    {
        private const string Migration = "20270316000000_Donn_fithik_abilities";
        private const string Previous = "20270315000000_Add_creature_scale";

        [TestMethod]
        public void TheDonnFithikLungesAndSpraysAtItsLevelBesideItsBiteAndSelfDestruct()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            // The bite and the self-destruct it had, then the lunge and the acid spray.
            CollectionAssert.AreEqual(new long[] { 174, 180, 405, 201 }, Actions(database));
            Assert.AreEqual((9L, 25L, 32L), (Level(database), Damage(database, DonnFithikAbilities.Lunge, "min"), Damage(database, DonnFithikAbilities.Lunge, "max")));
            Assert.AreEqual((19L, 32L), (Damage(database, DonnFithikAbilities.AcidSpray, "min"), Damage(database, DonnFithikAbilities.AcidSpray, "max")));

            // As the Wilderness Fithik's rows are, but for the damage: range, reuse, windup and type.
            foreach (var (ours, kin) in new[] { (DonnFithikAbilities.Lunge, 76008L), (DonnFithikAbilities.AcidSpray, 76009L) })
                Assert.AreEqual(Shape(database, kin), Shape(database, ours), $"row {ours}");
        }

        [TestMethod]
        public void DownTakesThemAwayAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new long[] { 174, 180 }, Actions(database), "the bite and the self-destruct, as Caves_of_donn made it");

            migrator.Migrate(Migration);
            foreach (var sql in DonnFithikAbilities.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(2L, Query(database, "SELECT COUNT(*) AS Value FROM creature_action WHERE id IN (566103, 566104)").Single(), "once");
            CollectionAssert.AreEqual(new long[] { 174, 180, 405, 201 }, Actions(database));

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new long[] { 174, 180 }, Actions(database));
            Assert.AreEqual((0L, 0L), (Slot(database, 3), Slot(database, 4)));
            Assert.AreEqual(0L, Query(database, "SELECT COUNT(*) AS Value FROM creature_action WHERE id IN (566103, 566104)").Single());
        }

        [TestMethod]
        public void AWorldWithTheOriginalTimestampKeepsItsAbilitiesWithoutStaleHistory()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            foreach (var sql in DonnFithikAbilities.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{DonnFithikAbilities.OldId}', '9.0.20')");

            migrator.Migrate();

            CollectionAssert.AreEqual(new long[] { 174, 180, 405, 201 }, Actions(database));
            Assert.AreEqual(2L, Query(database,
                "SELECT COUNT(*) AS Value FROM creature_action WHERE id IN (566103, 566104)").Single());
            var applied = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(applied, Migration);
            CollectionAssert.DoesNotContain(applied, DonnFithikAbilities.OldId);
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            StringAssert.Contains(up, "SELECT 566103, 'Fithik - Caves of Donn 566017 fithik_lunge', 405, 1, 1.0, 30.0, 15000, 1200, 25, 32, 1 FROM creature c WHERE c.id = 566017");
            StringAssert.Contains(up, "UPDATE creature SET action4 = 566104 WHERE id = 566017 AND action4 = 0");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(up, DonnFithikAbilities.ForgetOldId);
            StringAssert.Contains(down, "UPDATE creature SET action3 = 0 WHERE id = 566017 AND action3 = 566103;");
            StringAssert.Contains(down, "DELETE FROM creature_action WHERE id = 566104;");
        }

        private static long[] Actions(OrdinaryLootTestDatabase database) =>
            Enumerable.Range(1, 8).Select(index => Slot(database, index)).Where(id => id != 0)
                .Select(id => Query(database, $"SELECT action_id AS Value FROM creature_action WHERE id = {id}").Single()).ToArray();

        private static long Slot(OrdinaryLootTestDatabase database, int index) =>
            Query(database, $"SELECT action{index} AS Value FROM creature WHERE id = {DonnFithikAbilities.Fithik}").Single();

        private static long Level(OrdinaryLootTestDatabase database) =>
            Query(database, $"SELECT level AS Value FROM creature WHERE id = {DonnFithikAbilities.Fithik}").Single();

        private static long Damage(OrdinaryLootTestDatabase database, uint row, string end) =>
            Query(database, $"SELECT {end}_damage AS Value FROM creature_action WHERE id = {row}").Single();

        private static string Shape(OrdinaryLootTestDatabase database, long row) =>
            database.Context.Database.SqlQueryRaw<string>(
                "SELECT action_id || '/' || action_arg_id || ' ' || range_min || '-' || range_max || ' ' || cooldown || ' ' || windup || ' ' || damage_type AS Value " +
                $"FROM creature_action WHERE id = {row}").AsEnumerable().Single();

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
