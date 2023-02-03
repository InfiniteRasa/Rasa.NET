using System.IO;

namespace Rasa.Packets.Communicator;

using Rasa.Data;

public class ServerInfoResponsePacket : IOpcodedPacket<CommOpcode>
{
    public CommOpcode Opcode { get; } = CommOpcode.ServerInfoResponse;
    public int QueuePort { get; set; }
    public int GamePort { get; set; }
    public byte AgeLimit { get; set; }
    public byte PKFlag { get; set; }
    public ushort CurrentPlayers { get; set; }
    public ushort MaxPlayers { get; set; }

    public void Read(BinaryReader br)
    {
        QueuePort = br.ReadInt32();
        GamePort = br.ReadInt32();
        AgeLimit = br.ReadByte();
        PKFlag = br.ReadByte();
        CurrentPlayers = br.ReadUInt16();
        MaxPlayers = br.ReadUInt16();
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write((byte) Opcode);
        bw.Write(QueuePort);
        bw.Write(GamePort);
        bw.Write(AgeLimit);
        bw.Write(PKFlag);
        bw.Write(CurrentPlayers);
        bw.Write(MaxPlayers);
    }
}
