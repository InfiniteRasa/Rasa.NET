using System.IO;

namespace Rasa.Packets.Queue.Server
{
    using Data;

    public class QueuePositionPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.PositionInQueue;
        public int Position { get; set; }
        public int EstimatedTime { get; set; }

        public void Read(BinaryReader br)
        {
            Position = br.ReadInt32();
            EstimatedTime = br.ReadInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte) Opcode);
            bw.Write(Position);
            bw.Write(EstimatedTime);
        }
    }
}
