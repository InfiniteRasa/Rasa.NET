using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Memory;
    using Rasa.Packets.Protocol;

    [TestClass]
    public class ProtocolInflaterTests
    {
        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
        [DataRow(CompressionLevel.SmallestSize)]
        public void AcceptsValidCompressionModes(CompressionLevel level)
        {
            var input = Encoding.UTF8.GetBytes(new string('a', 5000));
            var compressed = Compress(input, level);

            using var result = ProtocolInflater.Decompress(compressed, input.Length);

            CollectionAssert.AreEqual(input, result.ToArray());
            Assert.AreEqual(0L, result.Position);
        }

        [TestMethod]
        public void AcceptsFixedHuffmanFixture()
        {
            using var result = ProtocolInflater.Decompress(Convert.FromHexString("730400"), 1);

            CollectionAssert.AreEqual(new byte[] { 65 }, result.ToArray());
        }

        [TestMethod]
        [DataRow("7304")]
        [DataRow("010100FEFF")]
        public void LookaheadCannotCompleteTruncatedInput(string compressed)
        {
            Assert.ThrowsExactly<EndOfStreamException>(() =>
                ProtocolInflater.Decompress(Convert.FromHexString(compressed), 1));
        }

        [TestMethod]
        public void BackReferenceBeforeAnyOutputIsRejected()
        {
            Assert.ThrowsExactly<InvalidDataException>(() =>
                ProtocolInflater.Decompress(Convert.FromHexString("030200"), 3));
        }

        [TestMethod]
        public void ExpandedPayloadIsNotCappedAtTheEncodedFrameSize()
        {
            var input = Encoding.UTF8.GetBytes(new string('a', 100000));
            var compressed = Compress(input, CompressionLevel.Optimal);
            Assert.IsTrue(compressed.Length < ProtocolPacket.MaxSize);

            using var result = ProtocolInflater.Decompress(compressed, input.Length);

            CollectionAssert.AreEqual(input, result.ToArray());
        }

        [TestMethod]
        public void AcceptsTheMaximumExpandedProtocolPayload()
        {
            var input = Encoding.UTF8.GetBytes(new string('a', ProtocolPacket.MaxExpandedSize));
            var compressed = Compress(input, CompressionLevel.Optimal);
            Assert.IsTrue(compressed.Length < ProtocolPacket.MaxSize);

            using var result = ProtocolInflater.Decompress(compressed, input.Length);

            Assert.AreEqual(ProtocolPacket.MaxExpandedSize, result.Length);
        }

        [TestMethod]
        public void RejectsExpandedPayloadAboveMaximumBeforeInflating()
        {
            var error = Assert.ThrowsExactly<InvalidDataException>(() =>
                ProtocolInflater.Decompress(new byte[] { 0x07 }, ProtocolPacket.MaxExpandedSize + 1));

            Assert.AreEqual(
                $"Decompressed protocol size cannot exceed {ProtocolPacket.MaxExpandedSize} bytes.",
                error.Message);
        }

        private static byte[] Compress(byte[] input, CompressionLevel level)
        {
            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, level, true))
                deflate.Write(input);
            return output.ToArray();
        }
    }
}
