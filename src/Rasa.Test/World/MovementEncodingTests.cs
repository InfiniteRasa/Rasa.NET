using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Memory;
    using Rasa.Game.Handlers;

    [TestClass]
    public class MovementEncodingTests
    {
        [TestMethod]
        [DataRow(-8388608)]
        [DataRow(-256)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(8388607)]
        public void PackedCoordinatesPreserveSigned24BitValues(int packed)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
                writer.WritePackedFloat(packed);
            stream.Position = 0;
            using var input = new BinaryReader(stream);
            using var reader = new ProtocolBufferReader(input, ProtocolBufferFlags.DontFragment);

            Assert.AreEqual(packed / 256.0f, reader.ReadPackedFloat());
        }

        [TestMethod]
        public void DuplicateChannelSequenceDoesNotReplayMovement()
        {
            var client = new Rasa.Game.Client(null, new ClientPacketHandler());

            Assert.IsTrue(client.TryAcceptSequence(1, 10));
            Assert.IsFalse(client.TryAcceptSequence(1, 10));
            Assert.IsTrue(client.TryAcceptSequence(1, 11));
        }

        [TestMethod]
        public void ChannelSequenceContinuesAcrossUnsignedWrap()
        {
            var client = new Rasa.Game.Client(null, new ClientPacketHandler());

            Assert.IsTrue(client.TryAcceptSequence(1, uint.MaxValue));
            Assert.IsTrue(client.TryAcceptSequence(1, 0));
        }

        // The client's send-timeout check on channel 0xFF carries no sequence number; every one
        // after the first was logged as a dropped out-of-order packet, every 3 s.
        [TestMethod]
        public void SendTimeoutChannelIsNotSequenced()
        {
            var client = new Rasa.Game.Client(null, new ClientPacketHandler());

            for (var check = 0; check < 3; check++)
                Assert.IsTrue(client.TryAcceptSequence(0xFF, 0));
        }
    }
}
