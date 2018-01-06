using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AppearanceDataPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<int, AppearanceData> AppearanceData { get; set; }

        public AppearanceDataPacket(Dictionary<int, AppearanceData> appearanceData)
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
                var appearence = t.Value;
                pw.WriteInt(appearence.SlotId);
                pw.WriteTuple(2);
                pw.WriteInt(appearence.ClassId);
                pw.WriteTuple(4);
                pw.WriteInt(appearence.Color.Red);
                pw.WriteInt(appearence.Color.Green);
                pw.WriteInt(appearence.Color.Blue);
                pw.WriteInt(appearence.Color.Alpha);
            }
        }
    }
}
