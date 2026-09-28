using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Data;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;

    [TestClass]
    public class RequestPerformAbilityPacketTests
    {
        [TestMethod]
        [DataRow(65537L, false)]
        [DataRow(65537L, true)]
        [DataRow(4294967297L, false)]
        [DataRow(4294967297L, true)]
        public void NativeLongSourceItemIsPreservedWithoutLosingTheFrame(long sourceItem, bool hasYaw)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(hasYaw ? 5 : 4);
                writer.WriteInt((int)ActionId.AaRecruitLightning);
                writer.WriteInt(1);
                writer.WriteNoneStruct();
                writer.WriteLong(sourceItem);
                if (hasYaw)
                    writer.WriteDouble(0.75);
            }
            stream.WriteByte(0x66);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestPerformAbilityPacket();

            packet.Read(reader);

            Assert.AreEqual((ulong)sourceItem, (ulong)packet.ItemId);
            Assert.AreEqual(hasYaw, packet.HasYaw);
            Assert.AreEqual(hasYaw ? 0.75 : 0, packet.Yaw);
            Assert.AreEqual(0x66, reader.ReadByte());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void IntegerAndAbsentSourceItemsRetainTheirSupportedEncoding(bool hasItem)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt((int)ActionId.AaRecruitLightning);
                writer.WriteInt(1);
                writer.WriteNoneStruct();
                if (hasItem)
                    writer.WriteUInt(65537);
                else
                    writer.WriteNoneStruct();
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new RequestPerformAbilityPacket();
            packet.Read(reader);
            Assert.AreEqual(hasItem ? 65537UL : 0UL, packet.ItemId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        [DataRow(3)]
        [DataRow(6)]
        public void UnsupportedTupleSizesAreRejected(int count)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
                writer.WriteTuple(count);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            Assert.ThrowsExactly<InvalidDataException>(() => new RequestPerformAbilityPacket().Read(reader));
        }

        [TestMethod]
        public void ASourceItemMustNotAcceptAFloat()
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteInt((int)ActionId.AaRecruitLightning);
                writer.WriteInt(1);
                writer.WriteNoneStruct();
                writer.WriteDouble(1.5);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            Assert.ThrowsExactly<InvalidDataException>(() => new RequestPerformAbilityPacket().Read(reader));
        }
    }
}
