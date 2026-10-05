using System.Collections.Generic;

namespace Rasa.Repositories.Char.PvpRecord
{
    using Structures.Char;

    /// <summary>The records of PvP matches: pvp_match, pvp_match_player and pvp_match_wager. Every call saves at once.</summary>
    public interface IPvpRecordRepository
    {
        /// <summary>Writes a match: a new row for an id of 0, the row with that id otherwise. Returns the row's id.</summary>
        uint SaveMatch(PvpMatchEntry match);

        /// <summary>Writes a match's players: each a new row, or the row that match and character have already.</summary>
        void SavePlayers(uint matchId, IReadOnlyCollection<PvpMatchPlayerEntry> players);

        /// <summary>The matches of a kind that have not ended (clan feuds still running), oldest first.</summary>
        List<PvpMatchEntry> GetOpenMatches(PvpMatchKind kind);

        PvpMatchEntry GetMatch(uint id);

        /// <summary>A match's players, side 1 first.</summary>
        List<PvpMatchPlayerEntry> GetPlayers(uint matchId);

        /// <summary>Writes a match's wagered items: each a new row, or the row that match and character have already.</summary>
        void SaveWagers(uint matchId, IReadOnlyCollection<PvpMatchWagerEntry> wagers);

        /// <summary>A match's wagered items, side 1 first.</summary>
        List<PvpMatchWagerEntry> GetWagers(uint matchId);

        /// <summary>The matches that have ended, the latest first.</summary>
        List<PvpMatchEntry> GetRecentMatches(int count);
    }
}
