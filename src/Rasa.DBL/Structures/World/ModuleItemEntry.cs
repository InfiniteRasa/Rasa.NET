using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A module as an item in a pack: a row of the client's moduleItemTemplateTable
    /// (generated/shared/crafting), by item template. It is what a crafting station reads an
    /// item put into it by - the module it would integrate, upgrade, or salvage for the
    /// module's Mimeogel.
    ///
    /// A module_class row names one item template of its own, the one an extraction makes. This
    /// table is the other way round and has more in it: the 5,050 pieces of salvage are item
    /// templates of the 24 salvage-value modules, up to hundreds to a module.
    /// </summary>
    [Table(TableName)]
    public class ModuleItemEntry : IHasId
    {
        public const string TableName = "module_item";

        /// <summary>ModuleItemTemplateId: the item template.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>ModuleClassId: the module_class row.</summary>
        [Column("module_id")]
        [Required]
        public uint ModuleId { get; set; }

        /// <summary>
        /// ModuleStrengthTypeId, 1 to 5. The client's integration cost is in proportion to it,
        /// and it draws the item with the quality one above it.
        /// </summary>
        [Column("strength")]
        [Required]
        public uint Strength { get; set; }
    }
}
