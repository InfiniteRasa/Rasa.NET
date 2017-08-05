using System;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class LoginMessage : ISubtypedPacket<LoginMessageSubtype>
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.Login;
        public byte RawSubtype { get; set; }

        public LoginMessageSubtype Subtype
        {
            get { return (LoginMessageSubtype) RawSubtype; }
            set { RawSubtype = (byte) value;  }
        }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 2;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public uint AccountId { get; set; }
        public uint OneTimeKey { get; set; }
        public string Version { get; set; }

        public void Read(ProtocolBufferReader reader)
        {
            SubtypeCheck();

            AccountId = reader.ReadUInt();
            OneTimeKey = reader.ReadUInt();
            Version = reader.ReadString();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            SubtypeCheck();

            writer.WriteUInt(AccountId);
            writer.WriteUInt(OneTimeKey);
            writer.WriteString(Version);
        }

        private void SubtypeCheck()
        {
            if (Subtype != LoginMessageSubtype.Type1 && Subtype != LoginMessageSubtype.Type2)
                throw new Exception($"Invalid subtype in LoginMessage! Value: {Subtype}");
        }
    }
}
