using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.PvpRecord
{
    using Context.Char;
    using Structures.Char;

    public class PvpRecordRepository : IPvpRecordRepository
    {
        private readonly CharContext _charContext;

        public PvpRecordRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint SaveMatch(PvpMatchEntry match)
        {
            var row = match.Id == 0
                ? null
                : _charContext.CreateTrackingQuery(_charContext.PvpMatchEntries).FirstOrDefault(e => e.Id == match.Id);

            if (row == null)
            {
                row = new PvpMatchEntry();
                _charContext.PvpMatchEntries.Add(row);
            }

            row.Kind = match.Kind;
            row.WargameId = match.WargameId;
            row.MapContextId = match.MapContextId;
            row.InstanceId = match.InstanceId;
            row.StartedAt = match.StartedAt;
            row.EndedAt = match.EndedAt;
            row.Outcome = match.Outcome;
            row.WinnerSide = match.WinnerSide;
            row.Reason = match.Reason ?? "";
            row.Side1Name = match.Side1Name ?? "";
            row.Side1ClanId = match.Side1ClanId;
            row.Side1Score = match.Side1Score;
            row.Side1Kills = match.Side1Kills;
            row.Side2Name = match.Side2Name ?? "";
            row.Side2ClanId = match.Side2ClanId;
            row.Side2Score = match.Side2Score;
            row.Side2Kills = match.Side2Kills;

            _charContext.SaveChanges();

            return row.Id;
        }

        public void SavePlayers(uint matchId, IReadOnlyCollection<PvpMatchPlayerEntry> players)
        {
            if (players == null || players.Count == 0)
                return;

            var ids = players.Select(p => p.CharacterId).Distinct().ToList();
            var rows = _charContext.CreateTrackingQuery(_charContext.PvpMatchPlayerEntries)
                .Where(e => e.MatchId == matchId && ids.Contains(e.CharacterId))
                .ToDictionary(e => e.CharacterId);

            foreach (var player in players)
            {
                if (!rows.TryGetValue(player.CharacterId, out var row))
                {
                    row = new PvpMatchPlayerEntry { MatchId = matchId, CharacterId = player.CharacterId };
                    rows[player.CharacterId] = row;
                    _charContext.PvpMatchPlayerEntries.Add(row);
                }

                row.Side = player.Side;
                row.Name = player.Name ?? "";
                row.FamilyName = player.FamilyName ?? "";
                row.ClanId = player.ClanId;
                row.Kills = player.Kills;
                row.Deaths = player.Deaths;
                row.Damage = player.Damage;
                row.Healing = player.Healing;
                row.Captures = player.Captures;
                row.Prestige = player.Prestige;
                row.PresentAtEnd = player.PresentAtEnd;
            }

            _charContext.SaveChanges();
        }

        public List<PvpMatchEntry> GetOpenMatches(PvpMatchKind kind)
        {
            var wanted = (byte)kind;

            return _charContext.CreateNoTrackingQuery(_charContext.PvpMatchEntries)
                .Where(e => e.Kind == wanted && e.EndedAt == null)
                .OrderBy(e => e.Id)
                .ToList();
        }

        public PvpMatchEntry GetMatch(uint id) =>
            _charContext.CreateNoTrackingQuery(_charContext.PvpMatchEntries).FirstOrDefault(e => e.Id == id);

        public List<PvpMatchPlayerEntry> GetPlayers(uint matchId) =>
            _charContext.CreateNoTrackingQuery(_charContext.PvpMatchPlayerEntries)
                .Where(e => e.MatchId == matchId)
                .OrderBy(e => e.Side)
                .ThenBy(e => e.CharacterId)
                .ToList();

        public void SaveWagers(uint matchId, IReadOnlyCollection<PvpMatchWagerEntry> wagers)
        {
            if (wagers == null || wagers.Count == 0)
                return;

            var ids = wagers.Select(w => w.CharacterId).Distinct().ToList();
            var rows = _charContext.CreateTrackingQuery(_charContext.PvpMatchWagerEntries)
                .Where(e => e.MatchId == matchId && ids.Contains(e.CharacterId))
                .ToDictionary(e => e.CharacterId);

            foreach (var wager in wagers)
            {
                if (!rows.TryGetValue(wager.CharacterId, out var row))
                {
                    row = new PvpMatchWagerEntry { MatchId = matchId, CharacterId = wager.CharacterId };
                    rows[wager.CharacterId] = row;
                    _charContext.PvpMatchWagerEntries.Add(row);
                }

                row.Side = wager.Side;
                row.ItemId = wager.ItemId;
                row.ItemTemplateId = wager.ItemTemplateId;
                row.QualityId = wager.QualityId;
                row.StackSize = wager.StackSize;
                row.Result = wager.Result;
                row.RecipientClanId = wager.RecipientClanId;
                row.RecipientCharacterId = wager.RecipientCharacterId;
            }

            _charContext.SaveChanges();
        }

        public List<PvpMatchWagerEntry> GetWagers(uint matchId) =>
            _charContext.CreateNoTrackingQuery(_charContext.PvpMatchWagerEntries)
                .Where(e => e.MatchId == matchId)
                .OrderBy(e => e.Side)
                .ThenBy(e => e.CharacterId)
                .ToList();

        public List<PvpMatchEntry> GetRecentMatches(int count) =>
            _charContext.CreateNoTrackingQuery(_charContext.PvpMatchEntries)
                .Where(e => e.EndedAt != null)
                .OrderByDescending(e => e.EndedAt)
                .ThenByDescending(e => e.Id)
                .Take(count < 0 ? 0 : count)
                .ToList();
    }
}
