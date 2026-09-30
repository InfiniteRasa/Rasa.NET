namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/inventory.py Recv_ResetAuctionInventory(): empties the client's auction inventory,
    /// the dictionary behind the "Your Auctions" tab and the MAX_AUCTION_ITEMS check the Create
    /// Auction button makes. The client clears it by itself only on the way back to the login
    /// screen - not at character select and not on a map change - and AuctionStatusSuccess
    /// (UpdateAuctionItems) only adds to it, so the server clears it before it lists the
    /// auctions again (InventoryManager.ShowAuctions, AuctionHouseManager.RequestAuctionStatus).
    /// </summary>
    public class ResetAuctionInventoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ResetAuctionInventory;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
