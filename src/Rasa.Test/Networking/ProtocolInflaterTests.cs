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
        [DataRow("730400FF")]
        [DataRow("730400FFEE")]
        public void BytesAfterTheFinalBlockAreRejected(string compressed)
        {
            var error = Assert.ThrowsExactly<InvalidDataException>(() =>
                ProtocolInflater.Decompress(Convert.FromHexString(compressed), 1));

            Assert.AreEqual("Compressed payload contains trailing bytes.", error.Message);
        }

        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
        [DataRow(CompressionLevel.SmallestSize)]
        public void BytesAfterALargerStreamAreRejected(CompressionLevel level)
        {
            var input = Encoding.UTF8.GetBytes(new string('a', 5000));
            var compressed = Compress(input, level);
            var padded = new byte[compressed.Length + 4];
            compressed.CopyTo(padded, 0);
            padded[^1] = 0x55;

            Assert.ThrowsExactly<InvalidDataException>(() => ProtocolInflater.Decompress(padded, input.Length));
        }

        // A stream cut off before its final block can still have given every byte of the
        // declared length - with a random payload it is the last byte of the stream that is
        // missing, and it carried nothing but the end of the block. DeflateStream reports no
        // error for it; the inflater has to.
        [TestMethod]
        [DataRow(CompressionLevel.Optimal)]
        [DataRow(CompressionLevel.SmallestSize)]
        [DataRow(CompressionLevel.Fastest)]
        public void AStreamCutOffAfterItsLastOutputIsRejected(CompressionLevel level)
        {
            var input = new byte[200000];
            new Random(1).NextBytes(input);
            for (var i = 0; i < input.Length; i += 3)
                input[i] = 7;

            var compressed = Compress(input, level);

            using (var whole = ProtocolInflater.Decompress(compressed, input.Length))
                CollectionAssert.AreEqual(input, whole.ToArray());

            var cut = compressed[..^1];

            using (var plain = new DeflateStream(new MemoryStream(cut), CompressionMode.Decompress))
            using (var copy = new MemoryStream())
            {
                plain.CopyTo(copy);
                Assert.AreEqual(input.Length, copy.Length, "every byte came out of the cut stream");
            }

            var error = Assert.ThrowsExactly<EndOfStreamException>(() => ProtocolInflater.Decompress(cut, input.Length));
            Assert.AreEqual("DEFLATE payload did not reach its final block.", error.Message);
        }

        [TestMethod]
        public void AnEmptyPayloadIsCutOff()
        {
            Assert.ThrowsExactly<EndOfStreamException>(() => ProtocolInflater.Decompress(Array.Empty<byte>(), 1));
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
