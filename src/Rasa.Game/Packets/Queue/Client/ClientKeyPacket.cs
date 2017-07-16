using System.IO;

namespace Rasa.Packets.Queue.Client
{
    using Data;
    using Extensions;

    public class ClientKeyPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.ClientKey;
        public string PublicKey { get; set; }

        public void Read(BinaryReader br)
        {
            PublicKey = br.ReadLengthedString();
        }

        public void Write(BinaryWriter bw)
        {
            bw.WriteLengthedString(PublicKey);
        }
    }
}
