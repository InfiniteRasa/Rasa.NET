using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UsableInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UsableInfo;

        public bool Enabled { get; set; }
        public UseObjectState CurState { get; set; }
        public uint NameOverrideId { get; set; }
        public uint WindupTime { get; set; }
        public uint MissionActivated { get; set; }
        public List<int> Args = new List<int>();

        public UsableInfoPacket(bool enabled, UseObjectState curState, uint nameOverrideId, uint windupTime, uint missionActivated)
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
            pw.WriteBool(Enabled);
            pw.WriteUInt((uint)CurState);
            if (NameOverrideId != 0)
                pw.WriteUInt(NameOverrideId);
            else
                pw.WriteNoneStruct();
            pw.WriteUInt(WindupTime);
            pw.WriteUInt(MissionActivated);
            pw.WriteList(Args.Count);
            foreach (var arg in Args)
                pw.WriteInt(arg);
        }
    }
}
