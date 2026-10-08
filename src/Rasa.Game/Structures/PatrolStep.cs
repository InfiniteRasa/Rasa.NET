using System.Numerics;

namespace Rasa.Structures
{
    /// <summary>
    /// One step of a patrol (spawnpool_patrol; Managers.Patrols): where the creature walks to,
    /// the way it turns to face once it is there, and how long it then stands.
    /// </summary>
    public sealed class PatrolStep
    {
        public PatrolStep(Vector3 position, float? facing = null, long pauseMs = 0)
        {
            Position = position;
            Facing = facing;
            PauseMs = pauseMs;
        }

        /// <summary>Where it stands at the step: the floor there. It is walked to in a straight line and not put on the navmesh.</summary>
        public Vector3 Position { get; }

        /// <summary>The yaw it turns to there, in radians; null to stay facing the way it came.</summary>
        public float? Facing { get; }

        /// <summary>How long it stands there once it has turned; 0 to walk on at once.</summary>
        public long PauseMs { get; }
    }
}
