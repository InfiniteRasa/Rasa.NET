using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>Eight logos shrines put back over their pedestals as the rest stand (Logos_shrine_heights, LogosShrineHeights).</summary>
    [TestClass]
    [DoNotParallelize]
    public class LogosShrineHeightsTests
    {
        private const string Migration = "20270213000000_Logos_shrine_heights";
        private const string Previous = "20270205000000_Twin_pillars_npcs";

        /// <summary>Each shrine's pedestal (ArchElohLogosDispenserBase), from its map's .map.</summary>
        private static readonly (uint Id, string Map, double X, double Y, double Z)[] Pedestals =
        {
            (321, "adv_foreas_concordia_divide_minoscaverns", 26.242589950561523, -33.22844314575195, -51.14646911621094),
            (58, "adv_foreas_concordia_wilderness_guardianprom", 351.74462890625, 333.0, 256.0083923339844),
            (54, "adv_foreas_concordia_divide", 140.21092224121094, 141.07913208007812, 411.9412536621094),
            (68, "adv_foreas_concordia_divide", 3.345346689224243, 115.33421325683594, 771.6075439453125),
            (373, "adv_arieki_torden_incline", -577.8339233398438, 242.20330810546875, -471.6412048339844),
            (122, "adv_arieki_torden_plains", -111.5, 374.283935546875, -655.75),
            (72, "adv_foreas_valverde_marshes", 557.5862426757812, 216.23883056640625, -190.31556701660156),
            (125, "adv_arieki_torden_incline", 1180.0, 296.0, 384.0),
        };

        [TestMethod]
        public void EachShrineHangsOverTheCentreOfItsPedestalAsTheRestDo()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rows(database);
            foreach (var s in LogosShrineHeights.Shrines)
            {
                var row = before[s.Id];
                Assert.AreEqual(s.WasY, row.PosY, 0.001, $"{s.Name} where it was");
                Assert.AreEqual(s.MapContextId, row.MapContextId, s.Name);
            }

            migrator.Migrate(Migration);
            var after = Rows(database);
            foreach (var pedestal in Pedestals)
            {
                var row = after[pedestal.Id];
                Assert.IsLessThan(0.01, Math.Sqrt(Math.Pow(row.PosX - pedestal.X, 2) + Math.Pow(row.PosZ - pedestal.Z, 2)), $"{row.Name} over its pedestal's centre");
                Assert.AreEqual(LogosShrineHeights.OverPedestal, row.PosY - pedestal.Y, 0.01, $"{row.Name} as high over it as the rest");
            }

            // Nothing else moved.
            foreach (var (id, row) in after.Where(entry => LogosShrineHeights.Shrines.All(s => s.Id != entry.Key)))
                Assert.AreEqual((before[id].PosX, before[id].PosY, before[id].PosZ), (row.PosX, row.PosY, row.PosZ), $"{row.Name}");

            migrator.Migrate(Previous);
            var back = Rows(database);
            foreach (var (id, row) in before)
                Assert.AreEqual((row.PosX, row.PosY, row.PosZ), (back[id].PosX, back[id].PosY, back[id].PosZ), $"Down puts {row.Name} back exactly");
        }

        /// <summary>
        /// The tablet hangs 1.75 to 2.3 m over the walkable top of its pedestal, as the shrines from
        /// retail do (1.85 to 2.46 m); before, Those was 2 m under it and Death 4.3 m over it.
        /// </summary>
        [TestMethod]
        public void EachShrineHangsOverItsPedestalsWalkableTop()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var rows = Rows(database);
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");

            foreach (var group in Pedestals.GroupBy(pedestal => pedestal.Map))
            {
                var query = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, group.Key));

                foreach (var pedestal in group)
                {
                    var row = rows[pedestal.Id];
                    var floor = query.Nearest(new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ));

                    Assert.IsTrue(floor.HasValue, $"{row.Name} off the navmesh");
                    Assert.IsTrue(row.PosY - floor.Value.Y is >= 1.5 and <= 2.6, $"{row.Name}: {row.PosY - floor.Value.Y} m over the floor");
                }
            }
        }

        [TestMethod]
        public void AShrineAGmHasMovedIsLeftWhereTheyPutIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE logos SET pos_y = -32.0 WHERE id = 321");

            migrator.Migrate(Migration);
            Assert.AreEqual(-32.0, Rows(database)[321].PosY, "Those where the GM put it");
            Assert.AreEqual(298.74, Rows(database)[125].PosY, 0.001, "the rest moved");

            migrator.Migrate(Previous);
            Assert.AreEqual(-32.0, Rows(database)[321].PosY, "and Down leaves it");
        }

        [TestMethod]
        public void OnMySqlTheMovesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in LogosShrineHeights.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in LogosShrineHeights.Down())
                StringAssert.Contains(down, sql);
        }

        private static Dictionary<uint, LogosEntry> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.Set<LogosEntry>().AsNoTracking().ToDictionary(row => row.Id);

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
