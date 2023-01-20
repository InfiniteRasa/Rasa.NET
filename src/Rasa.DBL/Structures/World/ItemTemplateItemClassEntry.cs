using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class ItemTemplateItemClassEntry
    {
        public const string TableName = "itemtemplate_itemclass";

        [Key]
        [Column("itemTemplateId")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("itemClassId")]
        [Required]
        public uint ItemClass { get; set; }
    }
}
