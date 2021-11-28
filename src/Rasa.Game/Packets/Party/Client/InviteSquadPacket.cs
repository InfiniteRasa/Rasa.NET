namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class InviteSquadPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InviteSquad;

        internal string FamilyName { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FamilyName = pr.ReadUnicodeString();
        }
    }
}
