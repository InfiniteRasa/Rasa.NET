using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemTemplateArmorEntry : IHasId
    {
        public const string TableName = "itemtemplate_armor";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("armor_value")]
        [Required]
        public int ArmorValue { get; set; }
    }
}
