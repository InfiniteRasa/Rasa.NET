using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A creature that can go nowhere and is alone on a point pool stands on the point, at the
    /// height the pool has: the navmesh's height is for what moves, is scattered or is drawn from
    /// an area. Lt Col Cimoch (pool 196, Alia Das) was reported standing knee high off the
    /// ground: the mesh beside the sandbags there is 0.70 m over the terrain. The pools whose
    /// own height was not their floor's are put right by Stand_npcs_on_their_floors.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class StandingSpawnHeightTests
    {
        private const uint CimochPool = 196;
        private const uint Cimoch = 118;

        private const string Migration = "20261124000000_Stand_npcs_on_their_floors";
        private const string Before = "20261123000000_Add_npc_poses";

        private static readonly Creature Standing = new Creature { WalkSpeed = 0f, RunSpeed = 0f };
        private static readonly Creature Walking = new Creature { WalkSpeed = 2f, RunSpeed = 6f };

        private static Vector3 PositionOf(SpawnPoolEntry entry)
        {
            return new Vector3((float)entry.PosX, (float)entry.PosY, (float)entry.PosZ);
        }

        private static SpawnPoolEntry Row(WildernessRuntimeTestHarness harness, uint pool)
        {
            return harness.World.Set<SpawnPoolEntry>().AsNoTracking().Single(entry => entry.Id == pool);
        }

        private static Creature Spawned(WildernessRuntimeTestHarness harness, uint pool)
        {
            return harness.Map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Distinct()
                .Single(creature => creature.SpawnPool?.DbId == pool);
        }

        [TestMethod]
        public void WhatHasNeitherSpeedNeverMoves()
        {
            Assert.IsTrue(BehaviorManager.NeverMoves(Standing));
            Assert.IsFalse(BehaviorManager.NeverMoves(Walking));

            // A boss that waits where it is and runs when it fights; and the other way about.
            Assert.IsFalse(BehaviorManager.NeverMoves(new Creature { WalkSpeed = 0f, RunSpeed = 9f }));
            Assert.IsFalse(BehaviorManager.NeverMoves(new Creature { WalkSpeed = 2f, RunSpeed = 0f }));
            Assert.IsFalse(BehaviorManager.NeverMoves(null));
        }

        [TestMethod]
        public void CimochStandsAtTheHeightOfHisPool()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = Row(harness, CimochPool);

            Assert.AreEqual(Cimoch, entry.Creature1Id);
            Assert.AreEqual(0.0, entry.Radius, 0.000001);

            harness.SpawnWorld(CimochPool);

            var officer = harness.Npc(CimochPool);

            Assert.IsNotNull(officer, "Cimoch did not spawn.");
            Assert.IsTrue(BehaviorManager.NeverMoves(officer));
            Assert.AreEqual(PositionOf(entry), officer.Position);
            Assert.AreEqual(PositionOf(entry), officer.HomePos.Position);
        }

        [TestMethod]
        [DataRow(2f)]
        [DataRow(-0.25f)]
        public void ACreatureThatNeverMovesIsLeftOnItsPointAndOneThatDoesIsPutOnTheMesh(float offset)
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            // Cimoch's spot, over the mesh and under it.
            var point = PositionOf(Row(harness, CimochPool)) + new Vector3(0f, offset, 0f);
            var ground = harness.Map.NavMesh.GroundHeight(point);

            Assert.IsNotNull(ground, "No mesh under the spot.");
            Assert.IsTrue(Math.Abs(ground.Value - point.Y) > 0.1f && Math.Abs(ground.Value - point.Y) < NavMeshManager.SnapTolerance);

            var pool = new SpawnPool { DbId = 510004, Position = point, MapContextId = 1220 };

            Assert.AreEqual(point, SpawnPoolManager.SpawnPoint(harness.Map, pool, 1, Standing));

            foreach (var walker in new[] { Walking, new Creature { WalkSpeed = 0f, RunSpeed = 9f }, null })
            {
                var spot = SpawnPoolManager.SpawnPoint(harness.Map, pool, 1, walker);

                Assert.AreEqual(point.X, spot.X);
                Assert.AreEqual(point.Z, spot.Z);
                Assert.AreEqual(ground.Value, spot.Y, 0.001f);
            }
        }

        [TestMethod]
        public void SeveralThatNeverMoveOnOnePointAreScatteredAndPutOnTheMesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pool = new SpawnPool { DbId = CimochPool, Position = PositionOf(Row(harness, CimochPool)), MapContextId = 1220 };

            for (var i = 0; i < 20; i++)
            {
                var spot = SpawnPoolManager.SpawnPoint(harness.Map, pool, 3, Standing);
                var ground = harness.Map.NavMesh.GroundHeight(spot);

                Assert.IsTrue(Math.Abs(spot.X - pool.Position.X) <= 2.001f && Math.Abs(spot.Z - pool.Position.Z) <= 2.001f);
                Assert.IsNotNull(ground);
                Assert.AreEqual(ground.Value, spot.Y, 0.01f);
            }
        }

        [TestMethod]
        public void TheMigrationPutsEachPoolWhereStandingHeightsSaysAndDownPutsItBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var corrected = StandingHeights.Corrected().ToList();

            Assert.AreEqual(200, corrected.Count);
            Assert.AreEqual(200, corrected.Select(row => row.Pool).Distinct().Count());
            Assert.AreEqual(185, corrected.Count(row => row.X == null && row.Z == null));
            Assert.AreEqual(15, corrected.Count(row => row.X != null && row.Z != null));
            Assert.AreEqual(200, StandingHeights.WorldUp.Length);
            Assert.AreEqual(200, StandingHeights.WorldDown.Length);

            void AssertCorrected(bool expected)
            {
                harness.World.ChangeTracker.Clear();

                var rows = harness.World.Set<SpawnPoolEntry>().AsNoTracking().ToDictionary(entry => entry.Id);

                foreach (var (pool, x, y, z) in corrected)
                {
                    Assert.IsTrue(rows.TryGetValue(pool, out var row), $"spawnpool {pool} is not in the world.");

                    var there = Math.Abs(row.PosY - y) < 0.000001
                        && (x == null || Math.Abs(row.PosX - x.Value) < 0.000001)
                        && (z == null || Math.Abs(row.PosZ - z.Value) < 0.000001);

                    Assert.AreEqual(expected, there, $"spawnpool {pool} at ({row.PosX}, {row.PosY}, {row.PosZ})");
                }
            }

            // The migrated world.
            AssertCorrected(true);

            // The vendor of Torcastra Prison was 5.69 m over the floor of the hall; Private
            // Parsons a metre in the ground; General Beacham in the sandbags beside Cimoch.
            Assert.AreEqual(13.61, Row(harness, 500171).PosY, 0.000001);
            Assert.AreEqual(238.56, Row(harness, 510071).PosY, 0.000001);
            Assert.AreEqual((813.4, 294.52, 387.95), (Row(harness, 510002).PosX, Row(harness, 510002).PosY, Row(harness, 510002).PosZ));

            var migrator = harness.World.GetService<IMigrator>();

            migrator.Migrate(Before);
            AssertCorrected(false);
            Assert.AreEqual(19.3, Row(harness, 500171).PosY, 0.001);
            Assert.AreEqual(237.56, Row(harness, 510071).PosY, 0.001);
            Assert.AreEqual(812.9, Row(harness, 510002).PosX, 0.001);
            Assert.AreEqual(294.69, Row(harness, 510002).PosY, 0.001);
            Assert.AreEqual(388.2, Row(harness, 510002).PosZ, 0.001);

            migrator.Migrate(Migration);
            AssertCorrected(true);

            // A second run of the statements changes nothing.
            foreach (var statement in StandingHeights.WorldUp)
                Assert.AreEqual(0, harness.World.Database.ExecuteSqlRaw(statement), statement);
        }

        [TestMethod]
        public void EveryCorrectedPoolHoldsOneCreatureThatNeverMoves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var creatures = harness.World.Set<CreatureEntry>().AsNoTracking().ToDictionary(entry => entry.Id);
            var pools = harness.World.Set<SpawnPoolEntry>().AsNoTracking().ToDictionary(entry => entry.Id);

            foreach (var (id, _, _, _) in StandingHeights.Corrected())
            {
                var pool = pools[id];

                Assert.AreEqual(0.0, pool.Radius, 0.000001, $"spawnpool {id}");
                Assert.AreEqual((byte)1, pool.Creature1MaxCount, $"spawnpool {id}");
                Assert.AreEqual(0, pool.Creature2MaxCount + pool.Creature3MaxCount + pool.Creature4MaxCount + pool.Creature5MaxCount + pool.Creature6MaxCount, $"spawnpool {id}");

                var creature = creatures[pool.Creature1Id];

                Assert.IsTrue(creature.WalkSpeed < 0.01 && creature.RunSpeed < 0.01, $"spawnpool {id} holds {creature.Comment}, which moves.");
            }
        }

        [TestMethod]
        [DataRow(501004U)]   // Ranger Trainer: Alia Das, who was entered in a cot
        [DataRow(510002U)]   // Brigadier General Beacham, who was entered in the sandbags
        [DataRow(501005U)]   // Sapper Trainer: Alia Das, who is where he was
        public void AStandingNpcSpawnsWhereItsPoolIs(uint pool)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var entry = Row(harness, pool);

            harness.SpawnWorld(pool);

            var npc = Spawned(harness, pool);

            Assert.IsTrue(BehaviorManager.NeverMoves(npc));
            Assert.AreEqual(PositionOf(entry), npc.Position);
        }
    }
}
