using System;
using System.IO;

namespace Rasa.Packets.Login.Client
{
    using Cryptography;
    using Data;
    using Extensions;

    public class ClientKeyPacket : IOpcodedPacket<LoginOpcode>
    {
        public LoginOpcode Opcode { get; } = LoginOpcode.ClientKey;
        public BigNum B { get; set; } = new BigNum();

        public void Read(BinaryReader br)
        {
            var bLen = br.ReadInt32();
            if (bLen <= 0 || bLen > 64)
                throw new InvalidDataException("Game key length must be between 1 and 64 bytes.");

            B.ReadBigEndian(br.ReadBytesExactly(bLen), 0, bLen);
            br.EnsureFullyConsumed("Game key payload");
        }

        public void Write(BinaryWriter bw)
        {
            var data = new byte[64];
            B.WriteToBigEndian(data, 0, data.Length);
        }
    }
}
