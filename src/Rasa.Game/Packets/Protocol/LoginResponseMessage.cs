using System.Net;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class LoginResponseMessage : ISubtypedPacket<LoginResponseMessageSubtype>
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.LoginResponse;
        public byte RawSubtype { get; set; }

        public LoginResponseMessageSubtype Subtype
        {
            get { return (LoginResponseMessageSubtype) RawSubtype; }
            set { RawSubtype = (byte) value; }
        }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 5;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public uint AccountId { get; set; }
        public uint OneTimeKey { get; set; }
        public uint MillisecondsUntilLogin { get; set; }
        public LoginErrorCodes ErrorCode { get; set; }
        public uint UnkUint { get; set; }
        public string UnkStr { get; set; } = "";
        public IPEndPoint Address { get; set; }

        public void Read(ProtocolBufferReader reader)
        {
            switch (Subtype)
            {
                case LoginResponseMessageSubtype.Handoff:
                    AccountId = reader.ReadUInt();
                    OneTimeKey = reader.ReadUInt();
                    Address = reader.ReadNetAddress();
                    break;

                case LoginResponseMessageSubtype.HandoffFailed:
                    ErrorCode = (LoginErrorCodes) reader.ReadUInt();
                    break;

                case LoginResponseMessageSubtype.WaitForLogout:
                    MillisecondsUntilLogin = reader.ReadUInt();
                    break;

                case LoginResponseMessageSubtype.Success:
                    AccountId = reader.ReadUInt();
                    UnkUint = reader.ReadUInt();
                    break;

                case LoginResponseMessageSubtype.Failed:
                    ErrorCode = (LoginErrorCodes) reader.ReadUInt();
                    break;
            }

            UnkStr = reader.ReadString();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            switch (Subtype)
            {
                case LoginResponseMessageSubtype.Handoff:
                    writer.WriteUInt(AccountId);
                    writer.WriteUInt(OneTimeKey);
                    writer.WriteNetAddress(Address);
                    break;

                case LoginResponseMessageSubtype.HandoffFailed:
                    writer.WriteUInt((uint) ErrorCode);
                    break;

                case LoginResponseMessageSubtype.WaitForLogout:
                    writer.WriteUInt(MillisecondsUntilLogin);
                    break;

                case LoginResponseMessageSubtype.Success:
                    writer.WriteUInt(AccountId);
                    writer.WriteUInt(UnkUint);
                    break;

                case LoginResponseMessageSubtype.Failed:
                    writer.WriteUInt((uint) ErrorCode);
                    break;
            }

            writer.WriteString(UnkStr);
        }
    }
}
