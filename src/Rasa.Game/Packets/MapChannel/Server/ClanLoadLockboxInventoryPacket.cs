using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ClanLoadLockboxInventoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.LoadClanLockboxInventory;

        public uint ClanId { get; set; }
        public List<uint> ListOfItems = new List<uint>();
        public uint MaxSize { get; set; }

        public ClanLoadLockboxInventoryPacket(uint clanId, List<uint> itemList, uint maxSize)
        {
            ClanId = clanId;
            ListOfItems = itemList;
            MaxSize = maxSize;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(ClanId);
            pw.WriteList(ListOfItems.Count);
            uint count = 0;
            foreach (var entry in ListOfItems)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(entry);
                pw.WriteUInt(count);
                count++;
            }
            pw.WriteUInt(MaxSize);
        }
    }
}
