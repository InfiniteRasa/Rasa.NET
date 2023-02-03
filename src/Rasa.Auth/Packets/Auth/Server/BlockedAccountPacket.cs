using System.IO;

namespace Rasa.Packets.Auth.Server;

using Rasa.Data;

public class BlockedAccountPacket : IOpcodedPacket<ServerOpcode>
{
    public uint Reason { get; set; }

    public ServerOpcode Opcode { get; } = ServerOpcode.BlockedAccount;

    public void Read(BinaryReader reader)
    {
        Reason = reader.ReadUInt32();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte) Opcode);
        writer.Write(Reason);
    }

    public override string ToString()
    {
        return $"BlockedAccountPacket({Reason})";
    }
}
