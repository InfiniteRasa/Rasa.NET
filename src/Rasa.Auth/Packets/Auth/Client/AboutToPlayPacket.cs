using System.IO;

namespace Rasa.Packets.Auth.Client;

using Rasa.Data;

public class AboutToPlayPacket : IOpcodedPacket<ClientOpcode>
{
    public uint SessionId1 { get; set; }
    public uint SessionId2 { get; set; }
    public byte ServerId { get; set; }

    public ClientOpcode Opcode { get; } = ClientOpcode.AboutToPlay;

    public void Read(BinaryReader reader)
    {
        SessionId1 = reader.ReadUInt32();
        SessionId2 = reader.ReadUInt32();
        ServerId = reader.ReadByte();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte) Opcode);
        writer.Write(SessionId1);
        writer.Write(SessionId2);
        writer.Write(ServerId);
    }

    public override string ToString()
    {
        return $"AboutToPlayPacket({SessionId1}, {SessionId2}, {ServerId})";
    }
}
