using System;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;
    using Rasa.Test.TestSupport;

    /// <summary>
    /// Where an automatic pool with an area puts its creatures when its point is off the map's
    /// navmesh (SpawnPoolManager.Anchor, HasWalkableGround), against the maps' real navmeshes.
    /// Eleven such pools on a fresh database had no walkable ground found for them, and put their
    /// creatures down in the air or under the ground; the point of each is as the data has it.
    /// </summary>
    [TestClass]
    public class SpawnPoolGroundTests
    {
        private const float PackRadius = 15f;

        [TestMethod]
        public void AnAttaColonyPackBesideTheGroundAnchorsOnIt()
        {
            // 531584: the Atta Harvesters whose point is 18 m from the nearest ground and 14 m
            // above it. The eight-probe ring at 32 m stepped over the ground it stands beside.
            var map = Map("adv_arieki_torden_plains_attacolony");
            var pool = Pool(531584, 9.1f, 192.3f, 252.8f);

            Assert.IsTrue(SpawnPoolManager.HasWalkableGround(map, pool));

            var anchor = SpawnPoolManager.Anchor(map, pool);

            Assert.IsTrue(anchor.HasValue);
            Assert.IsTrue(Vector3.Distance(pool.Position, anchor.Value) <= SpawnPoolManager.AnchorReach, $"{anchor}");
            Assert.IsTrue(map.NavMesh.IsOnMesh(anchor.Value));
            Assert.IsTrue(map.NavMesh.IsOnMesh(SpawnPoolManager.SpawnPoint(map, pool, 2)), "a creature of it is put on the mesh");
        }

        [TestMethod]
        [DataRow(540091U, "adv_arieki_torden_mires", 658.0f, 263.0f, -414.5f)]   // Barb Ticks, ground 39 m off
        [DataRow(540122U, "adv_arieki_torden_plains", 162.3f, 423.6f, -699.0f)]  // Flaregashers, ground 57 m off
        public void APackWithinReachOfGroundAnchorsOnIt(uint id, string mapName, float x, float y, float z)
        {
            var map = Map(mapName);
            var pool = Pool(id, x, y, z);

            Assert.IsTrue(SpawnPoolManager.HasWalkableGround(map, pool));
            Assert.IsFalse(pool.NoWalkableGround);

            var anchor = SpawnPoolManager.Anchor(map, pool);

            Assert.IsTrue(anchor.HasValue);
            Assert.IsTrue(map.NavMesh.IsOnMesh(anchor.Value));
            Assert.IsTrue(Math.Abs(anchor.Value.Y - y) < 16f, "the same ground, not a floor far below");
        }

        [TestMethod]
        public void TheThunderheadPackIsOnTheGroundAtItsCorrectedHeight()
        {
            // 540127: entered with a height of 0; its ground is 437 m up (Thunderhead_pool_height).
            var map = Map("adv_arieki_ligo_thunderhead");

            Assert.IsTrue(map.NavMesh.IsOnMesh(new Vector3(-810f, 437f, -530f)));
            Assert.IsTrue(SpawnPoolManager.HasWalkableGround(map, Pool(540127, -810f, 437f, -530f)));
        }

        [TestMethod]
        [DataRow(540112U, "adv_arieki_torden_plains", 1006.1f, 574.6f, -118.6f)]                  // Flaregashers: a canyon floor 300 m below
        [DataRow(540148U, "adv_arieki_torden_abyss", -90.0f, 532.3f, -673.5f)]                    // Granitours: the world floor 530 m below
        [DataRow(540182U, "adv_arieki_ligo_thunderhead_quassostation", 57.5f, 78.2f, 451.9f)]     // Beam Mantas: ground 128 m off
        public void APackWithNoGroundInReachSpawnsNothing(uint id, string mapName, float x, float y, float z)
        {
            var map = Map(mapName);
            var pool = Pool(id, x, y, z);

            Assert.IsFalse(SpawnPoolManager.Anchor(map, pool).HasValue);
            Assert.IsFalse(SpawnPoolManager.HasWalkableGround(map, pool));
            Assert.IsTrue(pool.NoWalkableGround, "marked, so that it is not looked for again");
            Assert.IsFalse(SpawnPoolManager.HasWalkableGround(map, pool));
        }

        [TestMethod]
        public void AMapWithoutANavmeshHasNothingToSay()
        {
            var map = new MapChannel { MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0) };
            var pool = Pool(1, 0, 0, 0);

            Assert.IsTrue(SpawnPoolManager.HasWalkableGround(map, pool));
            Assert.IsFalse(pool.NoWalkableGround);
        }

        private static SpawnPool Pool(uint id, float x, float y, float z) =>
            new SpawnPool { DbId = id, Position = new Vector3(x, y, z), Radius = PackRadius, Mode = SpawnPoolManager.ModeAutomatic };

        private static MapChannel Map(string mapName)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Repository root not found.");

            return new MapChannel
            {
                MapInfo = new MapInfo(1, mapName, 0, 0),
                NavMesh = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), mapName))
            };
        }
    }
}
