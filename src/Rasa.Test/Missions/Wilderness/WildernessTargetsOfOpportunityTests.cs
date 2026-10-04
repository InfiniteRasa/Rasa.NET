using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Models;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessTargetsOfOpportunityTests
    {
        [TestMethod]
        public void StoryRosterCountsEachBranchOnceAndRetainsAllInstanceGroups()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(196);
            var outdoor = new uint[]
            {
                421, 422, 425, 427, 428, 429, 430, 431, 432, 433, 434, 436, 441, 442, 444, 451,
                479, 506, 508, 549, 570, 574, 623, 665, 666, 679, 682, 695, 696, 697, 698, 700,
                701, 758, 769, 771, 776, 787, 795, 860, 1069, 1390, 1392, 1407
            };
            var instances = new uint[]
            {
                859, 489, 450, 593, 674, 691, 692, 693, 1054, 1055, 1059, 1056, 711, 1065, 575, 1075, 321, 323, 446
            };
            var definition = harness.Manager.LoadedMissions[1449].Objectives[55];
            Assert.IsNotNull(definition.HistoryAggregation);
            Assert.AreEqual(63U, definition.HistoryAggregation.TargetCount);
            Assert.AreEqual(13685U, definition.ClientCounterTextIds[0].Value);
            CollectionAssert.AreEquivalent(outdoor.Concat(new uint[] { 791, 820, 1393 }).Concat(instances).ToArray(),
                definition.HistoryAggregation.Groups.SelectMany(group => group).ToArray());
            CollectionAssert.AreEquivalent(new[] { "623/791", "700/820", "1392/1393" },
                definition.HistoryAggregation.Groups.Where(group => group.Count > 1)
                    .Select(group => string.Join("/", group)).ToArray());
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var id in outdoor)
                        unit.CharacterMissions.Runtime.Archive(Outcome(id, MissionState.Completed), harness.UtcNow);
                    unit.CharacterMissions.Runtime.Archive(Outcome(859, MissionState.Failed), harness.UtcNow);
                    unit.CharacterMissions.Runtime.Archive(Outcome(489, MissionState.Success), harness.UtcNow);
                });

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(196).EntityId, 1449));

            Assert.AreEqual(44U, harness.Client.Player.Missions[1449].Objectives[55].Counters[0]);
            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();
            var counter = gained.MissionInfo.ObjectivesList.Single(objective => objective.ObjectiveId == 55).Counters[0];
            Assert.AreEqual(44U, counter.CounterValue);
            Assert.AreEqual(63U, counter.TargetValue);
            foreach (var alternative in new uint[] { 791, 820, 1393 })
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() =>
                        unit.CharacterMissions.Runtime.Archive(Outcome(alternative, MissionState.Completed), harness.UtcNow));
                Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Mission(alternative)));
            }
            Assert.AreEqual(44U, harness.Client.Player.Missions[1449].Objectives[55].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1449].Objectives[55].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1449].Objectives[58].State);
            Assert.IsFalse(harness.Client.Player.Missions[1449].Completeable);
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, harness.Npc(196).EntityId, 1449));
                Assert.AreEqual(0U, second.Player.Missions[1449].Objectives[55].Counters[0]);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }

            CharacterMissionEntry Outcome(uint missionId, MissionState state) =>
                new(harness.Client.Player.Id, missionId, (uint)state)
                {
                    AssignmentId = Guid.NewGuid().ToString("N"), Generation = 1,
                    ContentRevision = "story-roster-test"
                };
        }

        [TestMethod]
        public void OutdoorTargetsPersistWithoutExposingBookkeepingOrUnlockingInstanceRewards()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(196);
            var cimoch = harness.Npc(196);
            Assert.IsNotNull(cimoch);
            using (var unit = harness.CreateChar())
                unit.CharacterTeleporters.Add(new CharacterTeleporterEntry(1, 57, (byte)WaypointType.Waypoint));

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, cimoch.EntityId, 1449));
            var assignment = harness.Client.Player.Missions[1449].AssignmentId;
            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();
            CollectionAssert.AreEquivalent(new uint[] { 1, 3, 4, 5, 6, 7, 8, 40, 48, 55 },
                gained.MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
            Assert.AreEqual(10000044U, gained.MissionInfo.MissionConstantData.CategoryId);
            Assert.AreEqual(1U, harness.Client.Player.Missions[1449].Objectives[1].Counters[0]);

            foreach (var waypoint in new uint[] { 49, 50, 51, 61, 73, 156 })
            {
                using var unit = harness.CreateChar();
                var row = new CharacterTeleporterEntry(1, waypoint, (byte)WaypointType.Waypoint);
                unit.CharacterTeleporters.Add(row);
                harness.Client.Player.GainedWaypoints.Add(row);
                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Waypoint(waypoint)));
            }
            foreach (var logos in new uint[] { 1, 2, 6, 9, 10, 23, 24, 28, 38, 49, 53, 56 })
            {
                using var unit = harness.CreateChar();
                unit.CharacterLogoses.SetLogos(1, logos);
                harness.Client.Player.Logos.Add(logos);
                harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Logos(logos));
            }
            foreach (var (creature, quantity) in new[] { (87U, 40), (85U, 30), (3U, 200), (88U, 40) })
                for (var count = 0; count < quantity; count++)
                    Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(creature)));
            foreach (var creature in new uint[] { 82, 83, 84, 79, 80, 75 })
            {
                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(creature)));
                Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(creature)));
            }
            foreach (var areaId in new uint[] { 49, 50, 51, 52, 53, 541 })
            {
                Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1449, areaId, out var area));
                var completed = harness.Client.Player.Missions[1449].Objectives[48].Counters[0];
                Visit(harness, area.Position, 12);
                Assert.AreEqual(completed, harness.Client.Player.Missions[1449].Objectives[48].Counters[0],
                    "Movement above the cave entrance must not count as a visit.");
                Visit(harness, area.Position, 0);
                Assert.AreEqual(completed + 1, harness.Client.Player.Missions[1449].Objectives[48].Counters[0]);
            }
            Assert.IsTrue(harness.Manager.TryGetAreaDefinition(1449, 542, out var secondEntrance));
            Visit(harness, secondEntrance.Position, 0);

            var mission = harness.Client.Player.Missions[1449];
            foreach (var objectiveId in new uint[] { 1, 3, 4, 5, 6, 7, 8, 48 })
                Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[objectiveId].State);
            Assert.AreEqual(6U, mission.Objectives[6].Counters[0]);
            Assert.AreEqual(6U, mission.Objectives[48].Counters[0]);
            foreach (var objectiveId in new uint[] { 40, 55, 58 })
                Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[objectiveId].State);
            Assert.IsFalse(mission.Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, cimoch.EntityId, 1449, null));
            Assert.IsFalse(harness.Drain().OfType<ObjectiveCompletedPacket>().Any(packet =>
                packet.ObjectiveId is >= 20 and <= 25 or >= 49 and <= 54 or 58));

            var reconnected = harness.Context.CreateCompetingClient(harness.Manager);
            harness.Manager.PublishInitialState(reconnected);
            Assert.AreEqual(assignment, reconnected.Player.Missions[1449].AssignmentId);
            Assert.AreEqual(6U, reconnected.Player.Missions[1449].Objectives[48].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed, reconnected.Player.Missions[1449].Objectives[5].State);
            Assert.IsFalse(reconnected.Player.Missions[1449].Completeable);
        }

        private static void Visit(WildernessRuntimeTestHarness harness, Vector3 center, float elevation)
        {
            var before = center + new Vector3(8, elevation, 0);
            var after = center + new Vector3(6.5f, elevation, 0);
            harness.MoveTo(before);
            harness.Client.Movement = new Movement(before, 1, 0, Vector2.Zero);
            harness.Client.Player.MoveBudget = 10;
            Assert.IsTrue(harness.Client.HandleMovement(new Movement(after, 1, 0, Vector2.Zero)));
        }
    }
}
