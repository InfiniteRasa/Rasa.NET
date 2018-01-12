namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorSalePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorSale;

        public long VendorEntityId { get; set; }
        public int ItemEntityId { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"RequestVendorSalePacket:\n{pr.ToString()}");
            pr.ReadTuple();
            VendorEntityId = pr.ReadLong();
            ItemEntityId = pr.ReadInt();
            Quantity = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
