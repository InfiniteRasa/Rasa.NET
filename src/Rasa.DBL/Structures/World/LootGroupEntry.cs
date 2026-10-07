using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A loot pool: a named list of items (loot_group_item) that creatures are given
    /// (creature_loot_group). The game server's LootPools rolls every item of every pool a
    /// creature has, each on its own, when the creature is killed.
    ///
    /// The three tables are the ones gametools' Loot Table Editor writes, by its SQL export or
    /// through the REST API (POST /updatelootpools). They ship empty: a creature with no pool
    /// drops what it always has.
    /// </summary>
    [Table(TableName)]
    public class LootGroupEntry
    {
        public const string TableName = "loot_group";

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

        /// <summary>The editor's note on the pool; empty for none.</summary>
        [Column("comment", TypeName = "varchar(200)")]
        [Required]
        public string Comment { get; set; } = "";
    }

    /// <summary>One item of a loot pool. The key is the pool and the item, set in WorldContext.</summary>
    [Table(TableName)]
    public class LootGroupItemEntry
    {
        public const string TableName = "loot_group_item";

        /// <summary>The most a row may give at once; what is given is capped at the item's stack.</summary>
        public const uint QuantityLimit = 100000;

        [Column("group_id")]
        [Required]
        public uint GroupId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>The chance of the item on a kill, a percent from 0 to 100.</summary>
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

    /// <summary>A loot pool given to a creature row (creature.id). The key is the pair, set in WorldContext.</summary>
    [Table(TableName)]
    public class CreatureLootGroupEntry
    {
        public const string TableName = "creature_loot_group";

        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("group_id")]
        [Required]
        public uint GroupId { get; set; }
    }
}
