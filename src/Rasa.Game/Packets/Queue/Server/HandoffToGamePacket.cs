using System.IO;
using System.Net;

namespace Rasa.Packets.Queue.Server
{
    using Data;

    public class HandoffToGamePacket : IOpcodedPacket<QueueOpcode>
    {
        public uint OneTimeKey { get; set; }
        public uint UserId { get; set; }
        public IPAddress ServerIp { get; set; }
        public int ServerPort { get; set; }

        public QueueOpcode Opcode { get; } = QueueOpcode.HandoffToGame;

        public void Read(BinaryReader reader)
        {
            ServerIp = new IPAddress(reader.ReadBytes(4));
            ServerPort = reader.ReadInt32();
            UserId = reader.ReadUInt32();
            OneTimeKey = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(ServerIp.GetAddressBytes());
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
