using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterMissionEntry
    {
        public const string TableName = "character_mission";

        public CharacterMissionEntry()
        {
        }

        public CharacterMissionEntry(uint characterId, uint mission_id, uint mission_state)
        {
            CharacterId = characterId;
            MissionId = mission_id;
            MissionState = mission_state;
        }

        [Key]
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("mission_state")]
        [Required]
        public uint MissionState { get; set; }
    }
}
