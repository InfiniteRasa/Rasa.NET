using System.IO;

namespace Rasa.Packets.Login.Server
{
    using Cryptography;
    using Data;

    public class ServerKeyPacket : IOpcodedPacket<LoginOpcode>
    {
        public LoginOpcode Opcode { get; } = LoginOpcode.ServerKey;
        public BigNum PublicKey { get; set; } = new BigNum();
        public byte[] Prime { get; set; }
        public byte[] Generator { get; set; }

        public void Read(BinaryReader br)
        {
            var aLen = br.ReadInt32();
            var pLen = br.ReadInt32();
            var gLen = br.ReadInt32();

            PublicKey.ReadBigEndian(br.ReadBytes(aLen), 0, aLen);
            Prime = br.ReadBytes(pLen);
            Generator = br.ReadBytes(gLen);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(64);
            bw.Write(Prime.Length);
            bw.Write(Generator.Length);

            var data = new byte[64];
            PublicKey.WriteToBigEndian(data, 0, 64);

            bw.Write(data);
            bw.Write(Prime);
            bw.Write(Generator);
        }
    }
}
