using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A Miasma dissipates and coalesces, from its client classes:
    ///
    ///  - MiasmaDissipateEffect (MIASMA_DISSIPATE 277): "Allows a Miasma to dissipate into a
    ///    cloud and coalesce back into solid form." Its Recv_Dissipate blocks the Miasma's
    ///    movement and makes it untargetable, and plays its cloud (specialFX at level 1);
    ///    Recv_Coalesce makes it targetable and lets it move again.
    ///  - MiasmaCoalesceAbility (CR_MIASMA_COALESCE 485): "Miasma's coalesce implosion" -
    ///    TARGET_NONE, no windup, every hostile within RADIUS_AROUND_SOURCE (10 m) hit for
    ///    DAMAGE_AMOUNT of DAMAGE_TYPE (ice) and again for EXTRA_DAMAGE_PERCENT (100) of that as
    ///    EXTRA_DAMAGE_TYPE (sonic), the hitdata (clientInfo, extraClientInfo) its DoAbility floats
    ///    as two hits.
    ///
    /// So a Miasma with the coalesce in its row, fighting someone within its radius, turns to a
    /// cloud: MIASMA_DISSIPATE on it, announced, and Dissipate called on it. For DissipateMs it
    /// is a cloud - it does nothing, and no hit gets through to it (the effect's resistance is
    /// CreatureSummons.AbsorbAllResist, for what reaches it untargeted). Then it coalesces:
    /// Coalesce called, the implosion on everyone around it, and the effect off. One killed as a
    /// cloud - by nothing, since nothing reaches it - or taken off the map does not implode.
    ///
    /// Ours, the data giving no reason or length: when (the row's reuse, and its range, which is
    /// the implosion's radius) and how long a cloud lasts (DissipateMs).
    /// </summary>
    public static class CreatureMiasma
    {
        public const ActionId MiasmaCoalesce = (ActionId)485;
        public const int DissipateTypeId = 277;     // MIASMA_DISSIPATE

        /// <summary>Ours: how long a Miasma stays a cloud before it coalesces.</summary>
        public const long DissipateMs = 5000;

        private sealed class Cloud
        {
            public MapChannel MapChannel;
            public Creature Miasma;
            public CreatureAction Action;
            public GameEffect Effect;
            public long CoalesceAt;
        }

        private static readonly List<Cloud> Clouds = new List<Cloud>();
        private static readonly object CloudsLock = new object();
        private static readonly Random Random = new Random();

        public static bool Is(CreatureAction action) => action != null && action.ActionId == MiasmaCoalesce;

        /// <summary>Whether the Miasma is a cloud now: it does nothing until it coalesces.</summary>
        public static bool IsDissipated(Creature creature)
        {
            // A loop, not Any(): this is asked of every creature on every think, and Any with a
            // lambda that captures the creature allocates a closure and a delegate each time.
            lock (CloudsLock)
            {
                foreach (var cloud in Clouds)
                    if (cloud.Miasma == creature)
                        return true;

                return false;
            }
        }

        /// <summary>The Miasma turns to a cloud, if it is not one already; whether it did.</summary>
        public static bool Dissipate(MapChannel mapChannel, Creature miasma, CreatureAction action)
        {
            if (mapChannel == null || miasma == null || !Is(action) || IsDissipated(miasma) || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return false;

            var now = Environment.TickCount64;

            BehaviorManager.Instance.StopMoving(miasma);
            miasma.Controller.Path.Clear();
            miasma.Controller.PathIndex = 0;

            var cloud = new GameEffect
            {
                TypeId = DissipateTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                ActionId = info.ActionId,
                SourceId = miasma.EntityId,
                Source = miasma,
                SourceLevel = (int)miasma.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ResistModifier = CreatureSummons.AbsorbAllResist,
                ExpiresTick = now + DissipateMs + 5000      // a backstop; coalescing takes it off
            };

            GameEffectManager.Instance.Attach(mapChannel, miasma, cloud);

            if (!miasma.ActiveEffects.ContainsKey(cloud.EffectId))
                return false;

            CellManager.Instance.CellCallMethod(mapChannel, miasma, new GameEffectCallPacket(cloud.EffectId, "Dissipate"));

            lock (CloudsLock)
                Clouds.Add(new Cloud { MapChannel = mapChannel, Miasma = miasma, Action = action, Effect = cloud, CoalesceAt = now + DissipateMs });

            return true;
        }

        /// <summary>The clouds on this map whose time is up coalesce. Run every map tick.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Cloud> due;
            var now = Environment.TickCount64;

            lock (CloudsLock)
            {
                due = Clouds.Where(c => c.MapChannel == mapChannel && now >= c.CoalesceAt).ToList();

                foreach (var cloud in due)
                    Clouds.Remove(cloud);
            }

            foreach (var cloud in due)
                Coalesce(mapChannel, cloud);
        }

        /// <summary>The extra damage an implosion carries: EXTRA_DAMAGE_PERCENT of the hit, as EXTRA_DAMAGE_TYPE; none without a type.</summary>
        public static (DamageType Type, int Amount) ExtraOf(int rolled, ActionLevelInfo info)
        {
            if (info == null || info.Get(AbilityProperty.ExtraDamageType) <= 0)
                return (0, 0);

            var percent = Math.Max(0, info.Get(AbilityProperty.ExtraDamagePercent, 100));

            return ((DamageType)info.Get(AbilityProperty.ExtraDamageType), Math.Max(0, rolled) * percent / 100);
        }

        private static void Coalesce(MapChannel mapChannel, Cloud cloud)
        {
            var miasma = cloud.Miasma;
            var alive = miasma.State != CharacterState.Dead && miasma.State != CharacterState.Dying
                && miasma.MapContextId == mapChannel.MapInfo.MapContextId
                && miasma.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;

            if (!alive)
            {
                if (miasma.ActiveEffects.ContainsKey(cloud.Effect.EffectId))
                    GameEffectManager.Instance.DettachEffect(mapChannel, miasma, cloud.Effect);

                return;
            }

            CellManager.Instance.CellCallMethod(mapChannel, miasma, new GameEffectCallPacket(cloud.Effect.EffectId, "Coalesce"));

            var action = cloud.Action;

            if (AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
            {
                CellManager.Instance.CellCallMethod(mapChannel, miasma,
                    new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

                var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.DamagePair);
                var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);

                foreach (var victim in CreatureBombs.Caught(mapChannel, miasma, miasma.Position, CreatureBombs.RadiusOf(info)))
                {
                    int rolled;

                    lock (Random)
                        rolled = Random.Next((int)action.MinDamage, (int)Math.Max(action.MinDamage, action.MaxDamage) + 1);

                    var crit = CriticalHits.Resolve(miasma, victim, false, CriticalHits.AttackerChance(miasma, false), ref rolled);
                    var amount = GameEffectManager.ApplyResist(victim, rolled, out var resisted, type);

                    ActorManager.Instance.Damage(mapChannel, victim, amount, miasma, out var absorbed, type);
                    Reflection.Reflect(mapChannel, victim, miasma, amount - absorbed, type);

                    var hit = new AbilityHit { EntityId = victim.EntityId, Amount = amount - absorbed, Absorbed = absorbed, Resisted = resisted, DamageType = type, IsCritical = crit };
                    var (extraType, extraRolled) = ExtraOf(rolled, info);

                    if (extraType != 0 && extraRolled > 0)
                    {
                        var extra = GameEffectManager.ApplyResist(victim, extraRolled, out var extraResisted, extraType);

                        ActorManager.Instance.Damage(mapChannel, victim, extra, miasma, out var extraAbsorbed, extraType);
                        Reflection.Reflect(mapChannel, victim, miasma, extra - extraAbsorbed, extraType);
                        hit.Extra = new AbilityHit { EntityId = victim.EntityId, Amount = extra - extraAbsorbed, Absorbed = extraAbsorbed, Resisted = extraResisted, DamageType = extraType };
                    }

                    recovery.Hits.Add(hit);
                }

                CellManager.Instance.CellCallMethod(mapChannel, miasma, recovery);
            }

            GameEffectManager.Instance.DettachEffect(mapChannel, miasma, cloud.Effect);
        }
    }
}
