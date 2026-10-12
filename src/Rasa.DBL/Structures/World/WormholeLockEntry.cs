using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A condition on travelling through the world's wormhole network (the game server's
    /// Wormholes). The world wormholes (teleporter rows of type 3) lead to one another, and a
    /// player standing at one may go to any other unless a row here says otherwise. A trip
    /// needs every row that matches it to be met.
    ///
    /// A row matches a trip from <see cref="FromTeleporterId"/> to <see cref="ToTeleporterId"/>;
    /// 0 in either is any wormhole, so a row with 0 as its source keeps a wormhole shut from
    /// everywhere. What it asks for is <see cref="Kind"/> with <see cref="Value"/>:
    /// <see cref="KindMissionAccepted"/> a mission id, <see cref="KindRequiredLevel"/> a level.
    /// </summary>
    [Table(TableName)]
    public class WormholeLockEntry
    {
        public const string TableName = "wormhole_lock";

        /// <summary>The player has taken the mission <see cref="Value"/> at least once: it is in their log, or in their history however it ended.</summary>
        public const uint KindMissionAccepted = 1;

        /// <summary>The player is at least level <see cref="Value"/>. Not enforced yet: such a row is logged and the trip is open.</summary>
        public const uint KindRequiredLevel = 2;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>The wormhole the trip is from (teleporter.id); 0 for any.</summary>
        [Column("from_teleporter_id")]
        [Required]
        public uint FromTeleporterId { get; set; }

        /// <summary>The wormhole the trip is to (teleporter.id); 0 for any.</summary>
        [Column("to_teleporter_id")]
        [Required]
        public uint ToTeleporterId { get; set; }

        /// <summary>What is asked: <see cref="KindMissionAccepted"/> or <see cref="KindRequiredLevel"/>.</summary>
        [Column("kind")]
        [Required]
        public uint Kind { get; set; }

        /// <summary>The mission id or the level.</summary>
        [Column("value")]
        [Required]
        public uint Value { get; set; }

        [Column("comment", TypeName = "varchar(128)")]
        public string Comment { get; set; }
    }
}
