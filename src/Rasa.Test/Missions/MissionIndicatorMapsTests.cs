using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures;

namespace Rasa.Test.Missions
{
    using Rasa.Test.Database;

    /// <summary>
    /// The map of each mission indicator (Mission_indicator_maps, MissionIndicatorMapsV1): the column, its data, and
    /// that a player is sent only the indicators of the map it is on (MissionProtocolAdapter.OnPlayersMap).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionIndicatorMapsTests
    {
        private const string Before = "20271006000000_Pools_turpis_refinery";
        private const string Migration = "20271030000400_Mission_indicator_maps";

        [TestMethod]
        public void EveryIndicatorGetsItsMapAndTheDownDropsTheColumn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Migration);

            var rows = database.Context.Database.SqlQueryRaw<Row>(
                "SELECT mission_id AS Mission, content_revision AS Revision, objective_id AS Objective, indicator_id AS Indicator, " +
                "map_context_id AS Map FROM mission_indicator").AsEnumerable().ToList();
            Assert.IsNotEmpty(rows);
            foreach (var row in rows)
                Assert.AreEqual(MissionIndicatorMapsV1.MapOf(row.Mission, row.Revision, row.Objective, row.Indicator) ?? 0u, row.Map,
                    $"{row.Mission} {row.Revision} {row.Objective}/{row.Indicator}");

            // The player reports: Ancient Mysteries' bosses in Velon Hollow, its way in on the Plateau; Ustor Yard's own.
            Assert.IsTrue(rows.Where(row => row.Mission == 1978 && row.Objective == 1).All(row => row.Map == 2136));
            Assert.IsTrue(rows.Where(row => row.Mission == 1978 && row.Objective == 2).All(row => row.Map == 1497));
            Assert.IsTrue(rows.Where(row => row.Mission is >= 1187 and <= 1190).All(row => row.Map == 1502));
            Assert.IsTrue(rows.Where(row => row.Mission == 1057).All(row => row.Map == 1465), "Distance in the Lamnas Armory");
            Assert.AreEqual(1349u, rows.Single(row => row.Mission == 594 && row.Objective == 9).Map, "the four bunkers' area, in Torcastra Prison");
            Assert.IsTrue(rows.All(row => row.Map != 0), "every indicator of the world DB has its map");
            Assert.AreEqual(2029u, rows.Single(row => row.Mission == 935 && row.Indicator == 1_000_000_002).Map, "Veryin, in the Temporal Chamber");
            Assert.AreEqual(1497u, rows.Single(row => row.Mission == 935 && row.Indicator == 1_000_000_001).Map, "the conduits round the portal");
            foreach (var (mission, objective) in new[] { (381u, 2u), (670u, 2u), (671u, 7u) })
                Assert.AreEqual(1244u, rows.Single(row => row.Mission == mission && row.Objective == objective).Map,
                    $"{mission}: back at the giver on the Palisades, not in Devils Den");

            foreach (var sql in MissionIndicatorMapsV1.Up())
                database.Context.Database.ExecuteSqlRaw(sql);   // twice: the same

            migrator.Migrate(Before);
            Assert.AreEqual(0L, database.Context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM pragma_table_info('mission_indicator') WHERE name = 'map_context_id'").AsEnumerable().Single());
        }

        [TestMethod]
        public void OnMySqlTheColumnGoesInWithItsDataAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "map_context_id");
            foreach (var sql in MissionIndicatorMapsV1.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(down, "map_context_id");
        }

        [TestMethod]
        public void AnIndicatorIsOnItsMapOrOnEveryMap()
        {
            Assert.IsTrue(new MissionIndicator { MapContextId = 0 }.IsOn(1497), "0: every map");
            Assert.IsTrue(new MissionIndicator { MapContextId = 2136 }.IsOn(2136));
            Assert.IsFalse(new MissionIndicator { MapContextId = 2136 }.IsOn(1497));
        }

        private sealed class Row
        {
            public uint Mission { get; set; }
            public string Revision { get; set; }
            public uint Objective { get; set; }
            public uint Indicator { get; set; }
            public uint Map { get; set; }
        }
    }
}
