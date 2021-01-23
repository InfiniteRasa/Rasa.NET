namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorPurchasePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorPurchase;

        public ulong VendorEntityId { get; set; }
        public ulong ItemEntityId { get; set; }
        public uint Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadULong();
            ItemEntityId = pr.ReadULong();
            Quantity = pr.ReadUInt();
        }
    }
}
