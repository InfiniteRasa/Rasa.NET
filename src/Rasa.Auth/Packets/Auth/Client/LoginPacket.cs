using System;
using System.IO;
using System.Text;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace Rasa.Packets.Auth.Client
{
    using Data;

    public class LoginPacket : IOpcodedPacket<ClientOpcode>
    {
        private static readonly DesEngine Decrypter;
        //private static readonly DesEngine Encrypter;

        public string UserName { get; set; }
        public string Password { get; set; }
        public uint GameId { get; set; }
        public ushort CDKey { get; set; }

        public ClientOpcode Opcode { get; } = ClientOpcode.Login;

        static LoginPacket()
        {
            var key = new byte[] { 0x54, 0x45, 0x53, 0x54, 0x00, 0x00, 0x00, 0x00 };

            Decrypter = new DesEngine();
            Decrypter.Init(false, new DesParameters(key));

            //Encrypter = new DesEngine();
            //Encrypter.Init(true, new DesParameters(key));
        }

        public void Read(BinaryReader reader)
        {
            var buff = reader.ReadBytes(30);

            for (var i = 0; i < 24; i += 8)
                Decrypter.ProcessBlock(buff, i, buff, i);

            UserName = Encoding.UTF8.GetString(buff, 0, FirstZeroIndex(buff, 0, 14));
            Password = Encoding.UTF8.GetString(buff, 14, FirstZeroIndex(buff, 14, 16));
            GameId = reader.ReadUInt32();
            CDKey = reader.ReadUInt16();
        }

        public void Write(BinaryWriter writer)
        {
            var data = new byte[30];
            var unBuf = Encoding.UTF8.GetBytes(UserName);
            var pwBuf = Encoding.UTF8.GetBytes(Password);

            Array.Copy(unBuf, 0, data, 0, UserName.Length >= 14 ? 14 : UserName.Length);
            Array.Copy(pwBuf, 0, data, 14, Password.Length >= 16 ? 16 : Password.Length);

            //for (var i = 0; i < 24; i += 8)
                //Encrypter.ProcessBlock(data, i, data, i);

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
            return $"LoginPacket(\"{UserName}\", \"{Password}\", {GameId}, {CDKey})";
        }
    }
}
