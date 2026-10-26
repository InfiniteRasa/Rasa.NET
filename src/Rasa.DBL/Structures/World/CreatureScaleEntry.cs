using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// The size of a creature's body, for one bigger or smaller than its class's model: a boss
    /// whose class has the same mesh as the rank and file (Goliath, Bane_Kael_Standard_Boss, and
    /// the Kael, Bane_Kael_Standard, are both mesh 14434). The client sizes the model with it
    /// (Recv_BodyAttributes scale, body.SetScale) and plays its walk at 1 / scale of its speed.
    /// A creature with no row is 1.0, as every creature was before the table.
    ///
    /// A table of its own rather than a column on creature, as creature_actor_name is: the
    /// preloaders of earlier migrations insert creature by its entity's columns.
    /// </summary>
    [Table(TableName)]
    public class CreatureScaleEntry : IHasId
    {
        public const string TableName = "creature_scale";

        /// <summary>The creature row.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>Its size, as a factor of its model's: above 0; 1.0 is the model's own.</summary>
        [Column("scale")]
        [Required]
        public double Scale { get; set; }
    }
}
