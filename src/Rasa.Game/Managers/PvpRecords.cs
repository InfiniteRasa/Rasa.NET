using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Game;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The records kept of PvP matches between two sides: clan feuds (ClanFeuds), squad wargames
    /// (SquadWargames), a battleground's matches (Battlegrounds) and duels (Duels). For each: when and where it
    /// was fought, how it ended, which side won, the two sides' scores, every player with
    /// the side they were on and their own score, and the items that were wagered in it. Kept
    /// in the character database's pvp_match, pvp_match_player and pvp_match_wager tables
    /// (<see cref="IStore"/>); nothing here is sent to a client, and nothing in a match waits
    /// on it or fails with it: a store that cannot be written costs the record, and says so in
    /// the log.
    ///
    /// The two sides are side 1 and side 2 everywhere: the challenging clan, squad or duelist
    /// and Red Team are side 1, the challenged one and Blue Team side 2. A side's score is
    /// what it is judged by - its kills, or in a battleground the control points it holds at
    /// the end, with its kills beside it.
    ///
    /// A squad wargame, a battleground's match and a duel are short and are written once, when
    /// they end (<see cref="Record"/>). A clan feud runs for days and through restarts, so its
    /// record is opened when it starts (<see cref="FeudStarted"/>), brought up to date on every
    /// kill (<see cref="FeudKill"/>) - the two who fought get their rows then - and closed when
    /// the feud ends (<see cref="FeudEnded"/>), when the rest of both clans are added with
    /// nothing against their names. After a restart the feuds read back find their open
    /// records again (<see cref="FeudsRestored"/>).
    ///
    /// The wagered items (InventoryManager.Wager) are read when a match ends
    /// (<see cref="WageredItems"/>): one row for each of its players with an item in their wager
    /// slot then, saying which item and what became of it. Only a clan feud takes a wagered
    /// item: every other match's are kept, and so are a feud's until its forfeits are told
    /// (<see cref="WagersForfeited"/>).
    /// </summary>
    public class PvpRecords
    {
        private static PvpRecords _instance;
        private static readonly object InstanceLock = new object();

        public static PvpRecords Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new PvpRecords
                        {
                            WageredItems = characterIds => InventoryManager.Instance.WageredItemsOf(characterIds)
                        };

                return _instance;
            }
        }

        /// <summary>Where the records are kept. Every call may throw; none is retried.</summary>
        public interface IStore
        {
            /// <summary>Writes a match: a new row for an id of 0, else the row with that id. Returns the id.</summary>
            uint SaveMatch(PvpMatchEntry match);

            /// <summary>Writes players of a match, each a new row or the one that match and character have.</summary>
            void SavePlayers(uint matchId, IReadOnlyCollection<PvpMatchPlayerEntry> players);

            /// <summary>The clan feuds' records that have not been closed.</summary>
            List<PvpMatchEntry> OpenFeuds();

            List<PvpMatchPlayerEntry> Players(uint matchId);

            /// <summary>Writes wagered items of a match, each a new row or the one that match and character have.</summary>
            void SaveWagers(uint matchId, IReadOnlyCollection<PvpMatchWagerEntry> wagers);
        }

        /// <summary>A clan feud as its record needs it: its id, and the two clans.</summary>
        public readonly struct FeudInfo
        {
            public uint FeudId { get; }
            public uint ChallengerClanId { get; }
            public string ChallengerName { get; }
            public int ChallengerKills { get; }
            public uint TargetClanId { get; }
            public string TargetName { get; }
            public int TargetKills { get; }

            public FeudInfo(uint feudId, uint challengerClanId, string challengerName, int challengerKills, uint targetClanId, string targetName, int targetKills)
            {
                FeudId = feudId;
                ChallengerClanId = challengerClanId;
                ChallengerName = challengerName;
                ChallengerKills = challengerKills;
                TargetClanId = targetClanId;
                TargetName = targetName;
                TargetKills = targetKills;
            }
        }

        /// <summary>A feud's record while the feud runs: the match's row and the rows of those who have fought.</summary>
        private sealed class OpenFeud
        {
            public PvpMatchEntry Match;
            public readonly Dictionary<uint, PvpMatchPlayerEntry> Players = new Dictionary<uint, PvpMatchPlayerEntry>();
        }

        private readonly object _sync = new object();
        private readonly Dictionary<uint, OpenFeud> _feuds = new Dictionary<uint, OpenFeud>();

        /// <summary>Where the records go; null keeps none. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>
        /// The items these characters have wagered now, a row each for those who have one: the
        /// character, the item, its template, quality and stack. The live server's is
        /// InventoryManager.WageredItemsOf; null records no wagered items.
        /// </summary>
        public Func<IReadOnlyCollection<uint>, List<PvpMatchWagerEntry>> WageredItems { get; set; }

        /// <summary>The wall clock records are dated by, UTC. Replaceable for tests.</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>From now on records are kept in <paramref name="store"/>.</summary>
        public void Load(IStore store)
        {
            Store = store;

            lock (_sync)
                _feuds.Clear();
        }

        #region Any match that is over

        /// <summary>
        /// Writes the record of a match that has ended, with its players and what they have
        /// wagered now - all of it kept: a match recorded here takes nobody's item. The match's
        /// kind, sides, scores, outcome and start are the caller's; it is dated as ended now
        /// unless the caller dated it.
        /// </summary>
        public void Record(PvpMatchEntry match, IEnumerable<PvpMatchPlayerEntry> players)
        {
            var store = Store;

            if (store == null || match == null)
                return;

            match.EndedAt ??= UtcNow();

            if (match.StartedAt == default || match.StartedAt > match.EndedAt)
                match.StartedAt = match.EndedAt.Value;

            var rows = Rows(players);
            var saved = false;

            Try($"recording {(PvpMatchKind)match.Kind} {match.WargameId}", () =>
            {
                match.Id = store.SaveMatch(match);
                saved = true;
                store.SavePlayers(match.Id, rows);
            });

            if (saved)
                RecordWagers(store, match, rows.ToDictionary(p => p.CharacterId, p => p.Side));
        }

        /// <summary>A player's row for a record, with their names and clan as they are now.</summary>
        public static PvpMatchPlayerEntry PlayerOf(Manifestation player, byte side) => new PvpMatchPlayerEntry
        {
            CharacterId = player?.Id ?? 0,
            Side = side,
            Name = player?.Name ?? "",
            FamilyName = player?.FamilyName ?? "",
            ClanId = player?.ClanId ?? 0,
            PresentAtEnd = true
        };

        /// <summary>
        /// Writes what the characters of a match - each with the side they are on - have
        /// wagered now, as kept.
        /// </summary>
        private void RecordWagers(IStore store, PvpMatchEntry match, IReadOnlyDictionary<uint, byte> sides)
        {
            var wagered = WageredItems;

            if (wagered == null || match.Id == 0 || sides.Count == 0)
                return;

            Try($"recording the wagered items of {(PvpMatchKind)match.Kind} {match.WargameId}", () =>
            {
                var wagers = (wagered(sides.Keys.ToList()) ?? new List<PvpMatchWagerEntry>())
                    .Where(w => w != null && sides.ContainsKey(w.CharacterId))
                    .GroupBy(w => w.CharacterId)
                    .Select(g => g.First())
                    .ToList();

                foreach (var wager in wagers)
                {
                    wager.Side = sides[wager.CharacterId];
                    wager.Result = (byte)PvpWagerResult.Kept;
                    wager.RecipientClanId = 0;
                    wager.RecipientCharacterId = 0;
                }

                if (wagers.Count > 0)
                    store.SaveWagers(match.Id, wagers);
            });
        }

        /// <summary>
        /// What became of the wagered items a side of a match forfeited: the rows the match has
        /// for them are rewritten with it. <paramref name="matchId"/> is the record's
        /// (<see cref="FeudEnded"/> gives a feud's).
        /// </summary>
        public void WagersForfeited(uint matchId, byte side, IEnumerable<PvpMatchWagerEntry> wagers)
        {
            var store = Store;

            if (store == null || matchId == 0)
                return;

            var rows = (wagers ?? Enumerable.Empty<PvpMatchWagerEntry>())
                .Where(w => w != null && w.CharacterId != 0)
                .GroupBy(w => w.CharacterId)
                .Select(g => g.Last())
                .ToList();

            if (rows.Count == 0)
                return;

            foreach (var row in rows)
                row.Side = side;

            Try($"recording the forfeited items of match {matchId}", () => store.SaveWagers(matchId, rows));
        }

        /// <summary>One row a character, none for no character: a store keys them so.</summary>
        private static List<PvpMatchPlayerEntry> Rows(IEnumerable<PvpMatchPlayerEntry> players) =>
            (players ?? Enumerable.Empty<PvpMatchPlayerEntry>())
                .Where(p => p != null && p.CharacterId != 0)
                .GroupBy(p => p.CharacterId)
                .Select(g => g.Last())
                .ToList();

        #endregion

        #region Clan feuds

        /// <summary>A feud has begun: its record is opened.</summary>
        public void FeudStarted(FeudInfo feud)
        {
            if (Store == null)
                return;

            lock (_sync)
                Open(feud);
        }

        /// <summary>
        /// At startup, the feuds read back (ClanFeuds.Load): each finds the record it had open,
        /// by its id and its two clans, with the rows of those who had fought; one with no
        /// record - a feud from before records were kept - gets one opened now. A record left
        /// open by a feud that is no longer there is closed as cancelled.
        /// </summary>
        public void FeudsRestored(IEnumerable<FeudInfo> feuds)
        {
            var store = Store;

            if (store == null)
                return;

            List<PvpMatchEntry> open;

            try
            {
                open = store.OpenFeuds() ?? new List<PvpMatchEntry>();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"PvP records: the open feud records could not be read back: {e.Message}");
                open = new List<PvpMatchEntry>();
            }

            var restored = 0;
            var opened = 0;

            lock (_sync)
            {
                _feuds.Clear();

                foreach (var feud in feuds ?? Enumerable.Empty<FeudInfo>())
                {
                    var match = open.FirstOrDefault(m => m.WargameId == feud.FeudId
                                                         && m.Side1ClanId == feud.ChallengerClanId && m.Side2ClanId == feud.TargetClanId);

                    if (match == null)
                    {
                        if (Open(feud) != null)
                            opened++;

                        continue;
                    }

                    open.Remove(match);

                    var record = new OpenFeud { Match = match };

                    try
                    {
                        foreach (var player in store.Players(match.Id) ?? new List<PvpMatchPlayerEntry>())
                            record.Players[player.CharacterId] = player;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"PvP records: the players of feud {feud.FeudId}'s record could not be read back: {e.Message}");
                    }

                    _feuds[feud.FeudId] = record;
                    restored++;
                }
            }

            // Whatever is left belongs to no feud any more.
            foreach (var match in open)
            {
                match.EndedAt = UtcNow();
                match.Outcome = (byte)PvpMatchOutcome.Cancelled;
                match.WinnerSide = 0;
                match.Reason = "lost";

                Try($"closing the record of feud {match.WargameId}, which is gone", () => store.SaveMatch(match));
            }

            if (restored + opened + open.Count > 0)
                Logger.WriteLog(LogType.Initialize, $"PvP records: {restored} feud records found again, {opened} opened, {open.Count} closed for feuds that are gone.");
        }

        /// <summary>
        /// A kill in a feud that counted: the two clans' scores as they stand now, a kill for
        /// the killer and a death for the victim.
        /// </summary>
        public void FeudKill(FeudInfo feud, Manifestation killer, Manifestation victim)
        {
            var store = Store;

            if (store == null)
                return;

            PvpMatchEntry match;
            var changed = new List<PvpMatchPlayerEntry>();

            lock (_sync)
            {
                var record = RecordOf(feud);

                if (record == null)
                    return;

                Score(record.Match, feud);
                match = Copy(record.Match);

                var killerRow = RowOf(record, killer, SideOf(record.Match, killer?.ClanId ?? 0, 1));
                var victimRow = RowOf(record, victim, SideOf(record.Match, victim?.ClanId ?? 0, 2));

                if (killerRow != null)
                {
                    killerRow.Kills++;
                    changed.Add(Copy(killerRow));
                }

                if (victimRow != null)
                {
                    victimRow.Deaths++;
                    changed.Add(Copy(victimRow));
                }
            }

            Try($"recording a kill of feud {feud.FeudId}", () =>
            {
                store.SaveMatch(match);
                store.SavePlayers(match.Id, changed);
            });
        }

        /// <summary>
        /// A feud has ended: its record is closed with how it went, and the roster is written -
        /// everyone who fought, and everyone in either clan now (<paramref name="members"/>,
        /// with the side their clan is). One who fought and is no longer in their clan is kept,
        /// as not there at the end. The items at stake in it are written as they are wagered
        /// now, all kept: those of the members, and of <paramref name="stakes"/> - the characters
        /// who left a clan mid-feud with an item wagered, and the side they left. What a losing
        /// side then forfeits is told with <see cref="WagersForfeited"/> and the id returned
        /// here: the record's, 0 when there is none.
        /// </summary>
        public uint FeudEnded(FeudInfo feud, PvpMatchOutcome outcome, uint winnerClanId, string reason, IEnumerable<PvpMatchPlayerEntry> members,
            IEnumerable<KeyValuePair<uint, byte>> stakes = null)
        {
            var store = Store;

            if (store == null)
                return 0;

            PvpMatchEntry match;
            List<PvpMatchPlayerEntry> players;
            var atStake = new Dictionary<uint, byte>();

            lock (_sync)
            {
                var record = RecordOf(feud);

                if (record == null)
                    return 0;

                _feuds.Remove(feud.FeudId);

                Score(record.Match, feud);

                record.Match.EndedAt = UtcNow();
                record.Match.Outcome = (byte)outcome;
                record.Match.WinnerSide = outcome != PvpMatchOutcome.Won ? (byte)0
                    : winnerClanId == record.Match.Side1ClanId ? (byte)1
                    : winnerClanId == record.Match.Side2ClanId ? (byte)2 : (byte)0;
                record.Match.Reason = reason ?? "";

                foreach (var row in record.Players.Values)
                    row.PresentAtEnd = false;

                foreach (var member in Rows(members))
                {
                    atStake[member.CharacterId] = member.Side;

                    if (record.Players.TryGetValue(member.CharacterId, out var row))
                    {
                        row.PresentAtEnd = true;
                        row.Side = member.Side;
                        row.ClanId = member.ClanId;

                        if (!string.IsNullOrEmpty(member.Name))
                            row.Name = member.Name;

                        if (!string.IsNullOrEmpty(member.FamilyName))
                            row.FamilyName = member.FamilyName;
                    }
                    else
                    {
                        member.PresentAtEnd = true;
                        record.Players[member.CharacterId] = member;
                    }
                }

                match = record.Match;
                players = record.Players.Values.ToList();
            }

            Try($"closing the record of feud {feud.FeudId}", () =>
            {
                store.SaveMatch(match);
                store.SavePlayers(match.Id, players);
            });

            // One who left a clan with an item wagered has it at stake for that clan, whatever they have joined since.
            foreach (var stake in stakes ?? Enumerable.Empty<KeyValuePair<uint, byte>>())
                if (stake.Key != 0)
                    atStake[stake.Key] = stake.Value;

            RecordWagers(store, match, atStake);

            return match.Id;
        }

        /// <summary>The open record of a feud; one from before records were kept, or whose opening failed, is opened now.</summary>
        private OpenFeud RecordOf(FeudInfo feud)
        {
            if (_feuds.TryGetValue(feud.FeudId, out var record))
                return record;

            return Open(feud);
        }

        private OpenFeud Open(FeudInfo feud)
        {
            var store = Store;

            if (store == null)
                return null;

            var record = new OpenFeud
            {
                Match = new PvpMatchEntry
                {
                    Kind = (byte)PvpMatchKind.ClanFeud,
                    WargameId = feud.FeudId,
                    StartedAt = UtcNow(),
                    Outcome = (byte)PvpMatchOutcome.Running,
                    Side1Name = feud.ChallengerName ?? "",
                    Side1ClanId = feud.ChallengerClanId,
                    Side2Name = feud.TargetName ?? "",
                    Side2ClanId = feud.TargetClanId
                }
            };

            Score(record.Match, feud);

            try
            {
                record.Match.Id = store.SaveMatch(record.Match);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"PvP records: opening the record of feud {feud.FeudId} failed: {e.Message}");
                return null;
            }

            _feuds[feud.FeudId] = record;

            return record;
        }

        /// <summary>The feud's kills into its record, and the clans' names while they still have them.</summary>
        private static void Score(PvpMatchEntry match, FeudInfo feud)
        {
            match.Side1Kills = match.Side1Score = feud.ChallengerKills;
            match.Side2Kills = match.Side2Score = feud.TargetKills;

            if (!string.IsNullOrEmpty(feud.ChallengerName))
                match.Side1Name = feud.ChallengerName;

            if (!string.IsNullOrEmpty(feud.TargetName))
                match.Side2Name = feud.TargetName;
        }

        private static byte SideOf(PvpMatchEntry match, uint clanId, byte otherwise) =>
            clanId != 0 && clanId == match.Side1ClanId ? (byte)1
            : clanId != 0 && clanId == match.Side2ClanId ? (byte)2
            : otherwise;

        private static PvpMatchPlayerEntry RowOf(OpenFeud record, Manifestation player, byte side)
        {
            if (player == null || player.Id == 0)
                return null;

            if (!record.Players.TryGetValue(player.Id, out var row))
            {
                row = PlayerOf(player, side);
                record.Players[player.Id] = row;
            }
            else
            {
                // As they are now: a name changed, a clan left and joined again.
                row.Side = side;
                row.Name = player.Name ?? row.Name;
                row.FamilyName = player.FamilyName ?? row.FamilyName;
                row.ClanId = player.ClanId;
            }

            return row;
        }

        private static PvpMatchEntry Copy(PvpMatchEntry m) => new PvpMatchEntry
        {
            Id = m.Id,
            Kind = m.Kind,
            WargameId = m.WargameId,
            MapContextId = m.MapContextId,
            InstanceId = m.InstanceId,
            StartedAt = m.StartedAt,
            EndedAt = m.EndedAt,
            Outcome = m.Outcome,
            WinnerSide = m.WinnerSide,
            Reason = m.Reason,
            Side1Name = m.Side1Name,
            Side1ClanId = m.Side1ClanId,
            Side1Score = m.Side1Score,
            Side1Kills = m.Side1Kills,
            Side2Name = m.Side2Name,
            Side2ClanId = m.Side2ClanId,
            Side2Score = m.Side2Score,
            Side2Kills = m.Side2Kills
        };

        private static PvpMatchPlayerEntry Copy(PvpMatchPlayerEntry p) => new PvpMatchPlayerEntry
        {
            MatchId = p.MatchId,
            CharacterId = p.CharacterId,
            Side = p.Side,
            Name = p.Name,
            FamilyName = p.FamilyName,
            ClanId = p.ClanId,
            Kills = p.Kills,
            Deaths = p.Deaths,
            Damage = p.Damage,
            Healing = p.Healing,
            Captures = p.Captures,
            Prestige = p.Prestige,
            PresentAtEnd = p.PresentAtEnd
        };

        #endregion

        /// <summary>A record that cannot be written is lost and logged; the match goes on.</summary>
        private static void Try(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"PvP records: {what} failed: {e.GetBaseException().Message}");
            }
        }

        /// <summary>The live server's store: the character database.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public uint SaveMatch(PvpMatchEntry match)
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.PvpRecords.SaveMatch(match);
            }

            public void SavePlayers(uint matchId, IReadOnlyCollection<PvpMatchPlayerEntry> players)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.PvpRecords.SavePlayers(matchId, players);
            }

            public List<PvpMatchEntry> OpenFeuds()
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.PvpRecords.GetOpenMatches(PvpMatchKind.ClanFeud);
            }

            public List<PvpMatchPlayerEntry> Players(uint matchId)
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.PvpRecords.GetPlayers(matchId);
            }

            public void SaveWagers(uint matchId, IReadOnlyCollection<PvpMatchWagerEntry> wagers)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.PvpRecords.SaveWagers(matchId, wagers);
            }
        }
    }
}
