using System.Collections.Generic;

namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;
    using Structures;

    public class InviteToPartyPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InviteToParty;

        internal string FamilyName { get; set; }
        internal List<PartyMember> SenderSquadInfo = new List<PartyMember>();

        internal InviteToPartyPacket(string familyName, List<PartyMember> senderSquadInfo)
        {
            FamilyName = familyName;
            SenderSquadInfo = senderSquadInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteList(SenderSquadInfo.Count);
            foreach (var partyMemberInfo in SenderSquadInfo)
                pw.WriteStruct(partyMemberInfo);
        }
    }
}
