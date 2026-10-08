namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// A weapon into or out of the weapon drawer: (slot, inventory, drawerSlot),
    /// client/inventory.py _SendServerRequest. The slot is one of the inventory named, which is
    /// the pack, the footlocker or the clan lockbox. A drawer weapon dropped on one of the clan
    /// lockbox's tab buttons is sent with None for it (clanlockboxwindow.OnDNDDropTab,
    /// inventory.AddItemToClanInventoryTab): (None, CLANINVENTORY, drawerSlot).
    /// </summary>
    public class RequestEquipWeaponPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipWeapon;

        public uint SrcSlot { get; set; }
        public InventoryType InventoryType { get; set; }
        public uint DestSlot { get; set; }

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

            InventoryType = (InventoryType)pr.ReadInt();
            DestSlot = pr.ReadUInt();
        }
    }
}
