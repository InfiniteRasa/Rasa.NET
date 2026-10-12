using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;

    /// <summary>
    /// Prestige from fighting players. A clan feud gives it both ways: of the client's wargame
    /// flags (shared/gameconstants.py) WARGAME_FLAGS_CLAN alone has both WARGAME_GENERATE_PRESTIGE
    /// and WARGAME_STEAL_PRESTIGE. A team wargame - a battleground's match - generates it and
    /// steals none (<see cref="TeamKill"/>); a duel and a squad wargame have neither. The client's help says the
    /// same: prestige comes from "killing a player your clan is at war with".
    ///
    /// The numbers are gameconstants.py's, which the client itself never reads:
    ///  - BASE_GENERATED_PRESTIGE_POINT = 30, GENERATED_PRESTIGE_LEVEL_MODIFIER = 2,
    ///    MIN / MAX_GENERATED_PRESTIGE_POINT = 10 / 50;
    ///  - BASE_STOLEN_PRESTIGE_PERCENTAGE = 1, STOLEN_PRESTIGE_PERCENTAGE_LEVEL_MODIFIER = 0.1,
    ///    MIN / MAX_STOLEN_PRESTIGE_PERCENTAGE = 0 / 2;
    ///  - KILL_CREDIT_PRESTIGE_LEVEL_MAX_ABOVE = 4, KILL_CREDIT_PRESTIGE_LEVEL_MAX_BELOW = 8;
    ///  - GENERATED_PRESTIGE_MIN_INTERVAL = 600 s, MAX_PVP_KILL_RECORD_KEEPING_TIME = 900 s.
    ///
    /// What the kill says is the client's too: the killer reads PM_PRESTIGE_POINTS_RECEIVED_PVPKILL
    /// ("You received %(amount)s prestige points for killing player %(playerName)s
    /// (%(amountGenerated)s points generated and %(amountStolen)s stolen from them)"), the victim
    /// PM_PRESTIGE_POINTS_REMOVED_PVPDEATH, and UpdateCredits floats "+N Prestige" over the killer.
    ///
    /// Ours, since only the constants' names say how they go together:
    ///  - the level difference is the victim's level less the killer's: a kill generates the base
    ///    plus the modifier a level of it, between the minimum and the maximum, and steals the base
    ///    percentage plus its modifier a level of it, between its minimum and maximum, of the
    ///    prestige the victim holds, rounded down;
    ///  - a kill has credit while the killer is no more than MAX_ABOVE levels above the victim and
    ///    no more than MAX_BELOW below them. Outside that it gives no prestige and does not count
    ///    for the feud (ClanFeuds.Kill, PM_WARGAME_FEUD_NO_KILL_CREDIT_LEVELS);
    ///  - the same killer generates prestige from the same victim once in the minimum interval;
    ///    what is stolen is taken every time, being the victim's own. Kills are remembered for the
    ///    record keeping time;
    ///  - the prestige is the killer's alone, whoever else was in the fight, and a creature's
    ///    kill is its master's;
    ///  - winning a feud is worth <see cref="FeudVictoryPrestige"/> to each member of the winning
    ///    clan in the world when it ends: nothing, until a value is chosen;
    ///  - the killer's wagered item (WARGAME_ITEM_WAGERING, InventoryManager.Wager) adds its
    ///    quality's percent - 20, 35 or 50 - to what the kill generates, rounded down, and so may
    ///    take it past the maximum. Nothing is added to what is stolen: that is the victim's own
    ///    prestige changing hands, and a bonus on it would be prestige made from nothing at every
    ///    kill, with no interval to hold it back. The bonus is counted as generated in the
    ///    killer's message.
    /// </summary>
    public static class PvpPrestige
    {
        public const int BaseGenerated = 30;
        public const int GeneratedLevelModifier = 2;
        public const int MinGenerated = 10;
        public const int MaxGenerated = 50;

        public const double BaseStolenPercentage = 1;
        public const double StolenPercentageLevelModifier = 0.1;
        public const double MinStolenPercentage = 0;
        public const double MaxStolenPercentage = 2;

        /// <summary>How many levels above the victim the killer may be and still have credit for the kill.</summary>
        public const int KillCreditLevelMaxAbove = 4;

        /// <summary>How many levels below the victim the killer may be and still have credit for the kill.</summary>
        public const int KillCreditLevelMaxBelow = 8;

        public static readonly TimeSpan GeneratedMinInterval = TimeSpan.FromSeconds(60 * 10);
        public static readonly TimeSpan KillRecordKeepingTime = TimeSpan.FromSeconds(90 * 10);

        /// <summary>What winning a clan feud is worth to each member of the winning clan in the world. Nothing yet.</summary>
        public static int FeudVictoryPrestige { get; set; } = 0;

        /// <summary>The clock kills are remembered on; replaceable for tests.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>
        /// Changes a player's prestige by an amount, kept and shown (UpdateCredits); false if it
        /// could not be. Replaceable for tests.
        /// </summary>
        public static Func<Client, int, bool> Change { get; set; } = Persist;

        /// <summary>The percent the killer's wagered item adds to what a kill generates. Replaceable for tests.</summary>
        public static Func<Client, int> WagerBonusPercent { get; set; } = client => InventoryManager.WagerBonusOf(client?.Player);

        private static readonly object Sync = new object();

        /// <summary>When each killer last generated prestige from each victim, by character id.</summary>
        private static readonly Dictionary<(uint Killer, uint Victim), long> Generations = new Dictionary<(uint, uint), long>();

        #region The numbers

        /// <summary>Whether a kill between these levels has credit: for prestige, and for the feud.</summary>
        public static bool HasKillCredit(int killerLevel, int victimLevel)
        {
            return killerLevel - victimLevel <= KillCreditLevelMaxAbove && victimLevel - killerLevel <= KillCreditLevelMaxBelow;
        }

        /// <summary>The prestige a kill generates.</summary>
        public static int Generated(int killerLevel, int victimLevel)
        {
            return Math.Clamp(BaseGenerated + GeneratedLevelModifier * (victimLevel - killerLevel), MinGenerated, MaxGenerated);
        }

        /// <summary>What a wagered item worth this percent adds to a generated amount.</summary>
        public static int WagerBonus(int generated, int percent)
        {
            return generated <= 0 || percent <= 0 ? 0 : (int)Math.Floor(generated * percent / 100.0);
        }

        /// <summary>The share of the victim's prestige a kill takes, in percent.</summary>
        public static double StolenPercentage(int killerLevel, int victimLevel)
        {
            return Math.Clamp(BaseStolenPercentage + StolenPercentageLevelModifier * (victimLevel - killerLevel), MinStolenPercentage, MaxStolenPercentage);
        }

        /// <summary>The prestige a kill takes from a victim holding this much.</summary>
        public static int Stolen(int killerLevel, int victimLevel, int victimPrestige)
        {
            if (victimPrestige <= 0)
                return 0;

            return (int)Math.Floor(victimPrestige * StolenPercentage(killerLevel, victimLevel) / 100.0);
        }

        #endregion

        #region Awards

        /// <summary>
        /// A kill in a clan feud that has credit (ClanFeuds.Kill): the killer's generated prestige,
        /// if they have had none from this victim within the interval, with their wagered item's
        /// bonus on it, and the victim's stolen. Returns what the killer gained, the bonus in the
        /// generated.
        /// </summary>
        public static (int Generated, int Stolen) FeudKill(Client killer, Client victim)
        {
            var killerPlayer = killer?.Player;
            var victimPlayer = victim?.Player;

            if (killerPlayer == null || victimPlayer == null || ReferenceEquals(killerPlayer, victimPlayer)
                || !HasKillCredit(killerPlayer.Level, victimPlayer.Level))
                return (0, 0);

            var generated = MayGenerate(killerPlayer.Id, victimPlayer.Id) ? Generated(killerPlayer.Level, victimPlayer.Level) : 0;
            var bonus = generated > 0 ? WagerBonus(generated, WagerBonusPercent(killer)) : 0;

            generated += bonus;

            var stolen = Stolen(killerPlayer.Level, victimPlayer.Level, PrestigeOf(victim));

            // Taken before it is given: prestige that could not be taken was not stolen.
            if (stolen > 0 && !TryChange(victim, -stolen))
                stolen = 0;

            if (generated + stolen <= 0)
                return (0, 0);

            if (!TryChange(killer, generated + stolen))
            {
                if (stolen > 0)
                    TryChange(victim, stolen);

                return (0, 0);
            }

            if (generated > 0)
                lock (Sync)
                    Generations[(killerPlayer.Id, victimPlayer.Id)] = Now();

            EconomyAudit.Instance.Record(Structures.Char.EconomyLogKind.PvpPrestige, 0,
                EconomyAudit.MoneyLine(killer, CurencyType.Prestige, generated + stolen, PrestigeOf(killer), victimPlayer.Id),
                EconomyAudit.MoneyLine(victim, CurencyType.Prestige, -stolen, PrestigeOf(victim), killerPlayer.Id));

            Logger.WriteLog(LogType.Debug, $"PvP prestige: {killerPlayer.FamilyName} +{generated + stolen} for {victimPlayer.FamilyName} ({generated} generated, {bonus} of it for their wagered item, {stolen} stolen).");

            Say(killer, PlayerMessage.PmPrestigePointsReceivedPvpkill,
                ("amount", (generated + stolen).ToString()),
                ("playerName", victimPlayer.FamilyName ?? ""),
                ("amountGenerated", generated.ToString()),
                ("amountStolen", stolen.ToString()));

            if (stolen > 0)
                Say(victim, PlayerMessage.PmPrestigePointsRemovedPvpdeath,
                    ("amount", stolen.ToString()),
                    ("playerName", killerPlayer.FamilyName ?? ""));

            return (generated, stolen);
        }

        /// <summary>
        /// A kill between the teams of a battleground's match (Battlegrounds.Kill): a team wargame
        /// generates prestige and steals none (WARGAME_FLAGS_TEAM). The killer's generated
        /// prestige, by the feud's rules - the levels' credit, once from a victim in the
        /// interval - and without a wagered item's bonus, a team wargame having no wagering.
        /// Returns what the killer gained.
        /// </summary>
        public static int TeamKill(Client killer, Client victim)
        {
            var killerPlayer = killer?.Player;
            var victimPlayer = victim?.Player;

            if (killerPlayer == null || victimPlayer == null || ReferenceEquals(killerPlayer, victimPlayer)
                || !HasKillCredit(killerPlayer.Level, victimPlayer.Level) || !MayGenerate(killerPlayer.Id, victimPlayer.Id))
                return 0;

            var generated = Generated(killerPlayer.Level, victimPlayer.Level);

            if (generated <= 0 || !TryChange(killer, generated))
                return 0;

            lock (Sync)
                Generations[(killerPlayer.Id, victimPlayer.Id)] = Now();

            EconomyAudit.Instance.Record(Structures.Char.EconomyLogKind.PvpPrestige, 0,
                EconomyAudit.MoneyLine(killer, CurencyType.Prestige, generated, PrestigeOf(killer), victimPlayer.Id));

            Say(killer, PlayerMessage.PmPrestigePointsReceivedPvpkill,
                ("amount", generated.ToString()),
                ("playerName", victimPlayer.FamilyName ?? ""),
                ("amountGenerated", generated.ToString()),
                ("amountStolen", "0"));

            return generated;
        }

        /// <summary>
        /// A clan feud won, to a member of the winning clan in the world (ClanFeuds.End):
        /// <see cref="FeudVictoryPrestige"/>. Returns what they gained.
        /// </summary>
        public static int FeudWon(Client member)
        {
            var amount = FeudVictoryPrestige;

            if (amount <= 0 || member?.Player == null || !TryChange(member, amount))
                return 0;

            EconomyAudit.Instance.Record(Structures.Char.EconomyLogKind.PvpPrestige, member.Player.ClanId,
                EconomyAudit.MoneyLine(member, CurencyType.Prestige, amount));

            Say(member, PlayerMessage.PmPrestigePointsReceived, ("amount", amount.ToString()));

            return amount;
        }

        /// <summary>Forgets every kill remembered. For tests.</summary>
        public static void Reset()
        {
            lock (Sync)
                Generations.Clear();
        }

        #endregion

        #region Helpers

        /// <summary>Whether this killer may generate prestige from this victim now; old records go as it looks.</summary>
        private static bool MayGenerate(uint killerId, uint victimId)
        {
            var now = Now();

            lock (Sync)
            {
                foreach (var stale in Generations.Where(g => now - g.Value >= (long)KillRecordKeepingTime.TotalMilliseconds).Select(g => g.Key).ToList())
                    Generations.Remove(stale);

                return !Generations.TryGetValue((killerId, victimId), out var last) || now - last >= (long)GeneratedMinInterval.TotalMilliseconds;
            }
        }

        private static int PrestigeOf(Client client) =>
            client.Player.Credits.TryGetValue(CurencyType.Prestige, out var prestige) ? prestige : 0;

        private static bool TryChange(Client client, int amount)
        {
            try
            {
                return Change(client, amount);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"PvP prestige: {amount} for {client?.Player?.FamilyName} failed: {e.Message}");
                return false;
            }
        }

        private static bool Persist(Client client, int amount) =>
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Prestige, amount);

        private static void Say(Client client, PlayerMessage message, params (string Key, string Value)[] args)
        {
            client?.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(message, args.ToDictionary(a => a.Key, a => a.Value), MsgFilterId.PrestigeGainLose));
        }

        #endregion
    }
}
