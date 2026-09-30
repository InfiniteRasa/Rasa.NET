using System;
using System.Threading;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// The critical hit roll. The client never rolled one - it is told the outcome in each hit's
    /// isCrit - but it carries the constants the server rolled with (shared/gameconstants.py):
    ///
    ///  - BASE_CRITICAL_CHANCE = 5: every attack starts at 5%;
    ///  - SPIRIT_CRIT_DIVISOR = 15.0: Spirit "primarily increases ... chance for Critical Hit with
    ///    all weapon and ability attacks", a percent per 15 points. Taken over the base Spirit for
    ///    the level, as UpdateStatsValues does for Body's armour (its 0.667 is 1 / BODY_ARMOR_DIVISOR);
    ///  - CROUCHED_RANGED_TO_CRIT = 5: crouching adds 5% to a ranged attack's chance;
    ///  - CROUCHED_MELEE_TO_BE_CRIT_MOD = 5: and adds 5% to the chance of being critically hit
    ///    by a melee attack;
    ///  - CRITICAL_DAMAGE_MODIFIER = 1.5, PVP_CRITICAL_DAMAGE_MODIFIER = 1.25: what a crit does to
    ///    the damage. The strategy guide calls a crit "double damage"; the client says 1.5.
    ///
    /// shared/damageinfo.py orders the server's steps doCrit, doReflect, doAbsorb, doResist, so the
    /// crit multiplies the rolled damage before anything takes a share of it.
    /// </summary>
    public static class CriticalHits
    {
        public const double BaseChance = 5;
        public const double SpiritDivisor = 15.0;
        public const double CrouchedRangedBonus = 5;
        public const double CrouchedMeleeTakenBonus = 5;
        public const double DamageModifier = 1.5;
        public const double PvpDamageModifier = 1.25;

        /// <summary>Firearms on a rifle, by pump: "Rifles: +3% Crit Hit" at 3, +5% at 4, +7% at 5.</summary>
        private static readonly int[] FirearmsRifleByPump = { 0, 0, 0, 3, 5, 7 };

        /// <summary>"Rifles: +3% Crit Hit (+5% with full bead)", +5% (+10%), +7% (+15%).</summary>
        private static readonly int[] FirearmsRifleFullBeadByPump = { 0, 0, 0, 5, 10, 15 };

        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        /// <summary>The Spirit a character of this level has before anything is spent on it, as UpdateStatsValues reckons it.</summary>
        public static int BaseSpirit(int level) => 2 * (Math.Max(1, level) - 1) + 10;

        /// <summary>Percent crit chance from Spirit above the level's base.</summary>
        public static double SpiritChance(int spirit, int level) => Math.Max(0, spirit - BaseSpirit(level)) / SpiritDivisor;

        /// <summary>Firearms' rifle crit bonus at this pump, in percent.</summary>
        public static int FirearmsRifleChance(int pump) => FirearmsRifleByPump[Math.Max(0, Math.Min(5, pump))];

        /// <summary>Firearms' rifle crit bonus at this pump for a shot fired at full bead, in percent - in place of the other, not on top.</summary>
        public static int FirearmsRifleFullBeadChance(int pump) => FirearmsRifleFullBeadByPump[Math.Max(0, Math.Min(5, pump))];

        /// <summary>
        /// The attacker's own chance, in percent: base, Spirit, the effects on them (Crit Wave),
        /// crouching for a ranged attack, and whatever the attack itself adds (bonus).
        /// </summary>
        public static double AttackerChance(Actor source, bool melee, double bonus = 0)
        {
            if (source == null)
                return 0;

            var chance = BaseChance + bonus + GameEffectManager.CritChancePercentOf(source);

            if (source is Manifestation player && player.Attributes.TryGetValue(Attributes.Spirit, out var spirit))
                chance += SpiritChance(spirit.CurrentMax, player.Level);

            if (!melee && source.IsCrouching)
                chance += CrouchedRangedBonus;

            return chance;
        }

        /// <summary>What the target adds to the chance of being critically hit: crouching against a melee attack.</summary>
        public static double TargetChance(Actor target, bool melee)
        {
            return melee && target != null && target.IsCrouching ? CrouchedMeleeTakenBonus : 0;
        }

        /// <summary>A crit on player against player is the smaller PvP multiplier.</summary>
        public static bool IsPvp(Actor source, Actor target) => source is Manifestation && target is Manifestation;

        /// <summary>The damage a crit does from the damage the attack rolled.</summary>
        public static int Apply(int amount, bool pvp)
        {
            return (int)Math.Round(amount * (pvp ? PvpDamageModifier : DamageModifier), MidpointRounding.AwayFromZero);
        }

        /// <summary>Whether a roll at this percent chance comes up a crit.</summary>
        public static bool Roll(double chancePercent)
        {
            if (chancePercent <= 0)
                return false;

            return Rng.Value.NextDouble() * 100.0 < chancePercent;
        }

        /// <summary>
        /// The whole roll for one attack on one target: chance from both sides, and on a crit the
        /// damage multiplied. Returns whether it was a crit.
        /// </summary>
        public static bool Resolve(Actor source, Actor target, bool melee, double attackerChance, ref int amount)
        {
            if (amount <= 0 || !Roll(attackerChance + TargetChance(target, melee)))
                return false;

            amount = Apply(amount, IsPvp(source, target));
            return true;
        }
    }
}
