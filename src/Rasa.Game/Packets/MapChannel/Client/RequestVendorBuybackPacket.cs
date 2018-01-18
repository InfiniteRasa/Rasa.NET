namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestVendorBuybackPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestVendorBuyback;

        public long VendorEntityId { get; set; }
        public int ItemEntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"RequestVendorBuybackPacket: {pr.ToString()}");
            pr.ReadTuple();
            VendorEntityId = pr.ReadLong();
            ItemEntityId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}