using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UsableInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UsableInfo;

        public int Enabled { get; set; }
        public int CurState { get; set; }
        public int NameOverrideId { get; set; }
        public int WindupTime { get; set; }
        public int MissionActivated { get; set; }
        public List<int> Args = new List<int>();

        public UsableInfoPacket(int enabled, int curState, int nameOverrideId, int windupTime, int missionActivated)
        {
            Enabled = enabled;
            CurState = curState;
            NameOverrideId = nameOverrideId;
            WindupTime = windupTime;
            MissionActivated = missionActivated;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteInt(Enabled);
            pw.WriteInt(CurState);
            pw.WriteInt(NameOverrideId);
            pw.WriteInt(WindupTime);
            pw.WriteInt(MissionActivated);
            pw.WriteList(Args.Count);
            foreach (var arg in Args)
                pw.WriteInt(arg);
        }
    }
}
