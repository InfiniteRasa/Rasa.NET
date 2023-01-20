using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemTemplateEntry : IHasId
    {
        public const string TableName = "itemtemplate";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("quality_id")]
        [Required]
        public byte QualityId { get; set; }

        [Column("has_sellable_flag")]
        [Required]
        public byte HasSellableFlag { get; set; }

        [Column("not_tradable_flag")]
        [Required]
        public byte NotTradableFlag { get; set; }

        [Column("has_character_unique_flag")]
        [Required]
        public byte HasCharacterUniqueFlag { get; set; }

        [Column("has_account_unique_flag")]
        [Required]
        public byte HasAccountUniqueFlag { get; set; }

        [Column("has_boe_flag")]
        [Required]
        public byte HasBoEFlag { get; set; }

        [Column("bound_to_character_flag")]
        [Required]
        public byte BoundToCharacterFlag { get; set; }

        [Column("not_placable_in_lockbox_flag")]
        [Required]
        public byte NotPlacableInLockboxFlag { get; set; }

        [Column("inventory_category")]
        [Required]
        public byte InventoryCategory { get; set; }

        [Column("buy_price")]
        [Required]
        public int BuyPrice { get; set; }

        [Column("sell_price")]
        [Required]
        public int SellPrice { get; set; }
    }
}
