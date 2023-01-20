using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.Clan
{
    using Context.Char;
    using Structures.Char;

    public class ClanRepository : IClanRepository
    {
        private readonly CharContext _charContext;
        private readonly string _rank0 = "Grunt";
        private readonly string _rank1 = "Soldier";
        private readonly string _rank2 = "Officier";
        private readonly string _rank3 = "Clan Leader";

        public ClanRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public ClanEntry CreateClan(string clanName, bool isPvP)
        {
            var entry = new ClanEntry
            {
                Name = clanName,
                IsPvP = isPvP,
                RankTitle0 = _rank0,
                RankTitle1 = _rank1,
                RankTitle2 = _rank2,
                RankTitle3 = _rank3,
                CreatedAt = DateTime.UtcNow,
                Credits = 0,
                Prestige = 0,
                PurashedTabs = 0
            };

            _charContext.ClanEntries.Add(entry);
            _charContext.SaveChanges();

            entry.Id = _charContext.ClanEntries.Last().Id;

            return entry;
        }

        public bool DeleteClan(uint clanId)
        {
            try
            {
                var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
                var entry = query.Where(e => e.Id == clanId).FirstOrDefault();

                _charContext.Remove(entry);
                _charContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"Error in delete clan: {ex}");

                return false;
            }

        }

        public ClanEntry GetClanByCharacterId(uint characterId)
        {
            var clanQuery = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var clanMemberQuery = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var clan = clanQuery.FirstOrDefault(e => e.Id == clanMemberQuery.FirstOrDefault(e => e.CharacterId == characterId).ClanId);

            return clan;
        }

        public ClanEntry GetClanById(uint clanId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var entry = query.Where(e => e.Id == clanId).FirstOrDefault();

            return entry;
        }

        public ClanEntry GetClanByName(string clanName)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var entry = query.Where(e => e.Name == clanName).FirstOrDefault();

            return entry;
        }

        public List<ClanEntry> GetClans()
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var entries = query.ToList();

            return entries;
        }

        public void UpdateCredits(uint clanId, uint credits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var entry = query.Where(e => e.Id == clanId).FirstOrDefault();

            entry.Credits = credits;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateLastPvPClanTimeForMembers(uint clanId, DateTime lastPvPClanTimestamp)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanMemberEntries);
            var entry = query.Where(e => e.ClanId == clanId).ToList();

            foreach(var member in entry)
            {
                var characters = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
                var character = characters.Where(e => e.Id == member.CharacterId).FirstOrDefault();

                _charContext.Update(character);
            }

            _charContext.SaveChanges();
        }

        public void UpdatePrestige(uint clanId, uint prestige)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var entry = query.Where(e => e.Id == clanId).FirstOrDefault();

            entry.Prestige = prestige;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }

        public bool UpdateRankTitleByClanId(uint clanId, uint rank, string title)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ClanEntries);
            var entry = query.Where(e => e.Id == clanId).FirstOrDefault();

            switch (rank)
            {
                case 0:
                    entry.RankTitle0 = title;
                    break;
                case 1:
                    entry.RankTitle1 = title;
                    break;
                case 2:
                    entry.RankTitle2 = title;
                    break;
                case 3:
                    entry.RankTitle3 = title;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"Error in UpdateRankTitle: rank {rank} out of range.");
                    return false;
            }

            _charContext.Update(entry);
            _charContext.SaveChanges();

            return true;
        }
    }
}
