namespace Rasa.Game.Handlers
{
    using Managers;

    public partial class ClientPacketHandler
    {
        private readonly ICharacterManager _characterManager;

        public Client Client { get; }

        public ClientPacketHandler(Client client, ICharacterManager characterManager)
        {
            Client = client;
            _characterManager = characterManager;
        }
    }
}
