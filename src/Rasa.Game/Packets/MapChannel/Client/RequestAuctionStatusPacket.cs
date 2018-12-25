namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestAuctionStatusPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestAuctionStatus;

        public uint EntityId { get; set; }      // g_auctioneerId

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
        }
    }
}
