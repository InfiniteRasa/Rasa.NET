using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.ClanFeud
{
    using Context.Char;
    using Structures.Char;

    public class ClanFeudRepository : IClanFeudRepository
    {
        private readonly CharContext _charContext;

        public ClanFeudRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<ClanFeudEntry> GetFeuds() =>
            _charContext.CreateNoTrackingQuery(_charContext.ClanFeudEntries).OrderBy(e => e.Id).ToList();

        public List<ClanFeudChallengeEntry> GetChallenges() =>
            _charContext.CreateNoTrackingQuery(_charContext.ClanFeudChallengeEntries).OrderBy(e => e.WargameId).ToList();

        public void SaveFeud(ClanFeudEntry feud)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ClanFeudEntries).FirstOrDefault(e => e.Id == feud.Id);

            if (row == null)
                _charContext.ClanFeudEntries.Add(new ClanFeudEntry
                {
                    Id = feud.Id,
                    ChallengerClanId = feud.ChallengerClanId,
                    TargetClanId = feud.TargetClanId,
                    EndsAt = feud.EndsAt,
                    ChallengerKills = feud.ChallengerKills,
                    TargetKills = feud.TargetKills,
                    ChallengerCharacterId = feud.ChallengerCharacterId,
                    TargetCharacterId = feud.TargetCharacterId
                });
            else
            {
                row.ChallengerClanId = feud.ChallengerClanId;
                row.TargetClanId = feud.TargetClanId;
                row.EndsAt = feud.EndsAt;
                row.ChallengerKills = feud.ChallengerKills;
                row.TargetKills = feud.TargetKills;
                row.ChallengerCharacterId = feud.ChallengerCharacterId;
                row.TargetCharacterId = feud.TargetCharacterId;
            }

            _charContext.SaveChanges();
        }

        public void DeleteFeud(uint id)
        {
            var rows = _charContext.CreateTrackingQuery(_charContext.ClanFeudEntries).Where(e => e.Id == id).ToList();
            var stakes = _charContext.CreateTrackingQuery(_charContext.ClanFeudStakeEntries).Where(e => e.FeudId == id).ToList();

            if (rows.Count == 0 && stakes.Count == 0)
                return;

            _charContext.ClanFeudEntries.RemoveRange(rows);
            _charContext.ClanFeudStakeEntries.RemoveRange(stakes);
            _charContext.SaveChanges();
        }

        public List<ClanFeudStakeEntry> GetStakes() =>
            _charContext.CreateNoTrackingQuery(_charContext.ClanFeudStakeEntries).OrderBy(e => e.FeudId).ThenBy(e => e.CharacterId).ToList();

        public void SaveStake(ClanFeudStakeEntry stake)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ClanFeudStakeEntries)
                .FirstOrDefault(e => e.FeudId == stake.FeudId && e.CharacterId == stake.CharacterId);

            if (row == null)
                _charContext.ClanFeudStakeEntries.Add(new ClanFeudStakeEntry
                {
                    FeudId = stake.FeudId,
                    CharacterId = stake.CharacterId,
                    ClanId = stake.ClanId
                });
            else
                row.ClanId = stake.ClanId;

            _charContext.SaveChanges();
        }

        public void SaveChallenge(ClanFeudChallengeEntry challenge)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ClanFeudChallengeEntries).FirstOrDefault(e => e.WargameId == challenge.WargameId);

            if (row == null)
                _charContext.ClanFeudChallengeEntries.Add(new ClanFeudChallengeEntry
                {
                    WargameId = challenge.WargameId,
                    ChallengerClanId = challenge.ChallengerClanId,
                    TargetClanId = challenge.TargetClanId,
                    ChallengerCharacterId = challenge.ChallengerCharacterId
                });
            else
            {
                row.ChallengerClanId = challenge.ChallengerClanId;
                row.TargetClanId = challenge.TargetClanId;
                row.ChallengerCharacterId = challenge.ChallengerCharacterId;
            }

            _charContext.SaveChanges();
        }

        public void DeleteChallenge(uint wargameId)
        {
            var rows = _charContext.CreateTrackingQuery(_charContext.ClanFeudChallengeEntries).Where(e => e.WargameId == wargameId).ToList();

            if (rows.Count == 0)
                return;

            _charContext.ClanFeudChallengeEntries.RemoveRange(rows);
            _charContext.SaveChanges();
        }
    }
}
