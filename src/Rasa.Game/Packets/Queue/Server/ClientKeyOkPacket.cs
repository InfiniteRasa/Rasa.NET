using System.IO;

namespace Rasa.Packets.Queue.Server
{
    using Data;
    using Extensions;

    public class ClientKeyOkPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.ClientKeyOk;

        public void Read(BinaryReader br)
        {
        }

        public void Write(BinaryWriter bw)
        {
            bw.WriteUtf8String("ENC OK");
        }
    }
}
