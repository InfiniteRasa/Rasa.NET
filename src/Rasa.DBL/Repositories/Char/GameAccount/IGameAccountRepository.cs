using System.Net;

namespace Rasa.Repositories.Char.GameAccount
{
    using Structures.Char;

    public interface IGameAccountRepository
    {
        void CreateOrUpdate(uint id, string name, string email);

        GameAccountEntry Get(uint id);

        GameAccountEntry Get(string name);

        bool CanChangeFamilyName(uint id, string newFamilyName);

        void UpdateFamilyName(uint id, string newFamilyName);

        void UpdateLoginData(uint id, IPAddress remoteAddress);

        void UpdateSelectedSlot(uint id, byte selectedSlot);

        void UpdateAccountLevel(uint id, byte level);
    }
}