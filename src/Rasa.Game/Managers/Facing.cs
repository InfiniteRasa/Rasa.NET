using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// Which way an actor faces, and whether an attacker stands behind it - what Blades' backstab
    /// asks ("Backstab Damage (attacking a target from behind)").
    ///
    /// Both kinds of actor keep a yaw in the same convention, facing along (-sin yaw, -cos yaw):
    /// a player's Rotation is the client's Movement.ViewDirection.X, and a creature's LastYaw is
    /// what BehaviorManager.UpdateEntityMovement set from atan2(-dx, -dz) as it last turned
    /// towards where it was going or what it was fighting.
    ///
    /// Behind is the rear half: the attacker is further back than level with the target's
    /// shoulders. The client gives no arc, so this is a choice - and a creature fighting you turns
    /// to face you, so a backstab wants a creature busy with somebody else, or held still ("Use
    /// Netgun to hold a target still while delivering devastating backstabs with the Blade").
    /// </summary>
    public static class Facing
    {
        /// <summary>The way the actor faces, flat, as a unit vector.</summary>
        public static Vector2 Of(Actor actor)
        {
            var yaw = actor is Creature creature ? creature.LastYaw : actor.Rotation;

            return new Vector2((float)-Math.Sin(yaw), (float)-Math.Cos(yaw));
        }

        /// <summary>Whether a point lies behind something at <paramref name="origin"/> facing along <paramref name="facing"/>.</summary>
        public static bool IsBehind(Vector3 origin, Vector2 facing, Vector3 point)
        {
            var toPoint = new Vector2(point.X - origin.X, point.Z - origin.Z);

            // Standing on top of it is not behind it.
            if (toPoint.LengthSquared() < 0.01f)
                return false;

            return Vector2.Dot(facing, toPoint) < 0;
        }

        /// <summary>Whether the attacker stands behind the target.</summary>
        public static bool IsBehind(Actor attacker, Actor target)
        {
            return attacker != null && target != null && IsBehind(target.Position, Of(target), attacker.Position);
        }

        /// <summary>
        /// Whether a hit from behind is worth a backstab on this target: it is behind, and the
        /// target is not one of the creatures the client marks REAR_POSITION_BONUS_IMMUNITY.
        /// </summary>
        public static bool CanBackstab(Actor attacker, Actor target)
        {
            if (!IsBehind(attacker, target))
                return false;

            return !(target is Creature creature)
                || !CreatureManager.CreatureFlagsOf(creature).Contains((int)CreatureFlag.RearPositionBonusImmunity);
        }
    }
}
