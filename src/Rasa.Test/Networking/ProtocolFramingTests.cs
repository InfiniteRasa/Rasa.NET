using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Data;
    using Rasa.Memory;
    using Rasa.Packets.Protocol;
    using Rasa.Test.Memory;

    [TestClass]
    public class ProtocolFramingTests
    {
        [TestMethod]
        [DataRow((ushort)0)]
        [DataRow((ushort)1)]
        [DataRow((ushort)2)]
        [DataRow((ushort)3)]
        public void ReadRejectsLengthSmallerThanHeader(ushort size)
        {
            using var stream = new MemoryStream(new[] { (byte)size, (byte)0, (byte)0, (byte)0 });
            using var reader = new BinaryReader(stream);

            Assert.ThrowsExactly<InvalidDataException>(() => new ProtocolPacket().Read(reader));
        }

        [TestMethod]
        public void ReadRejectsBodyOutsideDeclaredFrame()
        {
            var bytes = CreatePing(123);
            bytes[0] = (byte)(bytes.Length - 1);
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            Assert.Throws<IOException>(() => new ProtocolPacket().Read(reader));
        }

        [TestMethod]
        public void ReadRejectsUnusedBytesInsideDeclaredFrame()
        {
            var bytes = CreatePing(123);
            Array.Resize(ref bytes, bytes.Length + 1);
            bytes[0] = (byte)bytes.Length;
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            Assert.ThrowsExactly<InvalidDataException>(() => new ProtocolPacket().Read(reader));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(9)]
        public void CompressedFrameDecodesIndependently(int prefixLength)
        {
            var bytes = CreateCompressedPing(123);
            using var stream = new MemoryStream();
            stream.Write(new byte[prefixLength]);
            stream.Write(bytes);
            stream.Position = prefixLength;
            using var reader = new BinaryReader(stream);
            var packet = new ProtocolPacket();

            packet.Read(reader);

            Assert.AreEqual(123U, ((PingMessage)packet.Message).ClientTime);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(9)]
        public void CompressedFrameDoesNotConsumeTheFollowingFrame(int prefixLength)
        {
            var first = CreateCompressedPing(123);
            var second = CreatePing(456);
            using var stream = new MemoryStream();
            stream.Write(new byte[prefixLength]);
            stream.Write(first);
            stream.Write(second);
            stream.Position = prefixLength;
            using var reader = new BinaryReader(stream);
            var packet = new ProtocolPacket();

            packet.Read(reader);

            Assert.AreEqual(123U, ((PingMessage)packet.Message).ClientTime);
            Assert.AreEqual((long)prefixLength + first.Length, stream.Position);
            var following = new ProtocolPacket();
            following.Read(reader);
            Assert.AreEqual(456U, ((PingMessage)following.Message).ClientTime);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ForgedExpandedLengthDoesNotReserveTheClaimedBuffer()
        {
            const int claimedLength = 4 * ushort.MaxValue + 1;
            using var stream = new MemoryStream(CreateCompressedPing(123, claimedLength));
            using var reader = new BinaryReader(stream);
            using var buffers = new ArrayPoolTracker();
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            var error = Assert.ThrowsExactly<InvalidDataException>(() => new ProtocolPacket().Read(reader));

            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Assert.AreEqual(
                $"Decompressed protocol size cannot exceed {4 * ushort.MaxValue} bytes.",
                error.Message);
            Assert.IsTrue(buffers.LargestRent < claimedLength,
                "An unverified expansion length must not reserve that much pooled memory.");
            Assert.IsTrue(allocated < claimedLength,
                "An unverified expansion length must not preallocate a managed buffer either.");
        }

        [TestMethod]
        public void MissingFinalDeflateBlockIsRejectedEvenWhenAllOutputWasProduced()
        {
            using var stream = new MemoryStream(CreateCompressedPing(123, missingFinalBlock: true));
            using var reader = new BinaryReader(stream);

            Assert.ThrowsExactly<EndOfStreamException>(() => new ProtocolPacket().Read(reader));
        }

        [TestMethod]
        public void BytesAfterTheFinalDeflateBlockAreRejected()
        {
            using var stream = new MemoryStream(CreateCompressedPing(123, trailingCompressedByte: true));
            using var reader = new BinaryReader(stream);

            Assert.ThrowsExactly<InvalidDataException>(() => new ProtocolPacket().Read(reader));
        }

        [TestMethod]
        public void PacketSpanningNonContiguousBuffersDecodesWithoutFlatteningTheStream()
        {
            var bytes = CreatePing(123);
            using var stream = new NonContiguousMemoryStream();
            stream.CopyFromArray(bytes, 0, 3);
            stream.CopyFromArray(bytes, 3, 2);
            stream.CopyFromArray(bytes, 5, bytes.Length - 5);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            var packet = new ProtocolPacket();
            packet.Read(reader);

            Assert.AreEqual(123U, ((PingMessage)packet.Message).ClientTime);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        internal static byte[] CreatePing(uint clientTime, byte channel = 0, uint sequence = 0)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            new ProtocolPacket(new PingMessage { ClientTime = clientTime },
                ClientMessageOpcode.Ping, false, channel) { SequenceNumber = sequence }.Write(writer);
            return stream.ToArray();
        }

        private static byte[] CreateCompressedPing(uint clientTime, int? declaredLength = null,
            bool missingFinalBlock = false, bool trailingCompressedByte = false)
        {
            using var body = new MemoryStream();
            using (var binary = new BinaryWriter(body, Encoding.UTF8, true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteProtocolFlags();
                writer.WriteUInt(clientTime);
                writer.WriteXORCheck(0);
            }
            var payload = body.ToArray();
            var compressed = Deflate(payload);
            var wireLength = compressed.Length + 5 + (trailingCompressedByte ? 1 : 0);
            payload[payload.Length - 1] = (byte)(wireLength ^ (wireLength >> 8) ^ (wireLength >> 16) ^ (wireLength >> 24));
            compressed = Deflate(payload);
            if (missingFinalBlock)
                compressed[0] &= 0xFE;
            if (trailingCompressedByte)
            {
                Array.Resize(ref compressed, compressed.Length + 1);
                compressed[compressed.Length - 1] = 0xAA;
            }

            using var frame = new MemoryStream();
            using var output = new BinaryWriter(frame, Encoding.UTF8, true);
            output.Write((ushort)0);
            output.Write((byte)0);
            output.Write((byte)0);
            using (var writer = new ProtocolBufferWriter(output, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteProtocolFlags();
                writer.WritePacketType((ushort)ClientMessageOpcode.Ping, true);
                writer.WriteXORCheck(3);
            }
            output.Write((byte)1);
            output.Write(declaredLength ?? payload.Length);
            output.Write(compressed);
            frame.Position = 0;
            output.Write((ushort)frame.Length);
            return frame.ToArray();
        }

        private static byte[] Deflate(byte[] bytes)
        {
            using var stream = new MemoryStream();
            using (var deflate = new DeflateStream(stream, CompressionLevel.NoCompression, true))
                deflate.Write(bytes);
            return stream.ToArray();
        }
    }
}
