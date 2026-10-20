using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Config;
    using Rasa.Voice;

    /// <summary>
    /// The voice server's connections before the login (VoiceServer.Hello): nothing before the
    /// login is authenticated, so a Hello - eight bytes, answered with a key offer of 35 - from any
    /// address, real or not, must neither grow the session table without bound, nor turn the
    /// server into a reflector, nor end somebody's logged-in connection.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class VoiceServerSessionTests
    {
        private const uint Squad = 77;
        private const ulong Account = 4242;

        [TestMethod]
        public void SpoofedHellosOpenNoMoreThanTheCapsAllow()
        {
            using var voice = new Fixture();

            // Many ports of one address: MaxPendingPerAddress.
            for (var port = 1; port <= 50; port++)
                voice.Server.Received(new IPEndPoint(IPAddress.Parse("127.0.9.1"), 40000 + port), Hello);

            Assert.AreEqual(VoiceServer.MaxPendingPerAddress, voice.Sessions().Count);

            // Many addresses: MaxPending in all.
            for (var i = 0; i < 400; i++)
                voice.Server.Received(new IPEndPoint(IPAddress.Parse($"127.0.{10 + i / 200}.{1 + i % 200}"), 50000), Hello);

            Assert.AreEqual(VoiceServer.MaxPending, voice.Sessions().Count);

            // The table full of strangers keeps nobody logged in from logging in again: a
            // logged-in connection is not counted, and has its own entry.
            Assert.IsTrue(voice.Sessions().All(s => !(bool)Field(s, "LoggedIn")));
        }

        [TestMethod]
        public void AHelloRepeatedAtOnceGetsOneKeyOffer()
        {
            using var voice = new Fixture();
            using var client = voice.Client();

            for (var i = 0; i < 20; i++)
                voice.Server.Received(client.EndPoint, Hello);

            Assert.AreEqual(1, client.Received().Count(d => d.Flags == Talkback.FlagKeyOffer), "one offer in KeyOfferResendMs");
            Assert.AreEqual(1, voice.Sessions().Count);
        }

        [TestMethod]
        public void AHelloDoesNotEndALoggedInConnection()
        {
            using var voice = new Fixture();
            using var client = voice.Client();

            voice.LogIn(client, voice.Server.IssueTicket(Squad, Account));
            Assert.AreEqual(Talkback.ResultSuccess, client.LoginResult());

            // A spoofed Hello from the player's own address and port: they stay in their squad.
            voice.Server.Received(client.EndPoint, Hello);

            var session = voice.Sessions().Single();
            Assert.IsTrue((bool)Field(session, "LoggedIn"));
            Assert.AreEqual(Account, (ulong)Field(session, "PlayerId"));
            Assert.IsTrue(voice.Server.Describe().Any(line => line.Contains($"squad {Squad}") && line.Contains($"account {Account}")));

            // Their own client really starting over on that port: the handshake again, and its
            // login with the ticket it has now is answered, on the same connection.
            client.Received();
            voice.Age(session, VoiceServer.KeyOfferResendMs + 1);
            voice.LogIn(client, voice.Server.IssueTicket(Squad, Account));
            Assert.AreEqual(Talkback.ResultSuccess, client.LoginResult());
            Assert.AreEqual(1, voice.Sessions().Count);
            Assert.IsTrue((bool)Field(voice.Sessions().Single(), "LoggedIn"));
        }

        [TestMethod]
        public void TheSameClientStartingOverForAnotherSquadMovesWithItsNewTicket()
        {
            using var voice = new Fixture();
            using var client = voice.Client();

            voice.LogIn(client, voice.Server.IssueTicket(Squad, Account));
            client.Received();

            voice.Age(voice.Sessions().Single(), VoiceServer.KeyOfferResendMs + 1);
            voice.LogIn(client, voice.Server.IssueTicket(Squad + 1, Account), Squad + 1);

            Assert.AreEqual(Talkback.ResultSuccess, client.LoginResult());
            Assert.AreEqual(Squad + 1, (uint)Field(voice.Sessions().Single(), "GroupId"));
            Assert.IsFalse(voice.Server.Describe().Any(line => line.Contains($"squad {Squad}:")), "out of the old squad");
        }

        [TestMethod]
        public void AConnectionThatNeverLogsInIsClosed()
        {
            using var voice = new Fixture();
            using var client = voice.Client();

            // The whole unauthenticated walk, which anybody can make blind: Hello, key, pong.
            voice.Server.Received(client.EndPoint, Hello);
            voice.Server.Received(client.EndPoint, Talkback.Datagram(Talkback.FlagClientKey, new byte[8]));
            voice.Server.Received(client.EndPoint, Talkback.Datagram(Talkback.FlagPong, ReadOnlySpan<byte>.Empty));
            client.Received();

            var session = voice.Sessions().Single();
            voice.Age(session, VoiceServer.LoginTimeoutMs + 1, created: true);
            voice.Housekeeping();

            Assert.IsEmpty(voice.Sessions());
            Assert.IsTrue(client.Received().Any(d => d.Flags == Talkback.FlagClose));
        }

        private static readonly byte[] Hello = Talkback.Datagram(Talkback.FlagHello, ReadOnlySpan<byte>.Empty);

        private static object Field(object session, string name) =>
            session.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(session);

        private sealed class Fixture : IDisposable
        {
            public VoiceServer Server { get; }

            public Fixture()
            {
                Server = (VoiceServer)Activator.CreateInstance(typeof(VoiceServer), nonPublic: true);
                Assert.IsTrue(Server.Apply(new VoiceConfig { Enabled = true, BindAddress = "127.0.0.1", Port = FreePort() }, "127.0.0.1"));
            }

            public List<object> Sessions() =>
                ((IDictionary)typeof(VoiceServer).GetField("_sessions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Server))
                .Values.Cast<object>().ToList();

            public Peer Client() => new Peer();

            /// <summary>Hello, the client's key, the pong, and the login: the client's whole way in.</summary>
            public void LogIn(Peer client, uint token, uint squad = Squad)
            {
                Server.Received(client.EndPoint, Hello);
                Server.Received(client.EndPoint, Talkback.Datagram(Talkback.FlagClientKey, new byte[8]));
                Server.Received(client.EndPoint, Talkback.Datagram(Talkback.FlagPong, ReadOnlySpan<byte>.Empty));

                var login = new Talkback.MessageWriter();
                login.U8(Talkback.Login);
                login.F32(Talkback.ClientVersion);
                login.U32(squad);
                login.U64(Account);
                login.U32(token);
                login.String("player");
                login.U8(VoiceServer.DefaultCodec);
                login.U8(0);
                login.String("");
                login.String("");

                Server.Received(client.EndPoint, Talkback.Datagram(Talkback.FlagData, login.ToArray()));
            }

            /// <summary>Moves a connection's clock back, as if that long had passed.</summary>
            public void Age(object session, long ms, bool created = false)
            {
                var type = session.GetType();
                var offer = type.GetField("LastOfferTick")!;
                offer.SetValue(session, (long)offer.GetValue(session) - ms);

                if (created)
                {
                    var made = type.GetField("CreatedTick")!;
                    made.SetValue(session, (long)made.GetValue(session) - ms);
                }
            }

            public void Housekeeping() =>
                typeof(VoiceServer).GetMethod("Housekeeping", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Server, null);

            public void Dispose() => Server.Stop();

            private static int FreePort()
            {
                using var probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                probe.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)probe.LocalEndPoint).Port;
            }
        }

        /// <summary>A client's UDP port, to see what the server sends it.</summary>
        private sealed class Peer : IDisposable
        {
            private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            public Peer()
            {
                _socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                _socket.ReceiveTimeout = 300;
            }

            public IPEndPoint EndPoint => (IPEndPoint)_socket.LocalEndPoint;

            public List<(byte Flags, byte[] Payload)> Received()
            {
                var datagrams = new List<(byte, byte[])>();
                var buffer = new byte[2048];

                while (true)
                {
                    try
                    {
                        var length = _socket.Receive(buffer);

                        if (Talkback.TryParse(buffer.AsSpan(0, length), out var flags, out var payload))
                            datagrams.Add((flags, payload.ToArray()));
                    }
                    catch (SocketException)
                    {
                        return datagrams;
                    }
                }
            }

            /// <summary>The result byte of the last login reply received.</summary>
            public byte LoginResult()
            {
                var reply = Received().Last(d => d.Flags == Talkback.FlagData && d.Payload.Length > 1 && d.Payload[0] == Talkback.LoginReply);
                return reply.Payload[1];
            }

            public void Dispose() => _socket.Dispose();
        }
    }
}
