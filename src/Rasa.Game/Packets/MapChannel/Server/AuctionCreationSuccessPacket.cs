namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AuctionCreationSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionCreationSuccess;

        public ulong ItemId { get; set; }

        public AuctionCreationSuccessPacket(ulong itemId)
        {
            ItemId = itemId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(ItemId);
        }
    }
}
