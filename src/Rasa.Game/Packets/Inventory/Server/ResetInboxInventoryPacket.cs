namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/inventory.py Recv_ResetInboxInventory(): empties the client's inbox list - the
    /// auction house's Pick Up Items tab - and redraws the tab. The client clears that list by
    /// itself only on the way back to the login screen (inputstate/login.py ResetAll), not at
    /// character select and not on a map change, so the server clears it before it lists the
    /// inbox again (InventoryManager.ShowInbox).
    /// </summary>
    public class ResetInboxInventoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ResetInboxInventory;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
