using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Cryptography
{
    using Rasa.Cryptography;
    using Rasa.Packets.Login.Client;

    // The game connection's key exchange (DHKeyExchange): a secret drawn for each connection,
    // BigInteger's arithmetic, and the client's public key checked.
    [TestClass]
    [DoNotParallelize]
    public class DHKeyExchangeTests
    {
        private static readonly BigInteger P = DHKeyExchange.Prime;

        [TestMethod]
        public void TheGroupIsTheClientsSafePrimeAndGeneratorFive()
        {
            Assert.AreEqual(512, (int)P.GetBitLength());
            Assert.AreEqual(new BigInteger(5), DHKeyExchange.Generator);

            // 5 is not in the subgroup of order (p - 1) / 2, so it generates the whole group.
            Assert.AreNotEqual(BigInteger.One, BigInteger.ModPow(5, (P - 1) / 2, P));
        }

        [TestMethod]
        public void EachConnectionHasItsOwnSecretAndPublicKey()
        {
            var (a1, publicKey1) = Generate();
            var (a2, publicKey2) = Generate();

            Assert.AreNotEqual(a1, a2);
            Assert.AreNotEqual(publicKey1, publicKey2);

            foreach (var (a, publicKey) in new[] { (a1, publicKey1), (a2, publicKey2) })
            {
                Assert.IsTrue(a >= 2 && a <= P - 2);
                Assert.AreEqual(BigInteger.ModPow(5, a, P), publicKey);
            }
        }

        // With the secret the old constant was, the public key is the one BigNum.ModExp made:
        // the new arithmetic gives the same numbers as the old.
        [TestMethod]
        public void TheOldConstantGivesTheOldPublicKey()
        {
            var a = new BigNum();
            var publicKey = new BigNum();

            WithRandomBytes(_ => Bytes(19234 - 2), () => DHKeyExchange.GeneratePrivateAndPublicA(a, publicKey));

            var old = new BigNum();
            var prime = new BigNum();
            var generator = new BigNum();
            var exponent = new BigNum();
            prime.ReadBigEndian(DHKeyExchange.ConstantPrime, 0, 64);
            generator.ReadBigEndian(DHKeyExchange.ConstantGenerator, 0, 1);
            exponent.SetUInt32(19234);
            old.ModExp(generator, exponent, prime);

            Assert.AreEqual(new BigInteger(19234), DHKeyExchange.ToBigInteger(a));
            CollectionAssert.AreEqual(old.Content, publicKey.Content);
        }

        [TestMethod]
        public void TheServerAndTheClientComeToTheSameKey()
        {
            for (var i = 0; i < 5; i++)
            {
                var a = new BigNum();
                var serverPublic = new BigNum();
                DHKeyExchange.GeneratePrivateAndPublicA(a, serverPublic);

                // The client's side: its own secret, its public key sent, the server's raised to it.
                var b = new BigInteger(RandomNumberGenerator.GetBytes(64), isUnsigned: true) % (P - 3) + 2;
                var clientPublic = Big(BigInteger.ModPow(5, b, P));
                var clientKey = BigInteger.ModPow(DHKeyExchange.ToBigInteger(serverPublic), b, P);

                var k = new BigNum();
                DHKeyExchange.GenerateServerK(a, clientPublic, k);

                Assert.AreEqual(clientKey, DHKeyExchange.ToBigInteger(k));

                // As LoginClient hands it to the cipher: 64 bytes, big-endian.
                var key = new byte[64];
                k.WriteToBigEndian(key, 0, key.Length);
                Assert.AreEqual(clientKey, new BigInteger(key, isUnsigned: true, isBigEndian: true));
            }
        }

        [TestMethod]
        public void TheSecretIsDrawnAgainUntilItIsInRange()
        {
            var draws = 0;
            var all = new byte[64];
            Array.Fill(all, (byte)0xFF);

            var secret = WithRandomBytes(_ => ++draws < 3 ? all : Bytes(0), DHKeyExchange.NewSecret);
            Assert.AreEqual(3, draws, "2^512 - 1 is not below p - 3");
            Assert.AreEqual(new BigInteger(2), secret);

            Assert.AreEqual(P - 2, WithRandomBytes(_ => Bytes(P - 4), DHKeyExchange.NewSecret));

            var exactly = 0;
            Assert.AreEqual(new BigInteger(5), WithRandomBytes(_ => ++exactly == 1 ? Bytes(P - 3) : Bytes(3), DHKeyExchange.NewSecret));
            Assert.AreEqual(2, exactly, "p - 3 is out");
        }

        [TestMethod]
        public void APublicKeyThatGivesAKeyAnyoneKnowsIsRefused()
        {
            foreach (var bad in new[] { BigInteger.Zero, BigInteger.One, P - 1, P, P + 1 })
            {
                Assert.ThrowsExactly<InvalidDataException>(() => ReadKey(bad), bad.ToString("X"));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DHKeyExchange.GenerateServerK(new BigNum(), Big(bad), new BigNum()));
            }

            foreach (var good in new[] { new BigInteger(2), P - 2, BigInteger.ModPow(5, 12345, P) })
                Assert.AreEqual(good, DHKeyExchange.ToBigInteger(ReadKey(good).B));
        }

        // Some 66 ms a handshake with BigNum and the 15-bit constant, and seconds with a real
        // secret; a millisecond or two with BigInteger.
        [TestMethod]
        public void AHandshakesArithmeticIsQuick()
        {
            var watch = Stopwatch.StartNew();

            for (var i = 0; i < 20; i++)
            {
                var a = new BigNum();
                var publicKey = new BigNum();
                DHKeyExchange.GeneratePrivateAndPublicA(a, publicKey);
                DHKeyExchange.GenerateServerK(a, publicKey, new BigNum());
            }

            Assert.IsLessThan(2000L, watch.ElapsedMilliseconds, "twenty handshakes");
        }

        private static (BigInteger Secret, BigInteger PublicKey) Generate()
        {
            var a = new BigNum();
            var publicKey = new BigNum();
            DHKeyExchange.GeneratePrivateAndPublicA(a, publicKey);
            return (DHKeyExchange.ToBigInteger(a), DHKeyExchange.ToBigInteger(publicKey));
        }

        private static ClientKeyPacket ReadKey(BigInteger value)
        {
            var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            stream.Position = 0;
            var packet = new ClientKeyPacket();
            packet.Read(new BinaryReader(stream));
            return packet;
        }

        private static BigNum Big(BigInteger value)
        {
            var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
            var result = new BigNum();
            result.Read(bytes, 0, bytes.Length);
            return result;
        }

        /// <summary>64 random bytes as DHKeyExchange draws them, big-endian, holding <paramref name="value"/>.</summary>
        private static byte[] Bytes(BigInteger value)
        {
            var raw = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            var bytes = new byte[64];
            raw.CopyTo(bytes, 64 - raw.Length);
            return bytes;
        }

        private static T WithRandomBytes<T>(Func<int, byte[]> source, Func<T> body)
        {
            var saved = DHKeyExchange.RandomBytes;
            DHKeyExchange.RandomBytes = source;

            try
            {
                return body();
            }
            finally
            {
                DHKeyExchange.RandomBytes = saved;
            }
        }

        private static void WithRandomBytes(Func<int, byte[]> source, Action body) =>
            WithRandomBytes(source, () => { body(); return 0; });
    }
}
