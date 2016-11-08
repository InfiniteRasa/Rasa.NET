using System.IO;

namespace Rasa.Packets.Queue.Client
{
    using Data;

    public class QueueLoginPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.QueueLogin;
        public uint UserId { get; set; }
        public uint OneTimeKey { get; set; }

        public void Read(BinaryReader br)
        {
            UserId = br.ReadUInt32();
            OneTimeKey = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(UserId);
            bw.Write(OneTimeKey);
        }
    }
}
