using System.Security.Cryptography;

namespace Rasa.Cryptography;

using Rasa.Cryptography.Game;

public static class GameCryptManager
{
    public static void Initialize(ClientCryptData cryptData, byte[] inputK)
    {
        Blowfish.SetKey(inputK, cryptData.Key);

        Array.Copy(MD5.HashData(inputK), cryptData.MD5, 0x10);
        Array.Copy(inputK, cryptData.K, 0x40);
    }

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
        for (var i = oldLen; i < length; ++i)
            data[offset + i] = 0xCC;

        var uintData = new uint[length / 4];
        Buffer.BlockCopy(data, offset, uintData, 0, length);

        var x = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 0));
        var y = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 4));

        for (var i = 0; i < (length / 8); ++i)
        {
            var a2 = i * 2;

            uintData[a2] = SwitchEndianInt(uintData[a2]);
            uintData[a2 + 1] = SwitchEndianInt(uintData[a2 + 1]);
            uintData[a2] ^= x;
            uintData[a2 + 1] ^= y;

            Blowfish.Encrypt(uintData, a2, cryptData.Key);

            x = uintData[a2];
            y = uintData[a2 + 1];

            uintData[a2] = SwitchEndianInt(uintData[a2]);
            uintData[a2 + 1] = SwitchEndianInt(uintData[a2 + 1]);
        }

        Buffer.BlockCopy(uintData, 0, data, offset, length);
    }

    public static bool Decrypt(byte[] data, int offset, int length, ClientCryptData cryptData)
    {
        if (length % 8 != 0)
            throw new ArgumentOutOfRangeException(nameof(length), "The lenght must be a multiple of 8!");

        var uintData = new uint[length / 4];
        Buffer.BlockCopy(data, offset, uintData, 0, length);

        var x = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 0));
        var y = SwitchEndianInt(BitConverter.ToUInt32(cryptData.MD5, 4));

        for (var i = 0; i < (length / 8); ++i)
        {
            //Switch endian first
            var a2 = i * 2;

            uintData[a2] = SwitchEndianInt(uintData[a2]);
            uintData[a2 + 1] = SwitchEndianInt(uintData[a2 + 1]);

            //Store new XOR
            var x2 = uintData[a2];
            var y2 = uintData[a2 + 1];

            Blowfish.Decrypt(uintData, a2, cryptData.Key);

            uintData[a2] ^= x;
            uintData[a2 + 1] ^= y;

            //Update XOR
            x = x2;
            y = y2;

            //Switch endian now
            uintData[a2] = SwitchEndianInt(uintData[a2]);
            uintData[a2 + 1] = SwitchEndianInt(uintData[a2 + 1]);
        }

        Buffer.BlockCopy(uintData, 0, data, offset, length);

        return true;
    }

    private static uint SwitchEndianInt(uint value)
    {
        return ((value & 0x000000FFU) << 24) | ((value & 0x0000FF00U) << 8) | ((value & 0x00FF0000U) >> 8) | ((value & 0xFF000000U) >> 24);
    }
}
