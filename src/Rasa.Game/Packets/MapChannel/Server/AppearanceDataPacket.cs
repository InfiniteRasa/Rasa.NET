using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;
    public class AppearanceDataPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        public Dictionary<int,AppearanceData> AppearanceData { get; set; }
        
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(21);
            for (var i = 0; i < 21; ++i)
            {
                pw.WriteInt(i + 1);
                pw.WriteTuple(2);
                pw.WriteInt(AppearanceData[i + 1].ClassId);
                pw.WriteTuple(4);
                pw.WriteInt(AppearanceData[i + 1].Color.Red);
                pw.WriteInt(AppearanceData[i + 1].Color.Green);
                pw.WriteInt(AppearanceData[i + 1].Color.Blue);
                pw.WriteInt(AppearanceData[i + 1].Color.Alpha);
            }
        }
    }
}
