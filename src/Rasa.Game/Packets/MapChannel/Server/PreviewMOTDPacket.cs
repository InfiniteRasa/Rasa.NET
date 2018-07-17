namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PreviewMOTDPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreviewMOTD;

        public string Motd { get; set; }

        public PreviewMOTDPacket(string motd)
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
