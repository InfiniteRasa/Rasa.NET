
using System.IO;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using Packets;

    public class SCCheckReqPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public uint UserId { get; set; }
        public byte CardKey { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.SCCheckReq;

        public void Read(BinaryReader reader)
        {
            UserId = reader.ReadUInt32();
            CardKey = reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) Opcode);
            writer.Write(UserId);
            writer.Write(CardKey);
        }

        public override string ToString()
        {
            return $"SCCheckReqPacket({UserId}, {CardKey})";
        }
    }
}
