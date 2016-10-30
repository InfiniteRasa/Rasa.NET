using System.IO;

namespace Rasa.Packets.Server
{
    using Data;

    public class PlayFailPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public FailReason ResultCode { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.PlayFail;

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
