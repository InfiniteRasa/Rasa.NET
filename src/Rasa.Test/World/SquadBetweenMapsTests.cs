extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Party.Client;
    using Rasa.Packets.Party.Server;
    using Rasa.Packets.Protocol;

    /// <summary>
    /// A squad member between maps - loading into the next one, or on a dropship - is a member
    /// still, and what the squad does reaches them there (PartyManager.FindMember). A kick looked
    /// for its member as Ingame only, so one kicked while loading kept the squad on its screen
    /// and its PartyId on the server until a relog.
    /// </summary>
    [TestClass]
    public class SquadBetweenMapsTests
    {
        [TestMethod]
        [DataRow(ClientState.Loading)]
        [DataRow(ClientState.Teleporting)]
        public void AMemberKickedWhileBetweenMapsIsOutOfTheSquad(ClientState between)
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 5, 0);
            var third = PlayerDeathTests.Player(world, 10, 0);
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate, third);

            mate.State = between;
            Drain(lead, mate, third);

            PartyManager.Instance.KickUserFromPartyById(lead, new KickUserFromPartyByIdPacket { UserId = mate.AccountEntry.Id });

            Assert.AreEqual(0u, mate.Player.PartyId, "out of the squad on the server");
            Assert.IsNull(squad.Party.Find(mate.AccountEntry.Id));
            Assert.AreEqual(squad.Party.Id, lead.Player.PartyId);
            Assert.AreEqual(squad.Party.Id, third.Player.PartyId);

            var told = Sent(mate).OfType<SetCurrentPartyIdPacket>().Single();

            Assert.AreEqual(0u, told.PartyId, "and told so, where they are");
            Assert.IsTrue(told.WasKicked);
            Assert.IsTrue(Sent(lead).OfType<RemovePartyMemberPacket>().Any(), "the others see them go");
        }

        [TestMethod]
        public void AMemberIngameIsKickedAsBefore()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 5, 0);
            var third = PlayerDeathTests.Player(world, 10, 0);
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate, third);

            Drain(lead, mate, third);

            PartyManager.Instance.KickUserFromPartyById(lead, new KickUserFromPartyByIdPacket { UserId = mate.AccountEntry.Id });

            Assert.AreEqual(0u, mate.Player.PartyId);
            Assert.AreEqual(0u, Sent(mate).OfType<SetCurrentPartyIdPacket>().Single().PartyId);
        }

        private static IEnumerable<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet);

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }
    }
}
