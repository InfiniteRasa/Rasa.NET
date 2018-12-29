namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestCreateAuctionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCreateAuction;

        public uint EntityId { get; set; }      // g_auctioneerId
        public uint ItemEntityId { get; set; }  // itemId
        public uint Price { get; set; }         // price
        public uint Duration { get; set; }      // duration

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.AI, $"{pr.ToString()}");
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
            ItemEntityId = pr.ReadUInt();
            Price = pr.ReadUInt();
            switch (pr.PeekType())
            {
                case PythonType.Int:
                    Duration = pr.ReadUInt();
                    break;
                case PythonType.Long:
                    Duration = (uint)pr.ReadLong();
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"Duration isn't long or int???");
                    break;
            }
        }
    }
}
