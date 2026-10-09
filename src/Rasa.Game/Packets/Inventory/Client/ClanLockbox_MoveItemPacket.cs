using System;

namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// An item moved inside the clan lockbox: (srcSlot, destSlot, quantity), client/inventory.py
    /// _SendServerRequest. Dropped on a slot, destSlot is that slot. Dropped on one of the
    /// window's tab buttons it is None (clanlockboxwindow.OnDNDDropTab,
    /// inventory.AddItemToClanInventoryTab), and nothing in the request says which tab.
    /// </summary>
    public class ClanLockbox_MoveItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanLockbox_MoveItem;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public int Quantity { get; set; }

        /// <summary>The destination arrived as None: the item was dropped on a tab button.</summary>
        public bool NoSlotNamed { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadUInt();

            if (pr.PeekType() == PythonType.Int)
                DestSlot = pr.ReadUInt();
            else
            {
                NoSlotNamed = true;
                pr.ReadNoneStruct();
            }

            if (pr.PeekType() == PythonType.Int)
                Quantity = pr.ReadInt();
            else if (pr.PeekType() == PythonType.Long)
                Quantity = (int)pr.ReadLong();
            else
                throw new Exception("ClanLockbox_MoveItem: unsuported PythonType");
        }
    }
}
