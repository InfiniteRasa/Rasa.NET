using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A spawn pool that sends a set number of creatures at a time, each drawn at random from its
    /// slots, a slot's chance its max count over all its slots' (the game server's
    /// SpawnPoolManager.DrawCreatures): a squad of three of mixed soldiers. A pool with no row
    /// sends each slot's own count, as every pool did before the table.
    ///
    /// A table of its own rather than a column on spawnpool, as spawnpool_pose is: the preloaders of
    /// earlier migrations insert spawnpool by its entity's columns.
    /// </summary>
    [Table(TableName)]
    public class SpawnPoolDrawEntry : IHasId
    {
        public const string TableName = "spawnpool_draw";

        /// <summary>The pool, as in spawnpool.id.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>How many it sends at a time.</summary>
        [Column("count")]
        [Required]
        public byte Count { get; set; }
    }
}
