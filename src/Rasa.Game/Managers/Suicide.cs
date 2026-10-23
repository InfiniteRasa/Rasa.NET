using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// /suicide: a player kills their own character, as a death by a fall is (PlayerDeath.AtZero
    /// with no source). The client has no such command of its own, so it sends it on as a
    /// PrivilegedCommand (ChatCommandsManager).
    ///
    /// It is asked twice: the first /suicide asks "are you sure", a second within
    /// <see cref="ConfirmWindowMs"/> starts a countdown of <see cref="CountdownMs"/>, and the
    /// death comes when it runs out. Moving, a fight, damage taken, an ability used or PvP calls
    /// the countdown off (<see cref="Worker"/>, AbilityManager).
    ///
    /// It is refused to one already dead, to one in a fight (Manifestation.InCombat), to one in
    /// PvP - a PvP fight (Pvp.IsEngaged), a duel, a squad wargame, a battleground or an arena
    /// map - and for <see cref="CooldownMs"/> after the last death it gave. A refusal is checked
    /// on both asks, and calls off one that was waiting.
    ///
    /// It is no quick way home: the one way back is the nearest of the player's hospitals, and
    /// the Rez Trauma is the most a death gives (PlayerDeath, Manifestation.DiedBySuicide), and
    /// no Rez Trauma Kit clears it (<see cref="IsKitProof"/>).
    /// </summary>
    internal static class Suicide
    {
        /// <summary>How long a first /suicide waits for the second.</summary>
        internal const long ConfirmWindowMs = 10_000;

        /// <summary>How long the countdown runs, from the second /suicide to the death.</summary>
        internal const long CountdownMs = 180_000;

        /// <summary>When the countdown says how little is left.</summary>
        internal const long WarningMs = 10_000;

        /// <summary>How long after a death by /suicide before the next: an hour.</summary>
        internal const long CooldownMs = 3_600_000;

        /// <summary>How far a player may drift (metres) before the countdown takes it for moving.</summary>
        internal const float MoveToleranceMetres = 1f;

        /// <summary>
        /// The arena maps, by their client names (MapTemplates): adv_wargame_indoorarena,
        /// adv_wargame_provinggroundsv002, adv_afs_arena. The battlegrounds are
        /// Battlegrounds.IsBattleground's. adv_zepic_pve_arena (2278) is a PvE map and is not one.
        /// </summary>
        internal static readonly IReadOnlySet<uint> ArenaMaps = new HashSet<uint> { 2259, 2361, 20000009 };

        internal const string AskText = "Are you sure? You will be sent to the nearest hospital with the maximum Rez Trauma for 6 minutes. A Rez Trauma Kit cannot clear it. Type /suicide again within 10 seconds to confirm.";
        internal const string StartText = "You will die in 3 minutes. Moving, fighting, taking damage, using an ability or a weapon, or PvP cancels it.";
        internal const string WarningText = "You will die in 10 seconds.";
        internal const string DeadText = "You are already dead.";
        internal const string CombatText = "You cannot use /suicide while in combat.";
        internal const string PvpText = "You cannot use /suicide in PvP, a duel, a wargame or an arena.";
        internal const string WaypointText = "You carry a Personal Waypoint: use it instead of /suicide.";
        internal const string KitText = "A Rez Trauma Kit cannot clear the Rez Trauma from a /suicide.";

        /// <summary>
        /// The Personal Waypoints (PersonalWaypoints): One-Way, Two-Way and Two-Way Squad. One
        /// who carries one has a way to the waypoints of their own.
        /// </summary>
        internal static readonly IReadOnlySet<uint> PersonalWaypointTemplates = new HashSet<uint> { 118781, 118782, 118783 };

        internal const string MovedReason = "you moved";
        internal const string CombatReason = "you are in combat";
        internal const string DamageReason = "you took damage";
        internal const string AbilityReason = "you used an ability";
        internal const string WeaponReason = "you used a weapon";
        internal const string PvpReason = "you are in PvP";

        /// <summary>The clock it runs on; replaceable for tests.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A countdown running: whose, on which map, since when, and where they stood and how hale they were.</summary>
        private sealed class Countdown
        {
            internal Client Client;
            internal MapChannel Map;
            internal long StartedAt;
            internal Vector3 Position;
            internal int Health;
            internal bool Warned;
        }

        // Character id -> when they asked, the countdown they have running, when /suicide last
        // killed them, and until when their Rez Trauma is one a kit cannot clear.
        private static readonly Dictionary<uint, long> Asked = new Dictionary<uint, long>();
        private static readonly Dictionary<uint, Countdown> Countdowns = new Dictionary<uint, Countdown>();
        private static readonly Dictionary<uint, long> Died = new Dictionary<uint, long>();
        private static readonly Dictionary<uint, long> KitProof = new Dictionary<uint, long>();

        /// <summary>/suicide, from ChatCommandsManager.PrivilegedCommand. Its arguments are no matter.</summary>
        internal static void Run(Client client, string args)
        {
            var player = client?.Player;

            // Between maps: nothing to count down on.
            if (player == null || player.Id == 0 || player.MapChannel == null)
                return;

            var now = Now();
            var refusal = Refusal(client, now);

            if (refusal != null)
            {
                Asked.Remove(player.Id);

                if (Countdowns.Remove(player.Id))
                    refusal = $"/suicide cancelled. {refusal}";

                Say(client, refusal);
                return;
            }

            if (Countdowns.TryGetValue(player.Id, out var running))
            {
                Say(client, $"You will die in {Left(CountdownMs - (now - running.StartedAt))}.");
                return;
            }

            if (!Asked.Remove(player.Id, out var askedAt) || now - askedAt > ConfirmWindowMs)
            {
                Asked[player.Id] = now;
                Say(client, AskText);
                return;
            }

            Countdowns[player.Id] = new Countdown
            {
                Client = client,
                Map = player.MapChannel,
                StartedAt = now,
                Position = player.Position,
                Health = HealthOf(player)
            };
            Say(client, StartText);
        }

        /// <summary>
        /// Calls off the player's countdown, if they have one, and says why: an ability asked for
        /// (AbilityManager.RequestPerformAbility), or what <see cref="Worker"/> saw.
        /// </summary>
        internal static void Cancel(Client client, string reason)
        {
            var player = client?.Player;

            if (player == null || !Countdowns.Remove(player.Id))
                return;

            Say(client, $"/suicide cancelled: {reason}.");
        }

        /// <summary>
        /// The player has taken damage - armour or health, from anyone or nothing (lava, a
        /// tesla coil): their countdown, if any, is off (AbilityManager.OnPlayerDamaged).
        /// </summary>
        internal static void Damaged(Manifestation player)
        {
            if (player != null && Countdowns.TryGetValue(player.Id, out var countdown))
                Cancel(countdown.Client, DamageReason);
        }

        /// <summary>The player leaves the map or the game: whatever /suicide they had asked for is gone (ManifestationManager.RemovePlayerCharacter).</summary>
        internal static void Forget(Client client)
        {
            var player = client?.Player;

            if (player == null)
                return;

            Asked.Remove(player.Id);
            Countdowns.Remove(player.Id);
        }

        /// <summary>
        /// The countdowns of this map, each tick: called off by what the player did or what
        /// befell them, dropped for one who has left the map or died otherwise, and the death
        /// for one that has run out.
        /// </summary>
        internal static void Worker(MapChannel map)
        {
            if (map == null || Countdowns.Count == 0)
                return;

            var now = Now();

            foreach (var (id, countdown) in Countdowns.Where(entry => ReferenceEquals(entry.Value.Map, map)).ToArray())
            {
                // Called off since the pass began (a death earlier in it damaged this player).
                if (!Countdowns.TryGetValue(id, out var current) || !ReferenceEquals(current, countdown))
                    continue;

                var client = countdown.Client;
                var player = client?.Player;

                // Gone from the map - logged out, moved on - or dead by something else: nothing to do.
                if (player == null || player.Id != id || !ReferenceEquals(player.MapChannel, map) || player.Disconected ||
                    player.RemoveFromMap || player.State == CharacterState.Dead || !map.ClientList.Contains(client))
                {
                    Countdowns.Remove(id);
                    continue;
                }

                // Healed back above where they stood: a hit later is measured from there.
                var health = HealthOf(player);

                if (health > countdown.Health)
                    countdown.Health = health;

                var reason = player.InCombat ? CombatReason
                    : health < countdown.Health ? DamageReason
                    : InPvp(client) ? PvpReason
                    : Vector3.Distance(player.Position, countdown.Position) > MoveToleranceMetres ? MovedReason
                    : null;

                if (reason != null)
                {
                    Cancel(client, reason);
                    continue;
                }

                var elapsed = now - countdown.StartedAt;

                if (elapsed < CountdownMs)
                {
                    if (!countdown.Warned && CountdownMs - elapsed <= WarningMs)
                    {
                        countdown.Warned = true;
                        Say(client, WarningText);
                    }

                    continue;
                }

                Countdowns.Remove(id);
                Die(client, now);
            }
        }

        private static void Die(Client client, long now)
        {
            var player = client.Player;

            if (player.Attributes.TryGetValue(Attributes.Health, out var health))
                health.Current = 0;

            player.DiedBySuicide = true;

            // A GM who has not said .allowdeath stands back up (PlayerDeath.IsDeathless): no cooldown for that.
            if (PlayerDeath.AtZero(player.MapChannel, player, null))
                Died[player.Id] = now;
            else
                player.DiedBySuicide = false;
        }

        /// <summary>Why /suicide is refused now, or null.</summary>
        private static string Refusal(Client client, long now)
        {
            var player = client.Player;

            if (player.State == CharacterState.Dead)
                return DeadText;

            if (player.InCombat)
                return CombatText;

            if (InPvp(client))
                return PvpText;

            // A Personal Waypoint in the backpack, where one may be put down and goes somewhere:
            // that is their way. With no waypoint gained on this map it goes nowhere.
            if (CarriesPersonalWaypoint(player) && PersonalWaypoints.Refusal(player) == null && HasWaypointNetwork(client))
                return WaypointText;

            if (Died.TryGetValue(player.Id, out var diedAt) && now - diedAt < CooldownMs)
                return $"You can use /suicide again in {Left(CooldownMs - (now - diedAt))}.";

            return null;
        }

        /// <summary>A PvP fight, a duel, a squad wargame, or a battleground or arena map.</summary>
        private static bool InPvp(Client client)
        {
            var player = client.Player;
            var mapContextId = player.MapChannel?.MapInfo?.MapContextId ?? player.MapContextId;

            return Pvp.IsEngaged(player) || Duels.Instance.IsDueling(client) || SquadWargames.Instance.IsWargaming(client)
                || Battlegrounds.Instance.IsBattleground(mapContextId) || ArenaMaps.Contains(mapContextId);
        }

        /// <summary>
        /// Whether the player has a waypoint of this map to go to: what a Personal Waypoint's
        /// window would offer (PersonalWaypoints.Network - gained, on this map, not contested).
        /// </summary>
        private static bool HasWaypointNetwork(Client client)
        {
            var mapContextId = client.Player.MapChannel?.MapInfo?.MapContextId ?? client.Player.MapContextId;

            return PersonalWaypoints.Network(client).TryGetValue(mapContextId, out var list) && list?.Waypoints?.Count > 0;
        }

        /// <summary>
        /// Whether a Personal Waypoint is among what the player carries and may use: in their
        /// backpack, their own, not used up - as AbilityManager.Grants lets an item act.
        /// </summary>
        internal static bool CarriesPersonalWaypoint(Manifestation player)
        {
            var carried = player?.Inventory?.PersonalInventory;

            if (carried == null)
                return false;

            foreach (var entityId in carried.ToArray())
            {
                var item = entityId == 0 ? null : EntityManager.Instance.GetItem(entityId);

                if (item?.ItemTemplate != null && PersonalWaypointTemplates.Contains(item.ItemTemplate.ItemTemplateId)
                    && item.OwnerId == player.Id && item.StackSize > 0)
                    return true;
            }

            return false;
        }

        private static int HealthOf(Manifestation player) =>
            player.Attributes.TryGetValue(Attributes.Health, out var health) ? health.Current : 0;

        /// <summary>The time left, rounded up: in minutes, or in seconds for the last minute.</summary>
        internal static string Left(long ms)
        {
            if (ms > 60_000)
            {
                var minutes = (ms + 59_999) / 60_000;
                return $"{minutes} minutes";
            }

            var seconds = Math.Max(1, (ms + 999) / 1000);
            return $"{seconds} second{(seconds == 1 ? "" : "s")}";
        }

        /// <summary>
        /// The Rez Trauma of a /suicide is on the player (PlayerDeath.Revive): a Rez Trauma Kit
        /// cannot clear it until it would run out (AbilityManager.RezTraumaKitRefusal). It is kept
        /// in memory, as the cooldown is: a relog keeps it, a restart of the server does not.
        /// </summary>
        internal static void TraumaAttached(Manifestation player, long durationMs) =>
            KitProof[player.Id] = Now() + durationMs;

        /// <summary>A Rez Trauma that is no /suicide's (PlayerDeath.Revive): kits clear it again.</summary>
        internal static void TraumaCleared(Manifestation player) => KitProof.Remove(player.Id);

        /// <summary>Whether the player's Rez Trauma is still the one /suicide gave, which no kit clears.</summary>
        internal static bool IsKitProof(Manifestation player)
        {
            if (player == null || !KitProof.TryGetValue(player.Id, out var until))
                return false;

            if (Now() < until && PlayerDeath.CurrentRezTrauma(player) != null)
                return true;

            KitProof.Remove(player.Id);
            return false;
        }

        private static void Say(Client client, string text) => CommunicatorManager.Instance.SystemMessage(client, text);

        /// <summary>Forgets every ask, countdown and cooldown; for tests.</summary>
        internal static void Reset()
        {
            Asked.Clear();
            Countdowns.Clear();
            Died.Clear();
            KitProof.Clear();
        }
    }
}
