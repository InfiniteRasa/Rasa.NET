namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class BarkPackage : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Bark;

        public uint BarkId { get; set; }

        public BarkPackage(uint barkId)
        {
            BarkId = barkId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(BarkId);
        }
    }
}
