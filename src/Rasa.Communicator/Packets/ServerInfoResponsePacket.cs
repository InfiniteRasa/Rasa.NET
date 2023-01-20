using System.IO;

using Rasa.Packets;

namespace Rasa.Communicator.Packets
{
    public class ServerInfoResponsePacket : IOpcodedPacket<CommunicatorOpcode>
    {
        public CommunicatorOpcode Opcode { get; } = CommunicatorOpcode.ServerInfoResponse;
        public ServerInfo Info { get; }

        public ServerInfoResponsePacket()
        {
            Info = new();
        }

        public ServerInfoResponsePacket(ServerInfo info)
        {
            Info = info;
        }

        public void Read(BinaryReader br)
        {
            Info.QueuePort = br.ReadInt32();
            Info.GamePort = br.ReadInt32();
            Info.AgeLimit = br.ReadByte();
            Info.PKFlag = br.ReadByte();
            Info.CurrentPlayers = br.ReadUInt16();
            Info.MaxPlayers = br.ReadUInt16();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)Opcode);
            bw.Write(Info.QueuePort);
            bw.Write(Info.GamePort);
            bw.Write(Info.AgeLimit);
            bw.Write(Info.PKFlag);
            bw.Write(Info.CurrentPlayers);
            bw.Write(Info.MaxPlayers);
        }
    }
}
