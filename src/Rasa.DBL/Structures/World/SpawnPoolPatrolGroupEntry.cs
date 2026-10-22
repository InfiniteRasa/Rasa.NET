using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// How a spawn pool's group walks its patrol (spawnpool_patrol; the game server's
    /// PatrolGroups). A pool with no row is led by the creature of its first slot, and its
    /// followers stroll where they stand while it is dead, as every group was before the table.
    ///
    /// With an acting leader, one of its followers walks the beat while there is no leader - and
    /// a pool whose first slot (creature_1_id) is empty has no leader of its own at all: one of
    /// the creatures of its other slots always leads, a patrol of equals. Once the first slot's
    /// creature is back, it leads again.
    ///
    /// Without its leader's top-up, the leader killed while some of its pool stands is not
    /// brought back with the rest of what the pool lost, by the clock the pool's first loss
    /// started (the game server's SpawnPoolManager.TopUp), but its respawn time after its own
    /// death: a boss killed late in a long fight does not stand again moments after.
    ///
    /// With a pace, the group walks its beat at that pace rather than the default stroll.
    ///
    /// A table of its own rather than a column on spawnpool, as spawnpool_pose is: the preloaders
    /// of earlier migrations insert spawnpool by its entity's columns.
    /// </summary>
    [Table(TableName)]
    public class SpawnPoolPatrolGroupEntry : IHasId
    {
        public const string TableName = "spawnpool_patrol_group";

        /// <summary>The pool, as in spawnpool.id.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>Whether one of its followers leads while it has no leader of its own.</summary>
        [Column("acting_leader")]
        [Required]
        public bool ActingLeader { get; set; }

        /// <summary>
        /// Whether its leader comes back by the pool's top-up, as the rest of it does; if not, its
        /// respawn time after its own death. The column defaults to true for rows inserted without it.
        /// </summary>
        [Column("leader_top_up")]
        [Required]
        public bool LeaderTopUp { get; set; } = true;

        /// <summary>
        /// The pace its beat is walked at, metres a second, by its leader and by its followers
        /// keeping their places; null for the default, its creatures' walk speed capped at a
        /// stroll (the game server's Patrols.Pace). Chases and catching up are run as before.
        /// </summary>
        [Column("pace")]
        public double? Pace { get; set; }
    }
}
