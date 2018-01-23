using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class EquipmentInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        public Dictionary<int, uint> EquipmentInfo { get; set; }

        public EquipmentInfoPacket(Dictionary<int, uint> equipmentInfo)
        {
            EquipmentInfo = equipmentInfo;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(EquipmentInfo.Count);
            foreach (var equipment in EquipmentInfo)
            {
                pw.WriteTuple(2);
                pw.WriteInt(equipment.Key);
                pw.WriteUInt(equipment.Value);
            }

        }
    }
}
