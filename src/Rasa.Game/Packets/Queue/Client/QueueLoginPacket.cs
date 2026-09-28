using System.IO;

namespace Rasa.Packets.Queue.Client
{
    using Data;
    using Extensions;

    public class QueueLoginPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.QueueLogin;
        public uint UserId { get; set; }
        public uint OneTimeKey { get; set; }

        public void Read(BinaryReader br)
        {
            UserId = br.ReadUInt32();
            OneTimeKey = br.ReadUInt32();
            br.EnsureFullyConsumed("Queue login payload");
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(UserId);
            bw.Write(OneTimeKey);
        }
    }
}
