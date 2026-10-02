namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// WagerItem (10000091), client/prestige.py WagerItem(slotId): the wager slot swapped with a
    /// backpack slot. The prestige window sends the slot of the backpack item dropped on its wager
    /// slot, as the client's inventory holds it. The backpack window sends the slot the wagered
    /// item was dropped on, counted within the tab it shows, or None when that tab is not the
    /// item's own (ui/inventorywindow.py OnDNDDrop).
    /// </summary>
    public class WagerItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WagerItem;

        /// <summary>The backpack slot; null for "wherever it fits".</summary>
        public uint? Slot { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            var slot = pr.ReadNullableInt();

            if (slot.HasValue && slot.Value >= 0)
                Slot = (uint)slot.Value;
        }
    }
}
