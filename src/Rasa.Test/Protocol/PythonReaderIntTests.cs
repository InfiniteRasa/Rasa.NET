using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;

namespace Rasa.Test.Protocol
{
    [TestClass]
    public class PythonReaderIntTests
    {
        [TestMethod]
        public void InlineIntsCarryTheValueInTheTypeByte()
        {
            Assert.AreEqual(0, Read(0x10));
            Assert.AreEqual(12, Read(0x1C));
        }

        [TestMethod]
        public void AOneByteIntIsSigned()
        {
            // A lockbox withdrawal of 100 is sent as -100: 0x1D 0x9C.
            Assert.AreEqual(-100, Read(0x1D, 0x9C));
            Assert.AreEqual(-1, Read(0x1D, 0xFF));
            Assert.AreEqual(-128, Read(0x1D, 0x80));
            Assert.AreEqual(127, Read(0x1D, 0x7F));
            Assert.AreEqual(13, Read(0x1D, 0x0D));
        }

        [TestMethod]
        public void TwoAndFourByteIntsAreSigned()
        {
            Assert.AreEqual(-200, Read(0x1E, 0x38, 0xFF));
            Assert.AreEqual(200, Read(0x1E, 0xC8, 0x00));
            Assert.AreEqual(-70000, Read(0x1F, 0x90, 0xEE, 0xFE, 0xFF));
            Assert.AreEqual(70000, Read(0x1F, 0x70, 0x11, 0x01, 0x00));
        }

        [TestMethod]
        public void TheWriterRoundTripsEveryWidth()
        {
            foreach (var value in new[] { -70000, -200, -100, -1, 0, 12, 13, 127, 128, 255, 256, 70000 })
            {
                using var stream = new MemoryStream();
                new PythonWriter(new BinaryWriter(stream)).WriteInt(value);
                stream.Position = 0;
                Assert.AreEqual(value, new PythonReader(new BinaryReader(stream)).ReadInt(), $"{value}");
                Assert.AreEqual(stream.Length, stream.Position, $"{value}: bytes left over");
            }
        }

        private static int Read(params byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            var reader = new PythonReader(new BinaryReader(stream));
            var value = reader.ReadInt();
            Assert.AreEqual(stream.Length, stream.Position, "bytes left over");
            return value;
        }
    }
}
