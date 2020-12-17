namespace Rasa.Game
{
    using Cryptography;
    using Networking;

    public interface IClientFactory
    {
        Client Create(LengthedSocket socket, ClientCryptData data, Server server);
    }
}