using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    /// <summary>
    /// One step of a spawn pool's patrol: a point its creature walks to in a straight line from
    /// the step before, the way it turns to face once it is there, and how long it stands. After
    /// the last step it walks to the first again, so the steps are a circuit: a beat walked there
    /// and back is the far end and the near end, and whatever it stops at on the way out only.
    ///
    /// A pool with no row has no patrol, as every pool had before the table. A table of its own
    /// rather than columns on spawnpool, as spawnpool_pose is: the preloaders of earlier
    /// migrations insert spawnpool by its entity's columns.
    ///
    /// The key is the pool and the step, set in WorldContext: a composite key cannot be declared
    /// with attributes.
    /// </summary>
    [Table(TableName)]
    public class SpawnPoolPatrolEntry
    {
        public const string TableName = "spawnpool_patrol";

        /// <summary>The pool, as in spawnpool.id.</summary>
        [Column("pool_id")]
        [Required]
        public uint PoolId { get; set; }

        /// <summary>Its place in the circuit: the steps are walked in this order, from the lowest.</summary>
        [Column("step")]
        [Required]
        public uint Step { get; set; }

        /// <summary>Where the creature stands at the step: the floor there, as a pool's own point is.</summary>
        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>Yaw in radians it turns to once it is there; null to stay facing the way it came.</summary>
        [Column("facing")]
        public double? Facing { get; set; }

        /// <summary>How long it stands there, once it has turned, before it walks on; 0 to walk on at once.</summary>
        [Column("pause_ms")]
        [Required]
        public uint PauseMs { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
