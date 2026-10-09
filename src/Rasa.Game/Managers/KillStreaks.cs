using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Kill streaks: kills in quick succession multiply the experience of the kills that follow.
    ///
    /// The client's side is the display. SetKillStreak(level) lights the flaming skull by the
    /// experience bar with "x2 XP" to "x6 XP" for levels 1 to 5, plays a sound as the level goes
    /// up or down and shows the tutorial tip the first time (clientmethod.Recv_SetKillStreak,
    /// experiencebarwindow._DisplayKillStreak). ExperienceChanged's streakMod is what adds
    /// " (+100% Kill Streak Bonus)" to the chat line of a kill (manifestation.Recv_ExperienceChanged),
    /// and the experience bar's tooltip shows the level times 100 as the bonus.
    ///
    /// What the client says of the rules:
    ///  - its help: "A rapid string of [kills] will activate a Kill Streak Multiplier, increasing
    ///    the experience points you receive with subsequent kills. The maximum multiplier you
    ///    can receive is based on your character's level": x2 up to level 9, x3 for 10 to 19,
    ///    x4 for 20 to 29, x5 for 30 to 39 and x6 from 40;
    ///  - shared/gameconstants.py: STREAK_BASE_PER_PARTY_MEMBER 3, STREAK_LEVEL_BASIS 10,
    ///    STREAK_MAX_VALUE 5, MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS 1;
    ///  - a module's tooltip, "Kill Streak: %(amount)s%% Credit per Kill": a kill is credit
    ///    towards the streak;
    ///  - "You received %(amount)s prestige points for reaching max kill streak."
    ///
    /// And footage of the game: the skull appeared on the third kill of a player on their own,
    /// and went eight seconds after their last kill.
    ///
    /// So:
    ///  - a kill is credit to everyone it is shared with (KillShares): a whole kill to a player
    ///    on their own, and one part each to a squad that shares it. Three kills' worth of
    ///    credit is a level: three kills alone, six by a squad of two;
    ///  - the level is at most the character's level / 10 + 1, and 5;
    ///  - a kill's experience is multiplied by 1 + the level the player had before the kill, so
    ///    the kill that raises the level is paid at the level before it;
    ///  - reaching the highest level the character can is worth 1 prestige, each time it is
    ///    reached; the Tabula Rasa wiki's prestige table has the same 1, whatever is wagered;
    ///  - eight seconds without a kill ends the streak: the credit is gone and the level is 0.
    ///
    /// Ours, where nothing says:
    ///  - the part of a kill is by those who share it, as its experience is, not by the size
    ///    of the squad: a member on another map neither gains nor slows the rest;
    ///  - the streak ends whole when its time is up, and not a level at a time;
    ///  - leaving the map or the game ends it; dying does not;
    ///  - every kill that pays experience counts, whatever the creature's level.
    ///
    /// Not kept: a streak is of the session, and nothing of it is written anywhere.
    /// </summary>
    public static class KillStreaks
    {
        /// <summary>STREAK_BASE_PER_PARTY_MEMBER: the kills a level takes, for each player the kills are shared with.</summary>
        public const int KillsPerLevelPerMember = 3;

        /// <summary>STREAK_LEVEL_BASIS: the character levels to each further level of streak a character can reach.</summary>
        public const int LevelBasis = 10;

        /// <summary>STREAK_MAX_VALUE: the highest level, "x6 XP".</summary>
        public const int MaxLevel = 5;

        /// <summary>MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS.</summary>
        public const int MaxStreakPrestige = 1;

        /// <summary>How long after a kill the streak lasts with no other.</summary>
        public const long LapseMs = 8000;

        /// <summary>A whole kill in credit: divisible by every size of squad, 1 to 6.</summary>
        public const int CreditPerKill = 60;

        /// <summary>The credit to a level.</summary>
        public const int CreditPerLevel = KillsPerLevelPerMember * CreditPerKill;

        /// <summary>The clock streaks run on; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        private sealed class Streak
        {
            public Manifestation Player;
            public int Credit;
            public int Level;
            public long LastKillAt;
        }

        private static readonly Dictionary<Client, Streak> Streaks = new Dictionary<Client, Streak>();
        private static readonly object Sync = new object();

        /// <summary>The highest level of streak a character of this level can reach: 1 to 5.</summary>
        public static int MaxLevelFor(int characterLevel)
        {
            return Math.Clamp(characterLevel / LevelBasis + 1, 1, MaxLevel);
        }

        /// <summary>The player's level of streak now: 0 with none, and when its time is up.</summary>
        public static int LevelOf(Client client)
        {
            lock (Sync)
                return Current(client)?.Level ?? 0;
        }

        /// <summary>The credit the player has towards levels now, CreditPerLevel to each.</summary>
        public static int CreditOf(Client client)
        {
            lock (Sync)
                return Current(client)?.Credit ?? 0;
        }

        /// <summary>What a kill's experience is multiplied by for this player now: 1 with no streak, to 6.</summary>
        public static int MultiplierOf(Client client)
        {
            return 1 + LevelOf(client);
        }

        /// <summary>
        /// A kill, shared between these players, counted for each of them: after its experience
        /// is paid, so that it is paid at the level before it.
        /// </summary>
        public static void KillCounted(IReadOnlyList<Client> sharers)
        {
            if (sharers == null)
                return;

            var counted = sharers.Where(InGame).Distinct().ToList();

            if (counted.Count == 0)
                return;

            var credit = Math.Max(1, CreditPerKill / counted.Count);
            var now = Now();

            lock (Sync)
            {
                foreach (var client in counted)
                {
                    var streak = Current(client);

                    if (streak == null)
                        Streaks[client] = streak = new Streak { Player = client.Player };

                    var cap = MaxLevelFor(client.Player.Level);
                    var before = streak.Level;

                    // No further than the highest level: nothing is banked past it. A level a
                    // game master set over it stays until it lapses.
                    streak.Credit = Math.Min(streak.Credit + credit, Math.Max(cap, before) * CreditPerLevel);
                    streak.Level = Math.Max(before, Math.Min(cap, streak.Credit / CreditPerLevel));
                    streak.LastKillAt = now;

                    if (streak.Level == before)
                        continue;

                    Tell(client, streak.Level);

                    if (before < cap && streak.Level == cap)
                        PayForTheHighest(client);
                }
            }
        }

        /// <summary>
        /// Gives the player a level of streak, 0 to 5, as if their kills had just earned it: a
        /// game master's (.setkillstreak). It lapses as any other, and pays no prestige.
        /// </summary>
        public static void Set(Client client, int level)
        {
            if (!InGame(client))
                return;

            level = Math.Clamp(level, 0, MaxLevel);

            if (level == 0)
            {
                End(client);
                return;
            }

            lock (Sync)
            {
                var streak = Current(client);

                if (streak == null)
                    Streaks[client] = streak = new Streak { Player = client.Player };

                var before = streak.Level;

                streak.Level = level;
                streak.Credit = level * CreditPerLevel;
                streak.LastKillAt = Now();

                if (before != level)
                    Tell(client, level);
            }
        }

        /// <summary>Ends the player's streak, and tells their client if it showed one: they are leaving the map or the game.</summary>
        public static void End(Client client)
        {
            if (client == null)
                return;

            lock (Sync)
            {
                if (Streaks.Remove(client, out var streak) && streak.Level > 0 && ReferenceEquals(streak.Player, client.Player))
                    Tell(client, 0);
            }
        }

        /// <summary>Ends the streaks whose time is up. Every tick of the main loop.</summary>
        public static void Worker()
        {
            lock (Sync)
            {
                if (Streaks.Count == 0)
                    return;

                foreach (var client in Streaks.Keys.ToList())
                    Current(client);
            }
        }

        /// <summary>Forgets every streak and tells nobody; a test's.</summary>
        internal static void Reset()
        {
            lock (Sync)
                Streaks.Clear();
        }

        /// <summary>The player's streak if it is still running; one whose time is up, or whose player has gone, is ended here.</summary>
        private static Streak Current(Client client)
        {
            if (client == null || !Streaks.TryGetValue(client, out var streak))
                return null;

            // Another character on the same connection, or the connection gone: nobody to tell.
            if (!ReferenceEquals(streak.Player, client.Player) || client.State == ClientState.Disconnected)
            {
                Streaks.Remove(client);
                return null;
            }

            if (Now() - streak.LastKillAt < LapseMs)
                return streak;

            Streaks.Remove(client);

            if (streak.Level > 0)
                Tell(client, 0);

            return null;
        }

        private static bool InGame(Client client)
        {
            return client?.Player != null && client.State == ClientState.Ingame;
        }

        private static void Tell(Client client, int level)
        {
            client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(level));
        }

        /// <summary>One prestige for reaching the highest level, kept and shown, and the client's line for it.</summary>
        private static void PayForTheHighest(Client client)
        {
            try
            {
                if (!PvpPrestige.Change(client, MaxStreakPrestige))
                    return;

                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(
                    PlayerMessage.PmPrestigePointsReceivedKillstreakmax,
                    new Dictionary<string, string> { { "amount", MaxStreakPrestige.ToString() } },
                    MsgFilterId.PrestigeGainLose));
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Kill streak: {MaxStreakPrestige} prestige for {client.Player?.FamilyName} failed: {e.Message}");
            }
        }
    }
}
