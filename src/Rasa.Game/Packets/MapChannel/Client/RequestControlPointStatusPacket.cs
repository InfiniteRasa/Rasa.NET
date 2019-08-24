namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestControlPointStatusPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestControlPointStatus;

        // 0 elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
