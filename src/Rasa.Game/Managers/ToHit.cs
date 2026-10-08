using System;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// Whether a weapon's shot hits.
    ///
    /// shared/gameconstants.py, which the client shares with the server it was written for, has
    /// the figures, and nothing in the client reads them: BASE_WEAPON_TO_HIT 100,
    /// TARGET_WALKING_TOHIT_MODIFIER -7 and TARGET_RUNNING_TOHIT_MODIFIER -15. So a shot at
    /// something standing still does not miss, one at something walking misses 7 times in a
    /// hundred and one at something running 15. How the original put them together is in
    /// nothing we have. They are added here, as "modifier" says, along with the one other to-hit
    /// modifier the data gives: Chaff's DEFENSIVE_TOHIT_MODIFIER (GameEffect.MissPercent), which
    /// was a roll of its own until there was this one for it to move.
    ///
    /// Whose shots: every weapon attack that is a shot - a player's or a creature's, at either,
    /// the constants saying "target" and nothing of who is shooting. Not a swing (IsMelee),
    /// which Chaff does not turn either. And not a creature's ability, which is no weapon and
    /// misses in its own way (CreatureWindups): for one of those the chance is what Chaff
    /// leaves of it, as it was.
    ///
    /// Only the one aimed at is rolled for. A launcher's round that misses still goes off where
    /// it comes down, and a cone weapon's other victims are caught as they were. The client's
    /// SHARE_MISSES weapon property says the original had a rule for them; its values are in
    /// nothing we have.
    ///
    /// Moving is the last step the target took: a creature's last published movement, a
    /// player's last accepted Move that both carried a velocity and took them somewhere.
    /// Running or walking is Actor.IsRunning - a creature's gait, a player's run toggle - which
    /// are the client's own _fast and _slow (Manifestation.UpdateAccuracyRates).
    ///
    /// A weapon's roll is made as it is fired (MissileManager.MissileLaunch), on how its target
    /// is moving then. A miss with nothing else to land is not held for its flight
    /// (ShotFlight): the shooter's client is already flying the round at its target, and is
    /// told it is a miss while that can still be shown (TargetedAction.DoAction hands the
    /// effect its new targets).
    /// </summary>
    public static class ToHit
    {
        /// <summary>BASE_WEAPON_TO_HIT.</summary>
        public const int Base = 100;

        /// <summary>TARGET_WALKING_TOHIT_MODIFIER.</summary>
        public const int TargetWalking = -7;

        /// <summary>TARGET_RUNNING_TOHIT_MODIFIER.</summary>
        public const int TargetRunning = -15;

        /// <summary>The roll: whether a chance of this many in a hundred comes up. Replaced in tests.</summary>
        internal static Func<int, bool> Roll { get; set; } = Stuns.Roll;

        /// <summary>Whether the actor's last step was one that took it somewhere.</summary>
        public static bool IsMoving(Actor actor) => actor switch
        {
            Creature creature => creature.Controller?.LastMovement is { Velocity: > 0 },
            Manifestation player => player.MoveVelocity > 0,
            _ => false
        };

        /// <summary>What the way the target is moving takes off a shot at it: nothing standing, TargetWalking, TargetRunning.</summary>
        public static int MovementModifier(Actor target)
        {
            if (!IsMoving(target))
                return 0;

            return target.IsRunning ? TargetRunning : TargetWalking;
        }

        /// <summary>The chance in a hundred that the missile hits what it is aimed at.</summary>
        public static int ChanceOf(Missile missile)
        {
            if (missile == null || missile.IsMelee || missile.TargetActor == null)
                return 100;

            var chance = Base - GameEffectManager.MissPercentOf(missile.TargetActor);

            if (CreatureAttacks.IsWeaponAttack(missile))
                chance += MovementModifier(missile.TargetActor);

            return Math.Max(0, Math.Min(100, chance));
        }

        /// <summary>Rolls for the missile: whether it goes wide.</summary>
        public static bool Misses(Missile missile)
        {
            var miss = 100 - ChanceOf(missile);

            return miss > 0 && Roll(miss);
        }
    }
}
