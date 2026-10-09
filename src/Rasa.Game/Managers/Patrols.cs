using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Structures;
    using Structures.World;

    /// <summary>
    /// An NPC walking a beat: a sentry pacing a wall, an officer walking up and down a line of
    /// recruits and stopping to look them over.
    ///
    /// The beat is the pool's (spawnpool_patrol): steps in order, each a point, optionally a
    /// facing and a pause. The pool's creature is given it as it is made, and walks it whenever
    /// it has nothing else to do (BehaviorManager.AdvancePatrol): to each step in turn and from
    /// the last back to the first, round and round.
    ///
    ///  - It walks at its own walk speed (creature.walk_speed, whole metres a second) in a
    ///    straight line from where it is to the step, heights and all, and is not put on the
    ///    navmesh as it goes: the steps are floor heights, as a standing NPC's point is
    ///    (SpawnPoolManager.SpawnPoint), and the mesh is not the floor. Ground that rises or
    ///    falls between two steps wants a step where it changes.
    ///  - On a step it stops. If the step has a facing it turns to it, then stands out the
    ///    step's pause, then goes on.
    ///  - It turns where it stands before it walks: when the next step is more than
    ///    <see cref="TurnInPlace"/> off the way it faces - the about-turn at the end of a line,
    ///    coming out of a stop where it looked aside - it is turned first and walks when the
    ///    turn is made. A step with no facing and no pause that the beat carries straight on
    ///    through is walked through without a stop.
    ///  - The client makes a turn itself, at its class's turn rate, when a standing creature is
    ///    sent a new yaw; the server sends the one movement and waits <see cref="TurnMs"/>.
    ///
    /// The beat is left for anything else - a fight, a run home, being thrown - and its place in
    /// it is kept (ActionPatrol). Idle again, or found somewhere the patrol did not leave it, the
    /// creature walks to the step it was going to as any creature walks, across the navmesh, and
    /// is on the beat again within <see cref="RejoinDistance"/> of it.
    ///
    /// A beat walked there and back is its two ends; a stop made in one direction only is a step
    /// between them on that side of the circuit. One creature to a beat: several would walk it
    /// on top of each other. A creature with no walk speed has no beat, and a pool with a beat
    /// has no pose (NpcPoses), which is for standing at a post.
    /// </summary>
    public static class Patrols
    {
        /// <summary>
        /// How fast a creature is taken to turn where it stands, radians a second: a whole turn.
        /// The client's entitymovementrate gives each class a slow and a fast turn rate in
        /// degrees a second, the fast one 360 for nearly all of them, and a creature's movement
        /// asks for the fast one (Movement.FastTurn).
        /// </summary>
        public const float TurnRate = 2f * MathF.PI;

        /// <summary>A change of heading up to this is made on the move, as any walk makes it; past it, the creature stops and turns first. 20 degrees.</summary>
        public const float TurnInPlace = 0.35f;

        /// <summary>A facing within this of the one asked for is that facing: no turn is sent.</summary>
        public const float FacingTolerance = 0.01f;

        /// <summary>How near a step, across the ground, a creature off its beat stops crossing the navmesh and walks straight to it.</summary>
        public const float RejoinDistance = 1f;

        /// <summary>Further than this from where the patrol left it, something else has moved the creature.</summary>
        public const float MovedTolerance = 0.05f;

        /// <summary>Whether the creature has a beat it can walk.</summary>
        public static bool Has(Creature creature) =>
            creature?.Patrol != null && creature.Patrol.Count > 0 && creature.WalkSpeed >= 0.01f;

        /// <summary>
        /// The beats in spawnpool_patrol, by pool, each in the order its steps are walked. A row
        /// that is not a place - a coordinate or a facing that is no number - is logged and its
        /// pool has no beat: a beat with a step missing is another beat.
        /// </summary>
        public static Dictionary<uint, List<PatrolStep>> FromRows(IEnumerable<SpawnPoolPatrolEntry> rows)
        {
            var patrols = new Dictionary<uint, List<PatrolStep>>();

            foreach (var pool in rows.GroupBy(row => row.PoolId))
            {
                var steps = new List<PatrolStep>();

                foreach (var row in pool.OrderBy(row => row.Step))
                {
                    if (!double.IsFinite(row.PosX) || !double.IsFinite(row.PosY) || !double.IsFinite(row.PosZ)
                        || (row.Facing is double facing && !double.IsFinite(facing)))
                    {
                        Logger.WriteLog(LogType.Error, $"spawnpool_patrol {row.PoolId} step {row.Step} is not a place. The pool has no patrol.");
                        steps = null;
                        break;
                    }

                    steps.Add(new PatrolStep(row.Position, row.Facing is double yaw ? (float)yaw : null, row.PauseMs));
                }

                if (steps != null && steps.Count > 0)
                    patrols[pool.Key] = steps;
            }

            return patrols;
        }

        /// <summary>The turn from one yaw to another, the short way round: radians, from -pi to pi.</summary>
        public static float Turn(float from, float to)
        {
            var turn = (to - from) % (2f * MathF.PI);

            if (turn > MathF.PI)
                turn -= 2f * MathF.PI;
            else if (turn < -MathF.PI)
                turn += 2f * MathF.PI;

            return turn;
        }

        /// <summary>How long a turn of this many radians is given, in milliseconds.</summary>
        public static long TurnMs(float turn) => (long)MathF.Ceiling(MathF.Abs(turn) / TurnRate * 1000f);

        /// <summary>
        /// Whether a creature that has walked onto this step facing <paramref name="yaw"/> simply
        /// keeps walking: the step asks for no facing and no pause, and the next one is ahead of it.
        /// </summary>
        public static bool WalksOn(IReadOnlyList<PatrolStep> steps, int index, float yaw)
        {
            var step = steps[index];

            if (steps.Count < 2 || step.Facing.HasValue || step.PauseMs > 0)
                return false;

            var next = steps[(index + 1) % steps.Count];
            var dx = next.Position.X - step.Position.X;
            var dz = next.Position.Z - step.Position.Z;

            if (dx * dx + dz * dz < 0.0001f)
                return false;

            return MathF.Abs(Turn(yaw, AbilityManager.YawTowards(step.Position, next.Position))) <= TurnInPlace;
        }

        /// <summary>Where a creature is on its beat, for a GM: .npcinfo.</summary>
        public static string Describe(Creature creature)
        {
            if (creature?.Patrol == null)
                return "none";

            var patrol = creature.Controller.ActionPatrol;
            var count = creature.Patrol.Count;
            var step = $"step {Math.Min(patrol.Step, count - 1)} of {count}";

            if (!Has(creature))
                return $"{count} steps, not walked: it has no walk speed";

            if (creature.Controller.CurrentAction != BehaviorManager.BehaviorActionPatrol)
                return $"off it, going to {step} when it is idle";

            if (patrol.Arrived)
                return $"standing on {step}";

            return patrol.Rejoining ? $"rejoining at {step}" : $"walking to {step}";
        }
    }
}
