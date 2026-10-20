using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// An item in a clan's lockbox. The lockbox is read by clan, when a member arrives and on
    /// every change to it, so it is indexed by clan (Index_character_and_clan_inventory).
    /// </summary>
    [Table(TableName)]
    [Index(nameof(ClanId), Name = "clan_inventory_index_clan_id")]
    public class ClanInventoryEntry
    {
        public const string TableName = "clan_inventory";

        public ClanInventoryEntry()
        {
        }

        public ClanInventoryEntry(uint clanId, uint slotId, uint itemId)
        {
            ClanId = clanId;
            SlotId = slotId;
            ItemId = itemId;
        }

        [Column("clan_id")]
        [Required]
        public uint ClanId { get; set; }

        [Column("slot_id")]
        [Required]
        public uint SlotId { get; set; }

        [Key]
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }
    }
}
