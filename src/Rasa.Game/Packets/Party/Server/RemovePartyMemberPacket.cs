namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class RemovePartyMemberPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemovePartyMember;

        internal ulong UserId { get; set; }
        internal bool WasKicked { get; set; }

        internal RemovePartyMemberPacket(ulong userId, bool wasKicked = false)
        {
            UserId = userId;
            WasKicked = wasKicked;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(UserId);
            pw.WriteBool(WasKicked);
        }
    }
}
