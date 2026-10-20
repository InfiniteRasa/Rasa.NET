using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Party.Client;
    using Rasa.Packets.Party.Server;
    using Rasa.Packets.Protocol;

    /// <summary>
    /// A request to join a squad goes to a player who may have the requester on their ignore
    /// list. An invitation from someone ignored is not put to the player it names (Invite); a join
    /// request reached them all the same, and could be sent again as often as the requester liked.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SquadJoinIgnoreTests
    {
        [TestMethod]
        public void APlayerWhoIgnoresTheRequesterIsNotAskedToLetThemIn()
        {
            using var world = new WorldTestContext();
            var asker = Lone(world, 0, "Asker");
            var asked = Lone(world, 5, "Asked");

            try
            {
                asked.Player.IgnoredPlayers.Add(asker.AccountEntry.Id);

                PartyManager.Instance.SendJoinRequestToPartyByName(asker, new SendJoinRequestToPartyByNamePacket { FamilyName = "Asked" });

                Assert.IsFalse(Sent(asked).OfType<JoinSquadRequestReceivedPacket>().Any(), "not asked");

                var toAsker = Sent(asker).ToList();
                var told = toAsker.OfType<DisplayClientMessagePacket>().Single();
                Assert.AreEqual(PlayerMessage.PmUserIgnoringYou, told.MsgId);
                Assert.AreEqual("Asked", told.Args["name"]);
                Assert.IsFalse(toAsker.OfType<JoinSquadRequestSentPacket>().Any(), "no request is waiting");

                // Not ignored, the request goes as before.
                asked.Player.IgnoredPlayers.Clear();

                PartyManager.Instance.SendJoinRequestToPartyByName(asker, new SendJoinRequestToPartyByNamePacket { FamilyName = "Asked" });

                Assert.AreEqual(asker.AccountEntry.Id, Sent(asked).OfType<JoinSquadRequestReceivedPacket>().Single().SenderUserId);
                Assert.HasCount(1, Sent(asker).OfType<JoinSquadRequestSentPacket>().ToList());

                PartyManager.Instance.CancelSquadJoinRequest(asker, new CancelSquadJoinRequestPacket { FamilyName = "Asked" });
            }
            finally
            {
                Leave(asker, asked);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ARequestIsNotHandedToANewLeaderWhoIgnoresTheRequester(bool ignores)
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 5, 0);
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate);
            var asker = Lone(world, 10, "Asker");

            lead.Player.FamilyName = "Lead";
            mate.Player.FamilyName = "Mate";

            try
            {
                if (ignores)
                    mate.Player.IgnoredPlayers.Add(asker.AccountEntry.Id);

                PartyManager.Instance.SendJoinRequestToPartyByName(asker, new SendJoinRequestToPartyByNamePacket { FamilyName = "Lead" });

                Assert.HasCount(1, Sent(lead).OfType<JoinSquadRequestReceivedPacket>().ToList(), "the leader does not ignore them");
                Sent(asker).ToList();
                Sent(mate).ToList();

                PartyManager.Instance.MakeUserPartyLeaderById(lead, new MakeUserPartyLeaderByIdPacket { UserId = mate.AccountEntry.Id });

                Assert.AreEqual(mate.AccountEntry.Id, squad.Party.PartyLeaderId);

                if (!ignores)
                {
                    // As before: the new leader has the request.
                    Assert.AreEqual(asker.AccountEntry.Id, Sent(mate).OfType<JoinSquadRequestReceivedPacket>().Single().SenderUserId);
                    Assert.IsFalse(Sent(asker).OfType<DisplayClientMessagePacket>().Any(message => message.MsgId == PlayerMessage.PmPartyRequestIsNoLongerValid));
                    PartyManager.Instance.CancelSquadJoinRequest(asker, new CancelSquadJoinRequestPacket { FamilyName = "Lead" });
                    return;
                }

                Assert.IsFalse(Sent(mate).OfType<JoinSquadRequestReceivedPacket>().Any(), "not handed to the new leader");
                Assert.IsTrue(Sent(asker).OfType<DisplayClientMessagePacket>().Any(message => message.MsgId == PlayerMessage.PmPartyRequestIsNoLongerValid));

                // Gone: asking again goes to the new leader, who refuses it.
                PartyManager.Instance.SendJoinRequestToPartyByName(asker, new SendJoinRequestToPartyByNamePacket { FamilyName = "Mate" });
                Assert.AreEqual(PlayerMessage.PmUserIgnoringYou, Sent(asker).OfType<DisplayClientMessagePacket>().Single().MsgId);
            }
            finally
            {
                Leave(asker);
            }
        }

        /// <summary>A player in the world in no squad, with an account and a name of their own, on the server's list.</summary>
        private static Client Lone(WorldTestContext world, float x, string familyName)
        {
            var client = PlayerDeathTests.Player(world, x, 0);
            SquadAfterZoningTests.TestSquad.GiveAccount(client);
            client.Player.FamilyName = familyName;

            lock (Server.Clients)
                Server.Clients.Add(client);

            return client;
        }

        private static void Leave(params Client[] clients)
        {
            lock (Server.Clients)
                foreach (var client in clients)
                    Server.Clients.Remove(client);
        }

        private static IEnumerable<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet);
    }
}
