extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Data;
    using Rasa.Memory;
    using Rasa.Networking;
    using Rasa.Packets;
    using Rasa.Packets.Communicator;
    using GameConfig = RasaGame::Rasa.Config.Config;
    using GameServer = RasaGame::Rasa.Game.Server;

    // The game server's link to the Auth server: one connection it makes and logs in on
    // (Server.ConnectCommunicator). Logins are handed over it, so while it is down nobody new gets
    // into the world. When it is lost the game server waits ten seconds ("CommReconnect" on the
    // server's timer) and makes it again.
    //
    // The Auth server here is one of two things. Most tests use a listener on the loopback with
    // the real socket layer on each connection it accepts, which counts the logins it is sent and
    // answers them as it is told to. The last three use the Auth server's own code for its end
    // of the link - its accept, its login check, the slot it gives and takes back - so that what
    // is said here about the two ends is what the two ends do. Ten seconds are passed by updating
    // the game server's timer.
    [TestClass]
    [DoNotParallelize]
    public class AuthLinkReconnectTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
            BufferManager.Initialize(8192, 8, 8);
            LengthedSocket.InitializeEventArgsPool(64);
        }

        private readonly List<GameServer> _games = new List<GameServer>();

        [TestCleanup]
        public void Cleanup()
        {
            // As the server's own Shutdown leaves it: stopping, and the link closed.
            foreach (var game in _games)
            {
                Set(game, "_shutDown", 1);
                game.AuthCommunicator?.Close();
            }
        }

        [TestMethod]
        public void TheLinkIsMadeAndLoggedIn()
        {
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();

            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");
            Assert.AreEqual(1, auth.Logins);
        }

        [TestMethod]
        public void WithNoAuthServerAtTheStartItIsTriedUntilThereIsOne()
        {
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            auth.StopListening();
            game.ConnectCommunicator();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                game.Timer.Update(10000);
                Thread.Sleep(200);
                Assert.IsFalse(game.AuthLinkUp);
            }

            auth.Listen();

            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 1), "no connection was made once it was there");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");
        }

        [TestMethod]
        public void AfterTheAuthServerClosesTheLinkItIsMadeAgain()
        {
            // A close by the other side is not a socket error: the receive completes with no
            // bytes, and the socket still says it is connected.
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            var first = game.AuthCommunicator;

            auth.Links[0].Close();

            Assert.IsTrue(Soon(() => !game.AuthLinkUp), "the link is known to be down");
            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 2), "no second connection was made");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in again");
            Assert.AreNotSame(first, game.AuthCommunicator);
            Assert.IsFalse(first.Connected, "the link that was closed is closed on this side too");
        }

        [TestMethod]
        public void AfterTheLinkStopsFramingItIsMadeAgain()
        {
            // Not a socket error either: the socket layer drops a connection whose stream it can
            // no longer cut into frames, and says so through OnDrop.
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            // A frame that says it is two bytes long, which is its own length header and nothing else.
            auth.Links[0].Socket.Send(new byte[] { 2, 0 });

            Assert.IsTrue(Soon(() => !game.AuthLinkUp), "the link is known to be down");
            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 2), "no second connection was made");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in again");
        }

        [TestMethod]
        public void AfterAResetTheLinkIsMadeAgain()
        {
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            auth.Links[0].Socket.LingerState = new LingerOption(true, 0);
            auth.Links[0].Socket.Close();

            Assert.IsTrue(Soon(() => !game.AuthLinkUp), "the link is known to be down");
            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 2), "no second connection was made");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in again");
        }

        [TestMethod]
        public void WithTheAuthServerDownItIsTriedUntilItIsBack()
        {
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            // Gone, and nothing listening where it was: each try is refused.
            auth.StopListening();
            auth.Links[0].Close();
            Assert.IsTrue(Soon(() => !game.AuthLinkUp), "the link is known to be down");

            for (var attempt = 0; attempt < 3; attempt++)
            {
                game.Timer.Update(10000);
                Thread.Sleep(200);
                Assert.IsFalse(game.AuthLinkUp);
            }

            auth.Listen();

            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 2), "no connection was made once it was back");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in again");
        }

        [TestMethod]
        public void ALoginTheAuthServerRefusesIsTriedAgain()
        {
            // The Auth server refuses a game server whose slot is still taken, which it is until
            // the Auth server has itself noticed that the old link is gone.
            using var auth = new FakeAuth { Refusals = 2 };
            var game = Game(auth.Port);

            game.ConnectCommunicator();

            Assert.IsTrue(Soon(() => auth.Logins == 1), "the first login");
            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 2), "no second try");
            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 3), "no third try");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in on the third");
            Assert.AreEqual(3, auth.Links.Count, "one connection for each try");
        }

        [TestMethod]
        public void AConnectThatCannotBeStartedIsTriedAgain()
        {
            // The config is not read again while the server runs; the address is put right here
            // only so that the next try has something to reach.
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.Config.CommunicatorConfig.Address = "not an address";
            game.ConnectCommunicator();
            game.Config.CommunicatorConfig.Address = IPAddress.Loopback.ToString();

            Assert.IsTrue(TenSecondsLater(game, () => auth.Logins == 1), "no connection was made");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");
        }

        [TestMethod]
        public void ALinkThatIsUpIsLeftAloneWhenTheTimerComesRound()
        {
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            // The link is lost, which asks for another in ten seconds, and one is made before
            // the ten seconds are up.
            auth.Links[0].Close();
            Assert.IsTrue(Soon(() => game.AuthCommunicator == null), "the closed link is let go of");

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => auth.Logins == 2 && game.AuthLinkUp), "logged in again");

            var link = game.AuthCommunicator;

            for (var round = 0; round < 5; round++)
            {
                game.Timer.Update(10000);
                Thread.Sleep(50);
            }

            Assert.AreSame(link, game.AuthCommunicator);
            Assert.IsTrue(game.AuthLinkUp);
            Assert.AreEqual(2, auth.Logins);
        }

        [TestMethod]
        public void ALinkThatHasBeenReplacedSaysNothingAboutTheOneThatReplacedIt()
        {
            // Replacing a link closes it, and the close ends its receive with an error: that
            // error is the old link's, and the new one is not to be taken down for it.
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            var first = game.AuthCommunicator;

            game.ConnectCommunicator();

            var second = game.AuthCommunicator;

            Assert.AreNotSame(first, second);
            Assert.IsTrue(Soon(() => auth.Logins == 2 && game.AuthLinkUp), "logged in on the second");

            for (var round = 0; round < 5; round++)
            {
                game.Timer.Update(10000);
                Thread.Sleep(50);
            }

            Assert.AreSame(second, game.AuthCommunicator);
            Assert.IsTrue(game.AuthLinkUp);
            Assert.AreEqual(2, auth.Logins);
            Assert.IsFalse(first.Connected, "the replaced link is closed");
        }

        [TestMethod]
        public void OnceTheServerIsStoppingTheLinkIsNotMadeAgain()
        {
            using var auth = new FakeAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in");

            Set(game, "_shutDown", 1);
            auth.Links[0].Close();
            Assert.IsTrue(Soon(() => game.AuthCommunicator == null), "the closed link is let go of");

            for (var round = 0; round < 5; round++)
            {
                game.Timer.Update(10000);
                Thread.Sleep(50);
            }

            Assert.IsNull(game.AuthCommunicator);
            Assert.AreEqual(1, auth.Links.Count);
        }

        [TestMethod]
        public void TheAuthServerGivesTheSlotToTheLinkThatIsMadeAgain()
        {
            using var auth = new RealAuth();
            var game = Game(auth.Port);

            game.ConnectCommunicator();
            Assert.IsTrue(Soon(() => game.AuthLinkUp && auth.Holder(1) != null), "logged in, and listed");

            var first = auth.Holder(1);

            // The Auth server lets the link go, as it does when it has given up on it.
            first.Socket.Close();

            Assert.IsTrue(Soon(() => auth.Holder(1) == null), "the slot is free");
            Assert.IsTrue(Soon(() => !game.AuthLinkUp), "the link is known to be down");
            Assert.IsTrue(TenSecondsLater(game, () => auth.Holder(1) != null), "the slot was not taken again");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in again");
            Assert.AreNotSame(first, auth.Holder(1));
        }

        [TestMethod]
        public void AnAuthServerThatIsRestartedGetsItsGameServerBack()
        {
            var auth = new RealAuth();
            var port = auth.Port;
            var game = Game(port);

            try
            {
                game.ConnectCommunicator();
                Assert.IsTrue(Soon(() => game.AuthLinkUp && auth.Holder(1) != null), "logged in, and listed");
            }
            finally
            {
                // The process ends: the listener and every connection close with it.
                auth.Dispose();
            }

            Assert.IsTrue(Soon(() => !game.AuthLinkUp), "the link is known to be down");

            // While it is away each try is refused.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                game.Timer.Update(10000);
                Thread.Sleep(200);
                Assert.IsFalse(game.AuthLinkUp);
            }

            // A new process: nothing of the old one's, on the same port.
            using var restarted = new RealAuth(port);

            Assert.IsTrue(TenSecondsLater(game, () => restarted.Holder(1) != null), "the game server did not come back");
            Assert.IsTrue(Soon(() => game.AuthLinkUp), "logged in again");
        }

        [TestMethod]
        public void WhileTheSlotIsStillHeldTheLoginIsRefusedAndTriedUntilItIsFree()
        {
            // The Auth server still has this server's previous link in the slot: it has not yet
            // found out that it is gone. The previous link here is a connection that logged in
            // with the same id and password and is still open.
            using var auth = new RealAuth();
            var game = Game(auth.Port);
            var previous = new LengthedSocket(SizeType.Word);

            previous.OnError = _ => { };
            previous.OnConnect = _ =>
            {
                previous.Send(new LoginRequestPacket { ServerId = 1, Password = "fixture", PublicAddress = IPAddress.Loopback });
                previous.ReceiveAsync();
            };

            try
            {
                previous.ConnectAsync(new IPEndPoint(IPAddress.Loopback, auth.Port));
                Assert.IsTrue(Soon(() => auth.Holder(1) != null), "the previous link holds the slot");

                var held = auth.Holder(1);

                game.ConnectCommunicator();

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    game.Timer.Update(10000);
                    Thread.Sleep(200);
                    Assert.IsFalse(game.AuthLinkUp, "refused while the slot is held");
                    Assert.AreSame(held, auth.Holder(1), "and the link that holds it is not disturbed");
                }

                // The Auth server finds out.
                previous.Close();
                Assert.IsTrue(Soon(() => auth.Holder(1) == null), "the slot is free");

                Assert.IsTrue(TenSecondsLater(game, () => game.AuthLinkUp), "not logged in once the slot was free");
                Assert.IsNotNull(auth.Holder(1));
            }
            finally
            {
                previous.Close();
            }
        }

        /// <summary>A game server with what its link to the Auth server reads, and nothing else: no world, no listener, no loop.</summary>
        private GameServer Game(int authPort)
        {
            var game = (GameServer)RuntimeHelpers.GetUninitializedObject(typeof(GameServer));

            // Its finalizer is Shutdown, which stops a voice server, an API host and a loop
            // that this one does not have.
            GC.SuppressFinalize(game);

            Set(game, "<Config>k__BackingField", new GameConfig
            {
                CommunicatorConfig = new RasaGame::Rasa.Config.CommunicatorConfig { Address = IPAddress.Loopback.ToString(), Port = authPort },
                ServerInfoConfig = new RasaGame::Rasa.Config.ServerInfoConfig { Id = 1, Password = "fixture" },

                // What it answers the Auth server's request for its info from.
                GameConfig = new RasaGame::Rasa.Config.GameConfig(),
                QueueConfig = new RasaGame::Rasa.Config.QueueConfig()
            });
            Set(game, "<Timer>k__BackingField", new Rasa.Timer.Timer());
            Set(game, "<PublicAddress>k__BackingField", IPAddress.Loopback);
            Set(game, "_router", new PacketRouter<GameServer, CommOpcode>());

            _games.Add(game);

            return game;
        }

        private static void Set(object server, string field, object value)
        {
            var info = server.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(info, field);
            info.SetValue(server, value);
        }

        private static bool Soon(Func<bool> condition, int milliseconds = 5000)
        {
            var until = Environment.TickCount64 + milliseconds;

            while (Environment.TickCount64 < until)
            {
                if (condition())
                    return true;

                Thread.Sleep(10);
            }

            return condition();
        }

        /// <summary>
        /// Ten seconds pass on the game server's timer, and then the condition is waited for.
        /// The reconnect is put on the timer from a socket thread, so the ten seconds are passed
        /// again for as long as nothing has come of them.
        /// </summary>
        private static bool TenSecondsLater(GameServer game, Func<bool> condition, int milliseconds = 10000)
        {
            var until = Environment.TickCount64 + milliseconds;

            while (Environment.TickCount64 < until)
            {
                game.Timer.Update(10000);

                if (Soon(condition, 100))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// What the game server connects to: accepts on the loopback, counts each login it is
        /// sent, and answers it - with a refusal for the first <see cref="Refusals"/>.
        /// </summary>
        private sealed class FakeAuth : IDisposable
        {
            private Socket _listener;
            private int _logins;

            internal int Port { get; private set; }
            internal int Refusals { get; set; }
            internal int Logins => Volatile.Read(ref _logins);
            internal List<LengthedSocket> Links { get; } = new List<LengthedSocket>();

            internal FakeAuth()
            {
                Listen();
            }

            /// <summary>Listens, on the port it had if it has had one.</summary>
            internal void Listen()
            {
                var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, Port));
                listener.Listen(8);

                Port = ((IPEndPoint)listener.LocalEndPoint).Port;
                _listener = listener;

                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        Socket accepted;

                        try
                        {
                            accepted = await listener.AcceptAsync();
                        }
                        catch (Exception)
                        {
                            return;
                        }

                        Accept(accepted);
                    }
                });
            }

            internal void StopListening()
            {
                _listener?.Close();
                _listener = null;
            }

            private void Accept(Socket accepted)
            {
                var link = new LengthedSocket(accepted, SizeType.Word, true);

                link.OnReceive = data =>
                {
                    if ((CommOpcode)data.Buffer[data.BaseOffset + data.Offset] != CommOpcode.LoginRequest)
                        return;

                    var refused = Interlocked.Increment(ref _logins) <= Refusals;

                    link.Send(new LoginResponsePacket { Response = refused ? CommLoginReason.Failure : CommLoginReason.Success });
                };
                link.OnError = _ => { };

                lock (Links)
                    Links.Add(link);

                link.ReceiveAsync();
            }

            public void Dispose()
            {
                StopListening();

                lock (Links)
                    foreach (var link in Links)
                        link.Close();
            }
        }

        /// <summary>
        /// The Auth server's end of the link, as the Auth server runs it: its accept handler on
        /// a listener, which makes its CommunicatorClient, whose login goes through its
        /// AuthenticateGameServer and whose loss goes through its DisconnectCommunicator. One
        /// server slot is defined, 1, with the password the game server here logs in with.
        /// Nothing else of the Auth server is there: no database, no clients, no loop.
        /// </summary>
        private sealed class RealAuth : IDisposable
        {
            private readonly Rasa.Auth.Server _server;
            private readonly LengthedSocket _listener;

            internal int Port { get; }

            internal RealAuth(int port = 0)
            {
                _server = (Rasa.Auth.Server)RuntimeHelpers.GetUninitializedObject(typeof(Rasa.Auth.Server));

                // Its finalizer is Shutdown, which stops a loop that this one does not have: an
                // exception on the finalizer thread, which ends the test process.
                GC.SuppressFinalize(_server);

                foreach (var field in new[] { "Config", "Clients", "ServerList", "Timer", "GameServerQueue", "GameServers" })
                    Set(_server, $"<{field}>k__BackingField", Activator.CreateInstance(Field(field).FieldType));

                var config = Field("Config").GetValue(_server);

                config.GetType().GetProperty("Servers").SetValue(config, new Dictionary<string, string> { ["1"] = "fixture" });

                _listener = new LengthedSocket(SizeType.Word);
                _listener.OnAccept = (LengthedSocket.AcceptHandler)Delegate.CreateDelegate(typeof(LengthedSocket.AcceptHandler), _server,
                    typeof(Rasa.Auth.Server).GetMethod("OnCommunicatorAccept", BindingFlags.Instance | BindingFlags.NonPublic));
                _listener.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Bind(new IPEndPoint(IPAddress.Loopback, port));
                _listener.Listen(8);

                Port = ((IPEndPoint)_listener.Socket.LocalEndPoint).Port;

                // The accept handler arms the next accept on this.
                Set(_server, "<AuthCommunicator>k__BackingField", _listener);

                _listener.AcceptAsync();
            }

            private static FieldInfo Field(string property)
            {
                var info = typeof(Rasa.Auth.Server).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(info, property);

                return info;
            }

            /// <summary>The link that holds a server slot, or null while nothing does.</summary>
            internal Rasa.Auth.CommunicatorClient Holder(byte serverId)
            {
                var servers = (Dictionary<byte, Rasa.Auth.CommunicatorClient>)Field("GameServers").GetValue(_server);

                lock (servers)
                    return servers.TryGetValue(serverId, out var client) ? client : null;
            }

            public void Dispose()
            {
                _listener.Close();

                var servers = (Dictionary<byte, Rasa.Auth.CommunicatorClient>)Field("GameServers").GetValue(_server);
                List<Rasa.Auth.CommunicatorClient> links;

                lock (servers)
                    links = new List<Rasa.Auth.CommunicatorClient>(servers.Values);

                foreach (var link in links)
                    link.Socket.Close();
            }
        }
    }
}
