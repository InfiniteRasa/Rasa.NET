extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Both;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    /// <summary>
    /// The team channel (CommunicatorManager.JoinTeamChannel, ChatChannelId.Team): one channel a
    /// team of a match, joined and left as the team is, which /team is said on. That a
    /// battleground's teams are put in theirs is with the battleground's tests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TeamChatTests
    {
        private const uint Red = 1, Blue = 2;

        private readonly List<Client> _clients = new List<Client>();
        private uint _nextAccount = 9100;

        [TestCleanup]
        public void OutOfEveryChannel()
        {
            foreach (var client in _clients)
            {
                CommunicatorManager.Instance.LeaveMapChannels(client);

                lock (Server.Clients)
                    Server.Clients.Remove(client);
            }
        }

        [TestMethod]
        public void ATeamsChannelIsHeardByItsPlayersAndNobodyElse()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var (first, second, enemy, outsider) = (Player(world), Player(world), Player(world), Player(world));

            chat.JoinTeamChannel(first, world.Map, Red);
            chat.JoinTeamChannel(second, world.Map, Red);
            chat.JoinTeamChannel(enemy, world.Map, Blue);

            // The client holds the channel from this: no map to its name, "[18. Team]".
            var joined = Sent<ChatChannelJoinedPacket>(first).Single();

            Assert.AreEqual(ChatChannelId.Team, joined.ChannelId);
            Assert.AreEqual(0u, joined.MapContextId);
            Assert.AreEqual(0ul, joined.MapEntityId);
            Assert.AreEqual(ChatChannelId.Team, Sent<ChatChannelJoinedPacket>(enemy).Single().ChannelId);
            Assert.IsEmpty(Sent<ChatChannelJoinedPacket>(outsider));
            Sent<ChatChannelJoinedPacket>(second);

            CollectionAssert.AreEquivalent(new[] { first.Player.EntityId, second.Player.EntityId }, chat.TeamChannelPlayers(world.Map, Red).ToArray());
            CollectionAssert.AreEquivalent(new[] { enemy.Player.EntityId }, chat.TeamChannelPlayers(world.Map, Blue).ToArray());

            Say(first, "push left");

            foreach (var listener in new[] { first, second })
            {
                var heard = Sent<ChannelChatPacket>(listener).Single();

                Assert.AreEqual(ChatChannelId.Team, heard.ChannelId);
                Assert.AreEqual("push left", heard.Message);
                Assert.AreEqual(first.Player.FamilyName, heard.Name);
            }

            Assert.IsEmpty(Sent<ChannelChatPacket>(enemy), "the other team has its own");
            Assert.IsEmpty(Sent<ChannelChatPacket>(outsider));

            Say(enemy, "they are coming left");
            Assert.AreEqual("they are coming left", Sent<ChannelChatPacket>(enemy).Single().Message);
            Assert.IsEmpty(Sent<ChannelChatPacket>(first));

            // One on no team is in no team's channel.
            Say(outsider, "anyone");

            foreach (var client in new[] { first, second, enemy, outsider })
                Assert.IsEmpty(Sent<ChannelChatPacket>(client));

            // The ignore list holds here as on every channel.
            second.Player.IgnoredPlayers.Add(first.AccountEntry.Id);
            Say(first, "second?");
            Assert.IsEmpty(Sent<ChannelChatPacket>(second));
            Assert.HasCount(1, Sent<ChannelChatPacket>(first));
        }

        [TestMethod]
        public void TheSameTeamOfAnotherMatchIsAnotherChannel()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var other = new MapChannel { MapInfo = new MapInfo(2374, "edmund_range", 1, 0), ClientList = new List<Client>(), InstanceId = 7 };
            var (here, there) = (Player(world), Player(world));

            chat.JoinTeamChannel(here, world.Map, Red);
            chat.JoinTeamChannel(there, other, Red);
            Drain(here, there);

            Say(here, "here");
            Assert.HasCount(1, Sent<ChannelChatPacket>(here));
            Assert.IsEmpty(Sent<ChannelChatPacket>(there));
            CollectionAssert.AreEqual(new[] { there.Player.EntityId }, chat.TeamChannelPlayers(other, Red).ToArray());
        }

        [TestMethod]
        public void LeavingTellsTheClientAndChangingTeamsLeavesTheOneForTheOther()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var client = Player(world);

            Assert.IsFalse(chat.LeaveTeamChannel(client), "in none");
            Assert.IsEmpty(Packets(client));

            chat.JoinTeamChannel(client, world.Map, Red);
            Drain(client);

            // Joining the one they are in says nothing.
            chat.JoinTeamChannel(client, world.Map, Red);
            Assert.IsEmpty(Packets(client));
            Assert.AreEqual(1, client.Player.JoinedChannels);

            // To the other team: out of the one, into the other.
            chat.JoinTeamChannel(client, world.Map, Blue);

            var packets = Packets(client);

            Assert.HasCount(2, packets);
            Assert.AreEqual(ChatChannelId.Team, ((ChatChannelLeftPacket)packets[0]).ChannelId);
            Assert.AreEqual(ChatChannelId.Team, ((ChatChannelJoinedPacket)packets[1]).ChannelId);
            Assert.AreEqual(1, client.Player.JoinedChannels);
            Assert.IsEmpty(chat.TeamChannelPlayers(world.Map, Red));
            CollectionAssert.AreEqual(new[] { client.Player.EntityId }, chat.TeamChannelPlayers(world.Map, Blue).ToArray());

            Assert.IsTrue(chat.LeaveTeamChannel(client));
            Assert.AreEqual(ChatChannelId.Team, Sent<ChatChannelLeftPacket>(client).Single().ChannelId);
            Assert.AreEqual(0, client.Player.JoinedChannels);
            Assert.IsFalse(chat.LeaveTeamChannel(client));

            // Left, it is not theirs to speak on.
            Say(client, "hello?");
            Assert.IsEmpty(Packets(client));

            // A connection that is gone is taken out and told nothing.
            chat.JoinTeamChannel(client, world.Map, Red);
            Drain(client);
            client.State = ClientState.Disconnected;
            Assert.IsTrue(chat.LeaveTeamChannel(client));
            Assert.IsEmpty(Packets(client));
            Assert.IsEmpty(chat.TeamChannelPlayers(world.Map, Red));
        }

        [TestMethod]
        public void LeavingTheTeamKeepsTheOtherChannels()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var (client, mate) = (Player(world), Player(world));

            foreach (var player in new[] { client, mate })
            {
                chat.JoinGlobalChannel(player, ChatChannelId.General);
                chat.JoinTeamChannel(player, world.Map, Red);
                chat.JoinDefaultLocalChannel(player, ChatChannelId.MapTrade);
            }

            Assert.AreEqual(3, client.Player.JoinedChannels);
            Assert.IsTrue(chat.LeaveTeamChannel(client));
            Assert.AreEqual(2, client.Player.JoinedChannels);
            Drain(client, mate);

            // The channel joined after it is still theirs, and the one before.
            foreach (var channel in new[] { ChatChannelId.MapTrade, ChatChannelId.General })
            {
                Say(client, "selling", channel);
                Assert.AreEqual(channel, Sent<ChannelChatPacket>(client).Single().ChannelId);
                Assert.AreEqual(channel, Sent<ChannelChatPacket>(mate).Single().ChannelId);
            }

            Say(client, "team?");
            Assert.IsEmpty(Packets(client));
            Assert.IsEmpty(Packets(mate));

            // And the team is still its other player's.
            Say(mate, "still here");
            Assert.HasCount(1, Sent<ChannelChatPacket>(mate));
            Assert.IsEmpty(Packets(client));
        }

        [TestMethod]
        public void LeavingTheMapLeavesTheTeamsChannelAndItGoesWithItsLastPlayer()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var (first, second) = (Player(world), Player(world));

            chat.JoinGlobalChannel(first, ChatChannelId.General);

            var defaults = CommunicatorManager.ChannelsBySeed.Count;

            chat.JoinTeamChannel(first, world.Map, Red);
            chat.JoinTeamChannel(second, world.Map, Red);
            Assert.AreEqual(defaults + 1, CommunicatorManager.ChannelsBySeed.Count, "one channel a team");

            chat.LeaveMapChannels(first);
            Assert.AreEqual(0, first.Player.JoinedChannels);
            CollectionAssert.AreEqual(new[] { second.Player.EntityId }, chat.TeamChannelPlayers(world.Map, Red).ToArray());
            Assert.AreEqual(defaults + 1, CommunicatorManager.ChannelsBySeed.Count);

            chat.LeaveMapChannels(second);
            Assert.IsEmpty(chat.TeamChannelPlayers(world.Map, Red));
            Assert.AreEqual(defaults, CommunicatorManager.ChannelsBySeed.Count, "the team's is gone; the default ones are kept");

            // The team forms again: a channel again.
            Drain(first, second);
            chat.JoinTeamChannel(first, world.Map, Red);
            Assert.HasCount(1, Sent<ChatChannelJoinedPacket>(first));
            Say(first, "back");
            Assert.HasCount(1, Sent<ChannelChatPacket>(first));
        }

        [TestMethod]
        public void APlayerInAsManyChannelsAsTheyCanHoldIsNotPutInTheTeams()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var client = Player(world);

            for (var i = 0; i < CommunicatorManager.ChannelHashesPerPlayer; i++)
                chat.JoinGlobalChannel(client, ChatChannelId.General);

            Drain(client);
            chat.JoinTeamChannel(client, world.Map, Red);

            Assert.IsEmpty(Packets(client));
            Assert.AreEqual(CommunicatorManager.ChannelHashesPerPlayer, client.Player.JoinedChannels);
            Assert.IsEmpty(chat.TeamChannelPlayers(world.Map, Red));
        }

        /// <summary>A player in the world with an account, whom chat can find.</summary>
        private Client Player(WorldTestContext world)
        {
            var client = world.CreateClient();
            var id = _nextAccount++;

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = id, FamilyName = $"Family{id}" });
            client.Player.FamilyName = $"Family{id}";

            lock (Server.Clients)
                Server.Clients.Add(client);

            _clients.Add(client);
            WorldTestContext.Drain(client);

            return client;
        }

        private static void Say(Client client, string message, uint channel = ChatChannelId.Team) =>
            CommunicatorManager.Instance.ChannelChat(client, new ChannelChatPacket { ChannelId = channel, Message = message });

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();
    }
}
