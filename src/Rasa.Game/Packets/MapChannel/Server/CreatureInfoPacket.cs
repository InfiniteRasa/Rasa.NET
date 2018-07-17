using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CreatureInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreatureInfo;

        public int CreatureNameId { get; set; }
        public bool IsFlyer { get; set; }
        //public int LeaderId { get; set; }
        public List<int> CreatureFlags { get; set; }

        public CreatureInfoPacket(int creatureNameId, bool isFlyer, List<int> creatureFlags)
        {
            CreatureNameId = creatureNameId;
            IsFlyer = isFlyer;
            CreatureFlags = creatureFlags;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            if (CreatureNameId == 0)
                pw.WriteNoneStruct();           // creatureNameId (none, server defines name)
            else
                pw.WriteInt(CreatureNameId);    // use creaturename table to lookup translated name

            pw.WriteBool(IsFlyer);
            pw.WriteNoneStruct();               // LeaderId is not used by client
            pw.WriteList(CreatureFlags.Count);  // generated.client.constant.creatureflag.pyo
            foreach (var flag in CreatureFlags)
                pw.WriteInt(flag);
        }
    }
}
