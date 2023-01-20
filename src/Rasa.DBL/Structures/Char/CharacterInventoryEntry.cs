using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterInventoryEntry
    {
        public const string TableName = "character_inventory";

        public CharacterInventoryEntry()
        {
        }

        public CharacterInventoryEntry(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            AccountId = accountId;
            CharacterId = characterId;
            InventoryType = inventoryType;
            SlotId = slotId;
            ItemId = itemId;
        }

        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("invenotry_type")]
        [Required]
        public uint InventoryType { get; set; }

        [Column("slot_id")]
        [Required]
        public uint SlotId { get; set; }

        [Key]
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }
    }
}
