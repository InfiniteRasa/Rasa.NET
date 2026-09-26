using System.Net;

namespace Rasa.Repositories.Char.GameAccount
{
    using Structures.Char;

    public interface IGameAccountRepository
    {
        void CreateOrUpdate(uint id, string name, string email);

        GameAccountEntry Get(uint id);

        GameAccountEntry Get(string name);

        /// <summary>Like Get(uint) with characters included, but null when the account does not exist.</summary>
        GameAccountEntry Find(uint id);

        /// <summary>
        /// Family name lookup for names a player typed: an exact match wins, otherwise the
        /// first case-insensitive one. Null when nothing matches.
        /// </summary>
        GameAccountEntry FindByFamilyName(string familyName);

        bool CanChangeFamilyName(uint id, string newFamilyName);

        void UpdateFamilyName(uint id, string newFamilyName);

        void UpdateLoginData(uint id, IPAddress remoteAddress);

        void UpdateSelectedSlot(uint id, byte selectedSlot);

        void UpdateAccountLevel(uint id, byte level);

        void UpdateCanSkipBootcamp(uint id, bool canSkipBootcamp);
        bool TryUpdateCanSkipBootcamp(uint id, bool expectedValue, bool canSkipBootcamp);
    }
}