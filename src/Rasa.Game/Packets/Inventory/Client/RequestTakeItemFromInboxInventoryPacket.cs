namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// An item taken out of the Pick Up Items tab: (auctioneerId, entityId, destSlot),
    /// client/inventory.py AddItemToPersonalInventory - the client will not send it at all unless
    /// an auction house is open, so the auctioneer is always present.
    ///
    /// Four things send it (client/ui/auctionpickupitems.py, inventorywindow.py):
    ///  - Receive and a right-click on a row: the row's id, int(widget.GetID()) - a Python int -
    ///    and None for the slot;
    ///  - Receive All: each id of the client's inbox list, a long, and None;
    ///  - a row dragged onto a pack slot of the item's own tab: a long and the slot;
    ///  - a row dragged onto a slot of another tab: a long and None.
    /// For an item from the inbox the client names no slot of its own choosing: None is the
    /// server's to fill.
    /// </summary>
    public class RequestTakeItemFromInboxInventoryPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTakeItemFromInboxInventory;

        public ulong EntityId { get; set; }         // the auctioneer
        public ulong ItemEntityId { get; set; }

        /// <summary>The pack slot the item was dropped on, or null when the client named none.</summary>
        public uint? DestSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadId();
            ItemEntityId = pr.ReadId();

            if (pr.PeekType() == PythonType.Int)
                DestSlot = pr.ReadUInt();
            else
                pr.ReadNoneStruct();
        }
    }
}
