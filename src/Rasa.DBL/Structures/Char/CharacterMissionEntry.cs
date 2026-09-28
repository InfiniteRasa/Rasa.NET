using System.Collections.Generic;
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

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("mission_state")]
        [Required]
        public uint MissionState { get; set; }

        [Column("completeable")]
        [Required]
        public bool Completeable { get; set; }

        [Column("assignment_id", TypeName = "varchar(32)")]
        [Required]
        public string AssignmentId { get; set; } = System.Guid.NewGuid().ToString("N");

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = "legacy";

        [Column("generation")]
        public uint Generation { get; set; } = 1;

        [Column("version")]
        [ConcurrencyCheck]
        public long Version { get; set; }

        public CharacterMissionDeadlineEntry Deadline { get; set; }
        public ICollection<CharacterMissionObjectiveEntry> Objectives { get; set; } =
            new List<CharacterMissionObjectiveEntry>();
        public ICollection<CharacterMissionScenarioStepEntry> ScenarioSteps { get; set; } =
            new List<CharacterMissionScenarioStepEntry>();
    }
}
