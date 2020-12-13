namespace Rasa.Game
{
    using Cryptography;
    using Managers;
    using Networking;
    using Repositories.GameAccount;

    public class ClientFactory : IClientFactory
    {
        private readonly IGameAccountRepository _gameAccountRepository;
        private readonly ICharacterManager _characterManager;

        public ClientFactory(IGameAccountRepository gameAccountRepository, ICharacterManager characterManager)
        {
            _gameAccountRepository = gameAccountRepository;
            _characterManager = characterManager;
        }

        public Client Create(LengthedSocket socket, ClientCryptData data, Server server)
        {
            return new Client(socket, data, server, _gameAccountRepository, _characterManager);
        }
    }
}