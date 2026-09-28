using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterMissionObjectiveItemCounterEntry
    {
        public const string TableName = "character_mission_objective_item_counter";

        public CharacterMissionObjectiveItemCounterEntry()
        {
        }

        public CharacterMissionObjectiveItemCounterEntry(
            uint characterId,
            uint missionId,
            uint objectiveId,
            uint itemClassId,
            uint counterValue)
        {
            CharacterId = characterId;
            MissionId = missionId;
            ObjectiveId = objectiveId;
            ItemClassId = itemClassId;
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

        [Column("item_class_id")]
        [Required]
        public uint ItemClassId { get; set; }

        [Column("counter_value")]
        [Required]
        public uint CounterValue { get; set; }

        public CharacterMissionObjectiveEntry Objective { get; set; }
    }
}
