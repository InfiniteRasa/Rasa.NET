using System;
using System.Collections.Generic;

namespace Rasa.Repositories.Char.Clan
{
    using Structures.Char;
	
    public interface IClanRepository
    {
        ClanEntry CreateClan(string clanName, bool isPvP);
        bool DeleteClan(uint clanId);
        ClanEntry GetClanByCharacterId(uint characterId);
        ClanEntry GetClanById(uint clanId);
        ClanEntry GetClanByName(string clanName);
        List<ClanEntry> GetClans();
        bool UpdateRankTitleByClanId(uint clanId, uint rank, string title);
        void UpdateLastPvPClanTimeForMembers(uint clanId, DateTime lastPvPClanTimestamp);
        void UpdateCredits(uint clanId, uint remainderOfCredits);
        void UpdatePrestige(uint clanId, uint remainderOfCredits);
    }
}
