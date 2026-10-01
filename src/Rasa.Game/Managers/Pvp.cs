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
    /// The wargames are Clan Feuds (ClanFeuds), Duels (Duels) and Squad Wargames (SquadWargames); <see cref="AreEnemies"/> reads
    /// the sides from their WargameData (Wargames.DataOf), as the client does.
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
    /// What belongs to a player fights for them (<see cref="Controller"/>): a turret, trap, crab
    /// mine, rift, minion, bot, clone or reanimated corpse of theirs may attack their enemies and
    /// is shown HOSTILE to them and attackable by them, and its hits on a player count as its
    /// master's - halved, stopped by PvP Safety, and a defeat credited to the master.
    ///
    /// The debuffs a player puts on an enemy player last PVP_EFFECT_DURATION_MODIFIER less
    /// (GameEffectManager.Attach); stuns and knockbacks are scaled where they are made
    /// (<see cref="ScaleDuration"/>).
    ///
    /// Ours, since nothing in the client says:
    ///  - A player brought to zero by an enemy dies in a clan feud, the kill counted, and comes
    ///    back from a hospital with PvP Safety (PlayerDeath). In a duel - whose flags carry no
    ///    penalty - the loser is defeated, not killed: the kill counts, they are back on full
    ///    health and armour, and have PvP Safety for its 60 seconds (<see cref="Defeat"/>). So is
    ///    a GM who may not die.
    ///  - PvP Safety protects its holder only: they take nothing from another player or a creature
    ///    of one (the hit shows Immune), but what they do lands. Attacking an enemy player - a hit,
    ///    a debuff or a control, theirs or their creature's, landing or not - ends it at once
    ///    (<see cref="Attack"/>). A damage-over-time tick is not an attack: it was started before.
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

            var mine = Wargames.DataOf(one);

            if (mine.Count == 0)
                return false;

            var theirs = Wargames.DataOf(other);

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

        /// <summary>
        /// The player behind an actor: a player themselves, or the master of a creature that belongs
        /// to one (Creature.MasterEntityId - a turret, trap, crab mine, rift, minion, bot, clone,
        /// reanimated corpse or pet). Null for anything else.
        /// </summary>
        public static Manifestation Controller(Actor actor)
        {
            if (actor is Manifestation player)
                return player;

            if (actor is Creature creature && creature.MasterEntityId != 0
                && EntityManager.Instance.Players.TryGetValue(creature.MasterEntityId, out var master))
                return master;

            return null;
        }

        /// <summary>Whether the creature belongs to an enemy of this player: theirs to shoot, and shown to them HOSTILE.</summary>
        public static bool IsEnemySummon(Manifestation player, Creature creature)
        {
            return creature != null && creature.MasterEntityId != 0 && creature.MasterEntityId != player?.EntityId
                   && AreEnemies(player, Controller(creature));
        }

        /// <summary>How a creature is to be shown to a player: HOSTILE if it belongs to an enemy of theirs, its own category otherwise.</summary>
        public static TargetCategory CategoryFor(Creature creature, Manifestation viewer)
        {
            return IsEnemySummon(viewer, creature) ? TargetCategory.Hostile : creature.TargetCategory;
        }

        /// <summary>
        /// Whether a player's creature may fight this entity because of a wargame: an enemy of its
        /// master, or a creature that belongs to one (BehaviorManager.MayFight).
        /// </summary>
        public static bool SummonMayFight(Creature creature, ulong entityId)
        {
            var master = Controller(creature);

            if (master == null || creature is null || creature.MasterEntityId == 0)
                return false;

            if (EntityManager.Instance.Players.TryGetValue(entityId, out var player))
                return AreEnemies(master, player);

            return EntityManager.Instance.Creatures.TryGetValue(entityId, out var other) && IsEnemySummon(master, other);
        }

        /// <summary>
        /// Whether this is damage to a player from another player, or from a creature of theirs:
        /// what the PvP numbers apply to.
        /// </summary>
        public static bool IsPvp(Actor source, Actor target) =>
            target is Manifestation victim && Controller(source) is Manifestation attacker && !ReferenceEquals(attacker, victim);

        /// <summary>Whether a player has PvP Safety on.</summary>
        public static bool IsSafe(Actor actor)
        {
            return actor is Manifestation player && player.ActiveEffects.Values.Any(e => e.TypeId == SafetyTypeId);
        }

        /// <summary>
        /// Whether PvP Safety stops a hit: a player's (or their creature's) on a player who holds
        /// it. The attacker's own Safety stops nothing. The hit lands as Immune.
        /// </summary>
        public static bool Shielded(Actor source, Actor target)
        {
            return IsPvp(source, target) && IsSafe(target) || Restrained(source, target);
        }

        /// <summary>
        /// A player (or their creature) is attacking a player: if it is an enemy, the attacker's
        /// PvP Safety comes off first, whether or not the attack then lands. Returns whether the
        /// attack is stopped (<see cref="Shielded"/>).
        /// </summary>
        public static bool Attack(MapChannel mapChannel, Actor source, Actor target)
        {
            if (Controller(source) is Manifestation attacker && target is Manifestation victim && AreEnemies(attacker, victim))
                EndSafety(mapChannel ?? attacker.MapChannel, attacker);

            return Shielded(source, target);
        }

        /// <summary>Takes PvP Safety off the player ("PvP damage enabled"). False when they had none.</summary>
        public static bool EndSafety(MapChannel mapChannel, Manifestation player)
        {
            var safety = player?.ActiveEffects.Values.Where(e => e.TypeId == SafetyTypeId).ToList();

            if (safety == null || safety.Count == 0)
                return false;

            foreach (var effect in safety)
                if (mapChannel != null)
                    GameEffectManager.Instance.DettachEffect(mapChannel, player, effect);
                else
                    player.ActiveEffects.Remove(effect.EffectId);

            return true;
        }

        /// <summary>
        /// Whether a Traitor on the attacking player (GameEffect.Restrains) stops this hit: one on
        /// its caster, on a player on the caster's side, or on a creature of theirs. The hit
        /// lands as Immune.
        /// </summary>
        public static bool Restrained(Actor source, Actor target)
        {
            if (!(Controller(source) is Manifestation attacker) || target == null)
                return false;

            var victim = target is Creature creature ? Controller(creature) : target as Manifestation;

            if (victim == null || ReferenceEquals(victim, attacker))
                return false;

            foreach (var effect in attacker.ActiveEffects.Values)
            {
                if (!effect.Restrains || effect.IsExpired || !(Controller(effect.Source) is Manifestation caster))
                    continue;

                if (ReferenceEquals(victim, caster) || !AreEnemies(caster, victim))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// A hit has landed: a Traitor's caster attacking the player they turned lets them go
        /// ("ends when you attack it").
        /// </summary>
        public static void OnHit(MapChannel mapChannel, Actor source, Actor target)
        {
            if (!(target is Manifestation victim) || !(Controller(source) is Manifestation attacker) || mapChannel == null)
                return;

            foreach (var effect in victim.ActiveEffects.Values.Where(e => e.Restrains && ReferenceEquals(Controller(e.Source), attacker)).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, victim, effect);
        }

        /// <summary>Whether the player is held from attacking (Mind Control P2-P5, GameEffect.NoAttack).</summary>
        public static bool MayNotAttack(Actor actor) => actor != null && actor.ActiveEffects.Values.Any(e => e.NoAttack && !e.IsExpired);

        /// <summary>Whether the player is held from helping anyone else (Mind Control P4-P5, GameEffect.NoAssist).</summary>
        public static bool MayNotAssist(Actor actor) => actor != null && actor.ActiveEffects.Values.Any(e => e.NoAssist && !e.IsExpired);

        /// <summary>Whether a player brought to zero by this source is defeated: an enemy player's doing, or their creature's.</summary>
        public static bool Defeats(Actor source, Manifestation victim)
        {
            return Controller(source) is Manifestation attacker && AreEnemies(attacker, victim);
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

            Engagements.GetOrCreateValue(Controller(source)).LastTick = now;
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

            // The creatures that belong to players: the player's own, to everyone around them, and
            // everyone else's, to the player.
            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var creature in cell.CreatureList.ToList())
                {
                    if (creature.MasterEntityId == 0)
                        continue;

                    if (creature.MasterEntityId == player.EntityId)
                    {
                        foreach (var onlooker in CellManager.Instance.GetClientsInCells(mapChannel, creature.Cells ?? player.Cells, client))
                            if (onlooker.Player != null)
                                onlooker.CallMethod(creature.EntityId, new TargetCategoryPacket(CategoryFor(creature, onlooker.Player)));
                    }
                    else
                        client.CallMethod(creature.EntityId, new TargetCategoryPacket(CategoryFor(creature, player)));
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

            var killer = Controller(source);

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

            CountKill(mapChannel, victim, source);

            Logger.WriteLog(LogType.Debug, $"PvP: {victim.FamilyName} ({victim.Id}) defeated by {killer?.FamilyName} ({killer?.Id}).");

            if (mapChannel != null)
                GiveSafety(mapChannel, victim);
        }

        /// <summary>An enemy player (or their creature) has brought this player down: the kill counts for the duel or feud between them.</summary>
        public static void CountKill(MapChannel mapChannel, Manifestation victim, Actor source)
        {
            var victimClient = ClientOf(mapChannel, victim);
            var killerClient = ClientOf(mapChannel, Controller(source));

            if (victimClient == null || killerClient == null)
                return;

            Duels.Instance.Kill(killerClient, victimClient);
            SquadWargames.Instance.Kill(killerClient, victimClient);
            ClanFeuds.Instance.Kill(killerClient, victimClient);
        }

        /// <summary>Whether the two are on opposite sides of a squad wargame: as in a duel, its loser is defeated, not killed (PlayerDeath).</summary>
        public static bool InSquadWargame(MapChannel mapChannel, Manifestation one, Manifestation other)
        {
            return SquadWargames.Instance.AreOpposed(ClientOf(mapChannel, one), ClientOf(mapChannel, other));
        }

        /// <summary>Whether the two are dueling each other: a duel's loser is defeated, not killed (PlayerDeath).</summary>
        public static bool InDuel(MapChannel mapChannel, Manifestation one, Manifestation other)
        {
            var client = ClientOf(mapChannel, one);
            var otherClient = ClientOf(mapChannel, other);

            return client != null && otherClient != null && Duels.Instance.DuelOf(client)?.Involves(otherClient) == true;
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
