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

        public CharacterAbilityDrawerEntry(uint characterId, int abilitieSlot, int abilityId, uint abilityLevel)
        {
            CharacterId = characterId;
            AbilitySlot = abilitieSlot;
            AbilityId = abilityId;
            AbilityLevel = abilityLevel;
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
    }
}
