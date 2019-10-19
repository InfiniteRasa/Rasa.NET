namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestControlPointBidStatusPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestControlPointBidStatus;

        // 0 elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
