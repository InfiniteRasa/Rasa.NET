namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/inventory.py Recv_ResetBuybackInventory(): empties the client's buyback list - the
    /// vendor's Recently Sold tab. The client clears it by itself only on the way back to the
    /// login screen, not at character select and not on a map change, so the server says so
    /// whenever the list it honours is discarded or rebuilt (NpcManager).
    /// </summary>
    public class ResetBuybackInventoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ResetBuybackInventory;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
