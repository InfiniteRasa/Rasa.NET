using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterMissionObjectiveEntry
    {
        public const string TableName = "character_mission_objective";

        public CharacterMissionObjectiveEntry()
        {
        }

        public CharacterMissionObjectiveEntry(
            uint characterId,
            uint missionId,
            uint objectiveId,
            byte objectiveState)
        {
            CharacterId = characterId;
            MissionId = missionId;
            ObjectiveId = objectiveId;
            ObjectiveState = objectiveState;
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

        [Column("objective_state")]
        [Required]
        public byte ObjectiveState { get; set; }

        public CharacterMissionEntry Mission { get; set; }
        public ICollection<CharacterMissionObjectiveCounterEntry> Counters { get; set; } =
            new List<CharacterMissionObjectiveCounterEntry>();
        public ICollection<CharacterMissionObjectiveItemCounterEntry> ItemCounters { get; set; } =
            new List<CharacterMissionObjectiveItemCounterEntry>();
    }
}
