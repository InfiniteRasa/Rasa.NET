using System.IO;

namespace Rasa.Packets.Auth.Server;

using Rasa.Data;

public class ProtocolVersionPacket : IOpcodedPacket<ServerOpcode>
{
    public uint ProtocolVersion { get; set; }
    public uint OneTimeKey { get; set; }

    public ServerOpcode Opcode { get; } = ServerOpcode.ProtocolVersion;

    public ProtocolVersionPacket(uint oneTimeKey = 0U, uint protocolVersion = 0U)
    {
        OneTimeKey = oneTimeKey;
        ProtocolVersion = protocolVersion;
    }

    public void Read(BinaryReader reader)
    {
        OneTimeKey = reader.ReadUInt32();
        ProtocolVersion = reader.ReadUInt32();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte) Opcode);
        writer.Write(OneTimeKey);
        writer.Write(ProtocolVersion);
    }

    public override string ToString()
    {
        return $"ProtocolVersionPacket({ProtocolVersion}, {OneTimeKey})";
    }
}
