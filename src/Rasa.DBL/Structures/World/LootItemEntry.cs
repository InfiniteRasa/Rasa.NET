using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class LootItemEntry
    {
        public const string TableName = "loot_item";

        [Column("profile_key", TypeName = "varchar(64)")]
        [MaxLength(64)]
        [Required]
        public string ProfileKey { get; set; } = string.Empty;

        [Column("group_key", TypeName = "varchar(32)")]
        [MaxLength(32)]
        [Required]
        public string GroupKey { get; set; } = string.Empty;

        [Column("item_template_id")]
        public uint ItemTemplateId { get; set; }

        [Column("weight")]
        public int Weight { get; set; }

        [Column("minimum_quantity")]
        public int MinimumQuantity { get; set; }

        [Column("maximum_quantity")]
        public int MaximumQuantity { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
