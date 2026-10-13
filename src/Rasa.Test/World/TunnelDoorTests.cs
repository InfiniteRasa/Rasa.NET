using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// The Bane tunnel mouths get their doors from the server, closed, and a door opens as a
    /// player comes up to it and closes once nobody has been near for a while (TunnelDoors).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TunnelDoorTests
    {
        private long _now;
        private readonly List<(MapChannel Map, DynamicObject Door)> _placed = new();

        [TestInitialize]
        public void Start()
        {
            _now = 5_000_000;
            TunnelDoors.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            foreach (var (map, door) in _placed)
            {
                CellManager.Instance.RemoveFromWorld(map, door);
                TunnelDoors.Forget(map);
            }

            TunnelDoors.Now = () => Environment.TickCount64;
        }

        private static MapChannel Divide() => new MapChannel
        {
            MapInfo = new MapInfo(1148, "adv_foreas_concordia_divide", 1584, 10),
            ClientList = new List<Rasa.Game.Client>(),
            PlayerLimit = 128
        };

        /// <summary>The Timora Mines mouth on Concordia Divide, turned half about.</summary>
        private static TunnelDoors.Tunnel Timora => TunnelDoors.All.Single(tunnel => tunnel.MouthEntityId == 132770324748883);

        private List<DynamicObject> Place(WorldTestContext world, MapChannel map)
        {
            world.AddClass(TunnelDoors.DoorClass);
            TunnelDoors.Place(map);

            var doors = TunnelDoors.OnMap(map.MapInfo.MapContextId).Select(tunnel => TunnelDoors.DoorOf(map, tunnel)).Where(door => door != null).ToList();
            foreach (var door in doors)
                _placed.Add((map, door));
            return doors;
        }

        /// <summary>A player on the map at <paramref name="position"/>, in its cells so what goes to those in range reaches them.</summary>
        private static Rasa.Game.Client At(WorldTestContext world, MapChannel map, Vector3 position)
        {
            var client = world.CreateClient();
            world.Map.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.MapContextId = map.MapInfo.MapContextId;
            client.Player.Position = position;
            map.ClientList.Add(client);

            var seed = CellManager.Instance.GetCellSeed(position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static List<UsePacket> Uses(Rasa.Game.Client client, DynamicObject door) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == door.EntityId).Select(message => message.Packet).OfType<UsePacket>().ToList();

        [TestMethod]
        public void TheTwentyTwoMouthsAreTheMapsAndEachDoorIsAtItsMouthOnItsAxis()
        {
            Assert.HasCount(22, TunnelDoors.All);
            Assert.AreEqual(22, TunnelDoors.All.Select(tunnel => tunnel.MouthEntityId).Distinct().Count());
            Assert.AreEqual(15, TunnelDoors.All.Select(tunnel => tunnel.MapContextId).Distinct().Count());

            // Turned half about, the door's middle is 15.5 m along +Z from the origin.
            var middle = Timora.DoorMiddle;
            Assert.AreEqual(-574f, middle.X, 0.01f);
            Assert.AreEqual(187f + 9.1f, middle.Y, 0.01f);
            Assert.AreEqual(-1133f + 15.5f, middle.Z, 0.01f);

            // Turned a quarter, along +X: the Torcastra Prison mouth on Concordia Divide.
            var torcastra = TunnelDoors.All.Single(tunnel => tunnel.MouthEntityId == 132770324747669).DoorMiddle;
            Assert.AreEqual(184f - 15.5f, torcastra.X, 0.01f);
            Assert.AreEqual(-1028f, torcastra.Z, 0.01f);

            // The way into Timora Mines is in the tunnel, in reach of the door (map_link: -574.2, 188.6, -1119.5).
            Assert.IsTrue(TunnelDoors.IsNear(Timora, new Vector3(-574.2f, 188.6f, -1119.5f)));
        }

        [TestMethod]
        public void ADoorIsPutInEachMouthOfTheMapClosedAndOnlyOnce()
        {
            using var world = new WorldTestContext();
            var map = Divide();

            var doors = Place(world, map);

            Assert.HasCount(2, doors);
            foreach (var door in doors)
            {
                Assert.AreEqual(TunnelDoors.DoorClass, door.EntityClassId);
                Assert.AreEqual(DynamicObjectType.Scenery, door.DynamicObjectType);
                Assert.AreEqual(UseObjectState.DoorStateClosed, door.StateId);
            }

            var timora = TunnelDoors.DoorOf(map, Timora);
            Assert.AreEqual(Timora.Position, timora.Position);
            Assert.AreEqual(Math.PI, timora.Rotation, 1e-5);

            Assert.AreEqual(0, TunnelDoors.Place(map), "a map that has them is left alone");
        }

        [TestMethod]
        public void APlayerComingUpOpensItAndItClosesOnceNobodyHasBeenNearForLongEnough()
        {
            using var world = new WorldTestContext();
            var map = Divide();
            Place(world, map);
            var door = TunnelDoors.DoorOf(map, Timora);

            var watcher = At(world, map, Timora.DoorMiddle + new Vector3(30f, -9f, 0f));
            var walker = At(world, map, Timora.DoorMiddle + new Vector3(0f, -9f, 40f));

            TunnelDoors.Worker(map);
            Assert.AreEqual(UseObjectState.DoorStateClosed, door.StateId, "nobody near");

            walker.Player.Position = Timora.DoorMiddle + new Vector3(0f, -8f, 10f);
            TunnelDoors.Worker(map);

            Assert.AreEqual(UseObjectState.DoorStateOpen, door.StateId);
            var open = Uses(watcher, door).Single();
            Assert.AreEqual((walker.Player.EntityId, UseObjectState.DoorStateOpen), (open.PlayerEntityId, open.CurState), "everyone in range is shown it open");
            Assert.AreEqual(UseObjectState.DoorStateOpen, Uses(walker, door).Single().CurState);

            // Through it and on, out of reach: open a while longer.
            walker.Player.Position = Timora.DoorMiddle + new Vector3(0f, -8f, -40f);
            _now += 1000;
            TunnelDoors.Worker(map);
            _now += TunnelDoors.CloseAfterMs - 1001;
            TunnelDoors.Worker(map);
            Assert.AreEqual(UseObjectState.DoorStateOpen, door.StateId);
            Assert.IsEmpty(Uses(watcher, door));

            _now += 1;
            TunnelDoors.Worker(map);
            Assert.AreEqual(UseObjectState.DoorStateClosed, door.StateId);
            Assert.AreEqual((walker.Player.EntityId, UseObjectState.DoorStateClosed), (Uses(watcher, door).Single().PlayerEntityId, door.StateId));

            // Closed and left alone, it is not told again.
            _now += 10_000;
            TunnelDoors.Worker(map);
            Assert.IsEmpty(Uses(watcher, door));
        }

        [TestMethod]
        public void SomeoneStandingInItKeepsItOpenAndTheDeadOpenNothing()
        {
            using var world = new WorldTestContext();
            var map = Divide();
            Place(world, map);
            var door = TunnelDoors.DoorOf(map, Timora);

            var dead = At(world, map, Timora.DoorMiddle + new Vector3(0f, -8f, 0f));
            dead.Player.State = CharacterState.Dead;
            TunnelDoors.Worker(map);
            Assert.AreEqual(UseObjectState.DoorStateClosed, door.StateId, "the dead open nothing");

            var standing = At(world, map, Timora.DoorMiddle + new Vector3(2f, -8f, 0f));
            TunnelDoors.Worker(map);
            Assert.AreEqual(UseObjectState.DoorStateOpen, door.StateId);

            _now += TunnelDoors.CloseAfterMs * 5;
            TunnelDoors.Worker(map);
            Assert.AreEqual(UseObjectState.DoorStateOpen, door.StateId, "not closed on somebody");

            // Too high above it, on a ridge over the tunnel, opens nothing.
            Assert.IsFalse(TunnelDoors.IsNear(Timora, Timora.DoorMiddle + new Vector3(0f, TunnelDoors.ReachHeight + 1f, 0f)));
            Assert.IsFalse(TunnelDoors.IsNear(Timora, Timora.DoorMiddle + new Vector3(TunnelDoors.OpenRadius + 0.5f, 0f, 0f)));
        }

        [TestMethod]
        public void AClientComingIntoRangeIsShownTheDoorAsItIs()
        {
            using var world = new WorldTestContext();
            var map = Divide();
            Place(world, map);
            var door = TunnelDoors.DoorOf(map, Timora);

            At(world, map, Timora.DoorMiddle + new Vector3(0f, -8f, 5f));
            TunnelDoors.Worker(map);

            var later = At(world, map, Timora.DoorMiddle + new Vector3(20f, -8f, 0f));
            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(later, door);

            var create = WorldTestContext.Drain(later).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<CreatePhysicalEntityPacket>().Single();
            Assert.AreEqual((uint)TunnelDoors.DoorClass, (uint)create.ClassId);
            var info = create.EntityData.OfType<UsableInfoPacket>().Single();
            Assert.AreEqual(UseObjectState.DoorStateOpen, info.CurState);
            Assert.IsFalse(info.Enabled, "out of service: no Use offered");
            Assert.IsFalse(create.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable);
        }
    }
}
