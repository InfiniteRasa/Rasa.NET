using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AppearanceDataPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }

        public AppearanceDataPacket(Dictionary<EquipmentData, AppearanceData> appearanceData)
        {
            AppearanceData = appearanceData;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(AppearanceData.Count);
            foreach (var t in AppearanceData)
            {
                var appearance = t.Value;
                pw.WriteInt((int)appearance.SlotId);
                pw.WriteTuple(2);
                pw.WriteUInt((uint)appearance.Class);
                pw.WriteTuple(4);
                pw.WriteInt(appearance.Color.Red);
                pw.WriteInt(appearance.Color.Green);
                pw.WriteInt(appearance.Color.Blue);
                pw.WriteInt(appearance.Color.Alpha);
            }
        }
    }
}
