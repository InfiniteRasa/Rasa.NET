using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// How a spawn pool's creatures stand at their post (the game server's NpcPose): at ease on
    /// the spot, with the weapon out, crouched, or one of the client's five ambient poses. A pool
    /// with no row has no pose, as every pool had before the table.
    ///
    /// A table of its own rather than a column on spawnpool: the preloaders of earlier migrations
    /// insert spawnpool by its entity's columns, and a world stopped at an earlier migration is
    /// still read through that entity.
    /// </summary>
    [Table(TableName)]
    public class SpawnPoolPoseEntry : IHasId
    {
        public const string TableName = "spawnpool_pose";

        /// <summary>The pool, as in spawnpool.id.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>The pose, an NpcPose: 1 standing, 2 weapon out ... 9 hand tool.</summary>
        [Column("pose")]
        [Required]
        public byte Pose { get; set; }
    }
}
