namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorBuybackPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorBuyback;

        public ulong VendorEntityId { get; set; }
        public ulong ItemEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            VendorEntityId = pr.ReadULong();
            ItemEntityId = pr.ReadULong();
        }
    }
}
