using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
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
