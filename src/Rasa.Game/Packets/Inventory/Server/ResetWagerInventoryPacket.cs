namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/inventory.py Recv_ResetWagerInventory(): the client forgets its wagered item. Like
    /// the inbox, the client clears it by itself only on the way back to the login screen, so the
    /// server clears it before it shows the slot again (InventoryManager.ShowWager).
    /// </summary>
    public class ResetWagerInventoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ResetWagerInventory;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
