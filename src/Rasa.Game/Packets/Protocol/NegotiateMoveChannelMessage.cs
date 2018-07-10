namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class NegotiateMoveChannelMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.NegotiateMoveChannel;
        public byte RawSubtype {
            get { return 0; }
            set { }
        }

        public byte MinSubtype { get; } = 0;
        public byte MaxSubtype { get; } = 0;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.None;

        public byte ChannelId { get; set; }
        public ulong UnkMaybeEntityId { get; set; }

        public void Read(ProtocolBufferReader reader)
        {
            ChannelId = reader.ReadByte();
            UnkMaybeEntityId = reader.ReadULong();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            writer.WriteByte(ChannelId);
            writer.WriteULong(UnkMaybeEntityId);
        }
    }
}
