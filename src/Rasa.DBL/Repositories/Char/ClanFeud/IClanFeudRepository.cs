using System.Collections.Generic;

namespace Rasa.Repositories.Char.ClanFeud
{
    using Structures.Char;

    /// <summary>The clan feuds and feud challenges kept through a restart. Every call saves at once.</summary>
    public interface IClanFeudRepository
    {
        List<ClanFeudEntry> GetFeuds();
        List<ClanFeudChallengeEntry> GetChallenges();

        /// <summary>Adds the feud, or updates the row with its id.</summary>
        void SaveFeud(ClanFeudEntry feud);
        void DeleteFeud(uint id);

        /// <summary>Adds the challenge, or updates the row with its id.</summary>
        void SaveChallenge(ClanFeudChallengeEntry challenge);
        void DeleteChallenge(uint wargameId);
    }
}
