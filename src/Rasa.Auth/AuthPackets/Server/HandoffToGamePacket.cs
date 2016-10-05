using System.IO;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using Packets;

    public class HandoffToGamePacket : IOpcodedPacket<AuthServerOpcode>
    {
        public uint OneTimeKey { get; set; }
        public uint UserId { get; set; }
        public uint ServerIp { get; set; }
        public uint ServerPort { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.HandoffToGame;

        public void Read(BinaryReader reader)
        {
            ServerIp = reader.ReadUInt32();
            ServerPort = reader.ReadUInt32();
            UserId = reader.ReadUInt32();
            OneTimeKey = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(ServerIp);
            writer.Write(ServerPort);
            writer.Write(UserId);
            writer.Write(OneTimeKey);
        }

        public override string ToString()
        {
            return $"HandoffToGamePacket({OneTimeKey}, {UserId}, {ServerIp}, {ServerPort})";
        }
    }
}
