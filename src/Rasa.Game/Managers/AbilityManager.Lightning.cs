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
    /// Lightning's extras on top of its direct damage, from its action data and the client's
    /// abilities/lightning.py:
    ///
    ///  - Arc (P2+): with PERCENTAGE_CHANCE (100), the bolt jumps from the target to
    ///    LightningArcTargets other hostile creature(s) within ARC_RADIUS (12 → 30 m) of it, nearest
    ///    first, for ARC_DAMAGE (90-210) scaled like the ability's damage ("Pump 2: +Arc to
    ///    additional Target"). The hit's onHitData carries these as arcData, which the client's
    ///    ARC_EFFECT floats and draws.
    ///  - Extra damage (P3+): EXTRA_DAMAGE_PERCENT (50) of the bolt's damage again as
    ///    EXTRA_DAMAGE_TYPE (7, sonic), resisted as that type; floated through the same arcData,
    ///    on the target itself.
    ///  - Stun (P4+): Stuns.OfAbility, from STUN_CHANCE / STUN_DURATION.
    ///  - Storm (P5, P7): LIGHTNINGSTORM_EFFECT 100 on the target for EFFECT_DURATION_MS (6 / 15 s),
    ///    ticking every EFFECT_INTERVAL_MS (2 s) for EFFECT_DAMAGE_MIN..MAX (60-90, scaled) on the
    ///    target and every other hostile within LightningStormRadius of it ("+AoE DoT"). The
    ///    client's StormEffect.OnTick(target, dotData, arcData) announces the first and draws arcs
    ///    to the rest.
    ///
    /// Chosen here, not in the client: one arc target per bolt, and the storm's radius.
    /// </summary>
    public partial class AbilityManager
    {
        internal const int LightningStormTypeId = 100;      // LIGHTNINGSTORM_EFFECT

        /// <summary>Creatures an arc jumps to: "+Arc to additional Target".</summary>
        public const int LightningArcTargets = 1;

        /// <summary>Metres around the storm's target that its damage reaches, a player's or a creature's (CreatureLightning). Not in the client.</summary>
        public const float LightningStormRadius = 10f;

        /// <summary>
        /// The arcs and extra damage of one bolt that hit <paramref name="target"/> for
        /// <paramref name="boltDamage"/> (before resistance), added to the hit's Arcs; and the
        /// storm, attached to the target. An arc to an object goes on <paramref name="objectHits"/>,
        /// for the caller to land once the recovery is out.
        /// </summary>
        private void ResolveLightningExtras(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, Actor target, int boltDamage, AbilityHit hit,
            List<(DynamicObject Target, int Damage)> objectHits)
        {
            var targetAlive = target.State != CharacterState.Dead && target.State != CharacterState.Dying && target.Attributes[Attributes.Health].Current > 0;

            // P3+: a share of the bolt again, as its own type.
            if (targetAlive && info.Has(AbilityProperty.ExtraDamageType) && info.Get(AbilityProperty.ExtraDamagePercent) > 0)
            {
                var extraType = (DamageType)info.Get(AbilityProperty.ExtraDamageType);
                var extra = Math.Max(1, boltDamage * info.Get(AbilityProperty.ExtraDamagePercent) / 100);

                hit.Arcs.Add(DealDamage(mapChannel, player, target, extra, extraType));
            }

            // P2+: the arc from the target to the nearest other hostile.
            ResolveLightningArc(mapChannel, player, info, target.Position, target, null, hit, objectHits);

            // P5 / P7: the storm on the target.
            if (targetAlive && target.State != CharacterState.Dying && info.Has(AbilityProperty.EffectDurationMs) && info.Get(AbilityProperty.EffectDamageMax) > 0)
                AttachLightningStorm(mapChannel, player, info, target);
        }

        /// <summary>
        /// P2+: the arc from where the bolt struck - a creature, an enemy player, or an object
        /// (a fuel container) - to whatever is nearest of the other hostiles and the objects a
        /// weapon can damage (PracticeTargetManager.TargetsWithin), added to the hit's Arcs. The
        /// client floats and draws each arc on its entity (ArcEffect, ClientDamageOthersEffect:
        /// AnnounceDamage, which an object has as a creature does). An object takes the arc's
        /// damage as it is - no resistance, as no weapon's hit has on one - once the caller has
        /// sent the recovery (<paramref name="objectHits"/>).
        /// </summary>
        private void ResolveLightningArc(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, Vector3 from,
            Actor struckActor, DynamicObject struckObject, AbilityHit hit, List<(DynamicObject Target, int Damage)> objectHits)
        {
            if (!info.Has(AbilityProperty.ArcRadius) || info.Get(AbilityProperty.ArcDamage) <= 0 || !Stuns.Roll(info.Get(AbilityProperty.PercentageChance, 100)))
                return;

            var arcDamage = GameEffectManager.ApplyDamageDealt(player, Scale(player.Level, info.Get(AbilityProperty.ArcDamage), info.Get(AbilityProperty.DamageScaleType)));
            var radius = info.Get(AbilityProperty.ArcRadius);

            var actors = NearestHostiles(mapChannel, player, from, struckActor, radius, LightningArcTargets)
                .Select(actor => (Position: actor.Position, EntityId: actor.EntityId, Actor: actor, Object: (DynamicObject)null));
            var objects = PracticeTargetManager.TargetsWithin(mapChannel, player, from, radius)
                .Where(obj => !ReferenceEquals(obj, struckObject) && !objectHits.Any(pending => ReferenceEquals(pending.Target, obj)))
                .Select(obj => (Position: obj.Position, EntityId: obj.EntityId, Actor: (Actor)null, Object: obj));

            foreach (var arc in actors.Concat(objects)
                         .OrderBy(candidate => Vector3.DistanceSquared(candidate.Position, from))
                         .ThenBy(candidate => candidate.EntityId)
                         .Take(LightningArcTargets)
                         .ToList())
            {
                if (arc.Actor != null)
                {
                    hit.Arcs.Add(DealDamage(mapChannel, player, arc.Actor, arcDamage, DamageType.Electrical));
                    continue;
                }

                if (arcDamage <= 0)
                    continue;

                hit.Arcs.Add(new TickEntry { EntityId = arc.EntityId, Amount = arcDamage, DamageType = DamageType.Electrical });
                objectHits.Add((arc.Object, arcDamage));
            }
        }

        /// <summary>Up to <paramref name="count"/> hostile creatures or enemy players (Pvp) other than <paramref name="from"/> within radius of it, nearest first.</summary>
        internal static List<Actor> NearestHostiles(MapChannel mapChannel, Manifestation player, Actor from, float radius, int count) =>
            NearestHostiles(mapChannel, player, from.Position, from, radius, count);

        /// <summary>Up to <paramref name="count"/> hostile creatures or enemy players (Pvp) but <paramref name="except"/> within radius of a point, nearest first.</summary>
        internal static List<Actor> NearestHostiles(MapChannel mapChannel, Manifestation player, Vector3 from, Actor except, float radius, int count)
        {
            return VictimsWithin(mapChannel, player, from, radius)
                .Where(c => c != except)
                .OrderBy(c => Vector3.DistanceSquared(c.Position, from))
                .ThenBy(c => c.EntityId)
                .Take(count)
                .ToList();
        }

        /// <summary>Damage of a type to a creature or enemy player from the player, resisted as that type, as the entry the client floats.</summary>
        private static TickEntry DealDamage(MapChannel mapChannel, Manifestation player, Actor target, int damage, DamageType damageType)
        {
            var amount = GameEffectManager.ApplyResist(target, damage, out var resisted, damageType);
            var taken = ActorManager.Instance.Damage(mapChannel, target, amount, player, out var outcome, damageType);

            return new TickEntry
            {
                EntityId = target.EntityId,
                Amount = outcome.Delivered,
                Absorbed = outcome.Absorbed,
                WasImmune = outcome.Immune,
                Resisted = resisted,
                DamageType = damageType,
                DeathBlow = taken > 0 && target.Attributes[Attributes.Health].Current <= 0
            };
        }

        private void AttachLightningStorm(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, Actor target)
        {
            var intervalMs = Math.Max(500, info.Get(AbilityProperty.EffectIntervalMs, 2000));
            var storm = NewEffect(mapChannel, player, info, LightningStormTypeId, null);

            // The storm cloud is the client's at level 1 alone, whatever the pump.
            storm.EffectLevel = GameEffectFx.LevelFor(LightningStormTypeId, info.Level);
            storm.IsBuff = false;
            storm.AnnounceOnAttach = true;
            storm.ExpiresTick = Environment.TickCount64 + info.Get(AbilityProperty.EffectDurationMs);
            storm.TickDamageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Electrical);
            storm.TickScaleType = info.Get(AbilityProperty.DamageScaleType);
            storm.TickIntervalMs = intervalMs;
            storm.NextTickTick = Environment.TickCount64 + intervalMs;

            var stormMin = info.Get(AbilityProperty.EffectDamageMin);
            var stormMax = Math.Max(stormMin, info.Get(AbilityProperty.EffectDamageMax));

            storm.OnTick = (m, holder, effect) => StormTick(m, holder, effect, stormMin, stormMax);

            GameEffectManager.Instance.Attach(mapChannel, target, storm);
        }

        /// <summary>One tick of the storm: its target, then every other hostile around it, each its own roll.</summary>
        private void StormTick(MapChannel mapChannel, Actor holder, GameEffect storm, int min, int max)
        {
            if (!(storm.Source is Manifestation player) || player.MapContextId != mapChannel.MapInfo.MapContextId || holder == null)
            {
                GameEffectManager.Instance.DettachEffect(mapChannel, holder, storm);
                return;
            }

            int Roll() => GameEffectManager.ApplyDamageDealt(player, Scale(storm.SourceLevel, _random.Next(min, max + 1), storm.TickScaleType));

            // Around the target first, while it is still there to be the centre.
            var target = holder;
            var others = VictimsWithin(mapChannel, player, target.Position, LightningStormRadius).Where(c => c != target).ToList();

            var tick = new GameEffectTickPacket(storm.EffectId, GameEffectTickPacket.TickKind.Storm);

            tick.Entries.Add(DealDamage(mapChannel, player, target, Roll(), storm.TickDamageType));

            foreach (var other in others)
                tick.ArcEntries.Add(DealDamage(mapChannel, player, other, Roll(), storm.TickDamageType));

            // And the objects around it a weapon can damage, arced to as the others are: the
            // client floats and draws each arc entry on its entity (StormEffect.OnTick, ArcEffect).
            tick.ArcEntries.AddRange(PracticeTargetManager.Entries(
                PracticeTargetManager.Blast(mapChannel, player, target.Position, LightningStormRadius, Roll, missions: _missionManager),
                storm.TickDamageType));

            CellManager.Instance.CellCallMethod(mapChannel, target, tick);
        }
    }
}
