using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// One action the client knows how to perform - a player ability, a creature attack, a
    /// consumable's effect, a weapon draw. The client's generated.client.actiondata.actionModules:
    /// the name is the ActionId constant, the module is the python class that runs it
    /// client-side, which is the best statement there is of what kind of action it is
    /// (abilities.lightning, abilities.heal, abilities.sprint, ...).
    ///
    /// Per-level timing, cost and effect strength are in action_level, action_cost and
    /// action_property, keyed by action and level.
    /// </summary>
    [Table(TableName)]
    public class ActionEntry : IHasId
    {
        public const string TableName = "action";

        /// <summary>The ActionId; the client's actionModules key.</summary>
        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        [MaxLength(64)]
        public string Name { get; set; }

        /// <summary>The client python module that implements the action, relative to client/actions.</summary>
        [Column("module", TypeName = "varchar(64)")]
        [Required]
        [MaxLength(64)]
        public string Module { get; set; }

        /// <summary>A charged action winds up for as long as the button is held; the request carries the charge time.</summary>
        [Column("is_charged")]
        [Required]
        public byte IsCharged { get; set; }
    }
}
