using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// The damage over time an injection gun's or a net gun's hit leaves on what it lands on.
    ///
    /// The client has an effect for each: INJECTION_GUN_ELECTRIC_EFFECT to INJECTION_GUN_VIRAL_EFFECT
    /// (241 to 246) and NET_GUN_DOT_EFFECT (10000016), all DamageOverTime classes. Nothing put them
    /// on, so both guns did all of their damage on the hit and the effects were never seen. No
    /// numbers for them are in the data, so these are ours: an injection gun's shot does half its
    /// damage on the hit and the other half over 6 s in three ticks; a net gun's does three
    /// quarters on the hit and the last quarter over the 3 s its net holds, in three ticks.
    ///
    /// Each tick is the shooter's, of the shot's damage type, through the target's resistance and
    /// the PvP damage share (GameEffectManager.TickDamage). A target carries one effect of a type
    /// (GameEffectManager.Attach), so a second hit before the first dose has run out starts it
    /// again with what was still to come added to it: none of either shot's damage is lost, and
    /// the whole is the latest shooter's. On an enemy player a debuff lasts the PvP share of its
    /// time (Pvp.ScaleDuration); the dose still deals all its ticks, closer together, as each is
    /// already PvP damage.
    /// </summary>
    public static class WeaponDots
    {
        public const int InjectionElectricTypeId = 241;    // INJECTION_GUN_ELECTRIC_EFFECT
        public const int InjectionEmpTypeId = 242;         // INJECTION_GUN_EMP_EFFECT
        public const int InjectionFireTypeId = 243;        // INJECTION_GUN_FIRE_EFFECT
        public const int InjectionIceTypeId = 244;         // INJECTION_GUN_ICE_EFFECT
        public const int InjectionSonicTypeId = 245;       // INJECTION_GUN_SONIC_EFFECT
        public const int InjectionViralTypeId = 246;       // INJECTION_GUN_VIRAL_EFFECT
        public const int NetGunTypeId = 10000016;          // NET_GUN_DOT_EFFECT

        public const int InjectionSharePercent = 50;
        public const int InjectionMs = 6000;
        public const int InjectionTicks = 3;

        public const int NetSharePercent = 25;
        public const int NetTicks = 3;

        /// <summary>What a weapon's shot leaves behind: the effect, the share of the shot's damage it takes, and how long and in how many ticks it deals it.</summary>
        public sealed class Spec
        {
            public int TypeId { get; init; }
            public int SharePercent { get; init; }
            public int DurationMs { get; init; }
            public int Ticks { get; init; }
        }

        /// <summary>The injection gun effect for a damage type; 0 for a type with none.</summary>
        public static int InjectionTypeOf(DamageType damageType) => damageType switch
        {
            DamageType.Electrical => InjectionElectricTypeId,
            DamageType.EMP => InjectionEmpTypeId,
            DamageType.Fire => InjectionFireTypeId,
            DamageType.Ice => InjectionIceTypeId,
            DamageType.Sonic => InjectionSonicTypeId,
            DamageType.Virulent => InjectionViralTypeId,
            _ => 0
        };

        /// <summary>What a shot of a weapon of this skill and damage type leaves behind; null for nothing.</summary>
        public static Spec Of(int skillId, DamageType damageType)
        {
            if (skillId == WeaponSkills.NetGuns)
                return new Spec { TypeId = NetGunTypeId, SharePercent = NetSharePercent, DurationMs = CrowdControl.NetGunRootMs, Ticks = NetTicks };

            if (skillId != WeaponSkills.InjectionGuns)
                return null;

            var typeId = InjectionTypeOf(damageType);

            return typeId == 0 ? null : new Spec { TypeId = typeId, SharePercent = InjectionSharePercent, DurationMs = InjectionMs, Ticks = InjectionTicks };
        }

        /// <summary>Takes the effect's share out of a shot's damage and returns it. The hit keeps at least 1.</summary>
        public static int Split(ref int damage, Spec spec)
        {
            if (spec == null || damage <= 1)
                return 0;

            var share = Math.Min(damage - 1, (int)Math.Round(damage * spec.SharePercent / 100.0));

            damage -= share;

            return Math.Max(0, share);
        }

        /// <summary>How many of an effect's ticks are still to come, the one on its expiry included.</summary>
        public static int TicksLeft(GameEffect effect)
        {
            if (effect.TickIntervalMs <= 0 || effect.IsExpired)
                return 0;

            var half = effect.TickIntervalMs / 2;

            if (effect.NextTickTick > effect.ExpiresTick + half)
                return 0;

            return (int)((effect.ExpiresTick + half - effect.NextTickTick) / effect.TickIntervalMs) + 1;
        }

        /// <summary>A hit that landed: its damage over time on what it hit, taking up what the last dose there had still to do. Null when none went on.</summary>
        public static GameEffect Apply(MapChannel mapChannel, Actor target, Missile missile)
        {
            var spec = missile.Dot;

            if (spec == null || missile.DotDamage <= 0 || !(missile.Source is Manifestation shooter) || target == null
                || target.State == CharacterState.Dead || target.State == CharacterState.Dying)
                return null;

            var now = Environment.TickCount64;
            var total = missile.DotDamage;

            var earlier = target.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == spec.TypeId);

            if (earlier != null)
            {
                total += earlier.TickDamageMin * TicksLeft(earlier);
                GameEffectManager.Instance.DettachEffect(mapChannel, target, earlier);
            }

            var ticks = Math.Max(1, spec.Ticks);
            var interval = Math.Max(1, spec.DurationMs / ticks);
            var perTick = Math.Max(1, (int)Math.Round(total / (double)ticks));
            var damageType = missile.DamageType == 0 ? DamageType.Physical : missile.DamageType;

            var effect = new GameEffect
            {
                TypeId = spec.TypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                ActionId = missile.ActionId,
                SourceId = shooter.EntityId,
                Source = shooter,
                SourceLevel = shooter.Level,
                IsBuff = false,
                AnnounceOnAttach = true,
                TickDamageMin = perTick,
                TickDamageMax = perTick,
                TickDamageType = damageType,
                TickScaleType = 0,          // the weapon's own damage, not scaled again
                TickIntervalMs = interval,
                NextTickTick = now + interval,
                ExpiresTick = now + interval * ticks
            };

            effect.Tooltip["dmgMin"] = perTick;
            effect.Tooltip["dmgMax"] = perTick;
            effect.Tooltip["interval"] = Math.Max(1, interval / 1000);

            GameEffectManager.Instance.Attach(mapChannel, target, effect);

            if (!target.ActiveEffects.ContainsKey(effect.EffectId))
                return null;

            // Shortened on an enemy player: the same ticks in the time it has.
            var length = effect.ExpiresTick - now;

            if (length > 0 && length < (long)interval * ticks)
            {
                effect.TickIntervalMs = (int)Math.Max(1, length / ticks);
                effect.NextTickTick = now + effect.TickIntervalMs;
                effect.ExpiresTick = now + (long)effect.TickIntervalMs * ticks;
            }

            return effect;
        }
    }
}
