using System.IO;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using Packets;

    public class LoginFailPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public FailReason ResultCode { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.LoginFail;

        public LoginFailPacket(FailReason resultCode)
        {
            ResultCode = resultCode;
        }

        public void Read(BinaryReader reader)
        {
            ResultCode = (FailReason) reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write((byte) ResultCode);
        }

        public override string ToString()
        {
            return $"LoginFailPacket({ResultCode})";
        }
    }
}
