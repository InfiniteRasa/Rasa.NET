using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterMissionScenarioStepEntry
    {
        public const string TableName = "character_mission_scenario_step";

        public CharacterMissionScenarioStepEntry()
        {
        }

        public CharacterMissionScenarioStepEntry(uint characterId, uint missionId, string stepKey)
        {
            CharacterId = characterId;
            MissionId = missionId;
            StepKey = stepKey;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("step_key", TypeName = "varchar(64)")]
        [Required]
        public string StepKey { get; set; } = string.Empty;

        public CharacterMissionEntry Mission { get; set; }
    }
}
