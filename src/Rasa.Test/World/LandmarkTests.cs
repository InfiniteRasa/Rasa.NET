using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A server object with a view radius is given to every client within that radius of it,
    /// wherever its cells are, and kept, relayed to, made anew and taken away as it should be
    /// (Landmarks). Without it the Proving Grounds' cave-in was not there until the player was
    /// halfway over the bridge, the doorway it fills seen open from the far end.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LandmarkTests
    {
        /// <summary>UsableInertDestTerraCaveinV02, the Proving Grounds' pile.</summary>
        private const EntityClasses CaveInClass = (EntityClasses)29434;

        [TestMethod]
        public void AClientWithinItsRadiusIsGivenItBeyondItsCellsAndAnOrdinaryObjectIsNot()
        {
            using var world = new WorldTestContext();
            var landmark = Place(world, new Vector3(0, 0, 0), 150);
            var ordinary = Place(world, new Vector3(0, 0, 5), 0);
            var far = Client(world, 100);

            Assert.IsFalse(CellManager.Instance.GetClientsInCells(world.Map, landmark.Cells()).Contains(far), "beyond its cells");

            Landmarks.Worker(world.Map);

            var created = Sent(far).OfType<CreatePhysicalEntityPacket>().ToList();
            Assert.HasCount(1, created);
            Assert.AreEqual(landmark.EntityId, created.Single().EntityId);
            Assert.IsTrue(Landmarks.HeldFromAfar(far, landmark));
            Assert.IsFalse(Landmarks.HeldFromAfar(far, ordinary));

            Landmarks.Worker(world.Map);
            Assert.IsEmpty(Sent(far), "given once");
        }

        [TestMethod]
        public void WhatItsCellsAreToldOneHoldingItFromAfarIsToldToo()
        {
            using var world = new WorldTestContext();
            var landmark = Place(world, new Vector3(0, 0, 0), 150);
            var far = Client(world, 100);
            Landmarks.Worker(world.Map);
            WorldTestContext.Drain(far);

            CellManager.Instance.CellCallMethod(landmark, new ForceStatePacket(UseObjectState.DoorStateOpen, 1000));

            var told = WorldTestContext.Drain(far).Select(p => p.Message).OfType<CallMethodMessage>().Single();
            Assert.AreEqual(landmark.EntityId, told.EntityId);
            Assert.AreEqual(UseObjectState.DoorStateOpen, ((ForceStatePacket)told.Packet).State, "the rocks blown, seen from across the bridge");
        }

        [TestMethod]
        public void WalkingIntoItsCellsMakesItAnewAndWalkingOutKeepsIt()
        {
            using var world = new WorldTestContext();
            var landmark = Place(world, new Vector3(0, 0, 0), 150);
            var client = Client(world, 100);
            Landmarks.Worker(world.Map);
            WorldTestContext.Drain(client);

            Walk(client, 10);

            var packets = Sent(client);
            var destroyed = packets.FindIndex(p => p is DestroyPhysicalEntityPacket d && d.EntityId == landmark.EntityId);
            var created = packets.FindIndex(p => p is CreatePhysicalEntityPacket c && c.EntityId == landmark.EntityId);
            Assert.IsGreaterThanOrEqualTo(0, destroyed, "taken off");
            Assert.IsGreaterThan(destroyed, created, "and made anew, not updated");
            Assert.IsFalse(Landmarks.HeldFromAfar(client, landmark), "held through its cells now");

            Walk(client, 100);

            Assert.IsFalse(Sent(client).Any(p => p is DestroyPhysicalEntityPacket d && d.EntityId == landmark.EntityId), "kept as its cells go");
            Assert.IsTrue(Landmarks.HeldFromAfar(client, landmark));
        }

        [TestMethod]
        public void ItIsTakenAwayOncePastItsRadiusAndALittleMore()
        {
            using var world = new WorldTestContext();
            var landmark = Place(world, new Vector3(0, 0, 0), 150);
            var client = Client(world, 100);
            Landmarks.Worker(world.Map);
            WorldTestContext.Drain(client);

            Walk(client, 155);
            Landmarks.Worker(world.Map);
            Assert.IsTrue(Landmarks.HeldFromAfar(client, landmark), "within the margin: no blinking at the edge");
            Assert.IsEmpty(Sent(client).OfType<DestroyPhysicalEntityPacket>().ToList());

            Walk(client, 170);
            Landmarks.Worker(world.Map);
            Assert.IsFalse(Landmarks.HeldFromAfar(client, landmark));
            Assert.AreEqual(landmark.EntityId, Sent(client).OfType<DestroyPhysicalEntityPacket>().Single().EntityId);

            Walk(client, 140);
            Landmarks.Worker(world.Map);
            Assert.IsTrue(Landmarks.HeldFromAfar(client, landmark), "and given again on coming back");
        }

        [TestMethod]
        public void TakenOutOfTheWorldItGoesFromThoseHoldingItFromAfar()
        {
            using var world = new WorldTestContext();
            var landmark = Place(world, new Vector3(0, 0, 0), 150);
            var client = Client(world, 100);
            Landmarks.Worker(world.Map);
            WorldTestContext.Drain(client);

            CellManager.Instance.RemoveFromWorld(world.Map, landmark);

            Assert.AreEqual(landmark.EntityId, Sent(client).OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
            Assert.IsFalse(Landmarks.HeldFromAfar(client, landmark));
        }

        [TestMethod]
        public void AClientLeavingTheMapHoldsNothingFromAfar()
        {
            using var world = new WorldTestContext();
            var landmark = Place(world, new Vector3(0, 0, 0), 150);
            var client = Client(world, 100);
            Landmarks.Worker(world.Map);

            CellManager.Instance.DetachClient(world.Map, client);

            Assert.IsFalse(Landmarks.HeldFromAfar(client, landmark));
        }

        private static DynamicObject Place(WorldTestContext world, Vector3 position, float viewRadius)
        {
            world.AddClass(CaveInClass);
            var obj = new DynamicObject
            {
                EntityClassId = CaveInClass,
                Position = position,
                MapContextId = world.Map.MapInfo.MapContextId,
                StateId = UseObjectState.DoorStateClosed,
                IsEnabled = false,
                ViewRadius = viewRadius
            };

            world.Map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(world.Map, obj);
            return obj;
        }

        private static Client Client(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x, 0);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static void Walk(Client client, float x)
        {
            client.Player.Position = new Vector3(x, 0, 0);
            CellManager.Instance.UpdateVisibility(client);
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet).ToList();
    }

    internal static class LandmarkTestExtensions
    {
        /// <summary>The five by five cells around the object, as its broadcasts reach.</summary>
        internal static uint[,] Cells(this DynamicObject obj)
        {
            Assert.IsTrue(CellManager.TryGetCellCoordinates(obj.CellPosition, out var x, out var z));
            return CellManager.Instance.CreateCellMatrix(obj.RuntimeMapChannel, x, z);
        }
    }
}
