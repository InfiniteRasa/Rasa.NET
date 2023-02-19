namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class InviteUserToPartyByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InviteUserToPartyByName;

        internal string FamilyName { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            if (pr.PeekType() == PythonType.UnicodeString)
                FamilyName = pr.ReadUnicodeString();
            else
                FamilyName = pr.ReadString();
        }
    }
}
