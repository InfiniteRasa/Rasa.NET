using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Rasa.Cryptography
{
    /// <summary>
    /// The game connection's Diffie-Hellman exchange, in the client's group: the 512-bit prime
    /// below and generator 5, which ServerKeyPacket sends and the client uses as given. The
    /// prime is a safe prime ((p - 1) / 2 is prime too) and 5 generates the whole group.
    ///
    /// The server's secret is drawn for every connection, uniformly from 2 to p - 2. It was the
    /// constant 19234, which made the server's public key the same on every connection, so the
    /// session key of any recorded handshake could be worked out from the client's half.
    ///
    /// The arithmetic is System.Numerics.BigInteger's. BigNum's own ModExp gives the same numbers
    /// but takes some 66 ms for the old 15-bit exponent and over two seconds for a 256-bit one;
    /// BigInteger.ModPow takes about a millisecond and a half for a full-size one. BigNum is
    /// still what the key packets carry.
    ///
    /// 512 bits, and nothing authenticates either side: this keeps a recorded stream from being
    /// read by anyone who has the handshake, and no more. The client's group cannot be changed
    /// from here.
    /// </summary>
    public static class DHKeyExchange
    {
        public static readonly byte[] ConstantPrime =
        {
            0x98, 0x0F, 0x91, 0xEA, 0xAD, 0xAD, 0x8E, 0x7D, 0xF9, 0xEC, 0x43, 0x1D, 0xD4, 0x1C, 0xEF, 0x3F,
            0xBE, 0xCF, 0xD1, 0xAE, 0xD2, 0x77, 0x1C, 0xCF, 0xF8, 0x5E, 0xF8, 0x85, 0x3E, 0x2F, 0x9B, 0xC8,
            0x30, 0x2E, 0xD3, 0xC4, 0x7F, 0xE6, 0x29, 0x72, 0xE0, 0x08, 0xE9, 0x32, 0x53, 0x97, 0xDB, 0x41,
            0x37, 0x98, 0xB3, 0x8A, 0xDC, 0xB8, 0xAF, 0xD3, 0x6A, 0x69, 0xD5, 0x12, 0xEC, 0x32, 0x61, 0xAF
        };
        public static readonly byte[] ConstantGenerator = { 5 };

        public static readonly BigInteger Prime = new BigInteger(ConstantPrime, isUnsigned: true, isBigEndian: true);
        public static readonly BigInteger Generator = new BigInteger(ConstantGenerator, isUnsigned: true, isBigEndian: true);

        /// <summary>Where the secrets come from; a test's to replace.</summary>
        internal static Func<int, byte[]> RandomBytes { get; set; } = RandomNumberGenerator.GetBytes;

        // ReSharper disable InconsistentNaming
        /// <summary>A new secret <paramref name="a"/> and the public key <paramref name="A"/> = 5^a mod p sent to the client.</summary>
        public static void GeneratePrivateAndPublicA(BigNum a, BigNum A)
        // ReSharper restore InconsistentNaming
        {
            var secret = NewSecret();

            Store(a, secret);
            Store(A, BigInteger.ModPow(Generator, secret, Prime));
        }

        /// <summary>The session key: the client's public key <paramref name="b"/> to the server's secret <paramref name="a"/>, mod p.</summary>
        public static void GenerateServerK(BigNum a, BigNum b, BigNum k)
        {
            var publicKey = ToBigInteger(b);

            if (!IsValidPublicKey(publicKey))
                throw new ArgumentOutOfRangeException(nameof(b), "The client's public key is not between 1 and p - 1.");

            Store(k, BigInteger.ModPow(publicKey, ToBigInteger(a), Prime));
        }

        /// <summary>
        /// Whether a public key may be used: 1 &lt; key &lt; p - 1. 0, 1 and p - 1 give a session
        /// key anyone can work out (0, or 1 or p - 1 whatever the secret), and p or more is not a
        /// member of the group.
        /// </summary>
        public static bool IsValidPublicKey(BigNum key) => IsValidPublicKey(ToBigInteger(key));

        private static bool IsValidPublicKey(BigInteger key) => key > BigInteger.One && key < Prime - BigInteger.One;

        /// <summary>
        /// A secret uniformly from 2 to p - 2: 512 random bits, drawn again while they are not
        /// below p - 3 (a little over half are), then moved up by 2.
        /// </summary>
        internal static BigInteger NewSecret()
        {
            var span = Prime - 3;

            while (true)
            {
                var candidate = new BigInteger(RandomBytes(ConstantPrime.Length), isUnsigned: true, isBigEndian: true);

                if (candidate < span)
                    return candidate + 2;
            }
        }

        internal static BigInteger ToBigInteger(BigNum value) =>
            new BigInteger(value.Content, isUnsigned: true, isBigEndian: false);

        private static void Store(BigNum target, BigInteger value)
        {
            var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);

            target.Read(bytes, 0, bytes.Length);
        }
    }
}
