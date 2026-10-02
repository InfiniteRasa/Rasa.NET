using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// Who holds a control point (the world database's control_point row with this id), once it
    /// has changed hands: written on every capture and read back when the server starts, so
    /// ownership lasts through a restart. A point with no row is with its default owner.
    /// </summary>
    [Table(TableName)]
    public class ControlPointStateEntry
    {
        public const string TableName = "control_point_state";

        [Key]
        [Column("control_point_id")]
        public uint ControlPointId { get; set; }

        /// <summary>0: the Bane; 1: the AFS.</summary>
        [Column("owner")]
        [Required]
        public byte Owner { get; set; }

        /// <summary>When it last changed hands: Unix time in milliseconds, UTC.</summary>
        [Column("changed_at")]
        [Required]
        public long ChangedAt { get; set; }
    }
}
