using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class InventoryCreatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryCreate;

        public InventoryType InventoryType { get; set; }
        public List<uint> ListOfItems = new List<uint>();
        public int InventorySize { get; set; }

        public InventoryCreatePacket(InventoryType inventoryType, List<uint> listOfItems, int inventorySize)
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
            foreach (var entry in ListOfItems)
            {
                uint count = 0;

                pw.WriteUInt(entry);
                pw.WriteUInt(count);

                count++;
            }

            pw.WriteInt(InventorySize);
        }
    }
}
