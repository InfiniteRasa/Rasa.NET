using System.Collections.Generic;

namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    public class MissionStatusInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionStatusInfo;

        public Dictionary<uint, MissionInfo> MissionStatusDict { get; set; }

        public MissionStatusInfoPacket(Dictionary<uint, MissionInfo> missionStatusDict)
        {
            MissionStatusDict = missionStatusDict;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(MissionStatusDict.Count);
            foreach (var entry in MissionStatusDict)
            {
                var missionInfo = entry.Value;
                pw.WriteUInt(entry.Key);
                pw.WriteStruct(missionInfo);
            }
        }
    }
}
