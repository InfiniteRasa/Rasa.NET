namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestAuctionBuyoutPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestAuctionBuyout;

        public ulong EntityId { get; set; }      // g_auctioneerId
        public uint ItemId { get; set; }        // itemId
        public uint Price { get; set; }         // price

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.AI, $"{pr.ToString()}");
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            ItemId = pr.ReadUInt();
            Price = pr.ReadUInt();
        }
    }
}
