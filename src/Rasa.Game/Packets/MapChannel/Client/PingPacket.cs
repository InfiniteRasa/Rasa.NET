namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PingPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Ping;

        public double Ping { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Ping = pr.ReadDouble();
        }
    }
}
