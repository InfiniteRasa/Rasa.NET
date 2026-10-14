using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Rasa.Cryptography
{
    using Game;

    public static class GameCryptManager
    {
        public static void Initialize(ClientCryptData cryptData, byte[] inputK)
        {
            Blowfish.SetKey(inputK, cryptData.Key);

            using (var md5 = MD5.Create())
                Array.Copy(md5.ComputeHash(inputK), cryptData.MD5, 0x10);

            Array.Copy(inputK, cryptData.K, 0x40);
        }

        /// <summary>
        /// Encrypts a frame in place: padded with 0xCC to whole blocks, each block XORed with the
        /// one before it (the first with the key's digest) and run through the cipher, the words
        /// big-endian.
        ///
        /// It used to copy the frame into a new array of words and back again, for every frame
        /// sent to every client: the encryption was close to half the time the main loop spent
        /// sending, and the array the most of what it allocated doing it. The bytes are the same.
        /// </summary>
        public static void Encrypt(byte[] data, int offset, ref int length, int maxLength, ClientCryptData cryptData)
        {
            var oldLen = length;

            // Make the length a multiple of 8
            var rem = length % 8;
            if (rem != 0)
                length += 8 - rem;

            if (length > maxLength)
                throw new ArgumentOutOfRangeException(nameof(length), "The length can't exceed the maximal buffer length!");

            // Fill up extra padding bytes
            data.AsSpan(offset + oldLen, length - oldLen).Fill(0xCC);

            var blocks = data.AsSpan(offset, length);
            var p = cryptData.Key.P;
            var s = cryptData.Key.S;

            var x = BinaryPrimitives.ReadUInt32BigEndian(cryptData.MD5);
            var y = BinaryPrimitives.ReadUInt32BigEndian(cryptData.MD5.AsSpan(4));

            for (var i = 0; i < length; i += 8)
            {
                var block = blocks.Slice(i, 8);
                var first = BinaryPrimitives.ReadUInt32BigEndian(block) ^ x;
                var second = BinaryPrimitives.ReadUInt32BigEndian(block.Slice(4)) ^ y;

                Blowfish.EncryptBlock(ref first, ref second, p, s);

                x = first;
                y = second;

                BinaryPrimitives.WriteUInt32BigEndian(block, first);
                BinaryPrimitives.WriteUInt32BigEndian(block.Slice(4), second);
            }
        }

        /// <summary>Decrypts a frame in place: <see cref="Encrypt"/> undone.</summary>
        public static bool Decrypt(byte[] data, int offset, int length, ClientCryptData cryptData)
        {
            if (length % 8 != 0)
                throw new ArgumentOutOfRangeException(nameof(length), "The lenght must be a multiple of 8!");

            var blocks = data.AsSpan(offset, length);
            var p = cryptData.Key.P;
            var s = cryptData.Key.S;

            var x = BinaryPrimitives.ReadUInt32BigEndian(cryptData.MD5);
            var y = BinaryPrimitives.ReadUInt32BigEndian(cryptData.MD5.AsSpan(4));

            for (var i = 0; i < length; i += 8)
            {
                var block = blocks.Slice(i, 8);
                var first = BinaryPrimitives.ReadUInt32BigEndian(block);
                var second = BinaryPrimitives.ReadUInt32BigEndian(block.Slice(4));

                // The ciphertext, which the next block was XORed with.
                var nextX = first;
                var nextY = second;

                Blowfish.DecryptBlock(ref first, ref second, p, s);

                BinaryPrimitives.WriteUInt32BigEndian(block, first ^ x);
                BinaryPrimitives.WriteUInt32BigEndian(block.Slice(4), second ^ y);

                x = nextX;
                y = nextY;
            }

            return true;
        }
    }
}
