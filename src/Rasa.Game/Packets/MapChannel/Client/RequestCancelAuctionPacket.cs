namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestCancelAuctionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCancelAuction;
        
        public uint EntityId { get; set; }      // g_auctioneerId
        public uint ItemEntityId { get; set; }  // itemId

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
            ItemEntityId = pr.ReadUInt();
        }
    }
}
