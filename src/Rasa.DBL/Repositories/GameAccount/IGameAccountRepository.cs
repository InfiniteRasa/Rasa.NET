using System.Net;

namespace Rasa.Repositories.GameAccount
{
    using Structures;

    public interface IGameAccountRepository
    {
        void CreateOrUpdate(uint id, string name, string email);

        GameAccountEntry Get(uint id);

        bool CanChangeFamilyName(uint id, string newFamilyName);

        void UpdateFamilyName(uint id, string newFamilyName);

        void UpdateLoginData(uint id, IPAddress remoteAddress);
    }
}