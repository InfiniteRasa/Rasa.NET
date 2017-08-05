using System;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class PingMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.Ping;

        public byte RawSubtype
        {
            get { return 0; }
            set { }
        }

        public byte MinSubtype { get; } = 0;
        public byte MaxSubtype { get; } = 0;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.None;

        public void Read(ProtocolBufferReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
