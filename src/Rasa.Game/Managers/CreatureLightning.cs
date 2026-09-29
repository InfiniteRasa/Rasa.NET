using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A creature's lightning (abilities.lightning: CR_FOREAN_LIGHTNING, CR_MOX_ENERGY_ATTACK,
    /// CR_WARNET_ZAP, CR_WARNET_QUEEN, CR_BEAMMANTA_LIGHTNING, CR_BRANN_LIGHTNING,
    /// CR_THRAX_LIGHTNING) is the player's Lightning in a creature's hands, and its class is the
    /// same LightningAbility: a DamageBase whose OnAbility hands each hit's onHitData, (arcData,),
    /// to an ARC_EFFECT on the target - which floats every (entityId, rawInfo) in it and draws an
    /// arc from the target to each.
    ///
    /// The bolt itself is the attack's hit, as any creature attack's. What this adds, as
    /// AbilityManager.Lightning does for a player's, from the argument's data:
    ///  - extra damage: EXTRA_DAMAGE_PERCENT of the bolt again on the target, as EXTRA_DAMAGE_TYPE
    ///    - or as the bolt's own type when the argument gives a share and no type (the Warnet
    ///    Queen's 50%);
    ///  - the arc: with PERCENTAGE_CHANCE (100 when not given), ARC_DAMAGE to the nearest other
    ///    player within ARC_RADIUS of the target that the creature may fight - one, as a player's
    ///    bolt jumps to one ("+Arc to additional Target").
    ///
    /// Both go into the hit's arcData, so the client floats them and draws the arc. The numbers
    /// are the creature's: ARC_DAMAGE scaled by the same factor the creature_action row puts on
    /// the argument's DAMAGE_AMOUNT (row average over the argument's), so an arc stands to the
    /// bolt as the client has it. A stun in the data (STUN_CHANCE / STUN_DURATION) lands through
    /// PlayerCrowdControl as any creature attack's does.
    ///
    /// Not done: the storm (EFFECT_DURATION_MS, EFFECT_DAMAGE_MIN..MAX), which only
    /// CR_BRANN_LIGHTNING's _BOSS_OPERATION argument carries, and no Brann spawns.
    /// </summary>
    public static class CreatureLightning
    {
        public const string Module = "abilities.lightning";

        /// <summary>Players an arc jumps to, as a player's bolt jumps to one creature.</summary>
        public const int ArcTargets = 1;

        /// <summary>The factor the row puts on the argument's damage: row average over the argument's; 1 if either is missing.</summary>
        public static double RowScale(CreatureAction row, ActionLevelInfo info)
        {
            if (row == null || info == null)
                return 1.0;

            var clientMin = info.Get(AbilityProperty.DamageAmountMin);
            var clientMax = Math.Max(clientMin, info.Get(AbilityProperty.DamageAmountMax, clientMin));

            if (clientMin + clientMax <= 0 || row.MinDamage + row.MaxDamage == 0)
                return 1.0;

            return (row.MinDamage + row.MaxDamage) / (double)(clientMin + clientMax);
        }

        /// <summary>The extra damage a bolt of boltDamage carries, and its type; 0 for none.</summary>
        public static (int Amount, DamageType Type) ExtraOf(ActionLevelInfo info, int boltDamage, DamageType boltType)
        {
            var percent = info?.Get(AbilityProperty.ExtraDamagePercent) ?? 0;

            if (percent <= 0 || boltDamage <= 0)
                return (0, boltType);

            var type = info.Has(AbilityProperty.ExtraDamageType) ? (DamageType)info.Get(AbilityProperty.ExtraDamageType) : boltType;

            return (Math.Max(1, boltDamage * percent / 100), type == 0 ? DamageType.Electrical : type);
        }

        /// <summary>
        /// The bolt has hit the missile's target: its extra damage and its arc, into the hit's
        /// arcData. Anything that is not a creature's lightning is left alone.
        /// </summary>
        public static void Extras(MapChannel mapChannel, Missile missile, HitData hit)
        {
            if (mapChannel == null || missile == null || hit == null || !(missile.Source is Creature attacker) || AbilityManager.Instance == null)
                return;

            if (!AbilityManager.Instance.TryGetAction(missile.ActionId, missile.ActionArgId, out var module, out var info) || module != Module || info == null)
                return;

            var target = missile.TargetActor;

            if (target == null)
                return;

            // A bolt the creature may not land on a player lands nothing more either.
            if (target is Manifestation struck && !TargetCategories.MayFightPlayer(attacker.TargetCategory, struck.CombatCategory))
                return;

            var bolt = missile.AreaDamage;

            if (Alive(target))
            {
                var (extra, extraType) = ExtraOf(info, bolt, missile.DamageType);

                if (extra > 0)
                    hit.Arcs.Add(Deal(mapChannel, attacker, target, extra, extraType));
            }

            var arcDamage = (int)Math.Round(info.Get(AbilityProperty.ArcDamage) * RowScale(missile.CreatureAction, info));

            if (info.Get(AbilityProperty.ArcRadius) <= 0 || arcDamage <= 0)
                return;

            if (!Stuns.Roll(info.Get(AbilityProperty.PercentageChance, 100)))
                return;

            foreach (var other in ArcTo(mapChannel, attacker, target, info.Get(AbilityProperty.ArcRadius)))
                hit.Arcs.Add(Deal(mapChannel, attacker, other, arcDamage, DamageType.Electrical));
        }

        /// <summary>Up to ArcTargets players other than the target within radius of it that the creature may fight, nearest first.</summary>
        public static List<Manifestation> ArcTo(MapChannel mapChannel, Creature attacker, Actor target, float radius)
        {
            return mapChannel.ClientList
                .Select(c => c?.Player)
                .Where(p => p != null && p != target && Alive(p) && p.MapContextId == mapChannel.MapInfo.MapContextId
                    && Vector3.DistanceSquared(p.Position, target.Position) <= radius * radius
                    && TargetCategories.MayFightPlayer(attacker.TargetCategory, p.CombatCategory))
                .OrderBy(p => Vector3.DistanceSquared(p.Position, target.Position))
                .Take(ArcTargets)
                .ToList();
        }

        private static bool Alive(Actor actor)
        {
            return actor.State != CharacterState.Dead && actor.State != CharacterState.Dying
                && actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
        }

        /// <summary>Damage of a type from the creature, resisted as that type, as the entry the client floats.</summary>
        private static TickEntry Deal(MapChannel mapChannel, Creature attacker, Actor victim, int damage, DamageType damageType)
        {
            var amount = GameEffectManager.ApplyResist(victim, damage, out var resisted, damageType);
            var taken = ActorManager.Instance.Damage(mapChannel, victim, amount, attacker, out var outcome, damageType);

            Reflection.Reflect(mapChannel, victim, attacker, outcome.Delivered, damageType);

            return new TickEntry
            {
                EntityId = victim.EntityId,
                Amount = outcome.Delivered,
                Absorbed = outcome.Absorbed,
                WasImmune = outcome.Immune,
                Resisted = resisted,
                DamageType = damageType,
                DeathBlow = taken > 0 && victim is Creature && victim.Attributes[Attributes.Health].Current <= 0
            };
        }
    }
}
