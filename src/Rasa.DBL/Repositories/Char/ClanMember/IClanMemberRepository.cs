using System.Collections.Generic;

namespace Rasa.Repositories.Char.ClanMember
{
    using Structures.Char;
    public interface IClanMemberRepository
    {
        bool DeleteClanMember(ClanMemberEntry member);
        bool DeleteClanMembers(uint clanId);
        List<ClanMemberEntry> GetAllClanMembersByClanId(uint clanId);
        ClanMemberEntry GetClanMemberByCharacterId(uint characterId);

        /// <summary>Every member of a clan with their character's name, level and map and their account's family name, in one query.</summary>
        List<ClanRosterEntry> GetRoster(uint clanId);

        /// <summary>The roster lines of every member of the given clans, in one query: what a clan war search counts and averages.</summary>
        List<ClanRosterEntry> GetRosters(ICollection<uint> clanIds);

        /// <summary>One character's roster line, or null when they are in no clan.</summary>
        ClanRosterEntry GetRosterEntry(uint characterId);

        bool InsertClanMemberData(uint clanId, uint characterid, byte rank, string note);
        void UpdateRankByCharacterId(byte rank, uint characterId);
    }
}
