using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// The two tints of a creature's body (BodyAttributes hue and hue2), for one given colours
    /// of its own (its retail look, as near as the sources show it). Each is a colour as creature_appearance.color is: (A &lt;&lt; 24) |
    /// (B &lt;&lt; 16) | (G &lt;&lt; 8) | R (Color.Hue). A row whose hue is NULL is "no tint": the
    /// model's stock colours, sent as None. A creature with no row is tinted as every creature
    /// was before the table (Creature.TintAtSpawn): none for a FRIENDLY one, two random colours
    /// for any other.
    ///
    /// A table of its own rather than columns on creature, as creature_scale is: the preloaders
    /// of earlier migrations insert creature by its entity's columns.
    /// </summary>
    [Table(TableName)]
    public class CreatureTintEntry : IHasId
    {
        public const string TableName = "creature_tint";

        /// <summary>The creature row.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>Its first tint (BodyAttributes hue), or NULL: no tint, stock colours.</summary>
        [Column("hue")]
        public uint? Hue { get; set; }

        /// <summary>Its second tint (BodyAttributes hue2); NULL with a hue is the hue again.</summary>
        [Column("hue2")]
        public uint? Hue2 { get; set; }
    }
}
