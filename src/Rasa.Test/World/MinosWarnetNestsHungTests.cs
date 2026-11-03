using System;
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
    using Rasa.Test.Database;

    /// <summary>Minos Caverns' warnet nests hang from the roof, where they can be seen and shot (Minos_warnet_nests_hung, MinosWarnetNestsHung).</summary>
    [TestClass]
    [DoNotParallelize]
    public class MinosWarnetNestsHungTests
    {
        private const string Migration = "20270928000000_Minos_warnet_nests_hung";
        private const string Previous = "20270927000000_Few_shaft_teleporter";
        private const string MapName = "adv_foreas_concordia_divide_minoscaverns";

        /// <summary>arch_forean_warnethive_v01.geo, hanging from its origin: its pod widest 4.25 m under it, its tip 8.9 m.</summary>
        private const float HiveTip = 8.9f, PodWidest = 4.25f;

        [TestMethod]
        public void EachNestHangsFromTheRoofOverWalkableFloorWithinReach()
        {
            var cover = CoverMesh.Read(CoverMesh.PathFor(NavmeshDirectory(), MapName));
            var floors = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), MapName));

            foreach (var (id, was, hangs) in MinosWarnetNestsHung.Nests)
            {
                var origin = new Vector3((float)hangs.X, (float)hangs.Y, (float)hangs.Z);
                Assert.IsTrue(cover.Blocked(origin - new Vector3(0, 0.3f, 0), origin + new Vector3(0, 1f, 0)), $"{id}: the roof is at its origin");
                Assert.IsFalse(cover.Blocked(origin - new Vector3(0, 0.3f, 0), origin - new Vector3(0, 7.5f, 0)), $"{id}: nothing in the way of the pod");

                // The floor: the first surface under the pod, and walkable.
                var ground = FirstSurface(cover, origin - new Vector3(0, 0.3f, 0), origin - new Vector3(0, 30f, 0));
                Assert.IsNotNull(ground, $"{id}: a floor under it");
                var floor = floors.Nearest(new Vector3(origin.X, ground.Value + 0.2f, origin.Z));
                Assert.IsTrue(floor.HasValue, $"{id}: walkable floor under it");
                Assert.IsLessThan(1f, Vector2.Distance(new Vector2(origin.X, origin.Z), new Vector2(floor.Value.X, floor.Value.Z)), $"{id}: right under it");
                Assert.IsTrue(origin.Y - HiveTip > ground.Value - 1f, $"{id}: its tip at most a metre into the floor");
                Assert.IsTrue(origin.Y - ground.Value < 20f, $"{id}: and within a rifle's easy reach");

                // As it was: the pod, widest 4.25 m down, under the floor it stood on.
                var stood = floors.Nearest(new Vector3((float)was.X, (float)was.Y, (float)was.Z));
                Assert.IsTrue(stood.HasValue && was.Y - PodWidest < stood.Value.Y - 2f, $"{id}: its pod was under the floor");
            }
        }

        [TestMethod]
        public void TheNestsMoveTheirPoolsStayAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rows(database);
            var pools = Pools(database);
            foreach (var (id, was, _) in MinosWarnetNestsHung.Nests)
                Assert.AreEqual(was.Y, before.Single(row => row.Id == id).Y, 0.01, $"{id} as seeded");

            migrator.Migrate(Migration);
            var after = Rows(database);
            foreach (var (id, _, hangs) in MinosWarnetNestsHung.Nests)
            {
                var row = after.Single(r => r.Id == id);
                Assert.AreEqual(hangs.X, row.X, 0.01, $"{id}");
                Assert.AreEqual(hangs.Y, row.Y, 0.01, $"{id}");
                Assert.AreEqual(hangs.Z, row.Z, 0.01, $"{id}");
            }
            CollectionAssert.AreEqual(pools, Pools(database), "the warnets' pools stay on the floor");

            foreach (var sql in MinosWarnetNestsHung.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Rows(database), "Down");
        }

        [TestMethod]
        public void ANestAGmHasMovedIsLeftWhereItIs()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE world_destructible SET pos_x = pos_x + 3 WHERE id = {MinosCavernsContent.FirstNest}");
            var moved = Rows(database).Single(row => row.Id == MinosCavernsContent.FirstNest);

            migrator.Migrate(Migration);
            Assert.AreEqual(moved, Rows(database).Single(row => row.Id == MinosCavernsContent.FirstNest));
        }

        [TestMethod]
        public void OnMySqlTheNestsMoveAndMoveBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in MinosWarnetNestsHung.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in MinosWarnetNestsHung.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The height of the first surface met going from <paramref name="from"/> straight to <paramref name="to"/>.</summary>
        private static float? FirstSurface(CoverMesh cover, Vector3 from, Vector3 to)
        {
            if (!cover.Blocked(from, to))
                return null;
            float near = from.Y, far = to.Y;
            for (var i = 0; i < 30; i++)
            {
                var middle = (near + far) / 2;
                if (cover.Blocked(from, new Vector3(from.X, middle, from.Z)))
                    far = middle;
                else
                    near = middle;
            }
            return far;
        }

        private sealed record Row(uint Id, double X, double Y, double Z);

        private static System.Collections.Generic.List<Row> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    $"SELECT id || ' ' || pos_x || ' ' || pos_y || ' ' || pos_z AS Value FROM world_destructible WHERE map_context_id = {MinosWarnetNestsHung.Map} ORDER BY id")
                .AsEnumerable().Select(line => line.Split(' '))
                .Select(p => new Row(uint.Parse(p[0]), double.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture)))
                .ToList();

        private static System.Collections.Generic.List<string> Pools(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    $"SELECT id || ' ' || pos_x || ' ' || pos_y || ' ' || pos_z AS Value FROM spawnpool WHERE id BETWEEN {MinosCavernsContent.FirstNestPool} AND {MinosCavernsContent.FirstNestPool + 8} ORDER BY id")
                .AsEnumerable().ToList();

        private static string NavmeshDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(CoverMesh.PathFor(Path.Combine(directory.FullName, "navmesh"), MapName)))
                directory = directory.Parent;
            return Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "navmesh");
        }
    }
}
