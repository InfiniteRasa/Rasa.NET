extern alias RasaGame;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Cryptography;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Memory;
    using Rasa.Models;
    using Rasa.Networking;
    using Rasa.Packets.Protocol;
    using Rasa.Test.World;

    /// <summary>
    /// The main loop reads every client's input before it sends anyone anything
    /// (Server.ServiceClients). It read a client and sent its queue, then went on to the next:
    /// whatever a client later in the list did to one earlier in it - a move, a shot, a hit -
    /// waited a whole tick, 100 ms, every time, for every pair of players one way round.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TickOrderTests
    {
        private static readonly FieldInfo Inbound = typeof(Client).GetField("_incomingDataQueue", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<Socket> _sockets = new List<Socket>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var socket in _sockets)
                socket.Dispose();

            _sockets.Clear();
        }

        [TestMethod]
        public void AMoveReachesAPlayerEarlierInTheListInTheTickItWasMade()
        {
            using var world = new WorldTestContext();
            var earlier = PlayerDeathTests.Player(world, 0, 0);
            var later = PlayerDeathTests.Player(world, 3, 0);
            var sentToEarlier = Connect(earlier);
            Connect(later);
            WorldTestContext.Drain(earlier);
            WorldTestContext.Drain(later);

            // Stood there a while before stepping: a step straight after the character is made
            // has no walking saved up to pay for it, and is refused as too fast (AcceptMove).
            later.Player.MoveBudgetTick = Environment.TickCount64 - 2000;
            later.Player.MoveBudget = 0;

            var step = new Movement(later.Player.Position + new Vector3(0.5f, 0, 0), new Vector2(0.25f, 0));
            Receive(later, Move(step));

            Server.ServiceClients(new List<Client> { earlier, later }, new List<Client>(), 100);

            Assert.AreEqual(step.Position, later.Player.Position, "the move was taken");
            Assert.IsEmpty(WorldTestContext.Drain(earlier), "nothing left waiting for the next tick");

            var expected = Frame(new ProtocolPacket(new MoveObjectMessage(later.Player.EntityId, later.Movement), ClientMessageOpcode.MoveObject, false, 1)
            {
                SequenceNumber = 1
            });

            Assert.IsTrue(sentToEarlier.Any(frame => frame.SequenceEqual(expected)), "sent this tick");
        }

        [TestMethod]
        public void EachClientIsSentWhatWasQueuedForItAndItsOwnRepliesStayInOrder()
        {
            using var world = new WorldTestContext();
            var first = PlayerDeathTests.Player(world, 0, 0);
            var second = PlayerDeathTests.Player(world, 3, 0);
            var sentToFirst = Connect(first);
            var sentToSecond = Connect(second);
            WorldTestContext.Drain(first);
            WorldTestContext.Drain(second);

            first.CallMethod(SysEntity.ClientMethodId, new Rasa.Packets.MapChannel.Server.GameEffectDetachedPacket { EffectId = 1 });
            second.CallMethod(SysEntity.ClientMethodId, new Rasa.Packets.MapChannel.Server.GameEffectDetachedPacket { EffectId = 2 });
            second.CallMethod(SysEntity.ClientMethodId, new Rasa.Packets.MapChannel.Server.GameEffectDetachedPacket { EffectId = 3 });

            Server.ServiceClients(new List<Client> { first, second }, new List<Client>(), 100);

            Assert.HasCount(1, sentToFirst);
            Assert.HasCount(2, sentToSecond);
            CollectionAssert.AreEqual(Detached(2), sentToSecond[0]);
            CollectionAssert.AreEqual(Detached(3), sentToSecond[1]);
        }

        [TestMethod]
        public void AClientWhoseInputClosesItIsSentNothingAndTheRestAreServed()
        {
            using var world = new WorldTestContext();
            var closing = PlayerDeathTests.Player(world, 0, 0);
            var other = PlayerDeathTests.Player(world, 3, 0);
            var sentToClosing = Connect(closing);
            var sentToOther = Connect(other);
            WorldTestContext.Drain(closing);
            WorldTestContext.Drain(other);

            closing.CallMethod(SysEntity.ClientMethodId, new Rasa.Packets.MapChannel.Server.GameEffectDetachedPacket { EffectId = 1 });
            other.CallMethod(SysEntity.ClientMethodId, new Rasa.Packets.MapChannel.Server.GameEffectDetachedPacket { EffectId = 2 });

            // A second Login from a client in the world is not the client: it is disconnected.
            Receive(closing, Frame(new ProtocolPacket(new LoginMessage { Subtype = LoginMessageSubtype.Type1, Version = "1.16.5.0" }, ClientMessageOpcode.Login, false, 0)));

            var read = new List<Client>();
            Server.ServiceClients(new List<Client> { closing, other }, read, 100);

            Assert.AreEqual(ClientState.Disconnected, closing.State);
            Assert.IsEmpty(sentToClosing);
            Assert.HasCount(1, sentToOther);
            Assert.IsEmpty(read, "left empty for the next tick");
        }

        private static byte[] Detached(uint effectId) =>
            Frame(new ProtocolPacket(new CallMethodMessage((ulong)SysEntity.ClientMethodId,
                new Rasa.Packets.MapChannel.Server.GameEffectDetachedPacket { EffectId = (int)effectId }), ClientMessageOpcode.CallMethod, false, 0));

        /// <summary>
        /// A loopback connection for the client, as RegisterAtServer gives it one, with every frame
        /// handed to the socket recorded as it is written (before it is encrypted).
        /// </summary>
        private List<byte[]> Connect(Client client)
        {
            BufferManager.Initialize(8192, 8, 8);
            LengthedSocket.InitializeEventArgsPool(64);

            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            peer.Connect(listener.LocalEndPoint);
            var accepted = listener.Accept();
            _sockets.AddRange(new[] { listener, peer, accepted });

            var transport = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false };
            var state = client.State;

            client.RegisterAtServer(null, transport, new ClientCryptData());
            client.State = state;

            var sent = new List<byte[]>();
            transport.OnEncrypt = (BufferData data, ref int length) =>
                sent.Add(data.Buffer.AsSpan(data.BaseOffset + data.Offset, length).ToArray());

            return sent;
        }

        private static byte[] Move(Movement movement)
        {
            var message = new MoveMessage();
            typeof(MoveMessage).GetProperty(nameof(MoveMessage.Movement)).SetValue(message, movement);

            return Frame(new ProtocolPacket(message, ClientMessageOpcode.Move, false, 0));
        }

        private static byte[] Frame(ProtocolPacket packet)
        {
            using var stream = new MemoryStream();

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                packet.Write(writer);

            return stream.ToArray();
        }

        /// <summary>Into the inbound stream, as Client.DrainPendingChunks puts what the socket read.</summary>
        private static void Receive(Client client, byte[] frame)
        {
            var pooled = ArrayPool<byte>.Shared.Rent(frame.Length);
            frame.CopyTo(pooled, 0);
            ((NonContiguousMemoryStream)Inbound.GetValue(client)).AddSharedPoolArray(pooled, frame.Length);
        }
    }
}
