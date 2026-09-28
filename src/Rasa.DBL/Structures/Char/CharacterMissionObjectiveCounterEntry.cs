using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterMissionObjectiveCounterEntry
    {
        public const string TableName = "character_mission_objective_counter";

        public CharacterMissionObjectiveCounterEntry()
        {
        }

        public CharacterMissionObjectiveCounterEntry(
            uint characterId,
            uint missionId,
            uint objectiveId,
            uint counterId,
            uint counterValue)
        {
            CharacterId = characterId;
            MissionId = missionId;
            ObjectiveId = objectiveId;
            CounterId = counterId;
            CounterValue = counterValue;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("counter_id")]
        [Required]
        public uint CounterId { get; set; }

        [Column("counter_value")]
        [Required]
        public uint CounterValue { get; set; }

        public CharacterMissionObjectiveEntry Objective { get; set; }
    }
}
