using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterSkillsEntry
    {
        public const string TableName = "character_skills";

        public CharacterSkillsEntry()
        {
        }

        public CharacterSkillsEntry(uint characterId, uint skillId, int abilityId, int skillLevel)
        {
            CharacterId = characterId;
            SkillId = skillId;
            AbilityId = abilityId;
            SkillLevel = skillLevel;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("skill_id")]
        [Required]
        public uint SkillId { get; set; }

        [Column("ability_id")]
        [Required]
        public int AbilityId { get; set; }

        [Column("skill_level")]
        [Required]
        public int SkillLevel { get; set; }
    }
}
