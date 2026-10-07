using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// One creature alone on a point that was entered by hand is brought down onto the navmesh
    /// and never lifted onto it; a point that was generated, scattered or drawn from an area is
    /// put on the mesh either way. Lt Col Cimoch (pool 196, Alia Das) was reported standing
    /// knee high off the ground: the mesh beside the sandbags there is 0.70 m over the terrain.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class HandSeededSpawnHeightTests
    {
        private const uint CimochPool = 196;
        private const uint Cimoch = 118;

        /// <summary>SpawnpoolPreloader's rows, which are the hand-seeded ones.</summary>
        private sealed class HandSeededRows : SpawnpoolPreloader
        {
            public List<object[]> All() => GetRows().ToList();
        }

        private static Vector3 PositionOf(SpawnPoolEntry entry)
        {
            return new Vector3((float)entry.PosX, (float)entry.PosY, (float)entry.PosZ);
        }

        [TestMethod]
        public void CimochStandsAtTheHeightHisPoolWasEnteredAt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = harness.World.Set<SpawnPoolEntry>().Single(pool => pool.Id == CimochPool);

            Assert.AreEqual(Cimoch, entry.Creature1Id);
            Assert.AreEqual(0.0, entry.Radius, 0.000001);

            harness.SpawnWorld(CimochPool);

            var officer = harness.Npc(CimochPool);

            Assert.IsNotNull(officer, "Cimoch did not spawn.");
            Assert.AreEqual(PositionOf(entry), officer.Position);
            Assert.AreEqual(PositionOf(entry), officer.HomePos.Position);
        }

        [TestMethod]
        public void AHandSeededPointInTheAirIsBroughtDownOntoTheMesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = harness.World.Set<SpawnPoolEntry>().Single(pool => pool.Id == CimochPool);
            var raised = PositionOf(entry) + new Vector3(0f, 2f, 0f);
            var ground = harness.Map.NavMesh.GroundHeight(raised);

            Assert.IsNotNull(ground, "No mesh under the spot.");
            Assert.IsTrue(ground.Value < raised.Y - 0.5f && raised.Y - ground.Value < NavMeshManager.SnapTolerance);

            var pool = new SpawnPool { DbId = CimochPool, Position = raised, MapContextId = 1220 };
            var spot = SpawnPoolManager.SpawnPoint(harness.Map, pool, 1);

            Assert.AreEqual(raised.X, spot.X);
            Assert.AreEqual(raised.Z, spot.Z);
            Assert.AreEqual(ground.Value, spot.Y, 0.001f);
        }

        [TestMethod]
        public void AHandSeededPointUnderTheMeshIsNotLifted()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = harness.World.Set<SpawnPoolEntry>().Single(pool => pool.Id == CimochPool);
            var lowered = PositionOf(entry) - new Vector3(0f, 0.25f, 0f);
            var ground = harness.Map.NavMesh.GroundHeight(lowered);

            Assert.IsNotNull(ground, "No mesh under the spot.");
            Assert.IsTrue(ground.Value > lowered.Y && ground.Value - lowered.Y < NavMeshManager.SnapTolerance);

            var pool = new SpawnPool { DbId = CimochPool, Position = lowered, MapContextId = 1220 };

            Assert.AreEqual(lowered, SpawnPoolManager.SpawnPoint(harness.Map, pool, 1));
        }

        [TestMethod]
        public void AGeneratedPointIsPutOnTheMesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = harness.World.Set<SpawnPoolEntry>().Single(pool => pool.Id == CimochPool);

            // The same spot under a generated pool's id, above the mesh and below it.
            foreach (var offset in new[] { 2f, -0.25f })
            {
                var point = PositionOf(entry) + new Vector3(0f, offset, 0f);
                var ground = harness.Map.NavMesh.GroundHeight(point);

                Assert.IsNotNull(ground, "No mesh under the spot.");
                Assert.IsTrue(Math.Abs(ground.Value - point.Y) > 0.1f && Math.Abs(ground.Value - point.Y) < NavMeshManager.SnapTolerance);

                var generated = new SpawnPool { DbId = SpawnPool.FirstGeneratedId + 10004, Position = point, MapContextId = 1220 };
                var spot = SpawnPoolManager.SpawnPoint(harness.Map, generated, 1);

                Assert.AreEqual(point.X, spot.X);
                Assert.AreEqual(point.Z, spot.Z);
                Assert.AreEqual(ground.Value, spot.Y, 0.001f);
            }
        }

        [TestMethod]
        public void SeveralOnAHandSeededPointAreScatteredAndPutOnTheMesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = harness.World.Set<SpawnPoolEntry>().Single(pool => pool.Id == CimochPool);
            var pool = new SpawnPool { DbId = CimochPool, Position = PositionOf(entry), MapContextId = 1220 };

            Assert.IsTrue(pool.IsHandSeeded);

            for (var i = 0; i < 20; i++)
            {
                var spot = SpawnPoolManager.SpawnPoint(harness.Map, pool, 3);
                var ground = harness.Map.NavMesh.GroundHeight(spot);

                Assert.IsTrue(Math.Abs(spot.X - pool.Position.X) <= 2.001f && Math.Abs(spot.Z - pool.Position.Z) <= 2.001f);
                Assert.IsNotNull(ground);
                Assert.AreEqual(ground.Value, spot.Y, 0.01f);
            }
        }

        [TestMethod]
        public void APoolIsHandSeededByItsIdAndAScenesOwnPoolNever()
        {
            Assert.IsTrue(new SpawnPool { DbId = 1 }.IsHandSeeded);
            Assert.IsTrue(new SpawnPool { DbId = CimochPool }.IsHandSeeded);
            Assert.IsTrue(new SpawnPool { DbId = SpawnPool.FirstGeneratedId - 1 }.IsHandSeeded);

            Assert.IsFalse(new SpawnPool().IsHandSeeded);
            Assert.IsFalse(new SpawnPool { DbId = SpawnPool.FirstGeneratedId }.IsHandSeeded);
            Assert.IsFalse(new SpawnPool { DbId = 510004 }.IsHandSeeded);

            // A scene's pool takes its id from the scene's spawn id: 1, 2, 3.
            Assert.IsFalse(new SpawnPool { DbId = 1, ScenarioKey = "run:1:generation:1:actor:guard" }.IsHandSeeded);
        }

        [TestMethod]
        public void EveryPoolUnderTheFirstGeneratedIdIsOneOfTheHandSeededRows()
        {
            var seeded = new HandSeededRows().All().Select(row => Convert.ToUInt32(row[0])).ToHashSet();

            Assert.IsTrue(seeded.Contains(CimochPool));
            Assert.IsTrue(seeded.All(id => id < SpawnPool.FirstGeneratedId));

            using var harness = WildernessRuntimeTestHarness.Create();
            var low = harness.World.Set<SpawnPoolEntry>()
                .Where(pool => pool.Id < SpawnPool.FirstGeneratedId)
                .Select(pool => pool.Id)
                .ToList();

            Assert.IsTrue(low.Count > 0);

            var strangers = low.Where(id => !seeded.Contains(id)).ToList();

            Assert.AreEqual(0, strangers.Count,
                $"Generated pools numbered under {SpawnPool.FirstGeneratedId} would not be put on the mesh: {string.Join(", ", strangers)}");
        }
    }
}
