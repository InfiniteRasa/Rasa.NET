namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorSalePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorSale;

        public ulong VendorEntityId { get; set; }
        public ulong ItemEntityId { get; set; }
        public long Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadULong();
            if (pr.PeekType() == PythonType.Long)
                ItemEntityId = pr.ReadULong();
            else
                ItemEntityId = pr.ReadUInt();
            if (pr.PeekType() == PythonType.Long)
                Quantity = pr.ReadLong();
            else
            Quantity = pr.ReadInt();
        }
    }
}
