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

            if (entry == null)
                return false;

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

        public List<ClanRosterEntry> GetRoster(uint clanId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);

            return ToRosterEntries(query.Where(e => e.ClanId == clanId)).ToList();
        }

        public List<ClanRosterEntry> GetRosters(ICollection<uint> clanIds)
        {
            if (clanIds == null || clanIds.Count == 0)
                return new List<ClanRosterEntry>();

            var ids = clanIds.ToList();
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);

            return ToRosterEntries(query.Where(e => ids.Contains(e.ClanId))).ToList();
        }

        public ClanRosterEntry GetRosterEntry(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);

            return ToRosterEntries(query.Where(e => e.CharacterId == characterId)).FirstOrDefault();
        }

        /// <summary>
        /// The membership joined to its character and that character's account, reading only the
        /// columns a roster line needs. Filtered before the projection, not after it, so the filter is
        /// on the table's own columns.
        /// </summary>
        private static IQueryable<ClanRosterEntry> ToRosterEntries(IQueryable<ClanMemberEntry> members)
        {
            return members.Select(e => new ClanRosterEntry
            {
                ClanId = e.ClanId,
                CharacterId = e.CharacterId,
                Rank = e.Rank,
                Note = e.Note,
                CharacterName = e.Character.Name,
                Level = e.Character.Level,
                MapContextId = e.Character.MapContextId,
                AccountId = e.Character.AccountId,
                FamilyName = e.Character.GameAccount.FamilyName
            });
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

            _charContext.ClanMemberEntries.Add(entry);
            _charContext.SaveChanges();

            return true;
        }

        public void UpdateRankByCharacterId(byte rank, uint characterId)
        {
            var entry = _charContext.CreateTrackingQuery(_charContext.ClanMemberEntries).FirstOrDefault(e => e.CharacterId == characterId);

            if (entry == null)
            {
                Logger.WriteLog(LogType.Error, $"Character {characterId} is not a clan member; rank update skipped.");
                return;
            }

            entry.Rank = rank;
            _charContext.SaveChanges();
        }
    }
}
