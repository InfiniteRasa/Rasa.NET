using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CreatureInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreatureInfo;

        public int CreatureNameId { get; set; }
        public bool IsFlyer { get; set; }
        //public int LeaderId { get; set; }
        public List<int> CreatureFlags { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(CreatureNameId);
            pw.WriteBool(IsFlyer);
            pw.WriteNoneStruct();               // LeaderId is not used by client
            pw.WriteList(CreatureFlags.Count);  // generated.client.constant.creatureflag.pyo
            foreach (var flag in CreatureFlags)
                pw.WriteInt(flag);
        }
    }
}
