using System.IO;

namespace Rasa.Packets.Queue.Server
{
    using Data;
    using Extensions;

    public class ServerKeyPacket : IOpcodedPacket<QueueOpcode>
    {
        public QueueOpcode Opcode { get; } = QueueOpcode.ServerKey;
        public string PublicKey { get; set; }
        public string Prime { get; set; }
        public string Generator { get; set; }

        public void Read(BinaryReader br)
        {
            var pubKeyLen = br.ReadInt32();
            var primeLen = br.ReadInt32();
            var generatorLen = br.ReadInt32();

            PublicKey = br.ReadUtf8StringOn(pubKeyLen);
            Prime = br.ReadUtf8StringOn(primeLen);
            Generator = br.ReadUtf8StringOn(generatorLen);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(PublicKey.Length);
            bw.Write(Prime.Length);
            bw.Write(Generator.Length);

            bw.WriteUtf8String(PublicKey);
            bw.WriteUtf8String(Prime);
            bw.WriteUtf8String(Generator);
        }
    }
}
