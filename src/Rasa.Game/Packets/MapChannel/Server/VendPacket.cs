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

        /// <summary>The vendor the stock belongs to, which prices it.</summary>
        public Vendor Vendor { get; set; }

        public VendPacket(Vendor vendor, List<Item> vendorItems)
        {
            Vendor = vendor;
            VendorItems = vendorItems;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(VendorItems.Count);
            for (var i = 0; i < VendorItems.Count; i++)
            {
                pw.WriteULong(VendorItems[i].EntityId);
                pw.WriteTuple(2);
                pw.WriteInt(Vendor.PriceOf(VendorItems[i]));
                pw.WriteInt(i);
            }
        }
    }
}
