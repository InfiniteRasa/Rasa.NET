using System.IO;

namespace Rasa.Packets.Auth.Server
{
    using Data;

    public class PlayFailPacket : IOpcodedPacket<ServerOpcode>
    {
        public FailReason ResultCode { get; set; }

        public ServerOpcode Opcode { get; } = ServerOpcode.PlayFail;

        public PlayFailPacket(FailReason resultCode)
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
            return $"PlayFailPacket({ResultCode})";
        }
    }
}
