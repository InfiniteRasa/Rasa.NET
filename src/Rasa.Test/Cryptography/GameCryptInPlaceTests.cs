using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Cryptography
{
    using Rasa.Cryptography;
    using Rasa.Cryptography.Game;

    /// <summary>
    /// The game cipher works on the frame in place (GameCryptManager). It copied every frame into
    /// a new array of words and back. What it makes must be what it made, byte for byte: the
    /// version that copied is kept here to compare against, on frames of every length up to a
    /// few blocks and some long ones, at offsets into a larger buffer, under several keys.
    /// </summary>
    [TestClass]
    public class GameCryptInPlaceTests
    {
        [TestMethod]
        public void EncryptionMakesTheBytesItAlwaysMade()
        {
            foreach (var key in Keys())
                foreach (var length in Lengths())
                    foreach (var offset in new[] { 0, 3, 8 })
                    {
                        var random = new Random(length * 31 + offset);
                        var buffer = new byte[offset + length + 16];
                        random.NextBytes(buffer);
                        var expected = (byte[])buffer.Clone();

                        var newLength = length;
                        var oldLength = length;

                        GameCryptManager.Encrypt(buffer, offset, ref newLength, buffer.Length - offset, key);
                        CopyingEncrypt(expected, offset, ref oldLength, buffer.Length - offset, key);

                        Assert.AreEqual(oldLength, newLength, $"length {length}");
                        CollectionAssert.AreEqual(expected, buffer, $"length {length} at {offset}: the frame, and nothing either side of it");
                    }
        }

        [TestMethod]
        public void DecryptionUndoesItAndMatchesWhatItAlwaysDid()
        {
            foreach (var key in Keys())
                foreach (var length in Lengths().Where(l => l % 8 == 0))
                    foreach (var offset in new[] { 0, 5 })
                    {
                        var random = new Random(length * 17 + offset);
                        var plain = new byte[offset + length + 8];
                        random.NextBytes(plain);

                        var cipher = (byte[])plain.Clone();
                        var encrypted = length;
                        GameCryptManager.Encrypt(cipher, offset, ref encrypted, cipher.Length - offset, key);

                        var decrypted = (byte[])cipher.Clone();
                        var expected = (byte[])cipher.Clone();

                        Assert.IsTrue(GameCryptManager.Decrypt(decrypted, offset, length, key));
                        Assert.IsTrue(CopyingDecrypt(expected, offset, length, key));

                        CollectionAssert.AreEqual(expected, decrypted, $"length {length} at {offset}");
                        CollectionAssert.AreEqual(plain, decrypted, "the frame as it was");
                    }
        }

        [TestMethod]
        public void NeitherAllocates()
        {
            var key = Keys().First();
            var buffer = new byte[4096];
            var length = 1000;

            GameCryptManager.Encrypt(buffer, 0, ref length, buffer.Length, key);
            GameCryptManager.Decrypt(buffer, 0, length, key);

            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 100; i++)
            {
                length = 1000;
                GameCryptManager.Encrypt(buffer, 0, ref length, buffer.Length, key);
                GameCryptManager.Decrypt(buffer, 0, length, key);
            }

            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [TestMethod]
        public void TheLimitsAreAsTheyWere()
        {
            var key = Keys().First();
            var length = 13;

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GameCryptManager.Encrypt(new byte[32], 0, ref length, 15, key));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GameCryptManager.Decrypt(new byte[32], 0, 12, key));

            var empty = 0;
            GameCryptManager.Encrypt(new byte[8], 0, ref empty, 8, key);
            Assert.AreEqual(0, empty);
        }

        private static int[] Lengths() => Enumerable.Range(0, 41).Concat(new[] { 63, 64, 65, 255, 1000, 4000 }).ToArray();

        private static ClientCryptData[] Keys() => new[] { 1, 7, 255 }.Select(seed =>
        {
            var key = new ClientCryptData();
            var bytes = new byte[64];
            new Random(seed).NextBytes(bytes);
            GameCryptManager.Initialize(key, bytes);
            return key;
        }).ToArray();

        #region The cipher as it was, copying through an array of words

        private static void CopyingEncrypt(byte[] data, int offset, ref int length, int maxLength, ClientCryptData cryptData)
        {
            var oldLen = length;

            var rem = length % 8;
            if (rem != 0)
                length += 8 - rem;

            if (length > maxLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            for (var i = oldLen; i < length; ++i)
                data[offset + i] = 0xCC;

            var uintData = new uint[length / 4];
            Buffer.BlockCopy(data, offset, uintData, 0, length);

            var x = Swap(BitConverter.ToUInt32(cryptData.MD5, 0));
            var y = Swap(BitConverter.ToUInt32(cryptData.MD5, 4));

            for (var i = 0; i < length / 8; ++i)
            {
                var a2 = i * 2;

                uintData[a2] = Swap(uintData[a2]);
                uintData[a2 + 1] = Swap(uintData[a2 + 1]);
                uintData[a2] ^= x;
                uintData[a2 + 1] ^= y;

                Blowfish.Encrypt(uintData, a2, cryptData.Key);

                x = uintData[a2];
                y = uintData[a2 + 1];

                uintData[a2] = Swap(uintData[a2]);
                uintData[a2 + 1] = Swap(uintData[a2 + 1]);
            }

            Buffer.BlockCopy(uintData, 0, data, offset, length);
        }

        private static bool CopyingDecrypt(byte[] data, int offset, int length, ClientCryptData cryptData)
        {
            var uintData = new uint[length / 4];
            Buffer.BlockCopy(data, offset, uintData, 0, length);

            var x = Swap(BitConverter.ToUInt32(cryptData.MD5, 0));
            var y = Swap(BitConverter.ToUInt32(cryptData.MD5, 4));

            for (var i = 0; i < length / 8; ++i)
            {
                var a2 = i * 2;

                uintData[a2] = Swap(uintData[a2]);
                uintData[a2 + 1] = Swap(uintData[a2 + 1]);

                var x2 = uintData[a2];
                var y2 = uintData[a2 + 1];

                Blowfish.Decrypt(uintData, a2, cryptData.Key);

                uintData[a2] ^= x;
                uintData[a2 + 1] ^= y;

                x = x2;
                y = y2;

                uintData[a2] = Swap(uintData[a2]);
                uintData[a2 + 1] = Swap(uintData[a2 + 1]);
            }

            Buffer.BlockCopy(uintData, 0, data, offset, length);

            return true;
        }

        private static uint Swap(uint value) =>
            ((value & 0x000000FFU) << 24) | ((value & 0x0000FF00U) << 8) | ((value & 0x00FF0000U) >> 8) | ((value & 0xFF000000U) >> 24);

        #endregion
    }
}
