using System.Collections.Generic;

namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;
    using Structures;

    public class PartyMemberListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PartyMemberList;
        
        internal List<PartyMember> PartyList = new List<PartyMember>();

        internal PartyMemberListPacket(List<PartyMember> partyList)
        {
            PartyList = partyList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(PartyList.Count);
            foreach (var partyMemberInfo in PartyList)
                pw.WriteStruct(partyMemberInfo);
        }
    }
}
