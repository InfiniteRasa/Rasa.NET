namespace Rasa.Game;

using Rasa.Cryptography;
using Rasa.Networking;

public interface IClientFactory
{
    Client Create(AsyncLengthedSocket socket, ClientCryptData data, Server server);
}