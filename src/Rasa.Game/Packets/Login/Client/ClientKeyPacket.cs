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

            // 0, 1, p - 1 and anything from p up make a session key that does not depend on the
            // server's secret, or none in the group (DHKeyExchange.IsValidPublicKey).
            if (!DHKeyExchange.IsValidPublicKey(B))
                throw new InvalidDataException("Game key is not a public key between 1 and p - 1.");
        }

        public void Write(BinaryWriter bw)
        {
            var data = new byte[64];
            B.WriteToBigEndian(data, 0, data.Length);
        }
    }
}
