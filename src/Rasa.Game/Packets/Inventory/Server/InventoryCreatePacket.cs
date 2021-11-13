using System.Collections.Generic;

namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    public class InventoryCreatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryCreate;

        public InventoryType InventoryType { get; set; }
        public List<ulong> ListOfItems = new List<ulong>();
        public int InventorySize { get; set; }

        public InventoryCreatePacket(InventoryType inventoryType, List<ulong> listOfItems, int inventorySize)
        {
            InventoryType = inventoryType;
            ListOfItems = listOfItems;
            InventorySize = inventorySize;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)InventoryType);
            pw.WriteList(ListOfItems.Count);
            for (var i = 0; i < ListOfItems.Count; i++)
            {
                pw.WriteTuple(2);
                pw.WriteInt(i);
                pw.WriteULong(ListOfItems[i]);

                // ToDo: it seems that we need to send data about item, not only itemEntityId
            }

            pw.WriteInt(InventorySize);
        }
    }
}
