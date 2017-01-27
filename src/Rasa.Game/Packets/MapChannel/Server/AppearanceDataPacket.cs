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
        
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(AppearanceData.Count);
            foreach (var t in AppearanceData)
            {
                var v = t.Value;
                pw.WriteInt(v.SlotId);
                pw.WriteTuple(2);
                pw.WriteInt(v.ClassId);
                pw.WriteTuple(4);
                pw.WriteInt(v.Color.Red);
                pw.WriteInt(v.Color.Green);
                pw.WriteInt(v.Color.Blue);
                pw.WriteInt(v.Color.Alpha);
            }
        }
    }
}
