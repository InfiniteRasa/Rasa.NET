using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Memory;
    using Rasa.Networking;
    using Rasa.Packets;

    /// <summary>
    /// What LengthedSocket.Send puts on the wire: each packet as a length and its bytes, end to
    /// end, as many to a pooled block as fit. The frames of a block are written through one
    /// writer over it (FillBlock); each used to have a stream and a writer of its own.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SendFramingTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
            BufferManager.Initialize(8192, 8, 8);
            LengthedSocket.InitializeEventArgsPool(64);
        }

        [TestMethod]
        public async Task FramesGoEndToEndAcrossBlocksEachWithItsLength()
        {
            // Enough to fill several blocks, of lengths that do not divide them.
            var packets = Enumerable.Range(0, 400).Select(i => new Bytes(Payload(i, 1 + i * 37 % 211))).ToList();

            var received = await SendAndReceive(packets, Wire(packets));

            CollectionAssert.AreEqual(Wire(packets), received);
        }

        [TestMethod]
        public async Task APacketThatFailsIsSkippedAndTheFramesEitherSideAreWhole()
        {
            var good = Enumerable.Range(0, 60).Select(i => new Bytes(Payload(i, 90))).ToList();
            var packets = new List<IBasePacket>(good);

            // Partway through a block, and failing after it has written some of itself.
            packets.Insert(30, new Failing());
            packets.Insert(0, new Failing());

            var received = await SendAndReceive(packets, Wire(good));

            CollectionAssert.AreEqual(Wire(good), received);
        }

        [TestMethod]
        public async Task TheEncryptionHookIsGivenEachFrameWhereItIs()
        {
            var packets = Enumerable.Range(0, 200).Select(i => new Bytes(Payload(i, 3 + i % 50))).ToList();
            var seen = new List<byte[]>();

            var expected = Wire(packets.Select(p => new Bytes(p.Data.Select(b => (byte)(b ^ 0x5A)).ToArray())).ToList());
            var received = await SendAndReceive(packets, expected, (BufferData data, ref int length) =>
            {
                seen.Add(data.Buffer.AsSpan(data.BaseOffset + data.Offset, length).ToArray());

                for (var i = 0; i < length; i++)
                    data[data.Offset + i] ^= 0x5A;
            });

            Assert.HasCount(packets.Count, seen);
            for (var i = 0; i < packets.Count; i++)
                CollectionAssert.AreEqual(packets[i].Data, seen[i], $"frame {i}, as written");

            CollectionAssert.AreEqual(expected, received, "and sent as the hook left it");
        }

        [TestMethod]
        public async Task WritingTheFramesNoLongerAllocatesForEachOne()
        {
            var packets = Enumerable.Range(0, 2000).Select(i => new Bytes(Payload(i, 40))).ToList();

            // Once to warm up, then measured.
            await SendAndReceive(packets.Take(50).ToList(), Wire(packets.Take(50).ToList()));

            long allocated = 0;
            await SendAndReceive(packets, Wire(packets), measured: bytes => allocated = bytes);

            // A stream and a writer each were about 150 bytes a frame; a block holds ~170 of these.
            Assert.IsLessThan(20 * packets.Count, allocated, $"{allocated} bytes for {packets.Count} frames");
        }

        private static byte[] Payload(int seed, int length) => Enumerable.Range(0, length).Select(j => (byte)(seed * 7 + j)).ToArray();

        private static byte[] Wire(IEnumerable<IBasePacket> packets)
        {
            using var stream = new MemoryStream();

            foreach (var packet in packets.OfType<Bytes>())
            {
                stream.Write(BitConverter.GetBytes(packet.Data.Length));
                stream.Write(packet.Data);
            }

            return stream.ToArray();
        }

        private static async Task<byte[]> SendAndReceive(IReadOnlyList<IBasePacket> packets, byte[] expected,
            LengthedSocket.EncryptDelegate encrypt = null, Action<long> measured = null)
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            using var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await peer.ConnectAsync(listener.LocalEndPoint);
            using var accepted = await listener.AcceptAsync();

            var transport = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false, OnEncrypt = encrypt };

            var before = GC.GetAllocatedBytesForCurrentThread();
            transport.Send(packets);
            measured?.Invoke(GC.GetAllocatedBytesForCurrentThread() - before);

            var received = new byte[expected.Length];
            var got = 0;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (got < received.Length)
                got += await peer.ReceiveAsync(received.AsMemory(got), SocketFlags.None, timeout.Token);

            // Nothing after what was expected.
            await Task.Delay(50);
            Assert.AreEqual(0, peer.Available, "nothing more on the wire");

            transport.Close();
            return received;
        }

        private sealed class Bytes : IBasePacket
        {
            public byte[] Data { get; }

            public Bytes(byte[] data) => Data = data;

            public void Read(BinaryReader br) => throw new NotSupportedException();

            public void Write(BinaryWriter bw) => bw.Write(Data);
        }

        private sealed class Failing : IBasePacket
        {
            public void Read(BinaryReader br) => throw new NotSupportedException();

            public void Write(BinaryWriter bw)
            {
                bw.Write(new byte[] { 1, 2, 3, 4, 5 });
                throw new InvalidOperationException("fails partway through");
            }
        }
    }
}
