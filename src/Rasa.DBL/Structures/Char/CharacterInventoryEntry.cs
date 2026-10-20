using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// Where an item is: one row per item. Rows are read by account (login loads all of an
    /// account's, its characters' and its lockbox's) and by account and character (a slot's row,
    /// a character's deletion), so the index is on both, in that order
    /// (Index_character_and_clan_inventory).
    /// </summary>
    [Table(TableName)]
    [Index(nameof(AccountId), nameof(CharacterId), Name = "character_inventory_index_account_character")]
    [Index(nameof(CharacterId), nameof(InventoryType), Name = "character_inventory_index_character_type")]
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
