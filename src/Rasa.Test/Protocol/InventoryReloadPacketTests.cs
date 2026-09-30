using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Inventory.Server;

namespace Rasa.Test.Protocol
{
    [TestClass]
    public class InventoryReloadPacketTests
    {
        [TestMethod]
        public void ASlottedReloadSendsOccupiedSlotsAsEntityThenSlot()
        {
            var packet = new InventoryReloadPacket(InventoryType.ClanInventory, new ulong[] { 0, 11, 0, 12, 0 }, 500);
            using var reader = Read(out var stream, packet);

            Assert.AreEqual(879, (int)packet.Opcode);
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(15, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadList(), "Empty slots are left out.");
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(11UL, reader.ReadULong());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(12UL, reader.ReadULong());
            Assert.AreEqual(3, reader.ReadInt());
            Assert.AreEqual(500, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        [DataRow(InventoryType.AuctionInventory)]
        [DataRow(InventoryType.InboxInventory)]
        [DataRow(InventoryType.WagerInventory)]
        public void ASlotlessReloadSendsBareEntityIds(InventoryType type)
        {
            using var reader = Read(out var stream, new InventoryReloadPacket(type, new ulong[] { 21, 22 }, 30));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual((int)type, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(21UL, reader.ReadULong());
            Assert.AreEqual(22UL, reader.ReadULong());
            Assert.AreEqual(30, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void AnEmptyReloadEmptiesTheInventory()
        {
            using var reader = Read(out var stream, new InventoryReloadPacket(InventoryType.HomeInventory, new ulong[] { 0, 0, 0 }, 50));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(50, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void InventoryCreateLeavesEmptySlotsOutToo()
        {
            var packet = new InventoryCreatePacket(InventoryType.WeaponDrawerInventory, new List<ulong> { 0, 0, 31, 0, 0 }, 5);
            using var reader = Read(out var stream, packet);

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(9, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(31UL, reader.ReadULong());
            Assert.AreEqual(2, reader.ReadInt());
            Assert.AreEqual(5, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
            Assert.HasCount(5, packet.ListOfItems, "The slot list itself is untouched.");
        }

        [TestMethod]
        public void InventoryDestroyNamesTheInventory()
        {
            var packet = new InventoryDestroyPacket(InventoryType.BuyBackInventory);
            using var reader = Read(out var stream, packet);

            Assert.AreEqual(87, (int)packet.Opcode);
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(10, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static PythonReader Read(out MemoryStream stream, PythonPacket packet)
        {
            stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                packet.Write(new PythonWriter(writer));
            stream.Position = 0;
            return new PythonReader(new BinaryReader(stream));
        }
    }
}
