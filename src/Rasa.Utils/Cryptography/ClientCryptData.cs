namespace Rasa.Cryptography;

public class ClientCryptData
{
    public BlowfishData Key { get; } = new BlowfishData();
    public byte[] MD5 { get; } = new byte[16];
    public byte[] K { get; } = new byte[64];
}
