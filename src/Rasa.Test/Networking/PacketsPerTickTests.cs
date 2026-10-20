using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Memory;
    using Rasa.Packets.Protocol;

    /// <summary>
    /// A connection's packets are handled at most Client.MaxPacketsPerTick a tick, the rest left
    /// in the stream for the next (Client.ReadIncomingPackets). The limit used to be checked
    /// after the next frame had been read out of the stream and its sequence accepted, so the
    /// packet one past it was lost every time it was reached (BR-219).
    /// </summary>
    [TestClass]
    public class PacketsPerTickTests
    {
        private static readonly FieldInfo Inbound = typeof(Client).GetField("_incomingDataQueue", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestMethod]
        public void ThePacketPastTheLimitWaitsForTheNextTick()
        {
            var client = new Client(null, new ClientPacketHandler());
            const int sent = Client.MaxPacketsPerTick + 6;

            // As the client sends them: one channel, each with the next sequence number.
            for (var sequence = 1u; sequence <= sent; sequence++)
                Receive(client, ProtocolFramingTests.CreatePing(sequence, channel: 1, sequence: sequence));

            var first = Tick(client);
            var second = Tick(client);

            CollectionAssert.AreEqual(Enumerable.Range(1, Client.MaxPacketsPerTick).Select(i => (uint)i).ToList(), first);
            CollectionAssert.AreEqual(Enumerable.Range(Client.MaxPacketsPerTick + 1, 6).Select(i => (uint)i).ToList(), second,
                "the rest, from the one past the limit");
            Assert.AreEqual(0, Tick(client).Count);
        }

        [TestMethod]
        public void AHandlerThatStopsLeavesTheRestInTheStream()
        {
            var client = new Client(null, new ClientPacketHandler());

            for (var time = 1u; time <= 3; time++)
                Receive(client, ProtocolFramingTests.CreatePing(time));

            var handled = new List<uint>();
            client.ReadIncomingPackets(packet =>
            {
                handled.Add(((PingMessage)packet.Message).ClientTime);
                return false;
            });

            CollectionAssert.AreEqual(new uint[] { 1 }, handled);
            CollectionAssert.AreEqual(new uint[] { 2, 3 }, Tick(client));
        }

        private static List<uint> Tick(Client client)
        {
            var handled = new List<uint>();

            client.ReadIncomingPackets(packet =>
            {
                handled.Add(((PingMessage)packet.Message).ClientTime);
                return true;
            });

            return handled;
        }

        /// <summary>Into the inbound stream, as Client.DrainPendingChunks puts what the socket read: a pooled array it then owns.</summary>
        private static void Receive(Client client, byte[] frame)
        {
            var pooled = ArrayPool<byte>.Shared.Rent(frame.Length);
            frame.CopyTo(pooled, 0);
            ((NonContiguousMemoryStream)Inbound.GetValue(client)).AddSharedPoolArray(pooled, frame.Length);
        }
    }
}
