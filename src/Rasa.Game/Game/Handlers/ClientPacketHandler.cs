namespace Rasa.Game.Handlers
{
    public partial class ClientPacketHandler
    {
        public Client Client { get; }

        public ClientPacketHandler(Client client)
        {
            Client = client;
        }
    }
}
