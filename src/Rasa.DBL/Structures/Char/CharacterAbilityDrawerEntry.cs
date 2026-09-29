using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterAbilityDrawerEntry
    {
        public const string TableName = "character_ability_drawer";

        public CharacterAbilityDrawerEntry()
        {
        }

        public CharacterAbilityDrawerEntry(uint characterId, int abilitieSlot, int abilityId, uint abilityLevel, uint? itemId = null)
        {
            CharacterId = characterId;
            AbilitySlot = abilitieSlot;
            AbilityId = abilityId;
            AbilityLevel = abilityLevel;
            ItemId = itemId;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("abilitiy_slot")]
        [Required]
        public int AbilitySlot { get; set; }

        [Column("ability_id")]
        [Required]
        public int AbilityId { get; set; }

        [Column("ability_level")]
        [Required]
        public uint AbilityLevel { get; set; }

        /// <summary>
        /// The item (item.id) whose action the slot holds, for a usable item dragged onto the tray -
        /// a pet, a medpack; null for a skill's ability.
        /// </summary>
        [Column("item_id")]
        public uint? ItemId { get; set; }
    }
}
