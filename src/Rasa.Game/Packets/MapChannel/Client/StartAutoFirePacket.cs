namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class StartAutoFirePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StartAutoFire;

        public double FromUi { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FromUi = pr.ReadDouble();
        }
    }
}
