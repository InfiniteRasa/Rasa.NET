using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class OrdinaryLootGroupEntry
    {
        public const string TableName = "ordinary_loot_group";

        [Column("profile_key", TypeName = "varchar(64)")]
        [MaxLength(64)]
        [Required]
        public string ProfileKey { get; set; } = string.Empty;

        [Column("group_key", TypeName = "varchar(32)")]
        [MaxLength(32)]
        [Required]
        public string GroupKey { get; set; } = string.Empty;

        [Column("kind")]
        public LootGroupKind Kind { get; set; }

        [Column("chance_basis_points")]
        public int ChanceBasisPoints { get; set; }

        [Column("minimum_rolls")]
        public int MinimumRolls { get; set; }

        [Column("maximum_rolls")]
        public int MaximumRolls { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
