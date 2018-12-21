namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestCancelAuctioneerPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCancelAuctioneer;

        // 0 elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
