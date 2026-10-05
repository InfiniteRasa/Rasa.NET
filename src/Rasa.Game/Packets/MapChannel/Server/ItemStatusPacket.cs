namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// An item's condition changed. Addressed to the item entity, not to a manager.
    ///
    /// Recv_ItemStatus is the only thing in the client that posts UI_UPDATE_ITEM_REPAIRED (the
    /// vendor's repair list) and, when an item crosses zero in either direction,
    /// UI_UPDATE_WEAPON_DRAWER_BROKEN_STATUS (the drawer's broken icon). UI_UPDATE_REPAIR_ITEMS
    /// fires only when an item enters or leaves the inventory.
    ///
    /// It decides both by comparing the hit points it carries with the ones the item had, so
    /// it has to be what tells the client of the change. Recv_ItemInfo stores the hit points
    /// too, and posts nothing: an ItemInfo with the new hit points ahead of this leaves it
    /// nothing to compare, and neither event is posted.
    /// </summary>
    public class ItemStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemStatus;

        public int CurrentHitPoints { get; set; }
        public int MaxHitPoints { get; set; }

        public ItemStatusPacket(int currentHitPoints, int maxHitPoints)
        {
            CurrentHitPoints = currentHitPoints;
            MaxHitPoints = maxHitPoints;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(CurrentHitPoints);
            pw.WriteInt(MaxHitPoints);
        }
    }
}
