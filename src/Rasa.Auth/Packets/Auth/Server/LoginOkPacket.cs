using System.IO;

namespace Rasa.Packets.Auth.Server;

using Rasa.Data;

public class LoginOkPacket : IOpcodedPacket<ServerOpcode>
{
    public uint SessionId1 { get; set; }
    public uint SessionId2 { get; set; }
    public uint UpdateKey1 { get; set; }
    public uint UpdateKey2 { get; set; }
    public uint PayStat { get; set; }
    public uint RemainingTime { get; set; }
    public uint QuotaTime { get; set; }
    public uint WarnFlag { get; set; }
    public uint LoginFlag { get; set; }
    public byte UnkByte { get; set; }

    public ServerOpcode Opcode { get; } = ServerOpcode.LoginOk;

    public void Read(BinaryReader reader)
    {
        SessionId1 = reader.ReadUInt32();
        SessionId2 = reader.ReadUInt32();
        UpdateKey1 = reader.ReadUInt32();
        UpdateKey2 = reader.ReadUInt32();
        PayStat = reader.ReadUInt32();
        RemainingTime = reader.ReadUInt32();
        QuotaTime = reader.ReadUInt32();
        WarnFlag = reader.ReadUInt32();
        LoginFlag = reader.ReadUInt32();
        UnkByte = reader.ReadByte();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte) Opcode);
        writer.Write(SessionId1);
        writer.Write(SessionId2);
        writer.Write(UpdateKey1);
        writer.Write(UpdateKey2);
        writer.Write(PayStat);
        writer.Write(RemainingTime);
        writer.Write(QuotaTime);
        writer.Write(WarnFlag);
        writer.Write(LoginFlag);
        writer.Write(UnkByte);
    }

    public override string ToString()
    {
        return $"LoginOkPacket({SessionId1}, {SessionId2}, {UpdateKey1}, {UpdateKey2}, {PayStat}, {RemainingTime}, {QuotaTime}, {WarnFlag}, {LoginFlag})";
    }
}
