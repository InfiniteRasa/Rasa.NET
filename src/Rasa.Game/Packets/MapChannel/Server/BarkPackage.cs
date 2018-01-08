namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class BarkPackage : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Bark;

        public int BarkId { get; set; }

        public BarkPackage(int barkId)
        {
            BarkId = barkId;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(BarkId);
        }
    }
}
