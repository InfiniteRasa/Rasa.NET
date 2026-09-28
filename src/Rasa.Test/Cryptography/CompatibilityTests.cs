using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Cryptography
{
    using Rasa.Cryptography;
    using Rasa.Packets.Auth.Client;
    using Rasa.Repositories.Auth.Account;

    [TestClass]
    public class CompatibilityTests
    {
        // Captured from the unchanged P0 baseline source and Portable.BouncyCastle 1.8.8
        // on .NET 10. These are source-parity fixtures, not a .NET 5 runtime baseline.
        private const string AuthCiphertext = "AFE60474DCD7DE5372DB73157A029CFBA79820AE1ACD2CC1";
        private const string GameCiphertext = "63906712FC104310E62458243963F760";
        private const string LoginCiphertext = "EF2C585131CA3F300B5A562319220221A56F7A904A604B89330000000000443322116655";

        [TestMethod]
        public void AuthEncryptionPreservesPaddingChecksumAndCiphertext()
        {
            var data = CreatePayload();
            var length = 13;

            AuthCryptManager.Encrypt(data, 0, ref length, data.Length);

            Assert.AreEqual(24, length);
            Assert.AreEqual(AuthCiphertext, Convert.ToHexString(data, 0, length));
        }

        [TestMethod]
        public void AuthDecryptionAcceptsLegacyFixture()
        {
            var data = Convert.FromHexString(AuthCiphertext);

            Assert.IsTrue(AuthCryptManager.Decrypt(data, 0, data.Length));
            AssertPayloadAndPadding(data);
        }

        [TestMethod]
        public void GameEncryptionPreservesKeyDigestPaddingAndCiphertext()
        {
            var data = CreatePayload();
            var length = 13;
            var key = CreateGameKey();

            GameCryptManager.Encrypt(data, 0, ref length, data.Length, key);

            Assert.AreEqual("B2D3F56BC197FD985D5965079B5E7148", Convert.ToHexString(key.MD5));
            Assert.AreEqual(16, length);
            Assert.AreEqual(GameCiphertext, Convert.ToHexString(data, 0, length));
        }

        [TestMethod]
        public void GameDecryptionAcceptsLegacyFixture()
        {
            var data = Convert.FromHexString(GameCiphertext);

            Assert.IsTrue(GameCryptManager.Decrypt(data, 0, data.Length, CreateGameKey()));
            AssertPayloadAndPadding(data);
        }

        [TestMethod]
        public void LoginDesDecryptionPreservesCredentialsAndTrailingFields()
        {
            using var stream = new MemoryStream(Convert.FromHexString(LoginCiphertext));
            using var reader = new BinaryReader(stream);
            var packet = new LoginPacket();

            packet.Read(reader);

            Assert.AreEqual("test", packet.UserName);
            Assert.AreEqual("password123", packet.Password);
            Assert.AreEqual(0x11223344U, packet.GameId);
            Assert.AreEqual((ushort)0x5566, packet.CDKey);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void DecryptionRejectsPartialBlocks()
        {
            Assert.IsFalse(AuthCryptManager.Decrypt(new byte[7], 0, 7));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                GameCryptManager.Decrypt(new byte[7], 0, 7, CreateGameKey()));
        }

        [TestMethod]
        public void PasswordHashPreservesSaltSeparatorAndLowercaseHex()
        {
            Assert.AreEqual("7c1845d5dace5dae3fece81aa6b654abad488fe56b1964169d13ec362827d2fb",
                AuthAccountRepository.Hash("password123", "fixture-salt"));
        }

        private static byte[] CreatePayload()
        {
            var data = new byte[32];
            for (var i = 0; i < 13; i++)
                data[i] = (byte)i;
            return data;
        }

        private static ClientCryptData CreateGameKey()
        {
            var key = new ClientCryptData();
            GameCryptManager.Initialize(key, Enumerable.Range(0, 64).Select(i => (byte)i).ToArray());
            return key;
        }

        private static void AssertPayloadAndPadding(byte[] data)
        {
            for (var i = 0; i < 13; i++)
                Assert.AreEqual((byte)i, data[i]);
            for (var i = 13; i < 16; i++)
                Assert.AreEqual((byte)0xCC, data[i]);
        }
    }
}
