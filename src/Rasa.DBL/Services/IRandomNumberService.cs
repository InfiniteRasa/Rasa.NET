namespace Rasa.Services
{
    public interface IRandomNumberService
    {
        byte[] CreateRandomBytes(uint length);
    }
}