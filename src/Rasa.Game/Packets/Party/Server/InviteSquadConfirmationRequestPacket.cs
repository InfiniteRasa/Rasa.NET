namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class InviteSquadConfirmationRequestPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InviteSquadConfirmationRequest;

        internal string InviteeName { get; set; }

        internal InviteSquadConfirmationRequestPacket(string inviteeName)
        {
            InviteeName = inviteeName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(InviteeName);
        }
    }
}
