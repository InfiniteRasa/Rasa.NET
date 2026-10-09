using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// An item class that takes modules: the client's ModifiableClassList, each with the class
    /// set it is a member of (entityClassSetTable) - 212 armor, 1212 weapons, 1273 tools. A
    /// module goes into the items of its own class set (module_class.class_set_id), and an item
    /// of no class here can be neither given a module nor salvaged as equipment.
    ///
    /// The client's data would let a class be in several sets. The three that modules use share
    /// no class, and together they are the modifiable list exactly, so a class has one row.
    /// </summary>
    [Table(TableName)]
    public class ModifiableClassEntry : IHasId
    {
        public const string TableName = "modifiable_class";

        /// <summary>The item's entity class.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("class_set_id")]
        [Required]
        public uint ClassSetId { get; set; }
    }
}
