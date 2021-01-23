namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestCancelAuctionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCancelAuction;
        
        public ulong EntityId { get; set; }
        public ulong ItemEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            ItemEntityId = pr.ReadULong();
        }
    }
}
