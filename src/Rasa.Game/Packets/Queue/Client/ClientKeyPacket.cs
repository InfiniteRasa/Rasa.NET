using System;
using System.IO;
using Rasa.Extensions;

namespace Rasa.Packets.Queue.Client
{
    using Data;

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
