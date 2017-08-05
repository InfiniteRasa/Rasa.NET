using System;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class LoginResponseMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.LoginResponse;
        public byte RawSubtype { get; set; }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 5;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

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
