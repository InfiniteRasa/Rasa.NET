using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class VendPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Vend;

        public List<Item> VendorItems { get; set; }

        public VendPacket(List<Item> vendorItems)
        {
            VendorItems = vendorItems;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(VendorItems.Count);
            for (var i = 0; i < VendorItems.Count; i++)
            {
                pw.WriteUInt(VendorItems[i].EntityId);
                pw.WriteTuple(2);
                pw.WriteInt(VendorItems[i].ItemTemplate.BuyPrice);
                pw.WriteInt(i);
            }
        }
    }
}
