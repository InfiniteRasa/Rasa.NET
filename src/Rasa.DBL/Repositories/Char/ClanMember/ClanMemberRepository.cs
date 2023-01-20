using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.ClanMember
{
    using Context.Char;
    using Structures.Char;

    public class ClanMemberRepository : IClanMemberRepository
    {
        private readonly CharContext _charContext;

        public ClanMemberRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public bool DeleteClanMember(ClanMemberEntry member)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var entry = query.Where(e => e.CharacterId == member.CharacterId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();

            return true;
        }

        public bool DeleteClanMembers(uint clanId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var entry = query.Where(e => e.ClanId == clanId).ToList();

            _charContext.RemoveRange(entry);
            _charContext.SaveChanges();

            return true;
        }

        public List<ClanMemberEntry> GetAllClanMembersByClanId(uint clanId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var entry = query.Where(e => e.ClanId == clanId).ToList();

            return entry;
        }

        public ClanMemberEntry GetClanMemberByCharacterId(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var entry = query.Where(e => e.CharacterId == characterId).FirstOrDefault();

            return entry;
        }

        public bool InsertClanMemberData(uint clanId, uint characterid, byte rank, string note)
        {
            var entry = new ClanMemberEntry
            {
                ClanId = clanId,
                CharacterId = characterid,
                Rank = rank,
                Note = note
            };

            return true;
        }

        public void UpdateRankByCharacterId(byte rank, uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var entry = query.Where(e => e.CharacterId == characterId).FirstOrDefault();

            entry.Rank = rank;

            _charContext.ClanMemberEntries.Update(entry);
            _charContext.SaveChanges();
        }
    }
}
