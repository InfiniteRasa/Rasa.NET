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

    // The NPCs whose death fails the mission they are escorted in - Matthew Corman (697), Sgt.
    // Pierre (666), Ranger Milpas (1390) and the Alia escort ranger (1407) - had 100 to 1000 health
    // (Escort_npc_stats).
    [TestClass]
    [DoNotParallelize]
    public class EscortNpcStatsMigrationTests
    {
        private const string Migration = "20261228000000_Escort_npc_stats";
        private const string Previous = "20261227000000_Anjuhi_stats";

        private static readonly (long, long, long, long, long) Raised =
            (EscortNpcStats.Attribute, EscortNpcStats.Attribute, EscortNpcStats.Attribute, EscortNpcStats.Health, EscortNpcStats.Armor);

        [TestMethod]
        public void EveryEscortedNpcHasTheHealthAndArmour()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            foreach (var creature in EscortNpcStats.Creatures)
                Assert.AreEqual(Raised, Stats(database, creature), $"creature {creature}");
        }

        [TestMethod]
        public void TheDownPutsBackWhatTheyWereSeededWith()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Count(database, EscortNpcStats.Matthew), "Matthew had no row");
            foreach (var (creature, health) in EscortNpcStats.Raised)
                Assert.AreEqual((15L, 15L, 15L, (long)health, 100L), Stats(database, creature), $"creature {creature} as seeded");

            migrator.Migrate(Migration);
            migrator.Migrate(Previous);

            Assert.AreEqual(0, Count(database, EscortNpcStats.Matthew));
            foreach (var (creature, health) in EscortNpcStats.Raised)
                Assert.AreEqual((15L, 15L, 15L, (long)health, 100L), Stats(database, creature), $"creature {creature} back");
        }

        [TestMethod]
        public void ARowSetByHandIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE creature_stat SET health = 9000, armor = 400 WHERE id = {EscortNpcStats.Milpas}");
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) VALUES ({EscortNpcStats.Matthew}, 20, 20, 20, 7000, 350)");

            migrator.Migrate(Migration);
            Assert.AreEqual((15L, 15L, 15L, 9000L, 400L), Stats(database, EscortNpcStats.Milpas));
            Assert.AreEqual((20L, 20L, 20L, 7000L, 350L), Stats(database, EscortNpcStats.Matthew));
            Assert.AreEqual(Raised, Stats(database, EscortNpcStats.Pierre), "the others raised all the same");

            migrator.Migrate(Previous);
            Assert.AreEqual((15L, 15L, 15L, 9000L, 400L), Stats(database, EscortNpcStats.Milpas));
            Assert.AreEqual((20L, 20L, 20L, 7000L, 350L), Stats(database, EscortNpcStats.Matthew));
        }

        [TestMethod]
        [DataRow(201U, EscortNpcStats.Matthew)]
        [DataRow(630070U, EscortNpcStats.Pierre)]
        [DataRow(630010U, EscortNpcStats.Milpas)]
        [DataRow(630001U, EscortNpcStats.Ranger)]
        public void TheySpawnWithThatHealthAndArmour(uint spawnId, uint creatureId)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(spawnId);
            var npc = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == spawnId && creature.DbId == creatureId);

            Assert.AreEqual((EscortNpcStats.Health, EscortNpcStats.Health),
                (npc.Attributes[Attributes.Health].CurrentMax, npc.Attributes[Attributes.Health].Current));
            Assert.AreEqual(EscortNpcStats.Armor, npc.Attributes[Attributes.Armor].CurrentMax);
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in EscortNpcStats.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in EscortNpcStats.Down())
                StringAssert.Contains(down, sql);
        }

        private static int Count(OrdinaryLootTestDatabase database, uint creature) =>
            database.Context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id = {creature}").AsEnumerable().Single();

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
