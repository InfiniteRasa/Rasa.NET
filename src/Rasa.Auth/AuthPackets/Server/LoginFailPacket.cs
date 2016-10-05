using System.IO;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using Packets;

    public class LoginFailPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public AuthFailedReason ResultCode { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.LoginFail;

        public LoginFailPacket(AuthFailedReason resultCode)
        {
            ResultCode = resultCode;
        }

        public void Read(BinaryReader reader)
        {
            ResultCode = (AuthFailedReason) reader.ReadByte();
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
