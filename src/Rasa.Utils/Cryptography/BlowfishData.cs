namespace Rasa.Cryptography
{
    public class BlowfishData
    {
        public uint[] P { get; set; } = new uint[6];
        public uint[] S { get; set; } = new uint[1024];
    }
}
