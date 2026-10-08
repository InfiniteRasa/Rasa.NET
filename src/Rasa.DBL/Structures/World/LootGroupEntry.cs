using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Archived numeric flat-pool configuration. This is not the weighted ordinary-loot model.
    /// Retained for archive diagnostics and phase-correct EF metadata; no editor or generator writes it.
    /// </summary>
    [Table(TableName)]
    public class LootGroupEntry
    {
        public const string TableName = "legacy_flat_loot_group";
        public const int MaxNameLength = 100;
        public const int MaxCommentLength = 200;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("name", TypeName = "varchar(100)")]
        [Required]
        public string Name { get; set; }

        /// <summary>The historical editor's note, retained verbatim.</summary>
        [Column("comment", TypeName = "varchar(200)")]
        [Required]
        public string Comment { get; set; } = "";
    }

    /// <summary>A historical flat item row, retained without activation or percent-to-weight conversion.</summary>
    [Table(TableName)]
    public class LootGroupItemEntry
    {
        public const string TableName = "legacy_flat_loot_group_item";

        /// <summary>The historical editor's quantity limit, not an active generation rule.</summary>
        public const uint QuantityLimit = 100000;

        [Column("group_id")]
        [Required]
        public uint GroupId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>The archived independent per-item percentage, never interpreted as a weight.</summary>
        [Column("chance")]
        [Required]
        public double Chance { get; set; }

        [Column("min_quantity")]
        [Required]
        public uint MinQuantity { get; set; }

        [Column("max_quantity")]
        [Required]
        public uint MaxQuantity { get; set; }
    }

    /// <summary>An inactive historical numeric assignment, retained for diagnostics and upgrade history.</summary>
    [Table(TableName)]
    public class CreatureLootGroupEntry
    {
        public const string TableName = "legacy_flat_creature_loot_group";

        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("group_id")]
        [Required]
        public uint GroupId { get; set; }
    }
}
