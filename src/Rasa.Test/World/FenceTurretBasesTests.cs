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

    /// <summary>
    /// The control-point turrets on the outpost fence's own gun bases (Fence_turret_bases,
    /// FenceTurretBases): Hydro Plant's eight were on the wire fence's corners and walls, two of
    /// them in the air, beside six empty round bases - and the same at eleven other points.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FenceTurretBasesTests
    {
        private const string Migration = "20271002000000_Fence_turret_bases";
        private const string Previous = "20271001000000_Foreas_hq_logos_pads";
        private const uint HydroPlant = 2;

        [TestMethod]
        public void TheTurretsGoOntoTheBasesTheRestComeOffAndDownPutsItAllBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var poolsBefore = Pools(database);
            var linksBefore = Links(database);

            foreach (var m in FenceTurretBases.Moves)
            {
                AssertAt(poolsBefore[m.AfsPool], m.AfsFrom, $"{m.AfsPool} before");
                AssertAt(poolsBefore[m.BanePool], m.BaneFrom, $"{m.BanePool} before");
            }
            foreach (var t in FenceTurretBases.Removed)
                AssertAt(poolsBefore[t.AfsPool], t.Afs, $"{t.AfsPool} before");
            Assert.IsFalse(FenceTurretBases.Added.Any(t => poolsBefore.ContainsKey(t.AfsPool) || poolsBefore.ContainsKey(t.BanePool)), "the new ids free");

            migrator.Migrate(Migration);
            var pools = Pools(database);
            var links = Links(database);

            foreach (var m in FenceTurretBases.Moves)
            {
                AssertAt(pools[m.AfsPool], m.AfsTo, $"{m.AfsPool} on its base");
                AssertAt(pools[m.BanePool], m.BaneTo, $"{m.BanePool} on the AFS base");
                Assert.AreEqual(0.38, pools[m.BanePool].PosY - pools[m.AfsPool].PosY, 0.001, $"{m.BanePool} stands on the AFS turret's base");
            }
            foreach (var t in FenceTurretBases.Removed)
            {
                Assert.IsFalse(pools.ContainsKey(t.AfsPool) || pools.ContainsKey(t.BanePool), $"{t.AfsPool} removed");
                Assert.IsFalse(links.Any(l => l.ObjectId == t.AfsPool || l.ObjectId == t.BanePool), $"{t.AfsPool}'s links removed");
            }
            foreach (var t in FenceTurretBases.Added)
            {
                AssertAt(pools[t.AfsPool], t.Afs, $"{t.AfsPool} added");
                AssertAt(pools[t.BanePool], t.Bane, $"{t.BanePool} added");
                Assert.AreEqual((byte)0, pools[t.AfsPool].Mode, "the AFS pool automatic");
                Assert.AreEqual((byte)1, pools[t.BanePool].Mode, "the Bane pool the control point's");
                CollectionAssert.Contains(links, (t.Point, ControlPointLinkEntry.KindAfsPool, t.AfsPool));
                CollectionAssert.Contains(links, (t.Point, ControlPointLinkEntry.KindBanePool, t.BanePool));
            }
            Assert.HasCount(poolsBefore.Count - 2 * FenceTurretBases.Removed.Length + 2 * FenceTurretBases.Added.Length, pools);

            // Hydro Plant, as reported: its six round bases filled, two turrets fewer, the platforms' two as they were.
            var hydro = links.Where(l => l.Point == HydroPlant && l.Kind == ControlPointLinkEntry.KindAfsPool).Select(l => pools[l.ObjectId]).ToList();
            Assert.HasCount(8, hydro);
            Assert.AreEqual(6, hydro.Count(p => Math.Abs(p.PosY - 63.256) < 0.001), "six on the round bases");

            migrator.Migrate(Previous);
            var poolsAfter = Pools(database);
            CollectionAssert.AreEquivalent(poolsBefore.Keys.ToList(), poolsAfter.Keys.ToList(), "Down puts the pools back and takes the new ones out");
            foreach (var (id, before) in poolsBefore)
            {
                var after = poolsAfter[id];
                Assert.AreEqual((before.PosX, before.PosY, before.PosZ, before.Rotation, before.Creature1Id, before.Mode),
                    (after.PosX, after.PosY, after.PosZ, after.Rotation, after.Creature1Id, after.Mode), $"{id} as it was");
            }
            CollectionAssert.AreEquivalent(linksBefore, Links(database), "and the links");
        }

        /// <summary>
        /// Every control point's turret on something: none with nothing under it, as twenty were;
        /// those on a dedicated platform stand on its gun mount, up to 3 m over the platform's
        /// floor. The ones moved or added stand on their base.
        /// </summary>
        [TestMethod]
        public void NoControlPointTurretStandsOverNothing()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var maps = database.Context.MapInfoEntries.AsNoTracking().ToDictionary(m => m.Id, m => m.MapName);
            var onBase = FenceTurretBases.Moves.Select(m => m.AfsPool).Concat(FenceTurretBases.Added.Select(t => t.AfsPool)).ToHashSet();
            var covers = new Dictionary<uint, CoverMesh>();
            var failures = new List<string>();

            foreach (var pool in database.Context.SpawnPoolEntries.AsNoTracking()
                .Where(p => p.Id >= ControlPointTurrets.PoolIdMin && p.Id <= ControlPointTurrets.PoolIdMax).ToList())
            {
                if (!covers.TryGetValue(pool.MapContextId, out var cover))
                    covers[pool.MapContextId] = cover = CoverMesh.Read(CoverMesh.PathFor(NavmeshDirectory(), maps[pool.MapContextId]));

                var at = new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ);
                var gap = GapUnder(cover, at);
                var allowed = onBase.Contains(pool.Id) ? 0.1f : 3f;

                if (gap == null || gap > allowed)
                    failures.Add($"{pool.Id} at {at}: {(gap == null ? "nothing under it" : $"{gap:F2} m over the surface")}");
            }

            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesAPoolAGmMoved()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var moved = FenceTurretBases.Moves[0];
            var removed = FenceTurretBases.Removed[0];

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE spawnpool SET pos_x = pos_x + 5 WHERE id IN ({moved.AfsPool}, {removed.AfsPool})");

            migrator.Migrate(Migration);
            var once = (Pools(database), Links(database));
            Assert.AreEqual(moved.AfsFrom.X + 5, once.Item1[moved.AfsPool].PosX, 0.001, "the GM's pool is not moved");
            Assert.IsTrue(once.Item1.ContainsKey(removed.AfsPool), "nor removed");
            AssertAt(once.Item1[moved.BanePool], moved.BaneTo, "its Bane pool goes all the same");

            foreach (var sql in FenceTurretBases.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEquivalent(once.Item1.Keys.ToList(), Pools(database).Keys.ToList(), "twice");
            CollectionAssert.AreEquivalent(once.Item2, Links(database), "twice");

            migrator.Migrate(Previous);
            Assert.AreEqual(moved.AfsFrom.X + 5, Pools(database)[moved.AfsPool].PosX, 0.001, "Down leaves the GM's pool");
        }

        [TestMethod]
        public void OnMySqlThePoolsMoveGoAndComeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in FenceTurretBases.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in FenceTurretBases.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>How far the first surface is under a point, from 0.3 m over it: null for none within 20 m.</summary>
        private static float? GapUnder(CoverMesh cover, Vector3 at)
        {
            for (var d = 0f; d <= 20f; d += 0.05f)
                if (cover.GeometryBlocks(at + new Vector3(0, 0.3f, 0), at - new Vector3(0, d, 0)))
                    return d;

            return null;
        }

        private static void AssertAt(SpawnPoolEntry pool, FenceTurretBases.Place at, string what)
        {
            Assert.AreEqual(at.X, pool.PosX, 0.001, $"{what}: x");
            Assert.AreEqual(at.Y, pool.PosY, 0.001, $"{what}: y");
            Assert.AreEqual(at.Z, pool.PosZ, 0.001, $"{what}: z");
            Assert.AreEqual(at.Rotation, pool.Rotation, 0.0001, $"{what}: rotation");
        }

        private static Dictionary<uint, SpawnPoolEntry> Pools(OrdinaryLootTestDatabase database) =>
            database.Context.SpawnPoolEntries.AsNoTracking()
                .Where(p => p.Id >= ControlPointTurrets.PoolIdMin && p.Id <= ControlPointTurrets.PoolIdMax).ToDictionary(p => p.Id);

        private static List<(uint Point, byte Kind, uint ObjectId)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.ControlPointLinkEntries.AsNoTracking().ToList()
                .Where(l => l.ObjectId >= ControlPointTurrets.PoolIdMin && l.ObjectId <= ControlPointTurrets.PoolIdMax)
                .Select(l => (l.ControlPointId, l.Kind, l.ObjectId)).ToList();

        private static string NavmeshDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "navmesh not found");
            return Path.Combine(directory.FullName, "navmesh");
        }
    }
}
