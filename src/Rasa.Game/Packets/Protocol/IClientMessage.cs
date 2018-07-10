namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public interface IClientMessage
    {
        ClientMessageOpcode Type { get; set; }
        byte RawSubtype { get; set; }

        ClientMessageSubtypeFlag SubtypeFlags { get; }
        byte MinSubtype { get; }
        byte MaxSubtype { get; }

        void Read(ProtocolBufferReader reader);
        void Write(ProtocolBufferWriter writer);
    }
}
