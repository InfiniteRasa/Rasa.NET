using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class EquipmentInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        public List<uint> EquipmentInfo { get; set; }

        public EquipmentInfoPacket(List<uint> equipmentInfo)
        {
            EquipmentInfo = equipmentInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(EquipmentInfo.Count);

            uint count = 0;

            foreach (var entry in EquipmentInfo)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(count);
                pw.WriteUInt(entry);

                count++;
            }

        }
    }
}
