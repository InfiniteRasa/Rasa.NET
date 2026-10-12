extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;

    // BR-165: a player placed in the cells while Teleporting - a dropship arrival, until the ship
    // sets them down - is passed over by the cells both ways. CellManager.CatchUp brings their
    // client and their neighbours' in line once they are in the world.
    [TestClass]
    [DoNotParallelize]
    public class ArrivalIntroductionTests
    {
        [TestMethod]
        public void TwoPlayersLandingTogetherSeeEachOtherOnceBothAreDown()
        {
            using var world = new WorldTestContext();
            var first = Arriving(world, 0, 0);
            var second = Arriving(world, 4, 0);
            Drain(first, second);

            Assert.AreEqual(0, Made(first).Count + Made(second).Count, "passed over while Teleporting");

            Down(first);
            Assert.AreEqual(0, Made(first).Count + Made(second).Count, "the other is not down yet");

            Down(second);
            CollectionAssert.AreEqual(new[] { first.Player.EntityId }, Made(second));
            CollectionAssert.AreEqual(new[] { second.Player.EntityId }, Made(first));
        }

        [TestMethod]
        public void AnArrivalIsGivenWhoCameAndLosesWhoLeftBothWays()
        {
            using var world = new WorldTestContext();
            var leaver = PlayerDeathTests.Player(world, 10, 0);
            var stayer = PlayerDeathTests.Player(world, -10, 0);
            var arrival = Arriving(world, 0, 0);
            CollectionAssert.AreEquivalent(new[] { leaver.Player.EntityId, stayer.Player.EntityId },
                Made(arrival).Where(id => id != arrival.Player.EntityId).ToList());
            Drain(leaver, stayer);

            Walk(leaver, 600, 0);
            var comer = PlayerDeathTests.Player(world, 0, 10);
            Drain(arrival, leaver, stayer, comer);

            Down(arrival);

            var arrivalCalls = Calls(arrival);
            CollectionAssert.AreEqual(new[] { leaver.Player.EntityId }, Destroyed(arrivalCalls), "the frozen copy goes");
            CollectionAssert.AreEqual(new[] { comer.Player.EntityId }, Made(arrivalCalls));
            CollectionAssert.AreEqual(new[] { arrival.Player.EntityId }, Destroyed(Calls(leaver)));
            CollectionAssert.AreEqual(new[] { arrival.Player.EntityId }, Made(comer));
            Assert.AreEqual(0, Calls(stayer).Select(m => m.Packet)
                .Count(p => p is CreatePhysicalEntityPacket || p is DestroyPhysicalEntityPacket), "already had them");
        }

        [TestMethod]
        public void ASquadMateWhoWalkedOffIsKeptFromAfarNotTaken()
        {
            using var world = new WorldTestContext();
            var mate = PlayerDeathTests.Player(world, 10, 0);
            mate.Player.PartyId = 7;
            var arrival = Arriving(world, 0, 0);
            arrival.Player.PartyId = 7;
            Walk(mate, 600, 0);
            Drain(arrival, mate);

            Down(arrival);

            Assert.AreEqual(0, Destroyed(Calls(arrival)).Count + Destroyed(Calls(mate)).Count);
            Assert.IsTrue(FarAllies.Holds(arrival, mate));
            Assert.IsTrue(FarAllies.Holds(mate, arrival));
        }

        [TestMethod]
        public void APlayerPlacedInTheWorldHasNothingToCatchUp()
        {
            using var world = new WorldTestContext();
            var one = PlayerDeathTests.Player(world, 0, 0);
            var other = PlayerDeathTests.Player(world, 4, 0);
            Drain(one, other);

            Assert.IsNull(one.PlacedView);
            CellManager.Instance.CatchUp(one);

            Assert.AreEqual(0, Calls(one).Count + Calls(other).Count);
        }

        private static Client Arriving(WorldTestContext world, float x, float z)
        {
            var client = world.CreateClient(x, z);
            client.State = ClientState.Teleporting;
            CellManager.Instance.AddToWorld(client);
            return client;
        }

        private static void Down(Client client)
        {
            client.State = ClientState.Ingame;
            CellManager.Instance.CatchUp(client);
        }

        private static void Walk(Client client, float x, float z)
        {
            client.Player.Position = new Vector3(x, 0, z);
            CellManager.Instance.UpdateVisibility(client);
        }

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static List<CallMethodMessage> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().ToList();

        private static List<ulong> Made(Client client) => Made(Calls(client));

        private static List<ulong> Made(List<CallMethodMessage> calls) =>
            calls.Select(m => m.Packet).OfType<CreatePhysicalEntityPacket>().Select(p => p.EntityId).ToList();

        private static List<ulong> Destroyed(List<CallMethodMessage> calls) =>
            calls.Select(m => m.Packet).OfType<DestroyPhysicalEntityPacket>().Select(p => p.EntityId).ToList();
    }
}
