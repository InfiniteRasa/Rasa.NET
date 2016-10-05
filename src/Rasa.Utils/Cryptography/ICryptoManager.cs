namespace Rasa.Cryptography
{
    using Game;

    public interface ICryptoManager
    {
        void Encrypt(byte[] data, int offset, ref int length, int maxLength, ClientCryptData cryptData = null);
        bool Decrypt(byte[] data, int offset, int length, ClientCryptData cryptData = null);
    }
}
