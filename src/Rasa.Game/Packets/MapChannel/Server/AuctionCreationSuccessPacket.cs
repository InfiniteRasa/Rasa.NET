namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AuctionCreationSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionCreationSuccess;

        public uint ItemId { get; set; }

        public AuctionCreationSuccessPacket(uint itemId)
        {
            ItemId = itemId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ItemId);
        }
    }
}
