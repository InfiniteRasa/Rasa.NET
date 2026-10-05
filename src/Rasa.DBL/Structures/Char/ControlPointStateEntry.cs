using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// Who holds a control point (the world database's control_point row with this id), once it
    /// has changed hands: written on every capture and read back when the server starts, so
    /// ownership lasts through a restart. A point with no row is with its default owner.
    ///
    /// A point the AFS hold may be a clan's (the game server's ControlPoints): the clan of the
    /// player who took it, kept here with how far the clan has been paid for holding it.
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

        /// <summary>When it last changed hands, from one side or one clan to another: Unix time in milliseconds, UTC.</summary>
        [Column("changed_at")]
        [Required]
        public long ChangedAt { get; set; }

        /// <summary>The clan that holds it for the AFS, or 0 for none: only ever set with the AFS as owner.</summary>
        [Column("clan_id")]
        [Required]
        public uint ClanId { get; set; }

        /// <summary>Up to when the clan has been paid for holding it: Unix time in milliseconds, UTC; 0 with no clan.</summary>
        [Column("clan_paid_at")]
        [Required]
        public long ClanPaidAt { get; set; }
    }
}
