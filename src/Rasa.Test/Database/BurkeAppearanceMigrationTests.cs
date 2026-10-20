using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    using Rasa.Test.Missions.Wilderness;

    // Lieutenant Burke (96), the NPC "Orders From High Command" (701) sends the player to, stood with
    // no head: his swapset body had no creature_appearance rows (Burke_appearance).
    [TestClass]
    [DoNotParallelize]
    public class BurkeAppearanceMigrationTests
    {
        private const string Migration = "20261226000000_Burke_appearance";
        private const string Previous = "20261225000000_Arieki_vent_eruptions";
        private const uint LtWood = 121;

        [TestMethod]
        public void BurkeWearsWhatTheOtherLieutenantsOnHisBodyWear()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var burke = Rows(database, BurkeAppearance.Burke);
            Assert.HasCount(6, burke);
            CollectionAssert.AreEqual(Rows(database, LtWood), burke);
        }

        [TestMethod]
        public void EveryPlacedNpcOnASwapsetBodyHasALook()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            // The bare bodies that take their face, hair and clothes from creature_appearance.
            var bare = database.Context.Database.SqlQueryRaw<long>(
                "SELECT c.id AS Value FROM creature c JOIN entityclass e ON e.id = c.class_id " +
                "WHERE e.class_name LIKE '%Swapset%' " +
                "AND (EXISTS (SELECT 1 FROM spawnpool p WHERE c.id IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id)) " +
                "     OR EXISTS (SELECT 1 FROM mission_spawn m WHERE m.creature_id = c.id)) " +
                "AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id)").ToList();

            Assert.IsEmpty(bare, string.Join(", ", bare));
        }

        [TestMethod]
        public void TheUpKeepsALookPutOnByHandAndTheDownTakesOnlyItsOwnRows()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.IsEmpty(Rows(database, BurkeAppearance.Burke));

            // A face given him by hand (.creatureappearance) before the migration ran.
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({BurkeAppearance.Burke}, 17, 24019, 4286886614)");

            migrator.Migrate(Migration);
            var rows = Rows(database, BurkeAppearance.Burke);
            Assert.HasCount(6, rows);
            CollectionAssert.Contains(rows, (17L, 24019L, 4286886614L), "his own face kept");
            Assert.AreEqual(1, rows.Count(row => row.Slot == 17));

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new[] { (17L, 24019L, 4286886614L) }, Rows(database, BurkeAppearance.Burke), "only the migration's rows go");
        }

        [TestMethod]
        public void TheServerLoadsHimWithHisHeadAndClothes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var burke = harness.Creatures.LoadedCreatures[BurkeAppearance.Burke];

            CollectionAssert.AreEquivalent(
                BurkeAppearance.Rows.Select(row => (uint)row.ClassId).ToList(),
                burke.AppearanceData.Values.Select(appearance => appearance.Class).ToList());
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in BurkeAppearance.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in BurkeAppearance.Down())
                StringAssert.Contains(down, sql);
        }

        private static (long Slot, long ClassId, long Color)[] Rows(OrdinaryLootTestDatabase database, uint creature) =>
            database.Context.Database.SqlQueryRaw<AppearanceRow>(
                    $"SELECT slot_id AS Slot, Class_id AS ClassId, color AS Color FROM creature_appearance WHERE id = {creature} ORDER BY slot_id")
                .AsEnumerable().Select(row => (row.Slot, row.ClassId, row.Color)).ToArray();

        private sealed class AppearanceRow
        {
            public long Slot { get; set; }
            public long ClassId { get; set; }
            public long Color { get; set; }
        }
    }
}
