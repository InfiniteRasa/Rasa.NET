using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class VendPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Vend;

        public List<VendorItem> VendorItems { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(3);

            pw.WriteInt(1003);
            pw.WriteTuple(2);
            pw.WriteInt(50);
            pw.WriteInt(1);

            pw.WriteInt(1008);
            pw.WriteTuple(2);
            pw.WriteInt(100);
            pw.WriteInt(2);
            pw.WriteInt(1009);
            pw.WriteTuple(2);
            pw.WriteInt(100);
            pw.WriteInt(0);
            /*pw.WriteDictionary(VendorItems.Count);
            foreach (var item in VendorItems)
            {
                pw.WriteInt((int)item.ItemTemplateId);
                pw.WriteInt(0);
            }*/
        }
    }
}