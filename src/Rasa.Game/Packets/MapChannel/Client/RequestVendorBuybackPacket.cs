namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorBuybackPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorBuyback;

        public long VendorEntityId { get; set; }
        public int ItemEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadLong();
            ItemEntityId = pr.ReadInt();
        }
    }
}
