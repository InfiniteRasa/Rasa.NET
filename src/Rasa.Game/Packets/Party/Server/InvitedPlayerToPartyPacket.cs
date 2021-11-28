namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class InvitedPlayerToPartyPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InvitedPlayerToParty;

        internal string FamilyName { get; set; }
        internal bool IsAfk { get; set; }

        internal InvitedPlayerToPartyPacket(string familyName, bool isAfk)
        {
            FamilyName = familyName;
            IsAfk = isAfk;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteBool(IsAfk);
        }
    }
}
