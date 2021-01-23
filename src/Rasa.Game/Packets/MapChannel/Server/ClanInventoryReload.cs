using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ClanInventoryReload : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.InventoryReload;

        public InventoryType Type { get; set; }
        public List<ulong> ListOfItems = new List<ulong>();
        public uint MaxSize { get; set; }

        public ClanInventoryReload(InventoryType type, List<ulong> itemList, uint maxSize)
        {
            Type = type;
            ListOfItems = itemList;
            MaxSize = maxSize;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            pw.WriteList(ListOfItems.Count);
            uint count = 0;
            foreach (var entry in ListOfItems)
            {
                pw.WriteTuple(2);
                pw.WriteULong(entry);
                pw.WriteUInt(count);
                count++;
            }
            pw.WriteUInt(MaxSize);
        }
    }
}
