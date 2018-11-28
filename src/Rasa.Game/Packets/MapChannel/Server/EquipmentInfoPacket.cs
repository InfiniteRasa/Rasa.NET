using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class EquipmentInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        public Dictionary<uint,uint> EquipmentInfo { get; set; }

        public EquipmentInfoPacket(Dictionary<uint,uint> equipmentInfo)
        {
            EquipmentInfo = equipmentInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(EquipmentInfo.Count);
            foreach (var entry in EquipmentInfo)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(entry.Key);
                pw.WriteUInt(entry.Value);
            }

        }
    }
}
