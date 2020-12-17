namespace Rasa.Game
{
    using Cryptography;
    using Managers;
    using Networking;
    using Repositories;
    using Repositories.UnitOfWork;

    public class ClientFactory : IClientFactory
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ICharacterManager _characterManager;

        public ClientFactory(IUnitOfWorkFactory unitOfWorkFactory, ICharacterManager characterManager)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
            _characterManager = characterManager;
        }

        public Client Create(LengthedSocket socket, ClientCryptData data, Server server)
        {
            return new Client(socket, data, server, _unitOfWorkFactory, _characterManager);
        }
    }
}