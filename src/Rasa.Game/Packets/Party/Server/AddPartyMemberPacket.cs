namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AddPartyMemberPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddPartyMember;
        
        internal PartyMember PartyMember { get; set; }
        
        internal AddPartyMemberPacket(PartyMember partyMember)
        {
            PartyMember = partyMember;
        }
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteStruct(PartyMember);
        }
    }
}
