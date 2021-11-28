namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class JoinSquadRequestSentPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.JoinSquadRequestSent;

        internal string InviteeName { get; set; }
        internal bool IsAfk { get; set; }

        internal JoinSquadRequestSentPacket(string inviteeName, bool isAfk)
        {
            InviteeName = inviteeName;
            IsAfk = isAfk;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(InviteeName);
            pw.WriteBool(IsAfk);
        }
    }
}
