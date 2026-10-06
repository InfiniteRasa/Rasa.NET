using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Navigation;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

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
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(829f, 286f, 733f), new Vector3(841f, 286f, 737f)));

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
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(826f, 160f, 934f), new Vector3(838f, 160f, 922f)));

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
                    // Past the first 1.7 m, where the corridor's own floor reaches into the slab.
                    var open = passage == SecretPassages.AliaCavernsDoor;
                    var from = open ? passage.Min.Z + 1.7f : passage.Min.Z;
                    var to = open ? passage.Max.Z : passage.Max.Z - 1.7f;

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
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 736.5f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(835f, 286.2f, 736.5f), player.Position);

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
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 922.5f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(832f, 160.1f, 922.5f), player.Position);

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

        private static void AssertNear(Vector3 expected, Vector3 actual, float tolerance, string what)
        {
            Assert.IsTrue(Vector3.Distance(expected, actual) <= tolerance, $"{what}: expected {expected}, was {actual}");
        }

        private static List<PythonPacket> Methods(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static void Remove(MapChannel map)
        {
            foreach (var doorway in SecretPassages.Doorways)
                if (SecretPassages.DoorwayObject(map, doorway) is { } obj)
                    CellManager.Instance.RemoveFromWorld(map, obj);
        }

        [TestMethod]
        public void EachWildernessDoorwayStandsMouthToItsCorridorJustInsideTheEnd()
        {
            // The corridors' ends are at z 739.14 and z 919.86, their floors at y 286.0 and y 159.93.
            AssertNear(new Vector3(835f, 286f, 738.7f), SecretPassages.AliaCavernsDoorway.Mouth, 0.01f, "the cave doorway's mouth");
            AssertNear(new Vector3(832f, 159.93f, 920.3f), SecretPassages.EnhanceShrineDoorway.Mouth, 0.01f, "the shrine doorway's mouth");

            Assert.AreSame(SecretPassages.AliaCavernsDoor, SecretPassages.AliaCavernsDoorway.Passage);
            Assert.AreSame(SecretPassages.EnhanceShrineExit, SecretPassages.EnhanceShrineDoorway.Passage);

            foreach (var doorway in SecretPassages.Doorways)
            {
                Assert.AreEqual(SecretPassages.CavernTransition, doorway.ClassId, doorway.Name);
                Assert.IsTrue(doorway.Passage.Contains(doorway.Mouth), $"{doorway.Name} is not in its passage");

                // Out of the stub and into the corridor, level; and across the corridor.
                var back = doorway.Mouth - doorway.Position;
                back.Y = 0f;
                back = Vector3.Normalize(back);
                var across = new Vector3(back.Z, 0f, -back.X);
                var head = new Vector3(0f, 0.3f, 0f);

                for (var side = -7f; side <= 7f; side += 1f)
                {
                    var at = doorway.Mouth + across * side + head;

                    // Walking at the doorway anywhere across it: through 1.2 m before the rock of the rim.
                    Assert.AreSame(doorway.Passage,
                        SecretPassages.Crossed(doorway.MapContextId, at + back * 1.3f, at + back * 1.15f), $"{doorway.Name}, {side} m across");
                    Assert.IsNull(SecretPassages.Crossed(doorway.MapContextId, at + back * 3f, at + back * 1.3f), $"{doorway.Name}, {side} m across");
                }

                // Whoever arrives in this corridor comes out of the other passage, clear of the stub.
                var arriving = SecretPassages.All.Single(passage => passage != doorway.Passage && passage.MapContextId == doorway.MapContextId &&
                    Vector3.Distance(passage.Destination, doorway.Mouth) < 20f);

                Assert.IsTrue(Vector3.Dot(arriving.Destination - doorway.Mouth, back) > 5f, $"{arriving.Name} ends too near {doorway.Name}");
            }
        }

        [TestMethod]
        public void TheDoorwaysArePlacedOnceOnTheirOwnMapAndNowhereElse()
        {
            using var world = new WorldTestContext();

            world.AddClass(SecretPassages.CavernTransition);

            var torden = new MapChannel
            {
                MapInfo = new MapInfo(SecretPassages.TordenAbyss, "adv_arieki_torden_abyss", 1556, 0),
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128
            };

            try
            {
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(null));
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(torden));
                Assert.AreEqual(0, torden.MapCellInfo.Cells.Count);

                Assert.AreEqual(2, SecretPassages.PlaceDoorways(world.Map));
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(world.Map), "placed a second time");

                foreach (var doorway in SecretPassages.Doorways)
                {
                    var obj = SecretPassages.DoorwayObject(world.Map, doorway);

                    Assert.IsNotNull(obj, doorway.Name);
                    Assert.AreEqual(DynamicObjectType.Scenery, obj.DynamicObjectType);
                    Assert.AreEqual(SecretPassages.CavernTransition, obj.EntityClassId);
                    Assert.AreEqual(doorway.Position, obj.Position);
                    Assert.AreEqual(doorway.Yaw, obj.Rotation, 1e-6);
                    Assert.AreEqual(Wilderness, obj.MapContextId);
                    Assert.AreSame(world.Map, obj.RuntimeMapChannel);
                    Assert.AreEqual(1, world.Map.MapCellInfo.Cells.Values.Sum(cell => cell.DynamicObjectList.Count(o => ReferenceEquals(o, obj))));
                }
            }
            finally
            {
                Remove(world.Map);
            }

            Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway));
        }

        [TestMethod]
        public void ADoorwayWhoseClassIsNotLoadedIsLeftOut()
        {
            using var world = new WorldTestContext();

            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var had = classes.Remove(SecretPassages.CavernTransition, out var kept);

            try
            {
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(world.Map));
                Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway));
                Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.EnhanceShrineDoorway));
            }
            finally
            {
                Remove(world.Map);

                if (had)
                    classes[SecretPassages.CavernTransition] = kept;
            }
        }

        [TestMethod]
        public void APlayerIsShownTheDoorwayInViewAsUntargetableSceneryAndTheOtherOnArrival()
        {
            using var world = new WorldTestContext();

            // A class the world data marks targetable would still be sent as not: scenery is never a target.
            world.AddClass(SecretPassages.CavernTransition);

            try
            {
                Assert.AreEqual(2, SecretPassages.PlaceDoorways(world.Map));

                var cave = SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway);
                var shrine = SecretPassages.DoorwayObject(world.Map, SecretPassages.EnhanceShrineDoorway);
                var client = world.CreateClient();
                var player = client.Player;

                player.PlaceAt(new Vector3(835f, 286.2f, 734f));
                CellManager.Instance.AddToWorld(client);

                var seen = Methods(client);
                var made = seen.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == cave.EntityId);

                Assert.AreEqual(SecretPassages.CavernTransition, made.ClassId);
                Assert.AreEqual(2, made.EntityData.Count, "scenery is sent a target flag and a place, and nothing a usable is");
                Assert.IsFalse(((IsTargetablePacket)made.EntityData[0]).IsTargetable);

                var where = (WorldLocationDescriptorPacket)made.EntityData[1];

                Assert.AreEqual(new Vector3(835f, 290.5f, 747f), where.Position);
                Assert.IsTrue(MathF.Abs(Quaternion.Dot(where.Rotation, Quaternion.Identity)) > 0.99999f, $"the cave doorway is turned: {where.Rotation}");

                // 181 m off: not in view from the cave.
                Assert.IsFalse(seen.OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == shrine.EntityId));

                // Through the doorway: the shrine's is there on arrival, turned about, and the cave's is gone.
                player.MoveBudget = 60;
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 738f), Vector2.Zero)));
                Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, player.Position);

                seen = Methods(client);
                made = seen.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == shrine.EntityId);
                where = (WorldLocationDescriptorPacket)made.EntityData[1];

                Assert.AreEqual(SecretPassages.CavernTransition, made.ClassId);
                Assert.AreEqual(new Vector3(832f, 164.43f, 912f), where.Position);
                Assert.IsTrue(MathF.Abs(Quaternion.Dot(where.Rotation, new Quaternion(0f, 1f, 0f, 0f))) > 0.99999f, $"the shrine doorway is not turned about: {where.Rotation}");
                Assert.IsTrue(seen.OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == cave.EntityId), "the cave doorway was left on the client");
            }
            finally
            {
                Remove(world.Map);
            }
        }

        [TestMethod]
        public void ADoorwayIsShownFromTheWholeLengthOfTheWayToIt()
        {
            using var world = new WorldTestContext();

            world.AddClass(SecretPassages.CavernTransition);

            try
            {
                Assert.AreEqual(2, SecretPassages.PlaceDoorways(world.Map));

                var cave = SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway);
                var shrine = SecretPassages.DoorwayObject(world.Map, SecretPassages.EnhanceShrineDoorway);

                // Filed by where it is seen from, sent as standing where it stands.
                Assert.AreEqual(new Vector3(835f, 286f, 705f), cave.CellPosition);
                Assert.AreEqual(new Vector3(835f, 290.5f, 747f), cave.Position);
                Assert.AreEqual(new Vector3(832f, 160f, 960f), shrine.CellPosition);

                bool Shown(Vector3 at, DynamicObject obj)
                {
                    var client = world.CreateClient();

                    client.Player.PlaceAt(at);
                    CellManager.Instance.AddToWorld(client);

                    var shown = Methods(client).OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == obj.EntityId);

                    Assert.AreEqual(shown, CellManager.Instance.ClientsSeeing(world.Map, obj).Contains(client));

                    return shown;
                }

                // The tunnel runs straight at the cave's doorway from z 647, 92 m off; its own cell would give it at 48 m.
                Assert.IsTrue(Shown(new Vector3(835f, 285.2f, 648f), cave), "at the far end of the straight tunnel");
                Assert.IsTrue(Shown(new Vector3(835f, 286f, 695f), cave), "at the corridor's coupler");
                Assert.IsTrue(Shown(new Vector3(835f, 286.2f, 737f), cave), "at the doorway");
                Assert.IsFalse(Shown(new Vector3(835f, 280f, 600f), cave), "outside the cave");

                // The shrine: from its corridor's end to the back wall of the room, 65 m.
                Assert.IsTrue(Shown(new Vector3(832f, 160.1f, 922f), shrine), "at the shrine corridor's end");
                Assert.IsTrue(Shown(new Vector3(832f, 160.1f, 984f), shrine), "at the back of the shrine");
                Assert.IsFalse(Shown(new Vector3(832f, 160.1f, 984f), cave));

                // Taken away again through the same cell it was filed in.
                CellManager.Instance.RemoveFromWorld(world.Map, cave);
                Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway));
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.DynamicObjectList.Contains(cave)));
            }
            finally
            {
                Remove(world.Map);
            }
        }

        [TestMethod]
        public void APrivateCopyOfTheMapHasItsOwnDoorways()
        {
            using var world = new WorldTestContext();

            world.AddClass(SecretPassages.CavernTransition);

            var maps = new MapChannelManager(null, privateInstances: new PrivateMapInstanceService());

            maps.MapChannelArray.Add(Wilderness, world.Map);

            MapChannel copy = null;

            try
            {
                Assert.AreEqual(2, SecretPassages.PlaceDoorways(world.Map));

                copy = maps.GetOrCreatePrivateInstance(Wilderness, 7);

                Assert.IsNotNull(copy);
                Assert.AreNotSame(world.Map, copy);

                foreach (var doorway in SecretPassages.Doorways)
                {
                    var open = SecretPassages.DoorwayObject(world.Map, doorway);
                    var own = SecretPassages.DoorwayObject(copy, doorway);

                    Assert.IsNotNull(own, doorway.Name);
                    Assert.AreNotSame(open, own);
                    Assert.AreNotEqual(open.EntityId, own.EntityId);
                    Assert.AreSame(copy, own.RuntimeMapChannel);
                    Assert.AreEqual(doorway.Position, own.Position);
                }
            }
            finally
            {
                Remove(copy);
                Remove(world.Map);
            }
        }
    }
}
