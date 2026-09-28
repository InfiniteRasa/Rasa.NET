using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Memory;

    [TestClass]
    public class ProtocolValueReaderTests
    {
        [TestMethod]
        public void UnsupportedFlagsAreInvalidInput()
        {
            using var binary = new BinaryReader(new MemoryStream(new byte[] { 64 }));
            using var reader = new ProtocolBufferReader(binary, ProtocolBufferFlags.DontFragment);

            Assert.ThrowsExactly<InvalidDataException>(() => reader.ReadProtocolFlags());
        }

        [TestMethod]
        public void IncorrectChecksumIsInvalidInput()
        {
            using var binary = new BinaryReader(new MemoryStream(new byte[] { 0 }));
            using var reader = new ProtocolBufferReader(binary, ProtocolBufferFlags.DontFragment);

            Assert.ThrowsExactly<InvalidDataException>(() => reader.ReadXORCheck(1));
        }

        [TestMethod]
        public void TruncatedCountDoesNotBecomeAValidLength()
        {
            using var binary = new BinaryReader(new MemoryStream(new byte[] { 0xC1 }));
            using var reader = new ProtocolBufferReader(binary, ProtocolBufferFlags.DontFragment);

            Assert.ThrowsExactly<EndOfStreamException>(() => reader.ReadCount());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TruncatedValuesAreRejected(bool text)
        {
            using var binary = new BinaryReader(new MemoryStream(new byte[] { 5, 65 }));
            using var reader = new ProtocolBufferReader(binary, ProtocolBufferFlags.DontFragment);

            Assert.ThrowsExactly<EndOfStreamException>(() =>
            {
                if (text)
                    reader.ReadString();
                else
                    reader.ReadArray();
            });
        }

        [TestMethod]
        [DataRow(new byte[] { 0x01 }, 1)]
        [DataRow(new byte[] { 0x41 }, -1)]
        [DataRow(new byte[] { 0xC1, 0x01 }, -65)]
        public void SignedSevenBitIntegersPreserveTheirSign(byte[] bytes, int expected)
        {
            using var binary = new BinaryReader(new MemoryStream(bytes));
            using var reader = new ProtocolBufferReader(binary, 0);

            Assert.AreEqual(expected, reader.ReadInt());
        }

        [TestMethod]
        public void ProtocolStringsUseUtf8ByteLength()
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
                writer.WriteString("é");

            stream.Position = 0;
            using var input = new BinaryReader(stream, Encoding.UTF8, true);
            using var reader = new ProtocolBufferReader(input, ProtocolBufferFlags.DontFragment);

            Assert.AreEqual("é", reader.ReadString());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
