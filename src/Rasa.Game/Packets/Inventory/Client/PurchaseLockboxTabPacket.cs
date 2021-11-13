namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class PurchaseLockboxTabPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PurchaseLockboxTab;

        public int TabId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            TabId = pr.ReadInt();
        }
    }
}
