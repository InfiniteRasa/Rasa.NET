using System.IO;

namespace Rasa.Packets.Client
{
    using Data;

    public class ServerListExtPacket : IOpcodedPacket<ClientOpcode>
    {
        public uint SessionId1 { get; set; }
        public uint SessionId2 { get; set; }
        public byte ListKind { get; set; }

        public ClientOpcode Opcode { get; } = ClientOpcode.ServerListExt;

        public void Read(BinaryReader reader)
        {
            SessionId1 = reader.ReadUInt32();
            SessionId2 = reader.ReadUInt32();
            ListKind = reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(SessionId1);
            writer.Write(SessionId2);
            writer.Write(ListKind);
        }

        public override string ToString()
        {
            return $"ServerListExtPacket({SessionId1}, {SessionId2}, {ListKind})";
        }
    }
}
