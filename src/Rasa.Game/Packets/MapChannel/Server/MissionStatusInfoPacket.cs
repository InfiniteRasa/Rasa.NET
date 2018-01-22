using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class MissionStatusInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionStatusInfo;
        
        public Dictionary<int, MissionInfo> MissionStatusDict { get; set; }

        public MissionStatusInfoPacket(Dictionary<int, MissionInfo> missionStatusDict)
        {
            MissionStatusDict = missionStatusDict;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(MissionStatusDict.Count);
            foreach (var entry in MissionStatusDict)
            {
                var missionInfo = entry.Value;
                pw.WriteInt(entry.Key);
                pw.WriteStruct(missionInfo);
            }
        }
    }
}
