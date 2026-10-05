using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessWalkingWoundedTests
    {
        [TestMethod]
        [TestCategory("W6Integrated")]
        public void WalkingWoundedReservesExistingMatthewAndRejectsEarlyQuincyCompletion()
        {
            using var harness = WildernessRanjaGorgeTests.CreateMigrated();
            Assert.IsFalse(harness.Map.IsPrivateInstance);
            harness.SpawnWorld(201, 202);

            var matthew = harness.Npc(201);
            var quincy = harness.Npc(202);
            Assert.IsNotNull(matthew,
                "Existing Matthew creature 123 / spawn 201 must be an interactable public mission NPC.");
            Assert.IsNotNull(quincy,
                "Existing Quincy creature 124 / spawn 202 must be the canyon infirmary contact.");
            Assert.AreEqual(123U, matthew.DbId);
            Assert.AreEqual(124U, quincy.DbId);
            Assert.AreEqual(121U, quincy.Npc.NpcPackageId);

            harness.MoveTo(matthew.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, matthew.EntityId, 697),
                "Matthew must offer The Walking Wounded without an unrelated mission prerequisite.");
            var lease = harness.Manager.PublicActors.Handle(harness.Map, 201);
            Assert.IsNotNull(lease, "Acceptance must reserve the existing public Matthew, not a personal copy.");
            Assert.IsTrue(harness.Manager.PublicActors.TryResolve(harness.Map, lease, out var reservedMatthew));
            Assert.AreSame(matthew, reservedMatthew);

            harness.MoveTo(quincy.Position);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, quincy.EntityId, 697, 2, 1),
                "Quincy's native conversation must remain unavailable before the living escort arrives.");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, quincy.EntityId, 697, 0),
                "Reaching the infirmary without Matthew must not complete or reward the escort.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[697].Objectives[1].State);
            Assert.IsFalse(harness.Client.Player.Missions[697].Completeable);
            Assert.IsTrue(harness.Manager.PublicActors.TryResolve(harness.Map, lease, out var stillReservedMatthew));
            Assert.AreSame(matthew, stillReservedMatthew);
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        public void CandidateMatthewRouteHasSixtyGroundedPointsAndBidirectionalLegs()
        {
            using var harness = WildernessRanjaGorgeTests.CreateMigrated();
            var route = WildernessRanjaGorgeV1.CandidateMatthewRoute();
            Assert.AreEqual(60, route.Points.Count);
            Vector3? previous = null;
            foreach (var waypoint in route.Points)
            {
                var point = new Vector3(waypoint.Position.X, waypoint.Position.Y, waypoint.Position.Z);
                var ground = harness.Map.NavMesh.GroundHeight(point);
                Assert.IsNotNull(ground);
                Assert.IsTrue(Math.Abs(ground.Value - point.Y) < 0.5f,
                    $"The installed navigation asset does not support candidate waypoint{point}.");
                if (previous.HasValue)
                {
                    WildernessRanjaGorgeTests.AssertConnected(harness, previous.Value, point,
                        $"Matthew's outbound leg {previous.Value}->{point} must really connect.");
                    WildernessRanjaGorgeTests.AssertConnected(harness, point, previous.Value,
                        $"Matthew's return leg {point}->{previous.Value} must really connect.");
                }
                previous = point;
            }
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        public void LivingMatthewMustActuallyWalkToQuincyThenReleaseForTheNextCharacter()
        {
            using var harness = WildernessRanjaGorgeTests.CreateMigrated();
            var (matthew, quincy, lease) = BeginEscort(harness);
            var start = matthew.Position;
            var infirmary = quincy.Position;
            Assert.IsTrue(Math.Abs(infirmary.Y - 170.13672f) < 2,
                "Quincy belongs in the canyon infirmary, not at an elevated research-camp marker.");
            var path = harness.Map.NavMesh.FindPath(start, infirmary, out var complete);
            Assert.IsNotNull(path, "Matthew needs a real navigation path through the caverns.");
            Assert.IsTrue(complete && path.Count > 0 && Vector3.Distance(path[^1], infirmary) < 5,
                "Nearest navigation polygons are insufficient: the cave route must connect to Quincy.");
            for (var tick = 0; tick < 3600 &&
                 harness.Client.Player.Missions[697].Objectives[1].State == MissionObjectiveState.Incomplete; tick++)
            {
                var previous = matthew.Position;
                harness.MoveTo(matthew.Position + new Vector3(0, 0, 1));
                harness.Tick();
                Assert.IsTrue(Vector3.Distance(previous, matthew.Position) < 10,
                    "The escort must move through navigation, not teleport to a completion point.");
            }
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[697].Objectives[1].State,
                $"The grounded cave route did not complete: Matthew={matthew.Position}, Quincy={quincy.Position}.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[697].Objectives[2].State);
            Assert.IsTrue(Vector3.Distance(start, matthew.Position) > 300);
            Assert.IsTrue(Vector3.Distance(infirmary, matthew.Position) < 10);
            Assert.IsTrue(Vector3.Distance(infirmary, quincy.Position) < 0.5f);
            Assert.IsTrue(matthew.Attributes[Attributes.Health].Current > 0);
            Assert.AreNotEqual(CharacterState.Dead, matthew.State);
            WaitForRelease(harness);
            Assert.IsFalse(harness.Manager.PublicActors.TryResolve(harness.Map, lease, out _));

            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                WildernessRanjaGorgeTests.SetQualification(harness, 2, CharacterStartingExperienceState.Completed);
                var returnedMatthew = harness.Npc(201);
                Assert.IsNotNull(returnedMatthew);
                second.SetWorldPosition(returnedMatthew.Position, second.Player.Rotation);
                CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, returnedMatthew.EntityId, 697));
                var secondLease = harness.Manager.PublicActors.Handle(harness.Map, 201);
                Assert.IsNotNull(secondLease);
                Assert.AreNotEqual(lease.RunId, secondLease.RunId);
                Assert.IsFalse(harness.Manager.Scenes.Submit(lease.RunId,
                    new SceneObservation(SceneEventKind.RouteCompleted, lease.Generation,
                        Role: "matthew", OperationKey: "escort-to-quincy")),
                    "A stale completed reservation must not advance its replacement.");
                harness.MoveTo(quincy.Position);
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, quincy.EntityId, 697, 2, 1));
                WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, quincy, 697, 0, 8000, 1600);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, 11567));
                Assert.AreEqual(secondLease, harness.Manager.PublicActors.Handle(harness.Map, 201));
            }
            finally
            {
                harness.Manager.TryAbandon(second, 697);
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        [TestCategory("W6Integrated")]
        [DataRow("death")]
        [DataRow("owner-loss")]
        [DataRow("abandonment")]
        public void InterruptedEscortCannotCompleteAndReleasesItsPublicActor(string interruption)
        {
            using var harness = WildernessRanjaGorgeTests.CreateMigrated();
            var (matthew, _, lease) = BeginEscort(harness);
            if (interruption == "death")
            {
                matthew.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(harness.Map, matthew, harness.Client.Player);
            }
            else if (interruption == "owner-loss")
            {
                CellManager.Instance.RemoveFromWorld(harness.Client);
                harness.Map.ClientList.Remove(harness.Client);
            }
            else
                Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, 697));

            WaitForRelease(harness);
            if (interruption != "abandonment")
                Assert.AreEqual((uint)MissionState.Failed, harness.Context.ReadMission(697).MissionState);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, 11567));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, 3061));
            Assert.IsFalse(harness.Manager.PublicActors.TryResolve(harness.Map, lease, out _));
            var second = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                WildernessRanjaGorgeTests.SetQualification(harness, 2, CharacterStartingExperienceState.Skipped);
                var availableMatthew = harness.Npc(201);
                Assert.IsNotNull(availableMatthew);
                second.SetWorldPosition(availableMatthew.Position, second.Player.Rotation);
                CellManager.Instance.UpdateVisibility(second);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(second, availableMatthew.EntityId, 697));
                Assert.IsNotNull(harness.Manager.PublicActors.Handle(harness.Map, 201));
            }
            finally
            {
                harness.Manager.TryAbandon(second, 697);
                CellManager.Instance.RemoveFromWorld(second);
                harness.Map.ClientList.Remove(second);
            }
        }

        private static (Creature Matthew, Creature Quincy, ActorHandle Lease) BeginEscort(
            WildernessRuntimeTestHarness harness)
        {
            Assert.IsFalse(harness.Map.IsPrivateInstance);
            harness.SpawnWorld(201, 202);
            var matthew = WildernessRanjaGorgeTests.RequireNpc(harness, 201, 123);
            var quincy = WildernessRanjaGorgeTests.RequireNpc(harness, 202, 124);
            Assert.AreEqual(121U, quincy.Npc.NpcPackageId);
            WildernessRanjaGorgeTests.AssertSelectable(WildernessRanjaGorgeTests.Offer(harness, matthew, 697),
                (11567, 6495), (3061, 11893));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, matthew.EntityId, 697));
            var lease = harness.Manager.PublicActors.Handle(harness.Map, 201);
            Assert.IsNotNull(lease);
            Assert.IsTrue(harness.Manager.PublicActors.TryResolve(harness.Map, lease, out var reserved));
            Assert.AreSame(matthew, reserved);
            return (matthew, quincy, lease);
        }

        private static void WaitForRelease(WildernessRuntimeTestHarness harness)
        {
            for (var tick = 0; tick < 1800 && harness.Manager.PublicActors.Handle(harness.Map, 201) != null; tick++)
            {
                harness.SpawnWorldAfter(250, 201);
                harness.Tick();
            }
            Assert.IsNull(harness.Manager.PublicActors.Handle(harness.Map, 201),
                "Matthew's terminal encounter must release its public lease so another character can start.");
        }
    }
}
