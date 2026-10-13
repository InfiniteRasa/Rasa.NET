using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// A creature whose target has come inside the minimum range of every attack it has backs
    /// off, facing its target, until one of them can be used again - if its class can walk
    /// backwards (CAN_WALK_BACKWARDS, creature flag 53, on the class or the creature's own
    /// ExtraFlags). One that cannot stands where it is, as before, and gives the fight up if
    /// the target stays there (BehaviorManager.Stalled).
    ///
    /// - Its minimum is the smallest RangeMin of the attacks it aims at its target
    ///   (<see cref="IsAimedAttack"/>), ready or cooling down: where it stands, not what it does
    ///   next. A creature with any attack that has no minimum has nothing to back off for.
    /// - It backs off straight away from its target, at its walking speed, to
    ///   <see cref="Margin"/> past that minimum - never past the middle of that attack's range -
    ///   facing its target all the way. The clients are told it is moving backwards
    ///   (movement direction 4, <see cref="BackwardDirection"/>), which plays the class's
    ///   backward walk.
    /// - Only along open ground: on a map with a navmesh, the way back has to be one straight
    ///   walk to a point on the mesh (<see cref="IsClear"/>). Against a wall or a drop it stands.
    /// - A target standing right on top of it: it backs off the way it is facing from.
    ///
    /// A step that gained ground keeps the fight going (BehaviorManager.MadeProgress). A target
    /// that keeps following is followed until the chase leash takes the creature home, as any
    /// chase is.
    /// </summary>
    public static class CreatureStepBack
    {
        /// <summary>How far past the minimum it backs off to: not stopping on the edge, where the next step of its target puts it inside again.</summary>
        public const float Margin = 0.5f;

        /// <summary>
        /// The movement direction the clients' controller keeps in bits 0 to 2 of a movement's
        /// flags: 0 forward, 2 right, 4 backward, 6 left (animationdata.movementDirection, its
        /// translation direction in eighths of a turn).
        /// </summary>
        public const byte BackwardDirection = 4;

        /// <summary>A target nearer than this is on top of the creature: there is no "away" from it.</summary>
        public const float OnTopDistance = 0.1f;

        /// <summary>How near the end of a straight navmesh path has to come to where it was asked to go for the way back to be clear.</summary>
        public const float ClearTolerance = 0.3f;

        /// <summary>Whether the creature can walk backwards: CAN_WALK_BACKWARDS on its class or on itself.</summary>
        public static bool CanWalkBackwards(Creature creature)
        {
            return CreatureManager.HasFlag(creature, CreatureFlag.CanWalkBackwards);
        }

        /// <summary>
        /// Whether the action is one the creature aims at its target from where it stands: an
        /// attack with a range. Not a heal, a habit, a Shield Drone's heal, a buff on itself or its
        /// side, a summon, an egg charge, a body blown up, a Miasma's cloud, a Fithik blowing
        /// itself up or an Amoeboid's vomit - none of which cares how near the target is.
        /// </summary>
        public static bool IsAimedAttack(CreatureAction action)
        {
            return action != null
                && action.RangeMax > 0
                && !CreatureSupport.Is(action)
                && !CreatureHabits.Is(action)
                && action.ActionId != ShieldDrone.HealAction
                && !CreatureBuffs.Is(action)
                && !CreatureSummons.IsSummon(action)
                && !CreatureBombs.IsOvulate(action)
                && !CreatureBombs.IsCorpseExplosion(action)
                && !CreatureBombs.IsSelfDestruct(action)
                && !CreatureMiasma.Is(action)
                && !AmoeboidVomit.IsVomit(action);
        }

        /// <summary>
        /// The aimed attack with the smallest minimum range (the longer reach between equals), or
        /// null when the creature has none, or has one with no minimum at all.
        /// </summary>
        public static CreatureAction NearestAttack(Creature creature)
        {
            CreatureAction nearest = null;

            foreach (var action in creature.Actions)
            {
                if (!IsAimedAttack(action))
                    continue;

                if (action.RangeMin <= 0)
                    return null;

                if (nearest == null || action.RangeMin < nearest.RangeMin
                    || (action.RangeMin == nearest.RangeMin && action.RangeMax > nearest.RangeMax))
                    nearest = action;
            }

            return nearest;
        }

        /// <summary>The nearest a target may be for the creature to use any of its attacks; 0 when there is no such limit.</summary>
        public static float MinimumRangeOf(Creature creature)
        {
            return (float)(NearestAttack(creature)?.RangeMin ?? 0);
        }

        /// <summary>How far from its target it backs off to: <see cref="Margin"/> past its minimum, and no further than the middle of that attack's range.</summary>
        public static float BackOffDistanceOf(CreatureAction nearest)
        {
            return (float)Math.Min(nearest.RangeMin + Margin, (nearest.RangeMin + nearest.RangeMax) / 2);
        }

        /// <summary>
        /// Whether the creature backs off from a target <paramref name="distance"/> away: it can
        /// walk backwards and walk at all, nothing holds it, and the target is inside the minimum
        /// range of every attack it has.
        /// </summary>
        public static bool Wants(Creature creature, float distance)
        {
            if (creature?.Controller == null || !CanWalkBackwards(creature))
                return false;

            if (creature.WalkSpeed < 0.01f || creature.MovementSpeed <= 0 || CrowdControl.IsRooted(creature))
                return false;

            var minimum = MinimumRangeOf(creature);

            return minimum > 0 && distance < minimum;
        }

        /// <summary>
        /// Where the creature backs off to from a target at <paramref name="target"/>: straight
        /// away from it across the ground, <see cref="BackOffDistanceOf"/> from it, at the
        /// creature's own height (the caller puts it on the ground). Null when it has nothing to
        /// back off for.
        /// </summary>
        public static Vector3? Destination(Creature creature, Vector3 target)
        {
            var nearest = NearestAttack(creature);

            if (nearest == null)
                return null;

            var away = new Vector2(creature.Position.X - target.X, creature.Position.Z - target.Z);

            // On top of it: back the way it faces from. Facing (fx, fz) is the yaw
            // atan2(-fx, -fz) (BehaviorManager.UpdateEntityMovement), so behind it is
            // (sin yaw, cos yaw).
            away = away.Length() < OnTopDistance
                ? new Vector2((float)Math.Sin(creature.LastYaw), (float)Math.Cos(creature.LastYaw))
                : Vector2.Normalize(away);

            var distance = BackOffDistanceOf(nearest);

            return new Vector3(target.X + away.X * distance, creature.Position.Y, target.Z + away.Y * distance);
        }

        /// <summary>
        /// Whether the way from <paramref name="from"/> to <paramref name="to"/> is one straight
        /// walk over the navmesh, ending where it was meant to. Always, on a map with no navmesh.
        /// </summary>
        public static bool IsClear(MapChannel mapChannel, Vector3 from, Vector3 to)
        {
            if (mapChannel?.NavMesh == null)
                return true;

            var path = NavMeshManager.FindPath(mapChannel, from, to);

            if (path == null || path.Count != 1)
                return false;

            var end = path[0];
            var dx = end.X - to.X;
            var dz = end.Z - to.Z;

            return dx * dx + dz * dz <= ClearTolerance * ClearTolerance;
        }
    }
}
