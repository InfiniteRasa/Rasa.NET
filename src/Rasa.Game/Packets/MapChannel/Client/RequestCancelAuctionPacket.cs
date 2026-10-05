namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// The My Auctions tab's Cancel Auction button: (g_auctioneerId, itemId),
    /// client/auctionhouse.py RequestCancelAuction. The item is the selected row's,
    /// int(itemWidget.GetID()) (client/ui/auctionyourauctions.py OnItemSelected): a Python int.
    /// </summary>
    public class RequestCancelAuctionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCancelAuction;
        
        public ulong EntityId { get; set; }
        public ulong ItemEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadId();
            ItemEntityId = pr.ReadId();
        }
    }
}
