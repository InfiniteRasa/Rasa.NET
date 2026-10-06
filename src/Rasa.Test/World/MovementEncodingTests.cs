using System.IO;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Memory;
    using Rasa.Models;
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

        [TestMethod]
        public void AMovementsTypeIsItsFirstByte()
        {
            var bytes = Write(Movement.Knockback(new Vector3(1, 2, 3), new Vector2(0.5f, 0)));

            Assert.AreEqual((byte)MovementType.Knockback, bytes[0]);
            Assert.AreEqual(3, bytes[0]);

            var read = Read(bytes);

            Assert.AreEqual(MovementType.Knockback, read.Type);
            Assert.AreEqual(new Vector3(1, 2, 3), read.Position);
            Assert.AreEqual(Movement.FastTurn, read.Flags);
        }

        [TestMethod]
        public void ARushCarriesItsSpeedAndItsType()
        {
            var read = Read(Write(Movement.Rush(new Vector3(10, 0, -4), 30f, new Vector2(0, 0))));

            Assert.AreEqual(MovementType.Rush, read.Type);
            Assert.AreEqual(4, (byte)read.Type);
            Assert.AreEqual(30f, read.Velocity, 0.001f);
        }

        // 70 m/s is 71680 in 1/1024ths, past sixteen bits: a bare cast wrapped it round to 6 m/s.
        [TestMethod]
        public void AVelocityPastSixteenBitsIsTheMostTheyHold()
        {
            var read = Read(Write(new Movement(MovementType.Rush, Vector3.Zero, 70f, 0, new Vector2(0, 0))));

            Assert.AreEqual(Movement.MaxVelocity, read.Velocity, 0.001f);
            Assert.IsTrue(read.Velocity > 63.9f);
        }

        [TestMethod]
        public void AClientsJumpIsReadAsAJump()
        {
            var bytes = Write(new Movement(MovementType.Normal, new Vector3(5, 1, 5), 6.5f, 0x09, new Vector2(1, 0)));
            bytes[0] = 1;

            var read = Read(bytes);

            Assert.AreEqual(MovementType.Jump, read.Type);
            Assert.AreEqual(0x09, read.Flags);
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

        private static byte[] Write(Movement movement)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
                writer.WriteMovementData(movement);

            return stream.ToArray();
        }

        private static Movement Read(byte[] bytes)
        {
            using var input = new BinaryReader(new MemoryStream(bytes));
            using var reader = new ProtocolBufferReader(input, ProtocolBufferFlags.DontFragment);

            return reader.ReadMovement();
        }
    }
}
