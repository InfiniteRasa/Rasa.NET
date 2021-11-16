namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class SendMOTDPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SendMOTD;

        public string Motd { get; set; }

        public SendMOTDPacket(string motd)
        {
            Motd = motd;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(Motd);
        }
    }
}
