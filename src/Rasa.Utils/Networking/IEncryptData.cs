namespace Rasa.Networking
{
    using Memory;

    public interface IEncryptData
    {
        BufferData Data { get; }

        int Length { get; set; }
    }
}