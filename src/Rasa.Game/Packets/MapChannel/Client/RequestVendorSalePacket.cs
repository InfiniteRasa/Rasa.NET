namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorSalePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorSale;

        public long VendorEntityId { get; set; }
        public long ItemEntityId { get; set; }
        public long Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadLong();
            if (pr.PeekType() == PythonType.Long)
                ItemEntityId = pr.ReadLong();
            else
                ItemEntityId = pr.ReadInt();
            if (pr.PeekType() == PythonType.Long)
                Quantity = pr.ReadLong();
            else
            Quantity = pr.ReadInt();
        }
    }
}
