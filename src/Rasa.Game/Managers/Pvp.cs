using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Player against player. There is no PvP flag in the client: two players fight when they are
    /// on opposite sides of a wargame they are both in, and nowhere else. The server says so in two
    /// ways, each per onlooker:
    ///
    ///  - WargameData on the actor, {wargameId: side} (ClanFeuds.ShowWargameData): the client's
    ///    Actor.GetWargameParticipantStatus calls two players in the same wargame allies when their
    ///    sides match and enemies when they do not. Its friendly actions (targetedaction.py
    ///    SetTarget, healdisc.py) refuse an enemy with it.
    ///  - TargetCategory, per recipient: HOSTILE for an enemy, FRIENDLY otherwise. That is what the
    ///    client's hostile targeting, right-click Attack, overhead colour and radar pip (PLAYER_PVP)
    ///    go by (targeting.py _IsTargetType, radarwindow.py _GetManifestationType).
    ///
    /// The only wargame there is yet is a Clan Feud (ClanFeuds); <see cref="AreEnemies"/> reads
    /// the sides from its WargameData, as the client does, so a duel or squad wargame that adds its
    /// own entries there is covered without changing anything here.
    ///
    /// shared/gameconstants.py has the numbers:
    ///  - PVP_DAMAGE_MODIFIER = 0.5: what a player's hit on a player does (<see cref="ScaleDamage"/>);
    ///  - PVP_CRITICAL_DAMAGE_MODIFIER = 1.25: a crit's multiplier between players (CriticalHits);
    ///  - PVP_HEALING_MODIFIER = 0.5: a player's heal on a player in a fight with another
    ///    (<see cref="ScaleHealing"/>);
    ///  - PVP_EFFECT_DURATION_MODIFIER = -0.5: what a player's stun or knockback on a player lasts
    ///    (<see cref="ScaleDuration"/>);
    ///  - PVP_SAFETY_DURATION = 60: the PvP Safety effect (gameeffectdata PVP_SAFETY 386,
    ///    client/gameeffects/pvpsafety.py), which says "PvP damage temporarily disabled" as it goes
    ///    on and "PvP damage enabled" as it comes off, and marks its holder safe overhead.
    ///
    /// Ours, since nothing in the client says:
    ///  - A player brought to zero by an enemy is defeated, not killed - there is no player death
    ///    yet (BuryMe, PlayerDead and the revive are not wired). The kill counts for the feud, the
    ///    loser is back on full health and armour, and has PvP Safety for its 60 seconds
    ///    (<see cref="Defeat"/>).
    ///  - PvP Safety stops PvP damage both ways: its holder takes none from an enemy player (the hit
    ///    shows Immune) and deals none to one.
    ///  - The healing modifier applies to a heal from a player on a player who has dealt or taken
    ///    PvP damage within the combat timeout (CombatRegen.CombatTimeoutMs).
    /// </summary>
    public static class Pvp
    {
        public const double DamageModifier = 0.5;
        public const double HealingModifier = 0.5;
        public const double EffectDurationModifier = -0.5;

        /// <summary>gameeffectdata PVP_SAFETY.</summary>
        public const int SafetyTypeId = 386;

        /// <summary>PVP_SAFETY_DURATION, in seconds.</summary>
        public const int SafetySeconds = 60;

        private sealed class Engagement
        {
            public long LastTick;
        }

        private static readonly ConditionalWeakTable<Manifestation, Engagement> Engagements = new();

        /// <summary>The clock engagements run on; replaceable for tests.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        #region Who fights whom

        /// <summary>
        /// Whether two players are on opposite sides of a wargame they are both in - what
        /// Actor.GetWargameParticipantStatus calls not allied.
        /// </summary>
        public static bool AreEnemies(Manifestation one, Manifestation other)
        {
            if (one == null || other == null || ReferenceEquals(one, other) || one.Id == 0 || other.Id == 0)
                return false;

            var mine = ClanFeuds.Instance.WargameDataOf(one);

            if (mine.Count == 0)
                return false;

            var theirs = ClanFeuds.Instance.WargameDataOf(other);

            return mine.Any(w => theirs.TryGetValue(w.Key, out var side) && side != w.Value);
        }

        /// <summary>Whether this is a player's attack on an enemy player who is alive to take it.</summary>
        public static bool IsEnemyTarget(Manifestation attacker, Actor target)
        {
            return target is Manifestation defender && AreEnemies(attacker, defender)
                   && defender.State != CharacterState.Dead && defender.State != CharacterState.Dying;
        }

        /// <summary>How the one player is to be shown to the other: HOSTILE to an enemy, FRIENDLY otherwise.</summary>
        public static TargetCategory CategoryFor(Manifestation shown, Manifestation viewer)
        {
            return AreEnemies(shown, viewer) ? TargetCategory.Hostile : TargetCategory.Friendly;
        }

        /// <summary>Whether this is damage from one player to another: what the PvP numbers apply to.</summary>
        public static bool IsPvp(Actor source, Actor target) => source is Manifestation && target is Manifestation && !ReferenceEquals(source, target);

        /// <summary>Whether a player has PvP Safety on.</summary>
        public static bool IsSafe(Actor actor)
        {
            return actor is Manifestation player && player.ActiveEffects.Values.Any(e => e.TypeId == SafetyTypeId);
        }

        /// <summary>
        /// Whether PvP Safety stops a hit: a player's on a player, when either of them holds it.
        /// The hit lands as Immune.
        /// </summary>
        public static bool Shielded(Actor source, Actor target)
        {
            return IsPvp(source, target) && (IsSafe(source) || IsSafe(target));
        }

        /// <summary>Whether a player brought to zero by this source is defeated: an enemy player's doing.</summary>
        public static bool Defeats(Actor source, Manifestation victim)
        {
            return source is Manifestation attacker && AreEnemies(attacker, victim);
        }

        #endregion

        #region The numbers

        /// <summary>A player's hit on a player: PVP_DAMAGE_MODIFIER of it, and at least 1 of a hit that did anything.</summary>
        public static int ScaleDamage(Actor source, Actor target, int amount)
        {
            if (amount <= 0 || !IsPvp(source, target))
                return amount;

            return Math.Max(1, (int)Math.Round(amount * DamageModifier, MidpointRounding.AwayFromZero));
        }

        /// <summary>What a player's stun or knockback on a player lasts: PVP_EFFECT_DURATION_MODIFIER off it.</summary>
        public static int ScaleDuration(Actor source, Actor target, int durationMs)
        {
            if (durationMs <= 0 || !IsPvp(source, target))
                return durationMs;

            return Math.Max(1, (int)Math.Round(durationMs * (1 + EffectDurationModifier), MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// A player's heal on a player who is in a fight with another player: PVP_HEALING_MODIFIER
        /// of it. Regeneration and anything without a player behind it is left alone.
        /// </summary>
        public static int ScaleHealing(Actor target, Actor source, int amount)
        {
            if (amount <= 0 || !(target is Manifestation healed) || !(source is Manifestation) || !IsEngaged(healed))
                return amount;

            return Math.Max(1, (int)Math.Round(amount * HealingModifier, MidpointRounding.AwayFromZero));
        }

        /// <summary>A PvP hit between the two: each is in a PvP fight for the combat timeout.</summary>
        public static void RecordEngagement(Actor source, Actor target)
        {
            if (!IsPvp(source, target))
                return;

            var now = Now();

            Engagements.GetOrCreateValue((Manifestation)source).LastTick = now;
            Engagements.GetOrCreateValue((Manifestation)target).LastTick = now;
        }

        /// <summary>Whether the player has dealt or taken PvP damage within the combat timeout.</summary>
        public static bool IsEngaged(Manifestation player)
        {
            return player != null && Engagements.TryGetValue(player, out var engagement)
                   && engagement.LastTick != 0 && Now() - engagement.LastTick < CombatRegen.CombatTimeoutMs;
        }

        #endregion

        #region Enemies around

        /// <summary>The living enemy players within radius metres of a point, from the cells around the performer.</summary>
        public static List<Manifestation> EnemiesWithin(MapChannel mapChannel, Manifestation player, Vector3 centre, float radius)
        {
            var found = new List<Manifestation>();

            if (mapChannel == null || player?.Cells == null || !(radius > 0))
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var client in cell.ClientList)
                {
                    var other = client?.Player;

                    if (other != null && other.MapChannel == mapChannel && !found.Contains(other)
                        && IsEnemyTarget(player, other) && Vector3.Distance(centre, other.Position) <= radius)
                        found.Add(other);
                }

            return found;
        }

        /// <summary>The living enemy players in a cone from the performer.</summary>
        public static List<Manifestation> EnemiesInCone(MapChannel mapChannel, Manifestation player, Vector3 aim, float range, float halfAngleDegrees)
        {
            return EnemiesWithin(mapChannel, player, player.Position, range)
                .Where(p => AbilityManager.InCone(player.Position, aim, p.Position, range, halfAngleDegrees))
                .ToList();
        }

        #endregion

        #region Showing it

        /// <summary>
        /// The player and everyone around them see each other as they now stand - HOSTILE across a
        /// wargame, FRIENDLY otherwise. After a player's wargames change: a feud starting or
        /// ending, joining or leaving a clan at feud.
        /// </summary>
        public static void RefreshCategories(Client client)
        {
            var player = client?.Player;
            var mapChannel = player?.MapChannel;

            if (mapChannel == null || player.Cells == null)
                return;

            foreach (var other in CellManager.Instance.GetClientsInCells(mapChannel, player.Cells, client))
            {
                if (other.Player == null || other.Player == player)
                    continue;

                if (!Detection.IsHiddenFrom(player, other))
                    other.CallMethod(player.EntityId, new TargetCategoryPacket(CategoryFor(player, other.Player)));

                if (!Detection.IsHiddenFrom(other.Player, client))
                    client.CallMethod(other.Player.EntityId, new TargetCategoryPacket(CategoryFor(other.Player, player)));
            }
        }

        #endregion

        #region Defeat

        /// <summary>
        /// A player brought to zero by an enemy player: the kill counted for the wargame, the loser
        /// back on full health and armour, and PvP Safety on them for PVP_SAFETY_DURATION.
        /// </summary>
        public static void Defeat(MapChannel mapChannel, Manifestation victim, Actor source)
        {
            if (victim == null)
                return;

            var killer = source as Manifestation;

            if (victim.Attributes.TryGetValue(Attributes.Health, out var health))
            {
                health.Current = health.CurrentMax;

                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, victim, new UpdateHealthPacket(health, 0));
            }

            if (victim.Attributes.TryGetValue(Attributes.Armor, out var armor))
            {
                armor.Current = armor.CurrentMax;

                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, victim, new UpdateArmorPacket(armor, 0));
            }

            var victimClient = ClientOf(mapChannel, victim);
            var killerClient = ClientOf(mapChannel, killer);

            if (victimClient != null && killerClient != null)
                ClanFeuds.Instance.Kill(killerClient, victimClient);

            Logger.WriteLog(LogType.Debug, $"PvP: {victim.FamilyName} ({victim.Id}) defeated by {killer?.FamilyName} ({killer?.Id}).");

            if (mapChannel != null)
                GiveSafety(mapChannel, victim);
        }

        /// <summary>PvP Safety on a player for PVP_SAFETY_DURATION, replacing any they had.</summary>
        public static GameEffect GiveSafety(MapChannel mapChannel, Manifestation player)
        {
            var effect = new GameEffect
            {
                TypeId = SafetyTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = player.EntityId,
                Source = player,
                ExpiresTick = Environment.TickCount64 + SafetySeconds * 1000L,
                IsBuff = true,
                AnnounceOnAttach = true
            };

            GameEffectManager.Instance.Attach(mapChannel, player, effect);

            return effect;
        }

        private static Client ClientOf(MapChannel mapChannel, Manifestation player)
        {
            if (player == null)
                return null;

            if (mapChannel?.ClientList != null)
                foreach (var client in mapChannel.ClientList)
                    if (client?.Player == player)
                        return client;

            lock (Server.Clients)
                return Server.Clients.FirstOrDefault(c => c?.Player == player);
        }

        #endregion
    }
}
