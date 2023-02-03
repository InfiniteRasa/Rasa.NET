namespace Rasa.Cryptography;

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

    // ReSharper disable InconsistentNaming
    public static void GeneratePrivateAndPublicA(BigNum a, BigNum A)
    // ReSharper restore InconsistentNaming
    {
        var prime = new BigNum();
        var generator = new BigNum();

        prime.ReadBigEndian(ConstantPrime, 0, ConstantPrime.Length);
        generator.ReadBigEndian(ConstantGenerator, 0, ConstantGenerator.Length);

        //Ok we are lame we just set a, to something low
        //Bignum_SetUsint32(a, 5 + (GetTickCount()&0xFFFF));
        a.SetUInt32(19234); //Hardcoded for testing purposes

        //A = G ^ a mod P
        A.ModExp(generator, a, prime);
    }

    public static void GenerateServerK(BigNum a, BigNum b, BigNum k)
    {
        var prime = new BigNum();

        prime.ReadBigEndian(ConstantPrime, 0, ConstantPrime.Length);

        k.ModExp(b, a, prime);
    }
}
