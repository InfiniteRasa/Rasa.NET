using System.IO;

namespace Rasa.Packets.Auth.Server;

using Rasa.Data;

public class LoginFailPacket : IOpcodedPacket<ServerOpcode>
{
    public FailReason ResultCode { get; set; }

    public ServerOpcode Opcode { get; } = ServerOpcode.LoginFail;

    public LoginFailPacket(FailReason resultCode)
    {
        ResultCode = resultCode;
    }

    public void Read(BinaryReader reader)
    {
        ResultCode = (FailReason) reader.ReadByte();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte) Opcode);
        writer.Write((byte) ResultCode);
    }

    public override string ToString()
    {
        return $"LoginFailPacket({ResultCode})";
    }
}
