using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Where a local teleporter takes whoever steps onto it (the game server's local teleporter
    /// worker, DynamicObjectManager): another local teleporter (teleporter rows of type 1) on the
    /// same map. One row a way, so a pair that goes both ways is two rows. A local teleporter
    /// with no row takes nobody anywhere.
    ///
    /// The client has no name for any local teleporter in its waypoint table, so none can be
    /// offered in the travel window: each goes to the one place its row says.
    /// </summary>
    [Table(TableName)]
    public class LocalTeleporterLinkEntry
    {
        public const string TableName = "local_teleporter_link";

        /// <summary>The local teleporter stepped onto (teleporter.id).</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("from_teleporter_id")]
        [Required]
        public uint FromTeleporterId { get; set; }

        /// <summary>The local teleporter it takes them to (teleporter.id).</summary>
        [Column("to_teleporter_id")]
        [Required]
        public uint ToTeleporterId { get; set; }
    }
}
