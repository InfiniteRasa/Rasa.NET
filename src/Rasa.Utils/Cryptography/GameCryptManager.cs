using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Rasa.Cryptography
{
    using Game;

    public class GameCryptManager : ICryptoManager
    {
        public static GameCryptManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf - To avoid another warning about double checked locking
                if (_instance == null)
                {
                    lock (InstLock)
                    {
                        if (_instance == null)
                            _instance = new GameCryptManager();
                    }
                }

                return _instance;
            }
        }

        private static GameCryptManager _instance;
        private static readonly object InstLock = new object();

        private GameCryptManager() { }

        public void Initialize(ClientCryptData cryptData, byte[] inputK)
        {
            Blowfish.SetKey(inputK, cryptData.Key);

            var md5 = MD5.Create();

            Array.Copy(md5.ComputeHash(inputK), cryptData.MD5, 0x10);
            Array.Copy(inputK, cryptData.K, 0x40);
        }

        public void Encrypt(byte[] packetData, int offset, ref int len, int maxLength, ClientCryptData cryptData)
        {
            if ((len & 7) != 0)
                Debugger.Break(); // TODO: add extra bytes and fill with const data, instead of breaking, if needed

            var data = new uint[len / 4];
            Buffer.BlockCopy(packetData, offset, data, 0, len);

            var x = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 0));
            var y = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 4));

            for (var i = 0; i < (len / 8); ++i)
            {
                var a2 = i * 2;

                data[a2] = SwitchEndianInt(data[a2]);
                data[a2 + 1] = SwitchEndianInt(data[a2 + 1]);
                data[a2] ^= x;
                data[a2 + 1] ^= y;

                Blowfish.Encrypt(data, a2, cryptData.Key);

                x = data[a2];
                y = data[a2 + 1];

                data[a2] = SwitchEndianInt(data[a2]);
                data[a2 + 1] = SwitchEndianInt(data[a2 + 1]);
            }

            Buffer.BlockCopy(data, 0, packetData, offset, len);
        }

        public bool Decrypt(byte[] packetData, int offset, int len, ClientCryptData cryptData)
        {
            if (len % 8 != 0)
                Debugger.Break();

            var data = new uint[len / 4];
            Buffer.BlockCopy(packetData, offset, data, 0, len);

            var x = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 0));
            var y = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 4));

            for (var i = 0; i < (len / 8); ++i)
            {
                //Switch endian first
                var a2 = offset + i * 2;

                data[a2] = SwitchEndianInt(data[a2]);
                data[a2 + 1] = SwitchEndianInt(data[a2 + 1]);

                //Store new XOR
                var x2 = data[a2];
                var y2 = data[a2 + 1];

                Blowfish.Decrypt(data, a2, cryptData.Key);

                data[a2] ^= x;
                data[a2 + 1] ^= y;

                //Update XOR
                x = x2;
                y = y2;

                //Switch endian now
                data[a2] = SwitchEndianInt(data[a2]);
                data[a2 + 1] = SwitchEndianInt(data[a2 + 1]);
            }

            Buffer.BlockCopy(data, 0, packetData, offset, len);

            return true;
        }

        private static uint SwitchEndianInt(uint value)
        {
            return ((value & 0x000000FFU) << 24) | ((value & 0x0000FF00U) << 8) | ((value & 0x00FF0000U) >> 8) | ((value & 0xFF000000U) >> 24);
        }
    }
}
