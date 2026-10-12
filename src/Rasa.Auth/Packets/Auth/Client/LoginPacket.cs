using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Rasa.Packets.Auth.Client
{
    using Data;
    using Extensions;

    public class LoginPacket : IOpcodedPacket<ClientOpcode>
    {
        /// <summary>
        /// The client encrypts the first 24 of the 30 bytes - the name and most of the
        /// password - with single DES, ECB, under this key ("TEST" and four zeroes).
        ///
        /// System.Security.Cryptography's DES: CNG on Windows; on Linux, OpenSSL 3, which keeps
        /// single DES in its legacy provider. .NET loads that provider itself, and the
        /// distributions' libssl3, the aspnet images' included, ship it (ossl-modules/legacy.so);
        /// an image built without it cannot decrypt a login.
        /// </summary>
        private static readonly byte[] Key = { 0x54, 0x45, 0x53, 0x54, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>The encrypted part of the 30 bytes.</summary>
        private const int EncryptedLength = 24;

        public string UserName { get; set; }
        public string Password { get; set; }
        public uint GameId { get; set; }
        public ushort CDKey { get; set; }

        public ClientOpcode Opcode { get; } = ClientOpcode.Login;

        public void Read(BinaryReader reader)
        {
            var buff = reader.ReadBytesExactly(30);

            // A new one for each login: a cipher object is not to be shared between threads.
            using (var des = DES.Create())
            {
                des.Key = Key;
                des.DecryptEcb(buff.AsSpan(0, EncryptedLength), buff.AsSpan(0, EncryptedLength), PaddingMode.None);
            }

            UserName = Encoding.UTF8.GetString(buff, 0, FirstZeroIndex(buff, 0, 14));
            Password = Encoding.UTF8.GetString(buff, 14, FirstZeroIndex(buff, 14, 16));
            GameId = reader.ReadUInt32();
            CDKey = reader.ReadUInt16();

            // TODO: Adding this breaks logging in, so commenting it out.
            // reader.EnsureFullyConsumed("Auth login payload");
        }

        public void Write(BinaryWriter writer)
        {
            var data = new byte[30];
            var unBuf = Encoding.UTF8.GetBytes(UserName);
            var pwBuf = Encoding.UTF8.GetBytes(Password);

            Array.Copy(unBuf, 0, data, 0, UserName.Length >= 14 ? 14 : UserName.Length);
            Array.Copy(pwBuf, 0, data, 14, Password.Length >= 16 ? 16 : Password.Length);

            // Left in the clear, as it always was: the server never sends this packet; only tests write one.

            writer.Write((byte) Opcode);
            writer.Write(data);
            writer.Write(GameId);
            writer.Write(CDKey);
        }

        private static int FirstZeroIndex(byte[] data, int off, int length)
        {
            for (var i = 0; i < length; ++i)
                if (data[off + i] == 0)
                    return i;

            return length;
        }

        public override string ToString()
        {
            // Never the password: this is what a log line or a debugger shows of the packet.
            return $"LoginPacket(\"{UserName}\", \"***\", {GameId}, {CDKey})";
        }
    }
}
