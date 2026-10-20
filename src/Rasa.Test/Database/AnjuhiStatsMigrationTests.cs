using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    using Rasa.Data;
    using Rasa.Test.Missions.Wilderness;

    // Ranger Anjuhi (98), whom three Xanx attack in "Childhood's End" (682), had no creature_stat
    // row and spawned with 100 health: the Xanx killed him in seconds (Anjuhi_stats).
    [TestClass]
    [DoNotParallelize]
    public class AnjuhiStatsMigrationTests
    {
        private const string Migration = "20261227000000_Anjuhi_stats";
        private const string Previous = "20261226000000_Burke_appearance";
        private const uint Xanx = 87;

        [TestMethod]
        public void AnjuhiOutlastsTheThreeXanxHealthAndArmour()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var anjuhi = Stats(database, AnjuhiStats.Anjuhi);
            Assert.AreEqual((15L, 15L, 15L, (long)AnjuhiStats.Health, (long)AnjuhiStats.Armor), anjuhi);

            var xanx = Stats(database, Xanx);
            Assert.IsTrue(anjuhi.Health > 3 * xanx.Health, "more health than the three Xanx together");
        }

        [TestMethod]
        public void TheUpKeepsARowPutInByHandAndTheDownTakesOnlyItsOwn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Count(database));

            migrator.Migrate(Migration);
            Assert.AreEqual(1, Count(database));
            migrator.Migrate(Previous);
            Assert.AreEqual(0, Count(database), "Down takes its row away");

            // A row given him by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) VALUES ({AnjuhiStats.Anjuhi}, 20, 20, 20, 9000, 400)");

            migrator.Migrate(Migration);
            Assert.AreEqual((20L, 20L, 20L, 9000L, 400L), Stats(database, AnjuhiStats.Anjuhi), "his own row kept");

            migrator.Migrate(Previous);
            Assert.AreEqual((20L, 20L, 20L, 9000L, 400L), Stats(database, AnjuhiStats.Anjuhi), "and not taken by the Down");
        }

        [TestMethod]
        public void HeSpawnsWithThatHealthAndArmour()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(176);
            var anjuhi = harness.Npc(176);

            Assert.IsNotNull(anjuhi);
            Assert.AreEqual(AnjuhiStats.Anjuhi, anjuhi.DbId);
            Assert.AreEqual((AnjuhiStats.Health, AnjuhiStats.Health),
                (anjuhi.Attributes[Attributes.Health].CurrentMax, anjuhi.Attributes[Attributes.Health].Current));
            Assert.AreEqual(AnjuhiStats.Armor, anjuhi.Attributes[Attributes.Armor].CurrentMax);
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in AnjuhiStats.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in AnjuhiStats.Down())
                StringAssert.Contains(down, sql);
        }

        private static int Count(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id = {AnjuhiStats.Anjuhi}").AsEnumerable().Single();

        private static (long Body, long Mind, long Spirit, long Health, long Armor) Stats(OrdinaryLootTestDatabase database, uint creature) =>
            database.Context.Database.SqlQueryRaw<StatRow>(
                    $"SELECT body AS Body, mind AS Mind, spirit AS Spirit, health AS Health, armor AS Armor FROM creature_stat WHERE id = {creature}")
                .AsEnumerable().Select(row => (row.Body, row.Mind, row.Spirit, row.Health, row.Armor)).Single();

        private sealed class StatRow
        {
            public long Body { get; set; }
            public long Mind { get; set; }
            public long Spirit { get; set; }
            public long Health { get; set; }
            public long Armor { get; set; }
        }
    }
}
