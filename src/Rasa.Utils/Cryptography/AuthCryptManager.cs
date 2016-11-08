using System;

namespace Rasa.Cryptography
{
    using Auth;

    public class AuthCryptManager : ICryptoManager
    {
        public static AuthCryptManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstLock)
                    {
                        if (_instance == null)
                            _instance = new AuthCryptManager();
                    }
                }

                return _instance;
            }
        }

        private static AuthCryptManager _instance;
        private static readonly object InstLock = new object();

        private AuthCryptManager() {}

        public bool Decrypt(byte[] data, int offset, int length, ClientCryptData cryptData = null)
        {
            // TODO: merge the 2 blowfish, use a static cryptdata instance with the other blowfish engine, if possible
            Blowfish.Decrypt(data, offset, length);

            return VerifyChecksum(data, offset, length);
        }

        public void Encrypt(byte[] data, int offset, ref int length, int maxLength, ClientCryptData cryptData = null)
        {
            // TODO: merge the 2 blowfish, use a static cryptdata instance with the other blowfish engine, if possible
            var oldLen = length;

            // Make the length a multiple of 8
            var rem = length % 8;
            if (rem != 0)
                length += 8 - rem;

            if (length + 8 > maxLength)
                throw new ArgumentOutOfRangeException(nameof(length), "The length can't exceed the maximal buffer length!");

            // Fill up extra padding bytes
            for (var i = oldLen; i < length; ++i)
                data[offset + i] = 0xCC;

            // Add checksum bytes to the length
            length += 8;

            AppendChecksum(data, offset, length);

            Blowfish.Encrypt(data, offset, length);
        }

        private static bool VerifyChecksum(byte[] data, int offset, int length)
        {
            long chksum = 0;

            for (var i = offset; i < (length - 4); i += 4)
                chksum ^= BitConverter.ToUInt32(data, i);

            return 0 == chksum;
        }

        private static void AppendChecksum(byte[] data, int offset, int length)
        {
            var chksum = 0U;
            var dataEnd = length - 8;

            for (var i = 0; i < dataEnd; i += 4)
                chksum ^= BitConverter.ToUInt32(data, offset + i);

            Array.Copy(BitConverter.GetBytes(chksum), 0, data, offset + dataEnd, 4);
        }
    }
}
