using System.IO;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using Packets;

    public class HandoffToQueuePacket : IOpcodedPacket<AuthServerOpcode>
    {
        public uint OneTimeKey { get; set; }
        public uint UserId { get; set; }
        public byte ServerId { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.HandOffToQueue;

        public void Read(BinaryReader reader)
        {
            OneTimeKey = reader.ReadUInt32();
            UserId = reader.ReadUInt32();
            ServerId = reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(OneTimeKey);
            writer.Write(UserId);
            writer.Write(ServerId);
        }

        public override string ToString()
        {
            return $"HandoffToQueuePacket({OneTimeKey}, {UserId}, {ServerId})";
        }
    }
}
