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
    using Rasa.Test.Gameplay;

    /// <summary>
    /// The New Player channel (CommunicatorManager.NewPlayerChannel, ChatChannelId.NewPlayer):
    /// every character below level 10 is in it, world-wide, and leaves it on reaching 10.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NewPlayerChannelTests
    {
        private readonly List<Client> _clients = new List<Client>();
        private uint _nextAccount = 9300;

        [TestCleanup]
        public void OutOfEveryChannel()
        {
            foreach (var client in _clients)
            {
                CommunicatorManager.Instance.LeaveMapChannels(client);

                lock (Server.Clients)
                    Server.Clients.Remove(client);
            }

            _clients.Clear();
        }

        [TestMethod]
        public void ACharacterBelowTenIsPutInItOnArrivingAndOneAtTenIsNot()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var (fresh, nine, ten) = (Player(world, 1), Player(world, 9), Player(world, 10));

            foreach (var client in new[] { fresh, nine, ten })
                chat.PlayerEnterMap(client);

            foreach (var client in new[] { fresh, nine })
            {
                var joined = Sent<ChatChannelJoinedPacket>(client).Where(packet => packet.ChannelId == ChatChannelId.NewPlayer).ToList();

                Assert.HasCount(1, joined, $"level {client.Player.Level}");
                Assert.AreEqual(0u, joined[0].MapContextId, "the whole world's, as General is");
            }

            Assert.IsFalse(Sent<ChatChannelJoinedPacket>(ten).Any(packet => packet.ChannelId == ChatChannelId.NewPlayer));
            Assert.AreEqual("New Player", CommunicatorManager.ChannelName(ChatChannelId.NewPlayer));

            // Heard by those in it, from any map, and by nobody else.
            Say(fresh, "how do I sprint?");

            foreach (var listener in new[] { fresh, nine })
                Assert.AreEqual("how do I sprint?", Sent<ChannelChatPacket>(listener).Single().Message);

            Assert.IsEmpty(Sent<ChannelChatPacket>(ten));

            // Arriving on another map is no second join.
            chat.LeaveMapChannels(fresh);
            chat.PlayerEnterMap(fresh);
            Assert.HasCount(1, Sent<ChatChannelJoinedPacket>(fresh).Where(packet => packet.ChannelId == ChatChannelId.NewPlayer).ToList());
        }

        [TestMethod]
        public void ReachingTenLeavesItAndGoingBelowTenJoinsItAgain()
        {
            using var world = new WorldTestContext();
            var chat = CommunicatorManager.Instance;
            var player = Player(world, 9);
            var other = Player(world, 3);

            chat.PlayerEnterMap(player);
            chat.PlayerEnterMap(other);
            Drain(player, other);

            var channelsBefore = player.Player.JoinedChannels;

            player.Player.Level = 10;
            chat.NewPlayerChannel(player);

            Assert.AreEqual(ChatChannelId.NewPlayer, Sent<ChatChannelLeftPacket>(player).Single().ChannelId);
            Assert.AreEqual(channelsBefore - 1, player.Player.JoinedChannels);

            // Its other channels are all still its own.
            Say(player, "trade anyone?", ChatChannelId.General);
            Assert.AreEqual("trade anyone?", Sent<ChannelChatPacket>(other).Single().Message);
            Assert.AreEqual("trade anyone?", Sent<ChannelChatPacket>(player).Single().Message, "said on General, heard there");

            Say(other, "hello?");
            Assert.IsEmpty(Sent<ChannelChatPacket>(player), "out of New Player");

            // Again at 10: nothing to leave.
            chat.NewPlayerChannel(player);
            Assert.IsEmpty(Packets(player));

            // Below 10 again (a game master's .setlevel): back in.
            player.Player.Level = 4;
            chat.NewPlayerChannel(player);
            Assert.AreEqual(ChatChannelId.NewPlayer, Sent<ChatChannelJoinedPacket>(player).Single().ChannelId);

            chat.NewPlayerChannel(null);
        }

        [TestMethod]
        public void ExperienceThatTakesACharacterPastTenTakesItOut()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var client = context.Client;
            var player = client.Player;

            _clients.Add(client);
            player.Level = 1;
            player.Experience = 0;
            player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                attribute => attribute,
                attribute => new ActorAttributes(attribute, 100, 100, 100, 0, 0));

            CommunicatorManager.Instance.NewPlayerChannel(client);
            Assert.AreEqual(ChatChannelId.NewPlayer, Sent<ChatChannelJoinedPacket>(client).Single().ChannelId);

            manager.GainExperience(client, 900000);

            Assert.IsGreaterThanOrEqualTo(10, (int)player.Level);
            Assert.AreEqual(ChatChannelId.NewPlayer, Sent<ChatChannelLeftPacket>(client).Single().ChannelId);
        }

        private Client Player(WorldTestContext world, byte level)
        {
            var client = world.CreateClient();
            var id = _nextAccount++;

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = id, FamilyName = $"Family{id}" });
            client.Player.FamilyName = $"Family{id}";
            client.Player.Level = level;
            client.State = ClientState.Ingame;

            lock (Server.Clients)
                Server.Clients.Add(client);

            _clients.Add(client);
            WorldTestContext.Drain(client);

            return client;
        }

        private static void Say(Client client, string message, uint channel = ChatChannelId.NewPlayer) =>
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
