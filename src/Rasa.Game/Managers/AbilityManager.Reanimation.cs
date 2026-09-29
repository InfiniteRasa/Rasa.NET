using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Reanimation (AA_EXOBIOLOGIST_REANIMATION 240, abilities.reanimation) and Reanimation Wave
    /// (SA_EXOBIOLOGIST_REANIMATION_WAVE 176, abilities.reanimationwave). The first "Reanimates a
    /// single biological enemy corpse to assist the user in combat for a set time. Corpse is
    /// reanimated at a lower level than the user. Can be targeted and destroyed"; the wave does
    /// the same to "all nearby biological enemy corpses" (uielement 1250-ish, skill text).
    ///
    /// From the client and its data:
    /// - ReanimationAction sets canTargetDead and allows only a dead, targetable player or
    ///   BIOLOGICAL creature; ReanimationWaveAction is TARGET_SELF with targetGameEffect
    ///   ReanimatedEffect, so its DoAbility announces REANIMATED (132) on every hit;
    /// - REANIMATED_DEATH (134).OnAnnounceAttach(target, duration) removes the entity duration
    ///   seconds later - the risen creature's end;
    /// - Reanimation per pump: DURATION 120 s, CREATURE_LEVEL_DIFFERENCE -4 → 0,
    ///   HATE_TRANSFER_PERCENT 50, range 40 m. The wave: RADIUS_AROUND_SOURCE 25 m, DURATION 120,
    ///   CREATURE_LEVEL_DIFFERENCE 0, HATE_TRANSFER_PERCENT 50, and CREATURE_VARIANT_ID 83, which
    ///   names a variant from a table we do not have; each corpse rises as its own kind instead.
    ///
    /// The server's part:
    /// - a corpse must be a dead, BIOLOGICAL, HOSTILE or NEUTRAL creature no other corpse ability has claimed
    ///   (IsUsableCorpse). A new creature of its kind - class, name, actions, appearance, speeds
    ///   and full health - rises where it lay, and the body is given up for despawn;
    /// - it is FRIENDLY, its level the player's plus CREATURE_LEVEL_DIFFERENCE (at least 1), its
    ///   master the player, Aggressive, and it follows the player (BehaviorManager), fighting the
    ///   HOSTILE creatures it finds; they fight it back. What it kills is the player's
    ///   (CreatureManager.HandleCreatureKill). It is not a commandable subordinate - those are
    ///   Create Clone, Spotter and Bot Construction - so it is not handed to MinionManager;
    /// - it carries REANIMATED for DURATION; when that runs out, or its master leaves the map,
    ///   it dies, REANIMATED_DEATH plays and it is taken away ReanimatedDeathSeconds later.
    ///   Killed in the fight, it is an ordinary corpse and despawns as one;
    /// - HATE_TRANSFER_PERCENT: of the hate the player earns on a creature within the risen
    ///   creature's attack range, 50% goes to it instead (HateSinkFor, Threat).
    ///
    /// Not done: player corpses (the PvP clone and forced hospital revive).
    /// </summary>
    public partial class AbilityManager
    {
        public const string ReanimationModule = "abilities.reanimation";
        public const string ReanimationWaveModule = "abilities.reanimationwave";
        public const int ReanimatedTypeId = 132;                 // REANIMATED
        public const int ReanimatedDeathTypeId = 134;            // REANIMATED_DEATH

        /// <summary>How long the risen creature's end plays before it is taken away.</summary>
        public const int ReanimatedDeathSeconds = 3;

        /// <summary>The least reach a risen creature draws hate over, for one whose attacks are all melee.</summary>
        public const float ReanimatedMinReach = 5f;

        private sealed class Risen
        {
            public MapChannel MapChannel;
            public Creature Creature;
            public Manifestation Owner;
            public GameEffect Effect;
            public int HateTransferPercent;
            public long RemoveAt;
        }

        private static readonly List<Risen> RisenCreatures = new List<Risen>();
        private static readonly object RisenLock = new object();

        /// <summary>A risen creature's level: the player's plus CREATURE_LEVEL_DIFFERENCE, at least 1.</summary>
        public static uint ReanimatedLevel(int playerLevel, int levelDifference)
        {
            return (uint)Math.Max(1, playerLevel + levelDifference);
        }

        public static bool IsReanimated(Creature creature)
        {
            lock (RisenLock)
                return RisenCreatures.Any(r => r.Creature == creature);
        }

        /// <summary>Reanimation: the corpse targeted rises.</summary>
        private void Reanimate(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);
            var corpse = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) as Creature : null;

            if (IsUsableCorpse(corpse))
            {
                var risen = Raise(mapChannel, player, corpse, info, true);

                if (risen != null)
                    Hit(recovery, risen);
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        /// <summary>Reanimation Wave: every usable corpse within RADIUS_AROUND_SOURCE rises.</summary>
        private void ReanimationWave(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);
            var radius = info.Get(AbilityProperty.RadiusAroundSource, 25);
            var corpses = new List<Creature>();

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var creature in cell.CreatureList)
                    if (!corpses.Contains(creature) && Vector3.Distance(player.Position, creature.Position) <= radius && IsUsableCorpse(creature))
                        corpses.Add(creature);

            // The client's targetGameEffect announces REANIMATED on each hit.
            foreach (var corpse in corpses)
            {
                var risen = Raise(mapChannel, player, corpse, info, false);

                if (risen != null)
                    Hit(recovery, risen);
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
        }

        /// <summary>A creature of the corpse's kind, fighting for the player, where the corpse lay; the corpse goes.</summary>
        private Creature Raise(MapChannel mapChannel, Manifestation player, Creature corpse, ActionLevelInfo info, bool announce)
        {
            var risen = new Creature(corpse)
            {
                TargetCategory = TargetCategory.Friendly,
                Level = ReanimatedLevel(player.Level, info.Get(AbilityProperty.CreatureLevelDifference)),
                MasterEntityId = player.EntityId,
                Stance = MinionStance.Aggressive,   // it looks for the fight; a creature with a master only scans when aggressive
                State = CharacterState.Idle,
                Name = corpse.Name,
                AggroRange = corpse.AggroRange,
                Scale = corpse.Scale
            };

            // Back to full: each attribute at its maximum.
            foreach (var (id, attribute) in corpse.Attributes)
                risen.Attributes[id] = new ActorAttributes(id, attribute.NormalMax, attribute.CurrentMax, attribute.CurrentMax, attribute.RefreshAmount, attribute.RefreshPeriod);

            if (!risen.Attributes.TryGetValue(Attributes.Health, out var health) || health.CurrentMax <= 0)
                return null;

            CreatureManager.Instance.SetLocation(risen, corpse.Position, corpse.Rotation, corpse.MapContextId);
            CellManager.Instance.AddToWorld(mapChannel, risen);

            BehaviorManager.Instance.SetActionFollow(risen, player.EntityId);

            var effect = NewEffect(mapChannel, player, info, ReanimatedTypeId, Math.Max(1, info.Get(AbilityProperty.Duration, 120)));

            effect.IsBuff = true;
            effect.AllowDetach = false;
            effect.AnnounceOnAttach = announce;
            effect.AnnounceToNewcomers = false;     // the rising is for those who saw it
            effect.OnExpired = (map, actor, e) => EndReanimation(risen);

            GameEffectManager.Instance.Attach(mapChannel, risen, effect);

            lock (RisenLock)
                RisenCreatures.Add(new Risen
                {
                    MapChannel = mapChannel,
                    Creature = risen,
                    Owner = player,
                    Effect = effect,
                    HateTransferPercent = info.Get(AbilityProperty.HateTransferPercent)
                });

            // What rose from the body takes its place; the body goes the usual way, loot and all.
            if (corpse.Controller != null)
                corpse.Controller.DeadTime = long.MaxValue / 2;

            return risen;
        }

        /// <summary>Its time is up, or its master has gone: it dies, its end plays, and it is taken away.</summary>
        private static void EndReanimation(Creature creature)
        {
            Risen risen;

            lock (RisenLock)
                risen = RisenCreatures.FirstOrDefault(r => r.Creature == creature);

            if (risen == null || risen.RemoveAt != 0)
                return;

            var mapChannel = risen.MapChannel;

            risen.RemoveAt = Environment.TickCount64 + ReanimatedDeathSeconds * 1000L;

            // Unmastered first, or the kill would be handed to its master.
            creature.MasterEntityId = 0;

            if (creature.State != CharacterState.Dead)
            {
                creature.Attributes[Attributes.Health].Current = 0;

                // Nobody's kill: no experience, no loot.
                CreatureManager.Instance.HandleCreatureKill(mapChannel, creature, creature);
            }

            // REANIMATED_DEATH.OnAnnounceAttach(target, duration): the client takes it away after duration.
            var death = new GameEffect
            {
                TypeId = ReanimatedDeathTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = risen.Effect.EffectLevel,
                SourceId = risen.Owner.EntityId,
                IsBuff = false,
                AttachArgs = new List<object> { ReanimatedDeathSeconds }
            };

            CellManager.Instance.CellCallMethod(mapChannel, creature, GameEffectManager.AttachedPacket(death, true));
        }

        /// <summary>Runs the risen on this map: end those whose master has gone, take away the spent ones, forget the fallen.</summary>
        internal void ReanimationWorker(MapChannel mapChannel)
        {
            List<Risen> risenHere;

            lock (RisenLock)
                risenHere = RisenCreatures.Where(r => r.MapChannel == mapChannel).ToList();

            if (risenHere.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var risen in risenHere)
            {
                var creature = risen.Creature;

                try
                {
                    if (risen.RemoveAt != 0)
                    {
                        if (now < risen.RemoveAt)
                            continue;

                        lock (RisenLock)
                            RisenCreatures.Remove(risen);

                        if (EntityManager.Instance.GetEntityType(creature.EntityId) == EntityType.Creature)
                            CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creature);

                        continue;
                    }

                    // Killed in the fight: an ordinary corpse now, which despawns as one.
                    if (creature.State == CharacterState.Dead)
                    {
                        lock (RisenLock)
                            RisenCreatures.Remove(risen);

                        creature.MasterEntityId = 0;
                        continue;
                    }

                    // Its master has left the map, or the game.
                    var owner = risen.Owner;

                    if (owner.MapChannel != mapChannel || owner.MapContextId != creature.MapContextId
                        || !mapChannel.ClientList.Any(c => c?.Player == owner))
                        EndReanimation(creature);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Reanimated creature {creature.EntityId} on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");

                    lock (RisenLock)
                        RisenCreatures.Remove(risen);
                }
            }
        }

        /// <summary>The risen creatures of this owner, alive, with the reach they draw hate over and their percent.</summary>
        private static List<(Creature Creature, float Reach, int Percent)> RisenOf(Manifestation owner)
        {
            lock (RisenLock)
                return RisenCreatures
                    .Where(r => r.Owner == owner && r.RemoveAt == 0 && r.Creature.State != CharacterState.Dead)
                    .Select(r => (r.Creature, Math.Max(ReanimatedMinReach, r.Creature.Actions.Count == 0 ? 0f : (float)r.Creature.Actions.Max(a => a.RangeMax)), r.HateTransferPercent))
                    .ToList();
        }
    }
}
