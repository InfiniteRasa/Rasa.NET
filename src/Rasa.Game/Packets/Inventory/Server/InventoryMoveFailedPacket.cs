namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// InventoryMoveFailed (464), on the client's inventory manager
    /// (SysEntity.ClientInventoryManagerId): the retail server's answer to a move it refused -
    /// PersonalInventory_MoveItem, HomeInventory_MoveItem, ClanLockbox_MoveItem,
    /// WeaponDrawerInventory_MoveItem, the moves to and from the lockbox, the clan lockbox deposit
    /// and withdrawal, RequestEquipWeapon and RequestEquipArmor. Wired so that it can be sent, and
    /// deliberately sent by nothing: the 1.16.5 client does nothing with it.
    ///
    /// client/inventory.py Recv_InventoryMoveFailed(type, fromSlot, toSlot) - "Called by the
    /// areaserver to tell the client the move failed" - is a bare return: nothing put back,
    /// logged, shown or played. Nothing else in the client names it, nor does tabula_rasa.exe.
    ///
    /// A silent refusal is all the client needs because it never moves anything itself. A drop
    /// is checked on the spot (dead, same slot, inventory full - the error sound for those),
    /// then sent (_SendServerRequest), the drop sound played and the drag accepted, and the slot
    /// tables left alone: only InventoryRemoveItem and InventoryAddItem change them. A move the
    /// server turns down leaves the item where it was, which is what this would have said.
    ///
    /// type is the inventory type, fromSlot and toSlot the request's slots; a clan lockbox
    /// deposit into a tab has no slot, written as None. The C++ server only listed the id.
    /// </summary>
    public class InventoryMoveFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryMoveFailed;

        public InventoryType Type { get; set; }
        public int? FromSlot { get; set; }
        public int? ToSlot { get; set; }

        public InventoryMoveFailedPacket(InventoryType type, int? fromSlot, int? toSlot)
        {
            Type = type;
            FromSlot = fromSlot;
            ToSlot = toSlot;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt((int)Type);
            WriteSlot(pw, FromSlot);
            WriteSlot(pw, ToSlot);
        }

        private static void WriteSlot(PythonWriter pw, int? slot)
        {
            if (slot.HasValue)
                pw.WriteInt(slot.Value);
            else
                pw.WriteNoneStruct();
        }
    }
}
