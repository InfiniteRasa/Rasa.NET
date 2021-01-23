namespace Rasa.Managers
{
    using Game;

    public interface IMapChannelManager
    {
        void MapLoaded(Client client);
        void PassClientToMap(Client client);
        void CharacterLogout(Client client);
    }
}