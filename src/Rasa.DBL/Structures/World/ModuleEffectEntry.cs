using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// One thing a module does: a line of the tuple list the client is sent as ModuleTooltipInfo
    /// and draws under the item's name (tooltipwindow._AddModuleEffects). A module's lines are
    /// sent in the order of their ids.
    ///
    /// The amount the client shows, and the amount the bonus is, for an item of level L - its
    /// level requirement:
    ///     ceil(flat_value + linear_value * L + exp_value * 2^((L - 1) / 8))
    /// </summary>
    [Table(TableName)]
    public class ModuleEffectEntry : IHasId
    {
        public const string TableName = "module_effect";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>The module_class row.</summary>
        [Column("module_id")]
        [Required]
        public uint ModuleId { get; set; }

        /// <summary>
        /// A game effect type of the client's gameeffectdata: its tooltip is the line's text
        /// ("Body: %(amount)s"), and an effect with none draws no line.
        /// </summary>
        [Column("effect_id")]
        [Required]
        public uint EffectId { get; set; }

        /// <summary>
        /// For a set's bonus, the pieces of the set that have to be worn for it; the client
        /// prints "(n)" before the line and greys it until they are. 0 for a bonus of the
        /// module itself.
        /// </summary>
        [Column("set_level")]
        [Required]
        public uint SetLevel { get; set; }

        [Column("flat_value")]
        [Required]
        public double FlatValue { get; set; }

        [Column("linear_value")]
        [Required]
        public double LinearValue { get; set; }

        [Column("exp_value")]
        [Required]
        public double ExpValue { get; set; }

        /// <summary>
        /// The effect's own arguments, as its tooltip names them: a damage type for "Resist:
        /// $damageType%(arg1)s". Null is sent as None, which is how the client spells "all" in
        /// the lines that take an attack type.
        /// </summary>
        [Column("arg1")]
        public int? Arg1 { get; set; }

        [Column("arg2")]
        public int? Arg2 { get; set; }

        [Column("arg3")]
        public int? Arg3 { get; set; }

        [Column("arg4")]
        public int? Arg4 { get; set; }
    }
}
