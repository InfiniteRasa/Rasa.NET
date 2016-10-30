using System.IO;

namespace Rasa.Packets.Server
{
    using Data;

    public class SendServerFailPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public byte ReasonCode { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.SendServerListFail;

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
