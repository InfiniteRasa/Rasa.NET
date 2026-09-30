using System;
using System.Numerics;

namespace Rasa.Managers
{
    /// <summary>
    /// A weapon's range is its optimal range, not a limit. The strategy guide (p14): "In most
    /// games, this stat tells you the weapon's maximum range, beyond which the weapon delivers
    /// zero damage. That isn't the case here. Tabula Rasa lets you fire past a weapon's optimal
    /// range, but the damage it delivers drops off severely." The client's tooltip calls the
    /// same figure "Optimal range" (ID_TOOLTIP_WEAPON_OPTIMAL_RANGE), and it is the template's
    /// range - itemtemplate_weapon.range, the attack action's max_range.
    ///
    /// The shape of the drop is not in the client, so it is chosen here: full damage out to the
    /// optimal range, falling in a straight line to <see cref="FarFactor"/> at
    /// <see cref="FarMultiple"/> times it, and <see cref="FarFactor"/> beyond. A 20 m pistol does
    /// full damage at 20 m, 62.5% at 30 m and a quarter from 40 m out. The guide's advice to stand
    /// "about two meters closer than the range rating" is what keeps a target that moves about
    /// inside the full-damage band.
    ///
    /// Distance is centre to centre, shooter to the creature hit - for a launcher's splash, to
    /// the creature the round was fired at, since that is how far the round flew. Players'
    /// weapons only: a creature's attacks have their own reach.
    /// </summary>
    public static class RangeFalloff
    {
        /// <summary>Ours: how many times the optimal range the drop reaches its floor at.</summary>
        public const double FarMultiple = 2.0;

        /// <summary>Ours: the share of its damage a shot keeps at <see cref="FarMultiple"/> times the optimal range and beyond.</summary>
        public const double FarFactor = 0.25;

        /// <summary>The share of its damage a shot keeps at this distance, 1 within the optimal range. No optimal range, no drop.</summary>
        public static double Factor(float distance, float optimalRange)
        {
            if (optimalRange <= 0 || distance <= optimalRange)
                return 1.0;

            var far = optimalRange * FarMultiple;

            if (distance >= far)
                return FarFactor;

            return 1.0 - (1.0 - FarFactor) * (distance - optimalRange) / (far - optimalRange);
        }

        /// <summary>A shot's damage as it lands from <paramref name="from"/> on <paramref name="to"/>; never less than 1 from a shot that did any.</summary>
        public static int Scale(int damage, float optimalRange, Vector3 from, Vector3 to)
        {
            if (damage <= 0)
                return damage;

            var factor = Factor(Vector3.Distance(from, to), optimalRange);

            return factor >= 1.0 ? damage : Math.Max(1, (int)Math.Round(damage * factor));
        }
    }
}
