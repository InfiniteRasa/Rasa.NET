namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class WhoAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WhoAck;

        public string CharacterName { get; set; }
        public string FamilyName { get; set; }
        public string ClanName { get; set; }
        public uint TitleId { get; set; }
        public uint CharacterClass { get; set; }
        public uint Level { get; set; }
        public uint ContextId { get; set; }
        public uint CurrentGameContextOrdinal { get; set; }
        public bool IsAfk { get; set; }
        public bool IsTrialAccount { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(10);
            pw.WriteUnicodeString(CharacterName);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteUnicodeString(ClanName);
            pw.WriteUInt(TitleId);
            pw.WriteUInt(CharacterClass);
            pw.WriteUInt(Level);
            pw.WriteUInt(ContextId);
            pw.WriteUInt(CurrentGameContextOrdinal);
            pw.WriteBool(IsAfk);
            pw.WriteBool(IsTrialAccount);
        }
    }
}
