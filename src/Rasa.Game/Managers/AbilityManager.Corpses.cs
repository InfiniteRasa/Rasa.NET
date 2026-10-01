using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Cadaver Immolation (abilities.corpseexplode), the Exobiologist's use for a body. The client
    /// (actions/abilities/corpseexplode.py) sets canTargetDead and refuses anything that is not a
    /// dead, targetable, BIOLOGICAL creature or player, so the target is a corpse: CORPSE_IMMOLATION
    /// 126, a BombEffect, goes on it, and DELAY (5 s) later it bursts, dealing DAMAGE_AMOUNT
    /// (300-450 → 300-750, scaled) of DAMAGE_TYPE (2, incendiary) to every hostile within
    /// EFFECT_RADIUS (12 → 20 m) of it, with a crit roll like any ability damage.
    ///
    /// The effect cannot be attached through GameEffectManager: the effect worker clears
    /// everything off a dead actor, which is right for a Ruin left ticking on a body and wrong
    /// here. So it is sent to the clients directly, as the Fire Support beacon's is, and timed
    /// here. The client's BombEffect removes the corpse when it goes off (removeTarget is its
    /// default 1), so the corpse is given up for despawn at the same moment and goes through the
    /// usual deletion, loot dispenser and all.
    /// </summary>
    public partial class AbilityManager
    {
        private const int CorpseImmolationTypeId = 126;     // CORPSE_IMMOLATION

        private sealed class BurningCorpse
        {
            public MapChannel MapChannel;
            public Manifestation Player;
            public Creature Corpse;
            public int EffectId;
            public long ExplodeAt;
            public float Radius;
            public int DamageMin, DamageMax, ScaleType;
            public DamageType DamageType;
        }

        private static readonly List<BurningCorpse> BurningCorpses = new List<BurningCorpse>();
        private static readonly object BurningCorpsesLock = new object();

        /// <summary>Whether this is a body Cadaver Immolation may be used on: dead, and biological.</summary>
        public static bool IsBiologicalCorpse(Creature creature)
        {
            if (creature == null || creature.State != CharacterState.Dead)
                return false;

            return CreatureManager.CreatureFlagsOf(creature).Contains((int)CreatureFlag.Biological);
        }

        private void ImmolateCorpse(MapChannel mapChannel, Manifestation player, Creature corpse, ActionLevelInfo info)
        {
            var effectId = GameEffectManager.Instance.NextEffectId(mapChannel);
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Fire);
            var min = info.Get(AbilityProperty.DamageAmountMin);

            CellManager.Instance.CellCallMethod(mapChannel, corpse, new GameEffectAttachedPacket
            {
                EffectTypeId = CorpseImmolationTypeId,
                EffectId = effectId,
                EffectLevel = Math.Max(1u, info.Level),
                SourceId = player.EntityId,
                Announced = true,
                Duration = null,
                DamageType = (int)damageType,
                AttrId = 1,
                IsActive = true,
                IsBuff = false,
                IsDebuff = true,
                IsNegativeEffect = true,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            });

            lock (BurningCorpsesLock)
                BurningCorpses.Add(new BurningCorpse
                {
                    MapChannel = mapChannel,
                    Player = player,
                    Corpse = corpse,
                    EffectId = effectId,
                    ExplodeAt = Environment.TickCount64 + Math.Max(500, info.Get(AbilityProperty.Delay, 5) * 1000),
                    Radius = info.Get(AbilityProperty.EffectRadius, 12),
                    DamageMin = min,
                    DamageMax = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min)),
                    ScaleType = info.Get(AbilityProperty.DamageScaleType),
                    DamageType = damageType
                });
        }

        /// <summary>Bursts the bodies on this map whose delay is up.</summary>
        internal void CorpseWorker(MapChannel mapChannel)
        {
            List<BurningCorpse> due;
            var now = Environment.TickCount64;

            lock (BurningCorpsesLock)
            {
                due = BurningCorpses.Where(c => c.MapChannel == mapChannel && now >= c.ExplodeAt).ToList();

                foreach (var corpse in due)
                    BurningCorpses.Remove(corpse);
            }

            foreach (var burning in due)
            {
                try
                {
                    Immolate(burning);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Cadaver Immolation on map {mapChannel.MapInfo.MapContextId} threw and was dropped: {e}");
                }
            }
        }

        private void Immolate(BurningCorpse burning)
        {
            var mapChannel = burning.MapChannel;
            var corpse = burning.Corpse;
            var player = burning.Player;

            // The body was cleared away, or the one who lit it has left the map.
            if (corpse.Controller == null || player == null || player.MapContextId != mapChannel.MapInfo.MapContextId
                || EntityManager.Instance.GetCreature(corpse.EntityId) != corpse)
                return;

            var blast = new GameEffectAnnounceDamagePacket(burning.EffectId, "DoExplosion");
            var critChance = CriticalHits.AttackerChance(player, false);
            var crits = new List<(Actor Victim, int Amount)>();

            foreach (var victim in VictimsWithin(mapChannel, player, corpse.Position, burning.Radius))
            {
                var rolled = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, BombRandom.Next(burning.DamageMin, burning.DamageMax + 1), burning.ScaleType));
                var crit = CriticalHits.Resolve(player, victim, false, critChance, ref rolled);
                var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, burning.DamageType);
                var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, player, out var outcome, burning.DamageType);

                blast.Hits.Add(new TickEntry
                {
                    EntityId = victim.EntityId,
                    Amount = outcome.Delivered,
                    Absorbed = outcome.Absorbed,
                    WasImmune = outcome.Immune,
                    Resisted = resisted,
                    DamageType = burning.DamageType,
                    IsCritical = crit,
                    DeathBlow = taken > 0 && victim.Attributes[Attributes.Health].Current <= 0
                });

                if (crit && !outcome.Immune)
                    crits.Add((victim, amount));
            }

            CellManager.Instance.CellCallMethod(mapChannel, corpse, blast);

            foreach (var (victim, amount) in crits)
                if (victim.State != CharacterState.Dead && victim.State != CharacterState.Dying && victim.Attributes[Attributes.Health].Current > 0)
                    CritEffects.OnCritical(mapChannel, victim, player, burning.DamageType, amount);

            // The clients have taken the body away with the blast; the server gives it up too, on
            // the deletion path that tidies its loot dispenser rather than by dropping it here.
            corpse.Controller.DeadTime = long.MaxValue / 2;
        }
    }
}
