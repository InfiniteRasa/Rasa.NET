namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdatePartyMemberInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdatePartyMemberInfo;

        internal PartyMember PartyMember { get; set; }

        internal UpdatePartyMemberInfoPacket(PartyMember partyMember)
        {
            PartyMember = partyMember;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteStruct(PartyMember);
        }
    }
}
