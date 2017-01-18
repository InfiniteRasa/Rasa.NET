namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class StartAutoFirePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StartAutoFire;

        public double RetryDelayMs { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            RetryDelayMs = pr.ReadDouble();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
