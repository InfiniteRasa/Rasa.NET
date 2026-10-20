using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Auth
{
    using Rasa.Auth;
    using Rasa.Packets.Communicator;

    /// <summary>
    /// What the server list says of a world comes from the game server's answer to the Auth
    /// server's info request. UpdateServerInfo used to drop the answer and rebuild the list from
    /// fields nothing ever set: every world showed 0 of 0 - "Low" in the client, never "Full" -
    /// with a queue port of 0, which the client reads as 8001.
    /// </summary>
    [TestClass]
    public class ServerListPopulationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
        }

        [TestMethod]
        public void TheListShowsWhatTheGameServerReports()
        {
            var server = NewServer();
            var world = Register(server, 1);

            server.UpdateServerInfo(world, Answer(37, 500));

            var listed = server.GetServerListSnapshot().Single();
            Assert.AreEqual((byte)1, listed.ServerId);
            Assert.AreEqual((ushort)37, listed.CurrentPlayers);
            Assert.AreEqual((ushort)500, listed.MaxPlayers);
            Assert.AreEqual((byte)1, listed.Status, "online");

            // The next answer, thirty seconds on.
            server.UpdateServerInfo(world, Answer(500, 500));

            listed = server.GetServerListSnapshot().Single();
            Assert.AreEqual((ushort)500, listed.CurrentPlayers, "full");
            Assert.AreEqual((ushort)500, listed.MaxPlayers);
        }

        [TestMethod]
        public void ThePortsAgeLimitAndPkFlagAreListedToo()
        {
            var server = NewServer();
            var world = Register(server, 1);

            var answer = Answer(3, 100);
            answer.QueuePort = 9001;
            answer.GamePort = 9002;
            answer.AgeLimit = 18;
            answer.PKFlag = 1;
            server.UpdateServerInfo(world, answer);

            var listed = server.GetServerListSnapshot().Single();
            Assert.AreEqual(9001, listed.QueuePort, "the queue the client connects to; 0 made it try 8001");
            Assert.AreEqual(9002, listed.GamePort);
            Assert.AreEqual((byte)18, listed.AgeLimit);
            Assert.AreEqual((byte)1, listed.PKFlag);
            Assert.AreEqual(IPAddress.Loopback, listed.Ip, "the address it logged in with");
        }

        [TestMethod]
        public void EachWorldHasItsOwnCount()
        {
            var server = NewServer();
            var first = Register(server, 1);
            var second = Register(server, 2);

            server.UpdateServerInfo(first, Answer(10, 200));
            server.UpdateServerInfo(second, Answer(150, 300));

            var listed = server.GetServerListSnapshot().ToDictionary(info => info.ServerId);
            Assert.AreEqual((ushort)10, listed[1].CurrentPlayers);
            Assert.AreEqual((ushort)200, listed[1].MaxPlayers);
            Assert.AreEqual((ushort)150, listed[2].CurrentPlayers);
            Assert.AreEqual((ushort)300, listed[2].MaxPlayers);
        }

        private static ServerInfoResponsePacket Answer(ushort current, ushort max) => new ServerInfoResponsePacket
        {
            CurrentPlayers = current,
            MaxPlayers = max
        };

        /// <summary>The Auth server's lists and settings, and nothing else of it: no sockets, no database, no loop.</summary>
        private static Server NewServer()
        {
            var server = (Server)RuntimeHelpers.GetUninitializedObject(typeof(Server));

            // Its finalizer is Shutdown, which stops a loop this one does not have.
            GC.SuppressFinalize(server);

            foreach (var field in new[] { "Config", "Clients", "ServerList", "Timer", "GameServerQueue", "GameServers" })
                Field(field).SetValue(server, Activator.CreateInstance(Field(field).FieldType));

            return server;
        }

        /// <summary>A game server logged in on the slot, as AuthenticateGameServer leaves it.</summary>
        private static CommunicatorClient Register(Server server, byte serverId)
        {
            var client = (CommunicatorClient)RuntimeHelpers.GetUninitializedObject(typeof(CommunicatorClient));
            client.ServerId = serverId;
            client.PublicAddress = IPAddress.Loopback;

            var servers = (Dictionary<byte, CommunicatorClient>)Field("GameServers").GetValue(server);

            lock (servers)
                servers[serverId] = client;

            return client;
        }

        private static FieldInfo Field(string property)
        {
            var info = typeof(Server).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(info, property);

            return info;
        }
    }
}
