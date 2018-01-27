using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UsePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Use;

        public uint PlayerEntityId { get; set; }
        public int CurState { get; set; }
        public int WindupTimeMs { get; set; }
        public List<int> Args = new List<int>();

        public UsePacket(uint playerEntityId, int curState, int windupTimeMs)
        {
            PlayerEntityId = playerEntityId;
            CurState = curState;
            WindupTimeMs = windupTimeMs;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(PlayerEntityId);
            pw.WriteInt(CurState);
            pw.WriteInt(WindupTimeMs);
        }
    }
}
