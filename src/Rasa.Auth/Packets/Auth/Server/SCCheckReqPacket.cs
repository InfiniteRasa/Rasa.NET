
using System.IO;

namespace Rasa.Packets.Auth.Server;

using Rasa.Data;

public class SCCheckReqPacket : IOpcodedPacket<ServerOpcode>
{
    public uint UserId { get; set; }
    public byte CardKey { get; set; }

    public ServerOpcode Opcode { get; } = ServerOpcode.SCCheckReq;

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
