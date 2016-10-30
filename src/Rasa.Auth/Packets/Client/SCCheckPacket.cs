using System.IO;

namespace Rasa.Packets.Client
{
    using Data;

    public class SCCheckPacket : IOpcodedPacket<ClientOpcode>
    {
        public uint UserId { get; set; }
        public uint CardValue { get; set; }

        public ClientOpcode Opcode { get; } = ClientOpcode.SCCheck;

        public void Read(BinaryReader reader)
        {
            UserId = reader.ReadUInt32();
            CardValue = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(UserId);
            writer.Write(CardValue);
        }

        public override string ToString()
        {
            return $"SCCheckPacket({UserId}, {CardValue})";
        }
    }
}
