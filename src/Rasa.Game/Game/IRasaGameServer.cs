namespace Rasa.Game
{
    using Hosting;
    using Structures;

    public interface IRasaGameServer : IRasaServer
    {
        void DisconnectClient(Client client);

        LoginAccountEntry AuthenticateClient(Client client, uint accountId, uint oneTimeKey);

        bool IsBanned(uint accountId);

        bool IsAlreadyLoggedIn(uint accountId);

    }
}