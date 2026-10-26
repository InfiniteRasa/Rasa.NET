using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// /stuck out of a pocket of navmesh inside a hill in Lower Eloh Creek, where a knockback had
    /// thrown a player: "unstuck from (343.52, 226, 194.99) to (343.54, 226.12, 194.64) (0.4 m)",
    /// still inside the hill. The pocket is 8 m under the terrain (234.0) and joined to nothing.
    /// And /stuck is an action with its windup, the wave, and its recovery (Unstick).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class UnstickTests
    {
        private static readonly Vector3 Stuck = new Vector3(343.52344f, 226f, 194.98828f);

        /// <summary>The Wilderness harness, with the map's height range set as NavMeshManager sets it when it loads the mesh.</summary>
        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            var (lowest, highest) = harness.Map.NavMesh.HeightRange().Value;
            harness.Map.SafetyFloorY = SafetyFloor.FloorFor(lowest);
            harness.Map.TopWalkableY = highest;
            return harness;
        }

        [TestMethod]
        public void OutOfThePocketTheyGoUpToTheGroundNotAStepAlongIt()
        {
            using var harness = Create();
            var map = harness.Map;
            var nav = map.NavMesh;

            var nearest = NavMeshManager.NearestWalkable(map, Stuck).Value;
            Assert.IsTrue(Vector3.Distance(Stuck, nearest) < 1f, "the nearest walkable point is the pocket itself");
            Assert.IsTrue(nav.IsUnderground(nearest), $"under the terrain at {nearest}");

            var destination = NavMeshManager.UnstickDestination(map, Stuck).Value;
            Assert.IsFalse(nav.IsUnderground(destination), $"{destination} is on the ground");
            Assert.IsTrue(destination.Y > 233f && Vector3.Distance(Stuck, destination) < 15f, $"up out of the hill, not far: {destination}");

            // Somewhere ordinary, on the mesh: clear ground a few metres off, on the same floor.
            var open = new Vector3(615f, 284f, 737f);
            var near = NavMeshManager.NearestWalkable(map, open).Value;
            var moved = NavMeshManager.UnstickDestination(map, open).Value;
            Assert.IsTrue(Vector3.Distance(near, moved) >= NavMeshManager.UnstickNearest - 0.1f &&
                Vector3.Distance(near, moved) <= NavMeshManager.UnstickFarthest + 0.1f, $"{near} to {moved}");
            Assert.AreEqual(true, nav.Reachable(near, moved));
            Assert.IsTrue(Math.Abs(moved.Y - near.Y) < 2f);

            // Off the mesh, the nearest walkable point.
            var high = near + new Vector3(0, 6f, 0);
            Assert.AreEqual(NavMeshManager.NearestWalkable(map, high), NavMeshManager.UnstickDestination(map, high));
        }

        /// <summary>
        /// The cave under Pinhole Falls is joined to the ground over it: /stuck in it stays in it,
        /// on clear floor a few metres from the corner where the sandbags meet the rock, not 55 m
        /// up on the hill.
        /// </summary>
        [TestMethod]
        public void InACaveTheyStayInTheCaveOnClearGround()
        {
            using var harness = Create();
            var map = harness.Map;
            var nav = map.NavMesh;
            var corner = new Vector3(336.2f, 228.8f, 476.0f);

            var nearest = NavMeshManager.NearestWalkable(map, corner).Value;
            Assert.IsTrue(nav.IsUnderground(nearest), "a cave");
            var hill = SafetyFloor.TopOfColumn(map, corner).Value;
            Assert.IsTrue(hill.Y > 280f && nav.Reachable(nearest, hill) == true, $"the hill {hill} is joined to the cave");

            var destination = NavMeshManager.UnstickDestination(map, corner).Value;
            Assert.IsTrue(nav.IsUnderground(destination), $"still in the cave: {destination}");
            Assert.IsTrue(Math.Abs(destination.Y - nearest.Y) < 2f, $"on its floor: {destination}");
            var distance = Vector3.Distance(nearest, destination);
            Assert.IsTrue(distance >= NavMeshManager.UnstickNearest - 0.1f && distance <= NavMeshManager.UnstickFarthest + 0.1f, $"{distance:F1} m");
            Assert.IsTrue(nav.DistanceToWall(destination, NavMeshManager.UnstickClearance) > nav.DistanceToWall(nearest, NavMeshManager.UnstickClearance),
                "clearer than the corner");
            Assert.IsNull(StuckSpots.At(1220, destination), "out of the corner");
        }

        /// <summary>A knockback goes along the mesh from where they stand, never onto mesh it does not join.</summary>
        [TestMethod]
        public void AKnockbackStaysOnTheMeshItStartedOn()
        {
            using var harness = Create();
            var map = harness.Map;
            var nav = map.NavMesh;
            var off = 0;
            var throws = 0;

            for (var dx = -10; dx <= 10; dx += 2)
                for (var dz = -10; dz <= 10; dz += 2)
                {
                    // From the ground, where a player walks.
                    var start = SafetyFloor.TopOfColumn(map, new Vector3(Stuck.X + dx, Stuck.Y, Stuck.Z + dz));

                    if (start == null || nav.IsUnderground(start.Value))
                        continue;

                    for (var step = 0; step < 16; step++)
                    {
                        var angle = step * MathF.PI / 8f;
                        var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
                        throws++;

                        if (nav.Connected(start.Value, OldDestination(nav, start.Value, dir, 10f)) == false)
                            off++;

                        var end = CrowdControl.KnockbackDestination(map, start.Value, dir, 10f);
                        Assert.AreNotEqual(false, nav.Connected(start.Value, end), $"from {start} along {dir}: {end}");
                    }
                }

            Assert.IsTrue(throws > 100, $"{throws} throws");
            Assert.IsTrue(off > 0, "the way it was, some of them left it");
        }

        [TestMethod]
        public void TheWindupRunsAndThenTheyAreMovedAndTheRequestResolved()
        {
            using var harness = Create();
            var player = harness.Client.Player;
            player.State = CharacterState.Idle;
            player.PlaceAt(Stuck);
            harness.Drain();

            Unstick.Request(harness.Client);

            Assert.AreEqual(Stuck, player.Position, "not before the windup has run");
            var pending = harness.Map.PerformRecovery.Single(a => a.Actor == player && a.ActionId == ActionId.Unstick);
            Assert.AreEqual((long)Unstick.WindupMs, pending.WaitTime);
            Assert.IsTrue(pending.StunInterrupts);
            Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any());

            // Asked again during the windup, or the minute after: refused.
            Unstick.Request(harness.Client);
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(1, harness.Map.PerformRecovery.Count(a => a.Actor == player && a.ActionId == ActionId.Unstick));

            harness.Map.PerformRecovery.Remove(pending);
            ActorActionManager.Instance.PerformRecovery(harness.Map, pending);

            Assert.IsTrue(!harness.Map.NavMesh.IsUnderground(player.Position) && player.Position.Y > 233f, $"out of the hill: {player.Position}");
            var sent = harness.Drain();
            Assert.AreEqual(ActionId.Unstick, sent.OfType<PerformRecoveryPacket>().Single().ActionId, "the request resolved");
            Assert.IsTrue(sent.OfType<DisplayClientMessagePacket>().Any(p => p.MsgId == PlayerMessage.PmStuckBugReportSent));
            Assert.IsTrue(player.ActionReuseUntil[ActionId.Unstick] > Environment.TickCount64 + Unstick.ReuseMs - 5000);

            Unstick.Request(harness.Client);
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
        }

        /// <summary>The log says where they were stuck: the position and the map it is on.</summary>
        [TestMethod]
        public void TheLogSaysWhichMapTheyWereStuckOn()
        {
            using var harness = Create();
            var player = harness.Client.Player;
            player.State = CharacterState.Idle;
            player.PlaceAt(Stuck);
            harness.Drain();

            var previousOutput = Console.Out;
            var previousLogging = Logger.Config;
            using var output = new StringWriter();
            Logger.UpdateConfig(new Logger.LoggerConfig { IsDebugMode = true, LogToFile = false });
            global::Rasa.Logger.Flush();
            Console.SetOut(output);

            try
            {
                Unstick.Request(harness.Client);
                var pending = harness.Map.PerformRecovery.Single(a => a.Actor == player && a.ActionId == ActionId.Unstick);
                harness.Map.PerformRecovery.Remove(pending);
                ActorActionManager.Instance.PerformRecovery(harness.Map, pending);
                global::Rasa.Logger.Flush();
            }
            finally
            {
                Console.SetOut(previousOutput);
                Logger.UpdateConfig(previousLogging);
            }

            var line = output.ToString().Split('\n').Single(l => l.Contains($"Character {player.Id} unstuck from"));
            StringAssert.Contains(line, $"from {Stuck} on map {harness.Map.MapInfo.MapContextId} to {player.Position}");
        }

        [TestMethod]
        public void AnInterruptedOrDeadUnstickMovesNobodyAndCostsNoTime()
        {
            using var harness = Create();
            var player = harness.Client.Player;
            player.State = CharacterState.Idle;
            player.PlaceAt(Stuck);
            harness.Drain();

            Unstick.Request(harness.Client);
            var pending = harness.Map.PerformRecovery.Single(a => a.Actor == player && a.ActionId == ActionId.Unstick);
            pending.IsInrerrupted = true;
            harness.Map.PerformRecovery.Remove(pending);
            ActorActionManager.Instance.PerformRecovery(harness.Map, pending);

            Assert.AreEqual(Stuck, player.Position);
            var told = harness.Drain().ToList();
            var interrupt = told.OfType<ActionInterruptPacket>().Single();
            Assert.AreEqual(ActionId.Unstick, interrupt.ActionId);
            var closed = told.OfType<UserActionFailedPacket>().Single();
            Assert.IsNull(closed.MsgId, "closed, silently");
            Assert.IsTrue(told.IndexOf(interrupt) < told.IndexOf(closed), "\"Interrupted\" over them, then the request closed");
            Assert.IsFalse(player.ActionReuseUntil.ContainsKey(ActionId.Unstick), "they may try again");

            player.State = CharacterState.Dead;
            Unstick.Request(harness.Client);
            Assert.AreEqual(PlayerMessage.PmCannotDoThatWhileDead, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.IsFalse(harness.Map.PerformRecovery.Any(a => a.Actor == player && a.ActionId == ActionId.Unstick));
        }

        /// <summary>The knockback as it was: each step's ground the nearest walkable point to the line, joined to the last or not.</summary>
        private static Vector3 OldDestination(Rasa.Navigation.NavMeshQuery nav, Vector3 from, Vector3 dir, float distance)
        {
            var end = from;

            for (var d = 0.5f; d <= distance + 0.001f; d += 0.5f)
            {
                var probe = from + dir * d;
                var ground = nav.Nearest(probe);

                if (ground == null || new Vector2(ground.Value.X - probe.X, ground.Value.Z - probe.Z).Length() > 1.0f)
                    break;

                end = ground.Value;
            }

            return end;
        }
    }
}
