using System.IO;

namespace Rasa.Packets.Auth.Server
{
    using Data;

    public class SendServerFailPacket : IOpcodedPacket<ServerOpcode>
    {
        public byte ReasonCode { get; set; }

        public ServerOpcode Opcode { get; } = ServerOpcode.SendServerListFail;

        public SendServerFailPacket(byte reasonCode)
        {
            ReasonCode = reasonCode;
        }

        public void Read(BinaryReader reader)
        {
            ReasonCode = reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(ReasonCode);
        }

        public override string ToString()
        {
            return $"SendServerFailPacket({ReasonCode})";
        }
    }
}
