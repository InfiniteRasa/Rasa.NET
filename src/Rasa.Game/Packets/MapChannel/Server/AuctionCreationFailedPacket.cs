namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AuctionCreationFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionCreationFailed;

        public uint ItemId { get; set; }
        public PlayerMessage PlayerMessageId { get; set; }

        public AuctionCreationFailedPacket(uint itemId, PlayerMessage playerMessageId )
        {
            ItemId = itemId;
            PlayerMessageId = playerMessageId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(ItemId);
            pw.WriteUInt((uint)PlayerMessageId);
        }
    }
}
