namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class SquadRequestCanceledPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SquadRequestCanceled;

        internal string InviterName { get; set; }

        internal SquadRequestCanceledPacket(string inviterName)
        {
            InviterName = inviterName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(InviterName);
        }
    }
}
