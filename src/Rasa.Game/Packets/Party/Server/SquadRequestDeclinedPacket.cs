namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class SquadRequestDeclinedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SquadRequestDeclined;

        internal string ReciverName { get; set; }

        internal SquadRequestDeclinedPacket(string reciverName)
        {
            ReciverName = reciverName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(ReciverName);
        }
    }
}
