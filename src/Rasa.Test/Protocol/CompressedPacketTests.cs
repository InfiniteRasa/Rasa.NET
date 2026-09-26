using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Protocol
{
    using Rasa.Data;
    using Rasa.Memory;
    using Rasa.Packets.Protocol;
    using Rasa.Test.Memory;

    [TestClass]
    [DoNotParallelize]
    public class CompressedPacketTests
    {
        private static readonly string LargeVersion = new string('x', 20000);

        [TestMethod]
        public void ReadDecodesEntireLargeCompressedMessage()
        {
            using var stream = new MemoryStream(CreatePacket());
            using var reader = new BinaryReader(stream);
            var packet = new ProtocolPacket();
            using var buffers = new ArrayPoolTracker();

            packet.Read(reader);

            buffers.AssertReturned();
            Assert.IsTrue(packet.Compress);
            Assert.AreEqual(ClientMessageOpcode.Login, packet.Type);
            Assert.IsInstanceOfType<LoginMessage>(packet.Message);
            var message = (LoginMessage)packet.Message;
            Assert.AreEqual(123U, message.AccountId);
            Assert.AreEqual(456U, message.OneTimeKey);
            Assert.AreEqual(LargeVersion, message.Version);
            Assert.AreEqual(stream.Length, stream.Position);
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadRejectsPrematureDecompressionEofAndReturnsBuffer(bool truncate)
        {
            using var stream = new MemoryStream(CreatePacket(truncate: truncate, extraDeclaredByte: !truncate));
            using var reader = new BinaryReader(stream);
            using var buffers = new ArrayPoolTracker();

            Assert.ThrowsExactly<EndOfStreamException>(() => new ProtocolPacket().Read(reader));

            buffers.AssertReturned();
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        public void ReadReturnsDecompressionBufferWhenMessageParsingFails()
        {
            using var stream = new MemoryStream(CreatePacket(subtype: 3));
            using var reader = new BinaryReader(stream);
            using var buffers = new ArrayPoolTracker();

            var error = Assert.ThrowsExactly<InvalidDataException>(() => new ProtocolPacket().Read(reader));

            buffers.AssertReturned();
            Assert.AreEqual("Invalid Subtype found!", error.Message);
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        public void ReadReturnsBufferWhenDeflateDataIsInvalid()
        {
            using var stream = new MemoryStream(CreatePacket(invalidDeflate: true));
            using var reader = new BinaryReader(stream);
            using var buffers = new ArrayPoolTracker();

            Assert.ThrowsExactly<InvalidDataException>(() => new ProtocolPacket().Read(reader));

            buffers.AssertReturned();
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public void UncompressedPacketPreservesFieldsAndCallerStream(byte channel)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            var outgoing = new ProtocolPacket(new PingMessage { ClientTime = 0x11223344 },
                ClientMessageOpcode.Ping, false, channel) { SequenceNumber = 1234 };
            outgoing.Write(writer);
            stream.Position = 0;
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var incoming = new ProtocolPacket();

            incoming.Read(reader);

            Assert.IsFalse(incoming.Compress);
            Assert.AreEqual(channel, incoming.Channel);
            Assert.AreEqual(channel == 0 ? 0U : 1234U, incoming.SequenceNumber);
            Assert.AreEqual(0x11223344U, ((PingMessage)incoming.Message).ClientTime);
            Assert.AreEqual(stream.Length, stream.Position);
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        public void WriteReturnsPacketBufferWhenMessageSerializationFails()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            using var buffers = new ArrayPoolTracker();
            var packet = new ProtocolPacket(new ThrowingMessage(), ClientMessageOpcode.Ping, false, 0);

            Assert.ThrowsExactly<InvalidDataException>(() => packet.Write(writer));

            buffers.AssertReturned();
        }

        private static byte[] CreatePacket(bool truncate = false, bool extraDeclaredByte = false,
            byte subtype = 1, bool invalidDeflate = false)
        {
            using var body = new MemoryStream();
            using (var binary = new BinaryWriter(body, Encoding.UTF8, true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteProtocolFlags();
                writer.WriteByte(subtype);
                writer.WriteUInt(123);
                writer.WriteUInt(456);
                writer.WriteString(LargeVersion);
                writer.WriteXORCheck(0);
            }
            var bytes = body.ToArray();
            var compressed = Deflate(bytes);
            // Preserve the existing compressed frame's wire-length XOR convention.
            var wireLength = compressed.Length + 5;
            bytes[bytes.Length - 1] = (byte)(wireLength ^ (wireLength >> 8) ^ (wireLength >> 16) ^ (wireLength >> 24));
            compressed = Deflate(bytes);
            if (invalidDeflate)
                compressed[0] = 0x07; // Reserved DEFLATE block type.

            using var packet = new MemoryStream();
            using var output = new BinaryWriter(packet, Encoding.UTF8, true);
            output.Write((ushort)0);
            output.Write((byte)0);
            output.Write((byte)0);
            using (var writer = new ProtocolBufferWriter(output, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteProtocolFlags();
                writer.WritePacketType((ushort)ClientMessageOpcode.Login, true);
                writer.WriteXORCheck(3);
            }
            output.Write((byte)1);
            output.Write(bytes.Length + (extraDeclaredByte ? 1 : 0));
            output.Write(compressed, 0, compressed.Length - (truncate ? 8 : 0));
            packet.Position = 0;
            output.Write((ushort)packet.Length);
            return packet.ToArray();
        }

        private static byte[] Deflate(byte[] bytes)
        {
            using var stream = new MemoryStream();
            using (var deflate = new DeflateStream(stream, CompressionLevel.NoCompression, true))
                deflate.Write(bytes);
            return stream.ToArray();
        }

        private sealed class ThrowingMessage : IClientMessage
        {
            public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.Ping;
            public byte RawSubtype { get; set; }
            public ClientMessageSubtypeFlag SubtypeFlags => ClientMessageSubtypeFlag.None;
            public byte MinSubtype => 0;
            public byte MaxSubtype => 0;

            public void Read(ProtocolBufferReader reader)
            {
            }

            public void Write(ProtocolBufferWriter writer)
            {
                throw new InvalidDataException("Synthetic serialization failure.");
            }
        }

    }
}
