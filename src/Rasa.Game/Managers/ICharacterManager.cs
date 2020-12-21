namespace Rasa.Managers
{
    using Game;
    using Packets.Game.Client;

    public interface ICharacterManager
    {
        void StartCharacterSelection(Client client);
        void RequestCharacterName(Client client, int gender);
        void RequestFamilyName(Client client);
        void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet);
        void RequestDeleteCharacterInSlot(Client client, RequestDeleteCharacterInSlotPacket packet);
        void RequestSwitchToCharacterInSlot(Client client, RequestSwitchToCharacterInSlotPacket packet);
    }
}