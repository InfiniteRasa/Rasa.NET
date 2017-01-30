using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class EquipmentInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        public EquipmentInfo EquipmentInfo { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(1);
                pw.WriteTuple(2);
                pw.WriteInt(EquipmentInfo.SlotId);
                pw.WriteInt((int)EquipmentInfo.EntityId);
            
        }
    }
}
