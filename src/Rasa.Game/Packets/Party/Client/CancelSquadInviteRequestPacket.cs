namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class CancelSquadInviteRequestPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CancelSquadInviteRequest;

        internal string FamilyName { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            var familyName = pr.ReadUnicodeString();

            if (familyName.Contains("(AFK)"))
                FamilyName = familyName.Substring(0, familyName.Length - 5);
            else
                FamilyName = familyName;
        }
    }
}
