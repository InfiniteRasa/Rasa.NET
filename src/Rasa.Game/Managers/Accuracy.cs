using System;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The aiming bead, kept on the server the way the client keeps it, so that the server knows
    /// when a shot was fired at full bead. The client never says: RequestWeaponAttack carries the
    /// action, its argument and the target, and the bead lives only in the client's
    /// Manifestation.UpdateAccuracy (client/augmentations/manifestation.py). This is that code,
    /// with the same constants from shared/gameconstants.py:
    ///
    ///  - accuracy runs from 0 to 100; the bead is full above 99 ("accuracyRatio > 0.99" in
    ///    reticlewindow.py, which is what shows the full-bead reticle);
    ///  - it climbs towards a ceiling at the weapon's aim rate per millisecond, twice that while
    ///    crouched (CROUCHED_ACCURACY_MOD 2.0), and falls at ACCURACY_LOSS_RATE (50 a second) when
    ///    it is above the ceiling;
    ///  - the ceiling is 100 crouched (CROUCHED_ACCURACY_MAX) and 80 standing still or walking
    ///    (STOPPED / SLOW_ACCURACY_MAX), 64 running (FAST_ACCURACY_MAX);
    ///  - each shot takes the weapon's recoil amount off it (baseweaponattack.py LocalDoAction);
    ///  - drawing a weapon starts it from 0 (actor.py, on an equipment update with a weapon);
    ///  - with no target the ceiling is 0 (UpdateAccuracyRates: "if self.GetTargetId() is None:
    ///    self.accuracyMaxValue = 0"), so the bead runs down, and starts building when a target
    ///    is picked - "Beading begins as soon as you target an enemy" (the strategy guide).
    ///
    /// The bead also sets the damage of a shot. The guide: "Failing to allow any bead time
    /// reduces the damage your attack delivers by a whopping 90% due to poor aim. Attacks with
    /// partial beading deliver greater damage, even if you can't afford to wait for a perfect
    /// shot." So a shot does NoBeadDamage of its damage at no bead, rising in a straight line to
    /// all of it at a full bead of 100 - the bead itself, not its share of the stance's ceiling.
    /// That is the reticle: its lines sit at accuracy / 100 of the way in (reticlewindow.py,
    /// Manifestation.accuracyRatio), and "the amount of damage your gun will do increases the
    /// closer these lines get to the center of the targeting reticule". Standing, they stop at 80
    /// and the shot does 82%; crouched they reach the centre and it does all of it - "Crouching
    /// increases your damage but also makes you easier to hit unless you are behind cover", and
    /// gets there twice as fast. Only a crouched shot is a full-bead shot, with its crit. The
    /// straight line is ours; the guide gives the two ends.
    ///
    /// The rates are worked out again whenever what they depend on changes - crouching, the
    /// target, the weapon - as UpdateAccuracyRates is on the client. Running is not told apart
    /// from walking: only crouching lifts the ceiling past 99, so whether a standing player could
    /// have reached 80 or only 64 never decides whether a shot was at full bead.
    ///
    /// A cone weapon - a shotgun or a propellant gun (ConeWeapons) - has no bead to build. The
    /// client's cone targeting clears the target as it starts (targeting.py: ClearDirectTarget),
    /// so its ceiling is 0, and the cone reticle's bead lines never move. Its shot does what a
    /// full bead from the same stance does instead (<see cref="ConeDamageFactor"/>): 82% standing,
    /// all of it crouched. Held to the bead, it did a tenth of its damage every time.
    ///
    /// The aim rate and recoil come from the weapon template, which is also what the client is
    /// sent in WeaponInfo, so both ends run the same bead. The aim rates are the guide's
    /// comparative bead times by weapon family (Retune_weapon_bead); recoil is still the shipped
    /// placeholder, 1.
    /// </summary>
    public static class Accuracy
    {
        public const double FullBeadRatio = 0.99;           // reticlewindow.py
        public const double LossPerMs = 50.0 / 1000.0;      // ACCURACY_LOSS_RATE
        public const double CrouchedMax = 100;              // CROUCHED_ACCURACY_MAX
        public const double CrouchedRateMod = 2.0;          // CROUCHED_ACCURACY_MOD
        public const double StandingMax = 80;               // STOPPED_ACCURACY_MAX, SLOW_ACCURACY_MAX
        public const double StandingRateMod = 1.0;          // STOPPED_ACCURACY_MOD

        /// <summary>The share of its damage a shot does with no bead at all: "reduces the damage ... by a whopping 90%".</summary>
        public const double NoBeadDamage = 0.10;

        /// <summary>Where the bead is now, brought up to date from the last time it was looked at.</summary>
        public static double Current(Manifestation player, long now)
        {
            var elapsed = Math.Max(0, now - player.AccuracyUpdatedTick);
            var amount = elapsed * player.AccuracyRate;

            if (amount > 0 && player.AccuracyValue < player.AccuracyMax)
                player.AccuracyValue = Math.Min(player.AccuracyValue + amount, player.AccuracyMax);

            if (amount < 0 && player.AccuracyValue > player.AccuracyMax)
                player.AccuracyValue = Math.Max(player.AccuracyValue + amount, player.AccuracyMax);

            player.AccuracyUpdatedTick = now;

            return player.AccuracyValue;
        }

        public static bool IsFullBead(Manifestation player, long now) => Current(player, now) / 100.0 > FullBeadRatio;

        /// <summary>
        /// What share of its damage a shot fired now does: NoBeadDamage with no bead, all of it at
        /// a full bead of 100 (only crouched can reach it), in a straight line between. Standing
        /// tops out at 80, and 82% of the damage.
        /// </summary>
        public static double DamageFactor(Manifestation player, long now)
        {
            var bead = Current(player, now);

            if (player.AccuracyMax <= 0)
                return NoBeadDamage;

            return NoBeadDamage + (1 - NoBeadDamage) * Math.Min(1.0, bead / CrouchedMax);
        }

        /// <summary>
        /// What share of its damage a cone weapon's shot does: what a full bead from the stance
        /// would - the stance's ceiling, 80 standing and 100 crouched, on the same line as
        /// <see cref="DamageFactor"/> - as there is no bead to wait for.
        /// </summary>
        public static double ConeDamageFactor(Manifestation player)
        {
            var ceiling = player.IsCrouching ? CrouchedMax : StandingMax;

            return NoBeadDamage + (1 - NoBeadDamage) * Math.Min(1.0, ceiling / CrouchedMax);
        }

        /// <summary>
        /// The ceiling and the rate towards it, worked out again: the player crouched or stood,
        /// changed target or weapon. The bead is brought up to date first, so the time before the
        /// change counts at the old rate.
        /// </summary>
        public static void UpdateRates(Manifestation player, double aimRate, long now)
        {
            Current(player, now);

            var crouched = player.IsCrouching;

            player.AccuracyMax = player.Target == 0 ? 0 : crouched ? CrouchedMax : StandingMax;

            if (player.AccuracyMax < player.AccuracyValue)
                player.AccuracyRate = -LossPerMs;
            else if (player.AccuracyMax > player.AccuracyValue)
                player.AccuracyRate = Math.Max(0, aimRate) * (crouched ? CrouchedRateMod : StandingRateMod);
        }

        /// <summary>A shot's recoil comes off the bead.</summary>
        public static void Recoil(Manifestation player, double recoil, double aimRate, long now)
        {
            Current(player, now);

            player.AccuracyValue = Math.Max(0, player.AccuracyValue - Math.Max(0, recoil));

            UpdateRates(player, aimRate, now);
        }

        /// <summary>A weapon drawn: the bead starts again from nothing.</summary>
        public static void Reset(Manifestation player, double aimRate, long now)
        {
            player.AccuracyValue = 0;
            player.AccuracyUpdatedTick = now;

            UpdateRates(player, aimRate, now);
        }
    }
}
