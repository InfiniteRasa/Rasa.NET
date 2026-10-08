using System;

namespace Rasa.Managers
{
    /// <summary>
    /// How long a shot is in the air, and what the server does about it.
    ///
    /// The client flies every shot itself. TargetedAction.GetDistanceDelay is the distance to the
    /// target over the weapon's velocity - the velocity column of the weapon class table, which
    /// both ends have, row for row - or over DEFAULT_PROJECTILE_VELOCITY (70 m/s,
    /// shared/gameconstants.py) where the class gives none. A negative velocity (-1: the Laser
    /// Torqueshell rifles, the Lightbender) is a shot that is there at once, the sum coming out
    /// before now. When the shot gets there the client plays what the recovery said of it: the
    /// number, the hit's effects, the death of a deathBlow, and the armour and health bars, which
    /// an update naming who did it (UpdateHealth's whoId, Actor.UpdateAttribute) leaves alone
    /// until that damage is announced.
    ///
    /// So the recovery is not something to hold back for the flight as a rule. Somebody else's
    /// shot - a creature's, another player's - starts on the client when its recovery arrives:
    /// the firing animation, the projectile, and the wait. Held on the server for the flight, it
    /// would be fired late and land later still. A creature's shot therefore resolves as it is
    /// fired, as it always did, and what it does to a player's bars waits on that player's
    /// client (MissileManager.StagedBy).
    ///
    /// A player's own shot is the other case. Their client starts its clock when the trigger is
    /// pulled and shows the result when both the shot has arrived and the server has answered,
    /// so an answer that comes as the shot lands is shown on time - and what the landing does
    /// in the world happens then too: the creature falls, and leaves its loot, when the round
    /// reaches it and not when it leaves the barrel. The limit is the next shot. The client
    /// gives a recovery to the action it has in hand (Actor.Recv_PerformRecovery), which is this
    /// shot only until the weapon is fired again, so a shot is held only if it lands before
    /// that: its flight, and the allowance the server already makes for a queue walked once a
    /// pass and an uneven network (ManifestationManager.ShotTolerance), within the weapon's
    /// refire - which is the client's own pace for the weapon's attack (Retune_weapon_swings).
    /// That is every shot of a launcher, a shotgun, an injection gun or a Torqueshell rifle at
    /// any range they reach, a rifle's to between 28 and 42 m, and a pistol's only close in for
    /// the fast ones. Past that the shot resolves as it is fired, as before.
    /// </summary>
    public static class ShotFlight
    {
        /// <summary>DEFAULT_PROJECTILE_VELOCITY: metres a second for a weapon class that gives no velocity of its own (0).</summary>
        public const double DefaultVelocity = 70.0;

        /// <summary>
        /// Milliseconds a shot takes over this distance from a weapon class of this velocity:
        /// the client's own figure. Nothing for a negative velocity.
        /// </summary>
        public static int Ms(float distance, int velocity)
        {
            if (!float.IsFinite(distance) || distance <= 0 || velocity < 0)
                return 0;

            return (int)Math.Round(distance / (velocity > 0 ? velocity : DefaultVelocity) * 1000.0);
        }

        /// <summary>
        /// How long the server holds a player's shot before it lands: its flight, if the weapon
        /// cannot be fired again before then, and otherwise not at all.
        /// </summary>
        public static int HeldMs(float distance, int velocity, long refireMs)
        {
            var flight = Ms(distance, velocity);

            return flight > 0 && flight + ManifestationManager.ShotTolerance <= refireMs ? flight : 0;
        }
    }
}
