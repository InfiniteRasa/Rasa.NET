namespace Rasa.Services.Random
{
    public interface IRandomNumberService
    {
        byte[] CreateRandomBytes(uint length);
    }
}