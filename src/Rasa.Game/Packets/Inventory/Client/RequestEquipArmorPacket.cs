namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A piece into or out of an equipment slot: (slot, inventory, equipmentSlot),
    /// client/inventory.py _SendServerRequest. The slot is one of the inventory named, which is
    /// the pack, the footlocker or the clan lockbox. A worn piece dropped on one of the clan
    /// lockbox's tab buttons is sent with None for it (clanlockboxwindow.OnDNDDropTab,
    /// inventory.AddItemToClanInventoryTab): (None, CLANINVENTORY, equipmentSlot).
    /// </summary>
    public class RequestEquipArmorPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipArmor;

        public uint SrcSlot { get; set; }        // Source Slot
        public InventoryType SrcInventory { get; set; }   // Source Inventory
        public uint DestSlot { get; set; }       // Destination Slot

        /// <summary>The first slot arrived as None.</summary>
        public bool NoSlotNamed { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            if (pr.PeekType() == PythonType.Int)
                SrcSlot = pr.ReadUInt();
            else
            {
                NoSlotNamed = true;
                pr.ReadNoneStruct();
            }

            SrcInventory = (InventoryType)pr.ReadInt();
            DestSlot = pr.ReadUInt();
        }
    }
}