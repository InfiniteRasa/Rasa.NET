using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DevStartMapAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DevStartMapAck;

        public List<uint> ContextIdList { get; set; }

        public DevStartMapAckPacket(List<uint> contextIdList)
        {
            ContextIdList = contextIdList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(ContextIdList.Count);
            foreach (var contextId in ContextIdList)
                pw.WriteUInt(contextId);
        }
    }
}
