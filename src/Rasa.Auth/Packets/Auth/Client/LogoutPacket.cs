using System.IO;

namespace Rasa.Packets.Auth.Client;

using Rasa.Data;

public class LogoutPacket : IOpcodedPacket<ClientOpcode>
{
    public uint SessionId1 { get; set; }
    public uint SessionId2 { get; set; }

    public ClientOpcode Opcode { get; } = ClientOpcode.Logout;

    public void Read(BinaryReader reader)
    {
        SessionId1 = reader.ReadUInt32();
        SessionId2 = reader.ReadUInt32();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte) Opcode);
        writer.Write(SessionId1);
        writer.Write(SessionId2);
    }

    public override string ToString()
    {
        return $"LogoutPacket({SessionId1}, {SessionId2})";
    }
}
