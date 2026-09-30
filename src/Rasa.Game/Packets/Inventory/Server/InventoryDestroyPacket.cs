namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// InventoryDestroy (87), on SysEntity.ClientInventoryManagerId: <c>(inventoryType,)</c>. Wired so
    /// that it can be sent, and deliberately sent by nothing: InventoryReload with the new contents
    /// (an empty list included) does the same and says what is there now.
    ///
    /// client/inventory.py Recv_InventoryDestroy calls Reset(type): the personal, home, equipped,
    /// weapon drawer and clan inventories lose their slot mappings (the weapon drawer also releases
    /// its weapons' assets), overflow, buyback, auction, inbox and wager their lists, and any other
    /// type nothing. Then ResetUI: personal force-hides the inventory window and forgets the vendor,
    /// equipped toggles the attributes window, buyback, inbox, auction and wager redraw, and home
    /// and clan nothing. The item entities are untouched. So never personal (1) or equipped (8) to
    /// a player who is using them.
    ///
    /// Its docstring has it as the reply to closing the home inventory. That request is gone:
    /// lockbox.py CancelLockbox and clanlockbox.py CancelClanLockbox would send
    /// RequestCancelLockbox and RequestCancelClanLockbox, but nothing calls them and neither name is
    /// in the method table; the 1.16.5 lockbox window just hides. The C++ server only listed the id.
    /// </summary>
    public class InventoryDestroyPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryDestroy;

        public InventoryType InventoryType { get; }

        public InventoryDestroyPacket(InventoryType inventoryType)
        {
            InventoryType = inventoryType;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)InventoryType);
        }
    }
}
