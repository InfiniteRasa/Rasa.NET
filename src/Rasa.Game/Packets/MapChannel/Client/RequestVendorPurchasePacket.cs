namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorPurchasePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorPurchase;

        public long VendorEntityId { get; set; }
        public int ItemEntityId { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadLong();
            ItemEntityId = pr.ReadInt();
            Quantity = pr.ReadInt();
        }
    }
}
