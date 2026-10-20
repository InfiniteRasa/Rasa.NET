using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// What a creature's kill is worth in experience against an ordinary one of its level: the
    /// XP_MODIFIER_* creature flag on its class or on itself. The flag names give the factor and
    /// nothing else in the client or the strategy guide does:
    ///
    ///   XP_MODIFIER_0_NONE ×0, 1_QUARTER ×¼, 2_HALF ×½, 3_NORMAL ×1, 3A_ONE_AND_A_HALF ×1½,
    ///   4_DOUBLE ×2, 4A_TRIPLE ×3, 5_QUADRUPLE ×4.
    ///
    /// A creature with none is ×1. One with more than one - a class's and a creature's own -
    /// takes the smallest, so a flag that is there to take experience away is never outvoted.
    ///
    /// The factor scales the kill's experience before it is shared out (KillShares) and before
    /// anyone's modules add to it, and a Critical Death finish pays the scaled amount twice.
    /// A kill worth nothing (×0) pays no experience and is not a kill in anyone's kill streak,
    /// whose whole worth is experience (KillStreaks); its adrenaline, loot and mission credit
    /// are not experience and are paid as for any kill.
    /// </summary>
    public static class CreatureExperience
    {
        /// <summary>Each flag's factor, in percent.</summary>
        public static readonly IReadOnlyDictionary<CreatureFlag, int> Percents = new Dictionary<CreatureFlag, int>
        {
            [CreatureFlag.XpModifier0None] = 0,
            [CreatureFlag.XpModifier1Quarter] = 25,
            [CreatureFlag.XpModifier2Half] = 50,
            [CreatureFlag.XpModifier3Normal] = 100,
            [CreatureFlag.XpModifier3aOneAndAHalf] = 150,
            [CreatureFlag.XpModifier4Double] = 200,
            [CreatureFlag.XpModifier4aTriple] = 300,
            [CreatureFlag.XpModifier5Quadruple] = 400
        };

        /// <summary>The creature's experience factor in percent: its XP_MODIFIER flag's, the smallest of several, 100 for none.</summary>
        public static int PercentOf(Creature creature)
        {
            int? percent = null;

            foreach (var (flag, value) in Percents)
                if (CreatureManager.HasFlag(creature, flag) && (percent == null || value < percent))
                    percent = value;

            return percent ?? 100;
        }

        /// <summary>A kill's experience as the creature's factor makes it, rounded to the nearest.</summary>
        public static uint Apply(Creature creature, uint experience)
        {
            var percent = PercentOf(creature);

            if (percent == 100 || experience == 0)
                return experience;

            return (uint)Math.Min(uint.MaxValue, Math.Round(experience * (double)percent / 100.0, MidpointRounding.AwayFromZero));
        }
    }
}
