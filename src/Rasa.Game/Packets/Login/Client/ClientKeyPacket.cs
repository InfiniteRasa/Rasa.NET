using System;
using System.IO;

namespace Rasa.Packets.Login.Client
{
    using Cryptography;
    using Data;

    public class ClientKeyPacket : IOpcodedPacket<LoginOpcode>
    {
        public LoginOpcode Opcode { get; } = LoginOpcode.ClientKey;
        public BigNum B { get; set; } = new BigNum();

        public void Read(BinaryReader br)
        {
            var bLen = br.ReadInt32();
            if (bLen > 64)
                throw new Exception("Why is it bigger?");

            B.ReadBigEndian(br.ReadBytes(bLen), 0, bLen);
        }

        public void Write(BinaryWriter bw)
        {
            var data = new byte[64];
            B.WriteToBigEndian(data, 0, data.Length);
        }
    }
}
