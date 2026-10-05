using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Something that belongs to a control point (ControlPointEntry): a spawn pool of one side's
    /// garrison, the hospital or waypoint the AFS have there while they hold it, a creature
    /// that is a boss of its garrison, or the clan lockbox that stands there while a clan holds
    /// it. The key is all three columns.
    /// </summary>
    [Table(TableName)]
    public class ControlPointLinkEntry
    {
        public const string TableName = "control_point_link";

        /// <summary><see cref="Kind"/>: object_id is a spawnpool of the Bane's garrison - what stands there while they hold it.</summary>
        public const byte KindBanePool = 1;

        /// <summary><see cref="Kind"/>: object_id is a spawnpool of the AFS's garrison - the medic, banker, vendors and soldiers there while they hold it.</summary>
        public const byte KindAfsPool = 2;

        /// <summary><see cref="Kind"/>: object_id is the teleporter row of the point's hospital.</summary>
        public const byte KindHospital = 3;

        /// <summary><see cref="Kind"/>: object_id is the teleporter row of the point's waypoint.</summary>
        public const byte KindWaypoint = 4;

        /// <summary><see cref="Kind"/>: object_id is a creature row that counts as a boss of the point's garrison.</summary>
        public const byte KindBoss = 5;

        /// <summary>
        /// <see cref="Kind"/>: object_id is the footlocker row of the point's clan lockbox - on the
        /// map only while a clan holds the point, and that clan's alone to open. Set down by a
        /// game master (".cp &lt;id&gt; lockbox"); one a point.
        /// </summary>
        public const byte KindClanLockbox = 6;

        [Column("control_point_id")]
        [Required]
        public uint ControlPointId { get; set; }

        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        [Column("object_id")]
        [Required]
        public uint ObjectId { get; set; }
    }
}
