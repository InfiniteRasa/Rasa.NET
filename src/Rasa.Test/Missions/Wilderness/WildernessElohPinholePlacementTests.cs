using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessElohPinholePlacementTests
    {
        [TestMethod]
        public void CanonicalWitherspoonActuallySpawnsForCreekHandoffs()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(101);
            var witherspoon = harness.Npc(101);
            Assert.IsNotNull(witherspoon,
                "Canonical Witherspoon pool 101 must introduce a real NPC, not remain a zero-count pool.");
            Assert.AreEqual(101U, witherspoon.DbId);
            Assert.AreEqual(208U, witherspoon.Npc.NpcPackageId);
            AssertSpawnPose(harness, 101, witherspoon, (505d, 238.757, 223d), 0.8);
            Assert.AreEqual(1, harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Count(creature => creature.DbId == 101));
            var surface = harness.Map.NavMesh.GroundHeight(witherspoon.Position);
            Assert.IsNotNull(surface);
            Assert.IsTrue(Math.Abs(surface.Value - witherspoon.Position.Y) < 0.5f,
                "The canonical creek contact must stand on the navigation surface.");
        }

        [TestMethod]
        public void RiverReconUsesTheUpperWaterfallRockRatherThanThePondOrCavernFloor()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(101);
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(429, 5, out var area));
            Assert.AreEqual(1220U, area.MapContextId);
            Assert.IsTrue(Vector3.Distance(new Vector3(309.512878f, 271.17935f, 436.969f), area.Position) < 0.001f);
            Assert.AreEqual(6d, area.Radius.Value);
            Assert.AreEqual(2d, area.ExtentY.Value);
            Assert.IsFalse(harness.Map.NavMesh.IsUnderground(area.Position));
            AssertGrounded(harness, area.Position);
            Assert.IsTrue(Math.Abs(area.Position.Y - 271.079346627698) < 0.5,
                "The volume must stay on the measured native waterfall rock3590 support.");
            AssertRoute(harness, area.Position, harness.Npc(101).Position);
            AssertRoute(harness, area.Position, new Vector3(460, 288.54892f, 589));
        }

        [TestMethod]
        public void HubConversationsUseCanonicalPublicNpcSpawnsAndReachableHandoffs()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            Assert.IsFalse(harness.Map.IsPrivateInstance);
            harness.SpawnWorld(100, 101, 216, 217, 199, 213, 204, 205, 208);
            foreach (var (spawn, creature, package) in new[]
            {
                (100U, 100U, 116U), (216U, 138U, 251U), (217U, 139U, 252U), (199U, 121U, 253U),
                (213U, 135U, 211U), (204U, 126U, 213U), (205U, 127U, 214U), (208U, 130U, 212U)
            })
            {
                var npc = harness.Npc(spawn);
                Assert.IsNotNull(npc, $"Canonical spawn {spawn} must produce a native NPC.");
                Assert.AreEqual(creature, npc.DbId);
                Assert.AreEqual(package, npc.Npc.NpcPackageId);
                AssertGrounded(harness, npc.Position);
            }
            AssertSpawnPose(harness, 100, harness.Npc(100), (870d, 294.21, 385.5), 0.8);
            foreach (var (from, to) in new[]
            {
                (101U, 216U), (216U, 217U), (217U, 199U), (199U, 101U),
                (101U, 208U), (204U, 205U)
            })
                AssertRoute(harness, harness.Npc(from).Position, harness.Npc(to).Position);
        }

        [TestMethod]
        public void HarvestersUseSupportedNativeMeshesAndApprovedPlayerApproaches()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(213);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(213).EntityId, 432));
            var expected = new[]
            {
                ("gas-harvester-1", new Vector3(347, 233.380158210f, 178), 230.3057209735256),
                ("gas-harvester-2", new Vector3(338, 230.731414425f, 251), 227.65697718776227),
                ("gas-harvester-3", new Vector3(313.5f, 233.363021230f, 339), 230.28858399328604)
            };
            foreach (var (role, position, support) in expected)
            {
                var obj = harness.Map.DynamicObjects.Single(candidate => candidate.SceneMissionId == 432 &&
                    candidate.SceneActorRole == role && candidate.SceneOwnerCharacterId == harness.Client.Player.Id);
                Assert.AreEqual(7906U, (uint)obj.EntityClassId);
                Assert.IsTrue(obj.IsInWorld);
                Assert.IsTrue(Vector3.Distance(position, obj.Position) < 0.01f);
                var metadata = EntityClassManager.Instance.GetClassInfo(obj.EntityClassId);
                Assert.IsFalse(metadata.Augmentations.Contains(AugmentationType.NPC));
                Assert.IsTrue(metadata.Augmentations.Contains(AugmentationType.InertDestroyable));
                var physicalBase = obj.Position.Y - 2.974437236785889;
                Assert.IsTrue(Math.Abs(physicalBase - support - 0.1) < 0.001,
                    "The native mesh base, not its elevated origin, must clear the sampled footprint by0.1m.");
                var (approach, hitPoint) = HarvesterApproach(role);
                AssertGrounded(harness, approach);
                AssertRoute(harness, harness.Npc(213).Position, approach);
                Assert.IsTrue(Vector3.Distance(approach + new Vector3(0, 1.6f, 0), hitPoint) < 3.1f);
            }
            Assert.AreEqual(3, harness.Map.DynamicObjects.Count(obj => obj.SceneMissionId == 432));
        }

        [TestMethod]
        public void SurveyUnitsUseNativeAssetsAtFiveDistinctSourceBackedSites()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(205);
            WildernessElohPinholeTests.CompleteEarlierHistory(harness, 506);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(205).EntityId, 508));
            var positions = new[]
            {
                new Vector3(442.5f, 203.9f, 561.3f),
                new Vector3(332.2f, 203.9f, 522.7f),
                new Vector3(318.6f, 212.5f, 555.2f),
                new Vector3(364.7f, 196.9f, 582.3f),
                new Vector3(315.3f, 196.2f, 571.1f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                var obj = harness.Map.DynamicObjects.Single(candidate => candidate.SceneMissionId == 508 &&
                    candidate.SceneActorRole == $"survey-unit-{index + 1}" &&
                    candidate.SceneOwnerCharacterId == harness.Client.Player.Id);
                Assert.AreEqual(7827U, (uint)obj.EntityClassId);
                Assert.IsTrue(obj.IsInWorld);
                Assert.IsTrue(Vector3.Distance(positions[index], obj.Position) < 0.01f);
                var metadata = EntityClassManager.Instance.GetClassInfo(obj.EntityClassId);
                Assert.IsFalse(metadata.Augmentations.Contains(AugmentationType.NPC));
                Assert.IsTrue(metadata.Augmentations.Contains(AugmentationType.TwoStateSwitch));
                AssertGrounded(harness, obj.Position);
                AssertRoute(harness, harness.Npc(205).Position, obj.Position);
            }
            Assert.AreEqual(5, harness.Map.DynamicObjects.Count(obj => obj.SceneMissionId == 508));
        }

        [TestMethod]
        public void EggLayersUseInteriorFloorsAndTreebacksUseTheApprovedHilltopHerd()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(204, 630040, 630041, 630042, 630043, 630044, 630045);
            var richards = harness.Npc(204);
            Assert.IsNotNull(richards);
            foreach (var (spawn, creatureId, entityClass, position, underground) in new[]
            {
                (630040U, 630040U, 10240U, (330d, 203.8, 526d), true),
                (630041U, 630040U, 10240U, (338.5, 213.1, 552d), true),
                (630042U, 630040U, 10240U, (364d, 212d, 535d), true),
                (630043U, 630043U, 6038U, (450d, 288.49, 584d), false),
                (630044U, 630043U, 6038U, (473d, 288.54892, 585d), false),
                (630045U, 630043U, 6038U, (460d, 288.54892, 601d), false)
            })
            {
                var creature = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Single(candidate => candidate.SpawnPool?.DbId == spawn);
                Assert.AreEqual(creatureId, creature.DbId);
                Assert.AreEqual(entityClass, (uint)creature.EntityClass);
                Assert.AreEqual(0U, creature.NameId);
                Assert.IsNull(creature.Npc);
                AssertSpawnPose(harness, spawn, creature, position, 0);
                Assert.AreEqual(underground, harness.Map.NavMesh.IsUnderground(creature.Position));
                AssertRoute(harness, richards.Position, creature.Position);
            }
        }

        [TestMethod]
        public void SixSnipersUseTheApprovedNativeProfileAndReachablePillboxRidgePoses()
        {
            using var harness = WildernessElohPinholeTests.CreateHarness();
            harness.SpawnWorld(101, 630046, 630047, 630048, 630049, 630050, 630051);
            var witherspoon = harness.Npc(101);
            Assert.IsNotNull(witherspoon);
            foreach (var (spawn, position, heading, lane) in new[]
            {
                (630046U, (243.340721, 227.445074, 241.396381), -1.9331716839989537,
                    new Vector3(270.9166024f, 230.2878418f, 251.8508714f)),
                (630047U, (255.207849, 230.380794, 263.403659), -1.593360459534133,
                    new Vector3(298.1712006f, 229.1013684f, 264.3732543f)),
                (630048U, (345.332512, 230.320493, 177.36105), -1.7367993608386485,
                    new Vector3(388.5864221f, 228.5609961f, 184.6080212f)),
                (630049U, (327.942408, 233.363417, 184.94948), -1.43718286305095,
                    new Vector3(369.8191494f, 227.3044425f, 179.3206472f)),
                (630050U, (265.933766, 239.813626, 297.786076), -0.4228539207043388,
                    new Vector3(279.720757f, 231.5349094f, 267.5983178f)),
                (630051U, (259.940889, 230.19304, 246.037072), -2.018010821944118,
                    new Vector3(298.1712006f, 229.1013684f, 264.3732543f))
            })
            {
                var sniper = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Single(creature => creature.SpawnPool?.DbId == spawn);
                WildernessElohPinholeTests.AssertSniperProfile(sniper);
                AssertSpawnPose(harness, spawn, sniper, position, heading);
                Assert.IsFalse(harness.Map.NavMesh.IsUnderground(sniper.Position));
                AssertRoute(harness, witherspoon.Position, sniper.Position);
                AssertGrounded(harness, lane);
                AssertRoute(harness, witherspoon.Position, lane);
                var distance = Vector3.Distance(sniper.Position, lane);
                Assert.IsTrue(distance > 20 && distance < 60,
                    "Each reconstructed firing lane must fit the dedicated60m sniper profile, not an ordinary short-range loadout.");
            }
        }

        private static void AssertSpawnPose(WildernessRuntimeTestHarness harness, uint spawnId, Creature creature,
            (double X, double Y, double Z) position, double heading)
        {
            var row = harness.World.SpawnPoolEntries.Single(entry => entry.Id == spawnId);
            Assert.AreEqual(position.X, row.PosX);
            Assert.AreEqual(position.Y, row.PosY);
            Assert.AreEqual(position.Z, row.PosZ);
            Assert.AreEqual(heading, row.Rotation);
            Assert.AreEqual(1220U, row.MapContextId);
            Assert.AreEqual(creature.DbId, row.Creature1Id);
            Assert.AreEqual((byte)1, row.Creature1MinCount);
            Assert.AreEqual((byte)1, row.Creature1MaxCount);
            Assert.AreEqual(spawnId, creature.SpawnPool.DbId);

            var authored = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            var surface = harness.Map.NavMesh.GroundHeight(authored);
            Assert.IsNotNull(surface);
            Assert.IsTrue(Vector3.Distance(authored, creature.Position) < 0.5f,
                $"Spawn {spawnId} may only project onto its qualified nearby surface.");
            Assert.AreEqual(authored.X, creature.Position.X);
            Assert.AreEqual(authored.Z, creature.Position.Z);
            // A hand-seeded pool (SpawnPool.IsHandSeeded): the mesh brings it down and never lifts it.
            var expected = creature.SpawnPool.IsHandSeeded ? Math.Min(surface.Value, authored.Y) : surface.Value;
            Assert.IsTrue(Math.Abs(expected - creature.Position.Y) < 0.001f);
            Assert.IsTrue(Math.Abs(Math.IEEERemainder(creature.Rotation - heading, 2 * Math.PI)) < 0.001);
            AssertGrounded(harness, creature.Position);
        }

        private static void AssertGrounded(WildernessRuntimeTestHarness harness, Vector3 position)
        {
            var surface = harness.Map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(surface, $"No navigation surface near {position}.");
            Assert.IsTrue(Math.Abs(surface.Value - position.Y) < 0.5f, $"Source {position} is above/below {surface}.");
        }

        internal static (Vector3 Approach, Vector3 HitPoint) HarvesterApproach(string role) => role switch
        {
            "gas-harvester-1" => (new Vector3(351, 229.832204f, 178), new Vector3(348.3482483f, 232.8031293f, 178)),
            "gas-harvester-2" => (new Vector3(338, 228.123194f, 255), new Vector3(338, 230.414494f, 252.549061f)),
            "gas-harvester-3" => (new Vector3(317.5f, 230.259457f, 339), new Vector3(314.848192f, 232.935802f, 339)),
            _ => throw new InvalidOperationException($"No approved harvester approach for {role}.")
        };

        private static void AssertRoute(WildernessRuntimeTestHarness harness, Vector3 from, Vector3 to)
        {
            var path = harness.Map.NavMesh.FindPath(from, to, out var complete);
            Assert.IsNotNull(path, $"No navigation route from {from} to {to}.");
            Assert.IsTrue(complete, $"Incomplete navigation route from {from} to {to}.");
            Assert.IsTrue(path.Count > 0 && Vector3.Distance(path[^1], to) < 0.5f,
                $"The route from {from} must reach the qualified endpoint {to} within0.5m.");
        }
    }
}
