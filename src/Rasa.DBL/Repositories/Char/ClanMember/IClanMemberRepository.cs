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
        bool InsertClanMemberData(uint clanId, uint characterid, byte rank, string note);
        void UpdateRankByCharacterId(byte rank, uint characterId);
    }
}
