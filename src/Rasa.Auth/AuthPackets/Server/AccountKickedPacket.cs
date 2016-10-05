using System.IO;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using Packets;

    public class AccountKickedPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public byte ReasonCode { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.AccountKicked;

        public AccountKickedPacket(byte reasonCode)
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
            return $"AccountKickedPacket({ReasonCode})";
        }
    }
}
