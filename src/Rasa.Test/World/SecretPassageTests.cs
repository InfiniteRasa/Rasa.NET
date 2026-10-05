using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Navigation;
    using Rasa.Packets.Protocol;

    [TestClass]
    [DoNotParallelize]
    public class SecretPassageTests
    {
        private const uint Wilderness = SecretPassages.ConcordiaWilderness;

        /// <summary>The Enhance logos (world data, logos 10), which Receptive Reception sends the player to.</summary>
        private static readonly Vector3 EnhanceLogos = new Vector3(832f, 161.838f, 960f);

        private static NavMeshQuery WildernessNavMesh()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Repository root not found.");

            return TestNavMeshes.Query(
                NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), "adv_foreas_concordia_wilderness"));
        }

        private static uint CenterCell(Vector3 position)
        {
            Assert.IsTrue(CellManager.TryGetCellCoordinates(position, out var x, out var z));

            return (x & 0xFFFF) | (z << 16);
        }

        [TestMethod]
        public void TheAliaCavernsDoorwayIsAPassageAndTheCorridorBeforeItIsNot()
        {
            // Walking up the corridor, short of the doorway.
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(835f, 286f, 724f), new Vector3(835f, 286f, 731f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(829f, 286f, 733f), new Vector3(841f, 286f, 737.5f)));

            // Through the doorway: at a walk, along either wall, and in one long jump over the edge.
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(835f, 286f, 737f), new Vector3(835f, 286f, 739f)));
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(827.5f, 286f, 738f), new Vector3(827.5f, 286f, 738.8f)));
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(842.5f, 286f, 738f), new Vector3(842.5f, 286f, 738.8f)));
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(835f, 288f, 736f), new Vector3(835f, 284f, 747f)));

            // The same step on another map is nothing.
            Assert.IsNull(SecretPassages.Crossed(SecretPassages.TordenAbyss, new Vector3(835f, 286f, 737f), new Vector3(835f, 286f, 739f)));
        }

        [TestMethod]
        public void TheEnhanceShrineCorridorEndIsAPassageAndTheShrineIsNot()
        {
            // Around the room and down the corridor, short of its end.
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(832f, 160f, 958f), new Vector3(832f, 160f, 940f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(826f, 160f, 934f), new Vector3(838f, 160f, 921.5f)));

            Assert.AreSame(SecretPassages.EnhanceShrineExit,
                SecretPassages.Crossed(Wilderness, new Vector3(832f, 160f, 922f), new Vector3(832f, 160f, 920f)));
            Assert.AreSame(SecretPassages.EnhanceShrineExit,
                SecretPassages.Crossed(Wilderness, new Vector3(824.5f, 160f, 921f), new Vector3(824.5f, 160f, 920.2f)));
            Assert.AreSame(SecretPassages.EnhanceShrineExit,
                SecretPassages.Crossed(Wilderness, new Vector3(839.5f, 160f, 921f), new Vector3(839.5f, 160f, 920.2f)));

            // The identical rooms above and below it, 64 m apart, are not this one.
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(832f, 224f, 922f), new Vector3(832f, 224f, 920f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(832f, 96f, 922f), new Vector3(832f, 96f, 920f)));
        }

        [TestMethod]
        public void NoPassageEndsInsideAnotherOrWithinAStepOfOne()
        {
            foreach (var passage in SecretPassages.All)
            {
                foreach (var other in SecretPassages.All.Where(other => other.MapContextId == passage.MapContextId))
                {
                    Assert.IsFalse(other.Contains(passage.Destination), $"{passage.Name} ends inside {other.Name}");

                    // Arriving at a run: three metres further on in any direction is still clear.
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = i * MathF.PI / 4f;
                        var step = passage.Destination + new Vector3(MathF.Sin(angle), 0f, MathF.Cos(angle)) * 3f;

                        Assert.IsFalse(other.Touches(passage.Destination, step), $"{passage.Name} ends a step from {other.Name}");
                    }
                }
            }
        }

        [TestMethod]
        public void TheAliaCavernsPassagesEndOnTheFloorFacingTheWayOn()
        {
            var nav = WildernessNavMesh();
            var door = SecretPassages.AliaCavernsDoor;
            var exit = SecretPassages.EnhanceShrineExit;

            foreach (var passage in new[] { door, exit })
            {
                Assert.IsTrue(nav.IsOnMesh(passage.Destination), passage.Name);

                var ground = nav.GroundHeight(passage.Destination);

                Assert.IsNotNull(ground, passage.Name);
                Assert.AreEqual(ground.Value, passage.Destination.Y, 0.35f, passage.Name);
            }

            // In the shrine: the floor runs unbroken to the logos, which is straight ahead (+Z; 0 faces -Z).
            nav.FindPath(door.Destination, new Vector3(EnhanceLogos.X, door.Destination.Y, EnhanceLogos.Z - 5f), out var toLogos);
            Assert.IsTrue(toLogos, "no floor from the shrine's corridor to the logos");
            Assert.AreEqual(EnhanceLogos.X, door.Destination.X, 0.01f);
            Assert.IsTrue(EnhanceLogos.Z > door.Destination.Z);
            Assert.AreEqual(MathF.PI, door.Rotation, 1e-4f);
            Assert.AreEqual(EnhanceLogos.Y, door.Destination.Y, 2f);

            // Back in the cave: the floor runs unbroken down the tunnels (-Z), which is straight ahead.
            nav.FindPath(exit.Destination, new Vector3(835f, 285.2f, 650f), out var toTunnels);
            Assert.IsTrue(toTunnels, "no floor from the cave corridor back down the tunnels");
            Assert.AreEqual(0f, exit.Rotation, 1e-4f);

            // And nothing but the passages joins the two: the reason they exist.
            nav.FindPath(exit.Destination, door.Destination, out var onFoot);
            Assert.IsFalse(onFoot, "the cave corridor and the shrine are joined on foot");
        }

        [TestMethod]
        public void NothingWalkableLiesInsideTheAliaCavernsPassages()
        {
            var nav = WildernessNavMesh();

            foreach (var passage in new[] { SecretPassages.AliaCavernsDoor, SecretPassages.EnhanceShrineExit })
            {
                for (var x = passage.Min.X; x <= passage.Max.X; x += 1f)
                {
                    // Past the first metre, where the corridor's own floor reaches into the slab.
                    var open = passage == SecretPassages.AliaCavernsDoor;
                    var from = open ? passage.Min.Z + 1f : passage.Min.Z;
                    var to = open ? passage.Max.Z : passage.Max.Z - 1f;

                    for (var z = from; z <= to; z += 0.5f)
                    {
                        for (var y = passage.Min.Y; y <= passage.Max.Y; y += 2f)
                        {
                            var hit = nav.NearestInColumn(new Vector3(x, y, z), 2f);

                            if (hit is Vector3 point && MathF.Abs(point.X - x) < 0.3f && MathF.Abs(point.Z - z) < 0.3f)
                                Assert.IsFalse(passage.Contains(point), $"{passage.Name} covers walkable ground at {point}");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void WalkingThroughTheAliaCavernsDoorwayPutsThePlayerInTheShrineAndBackAgain()
        {
            using var world = new WorldTestContext();

            world.Map.NavMesh = WildernessNavMesh();

            var client = world.CreateClient();
            var player = client.Player;

            CellManager.Instance.AddToWorld(client);
            player.PlaceAt(new Vector3(835f, 286.2f, 734f));
            player.MoveBudget = 60;
            WorldTestContext.Drain(client);

            // Up the corridor: an ordinary step.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 737f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(835f, 286.2f, 737f), player.Position);

            // Through the doorway.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 739f), Vector2.Zero)));
            Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, player.Position);
            Assert.AreEqual(MathF.PI, player.Rotation, 1e-4f);

            var told = WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<MoveObjectMessage>().ToList();

            Assert.IsTrue(told.Count > 0, "the player's own client was not moved");
            Assert.AreEqual(CenterCell(SecretPassages.AliaCavernsDoor.Destination), player.Cells[2, 2], "the player still sees the cave's cells");

            // To the logos and back to the corridor's end, as ordinary steps.
            player.MoveBudget = 60;
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 933f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(832f, 160.1f, 933f), player.Position);
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 922f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(832f, 160.1f, 922f), player.Position);

            // Out through the corridor's open end.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 920f), Vector2.Zero)));
            Assert.AreEqual(SecretPassages.EnhanceShrineExit.Destination, player.Position);
            Assert.AreEqual(0f, player.Rotation, 1e-4f);
            Assert.AreEqual(CenterCell(SecretPassages.EnhanceShrineExit.Destination), player.Cells[2, 2], "the player still sees the shrine's cells");

            // And on down the tunnels.
            player.MoveBudget = 60;
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.3f, 729f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(835f, 286.3f, 729f), player.Position);
        }
    }
}
