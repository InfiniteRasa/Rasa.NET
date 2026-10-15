using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Herbal Remedy's Three Devils stand together on the Lake Tinctuan landing pad, inside the
    /// objective's indicator: Archfiend and Old Scratch had been sent to lead Lower Eloh Creek
    /// camps, where they stood in walls (Three_devils).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ThreeDevilsTests
    {
        private const string Migration = "20261229000000_Three_devils";
        private const string Previous = "20261228000000_Escort_npc_stats";

        /// <summary>Herbal Remedy's objective 5 indicator: (615, 284.2578, 737), 35 m.</summary>
        private static readonly Vector3 Indicator = new Vector3(615f, 284.2578f, 737f);
        private const float IndicatorRadius = 35f;

        [TestMethod]
        public void AllThreeStandOnThePadInsideTheIndicator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(ThreeDevils.Devils.Select(d => d.Pool).Append(ThreeDevils.HorntailPool).ToArray());

            var devils = new[] { (75u, 580009u), (80u, 580013u), (79u, ThreeDevils.HorntailPool) }
                .Select(entry => harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                    .Single(creature => creature.SpawnPool?.DbId == entry.Item2 && creature.DbId == entry.Item1))
                .ToList();
            var nav = harness.Map.NavMesh;
            var horntail = nav.Nearest(devils[2].Position).Value;

            foreach (var devil in devils)
            {
                var flat = new Vector2(devil.Position.X - Indicator.X, devil.Position.Z - Indicator.Z).Length();
                Assert.IsTrue(flat <= IndicatorRadius, $"{devil.Name} {flat:F1} m from the indicator");
                Assert.IsTrue(nav.IsOnMesh(devil.Position), $"{devil.Name} on the navmesh at {devil.Position}");

                var path = nav.FindPath(nav.Nearest(devil.Position).Value, horntail, out var complete);
                Assert.IsTrue(complete && path.Count > 0, $"{devil.Name} can reach Horntail");
            }

            Assert.IsTrue(Vector3.Distance(devils[0].Position, devils[1].Position) >= 10f, "not on top of each other");
        }

        [TestMethod]
        public void DownSendsThemBackAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            foreach (var devil in ThreeDevils.Devils)
                Assert.AreEqual((System.Math.Round((double)devil.FromX, 2), System.Math.Round((double)devil.FromZ, 2)), Position(database, devil.Pool), devil.Name);

            // One a GM has put somewhere else is left there.
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x = 10 WHERE id = 580013");

            migrator.Migrate(Migration);
            Assert.AreEqual((605.0, 731.0), Position(database, 580009));
            Assert.AreEqual((10.0, 164.0), Position(database, 580013));

            foreach (var sql in ThreeDevils.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual((605.0, 731.0), Position(database, 580009), "once");

            migrator.Migrate(Previous);
            Assert.AreEqual((290.0, 269.4), Position(database, 580009));
            Assert.AreEqual((10.0, 164.0), Position(database, 580013));
            Assert.AreEqual((615.0, 737.0), Position(database, ThreeDevils.HorntailPool), "Horntail stays");

            // To the bit, as the preloader wrote it: single-precision floats.
            Assert.AreEqual((double)234.3709f, database.Context.Database.SqlQueryRaw<double>("SELECT pos_y AS Value FROM spawnpool WHERE id = 580009").Single());
        }

        private static (double, double) Position(OrdinaryLootTestDatabase database, uint pool)
        {
            var x = database.Context.Database.SqlQueryRaw<double>($"SELECT pos_x AS Value FROM spawnpool WHERE id = {pool}").Single();
            var z = database.Context.Database.SqlQueryRaw<double>($"SELECT pos_z AS Value FROM spawnpool WHERE id = {pool}").Single();
            return (System.Math.Round(x, 2), System.Math.Round(z, 2));
        }
    }
}
