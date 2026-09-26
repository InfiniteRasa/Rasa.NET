using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    public enum CharacterMissionDeadlineState : byte
    {
        Active = 1,
        Satisfied = 2,
        Expired = 3,
        Cancelled = 4
    }

    [Table(TableName)]
    public class CharacterMissionDeadlineEntry
    {
        public const string TableName = "character_mission_deadline";

        public CharacterMissionDeadlineEntry()
        {
        }

        public CharacterMissionDeadlineEntry(
            uint characterId,
            uint missionId,
            DateTime dueAtUtc,
            CharacterMissionDeadlineState state)
        {
            CharacterId = characterId;
            MissionId = missionId;
            DueAtUtc = dueAtUtc;
            State = state;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("due_at_utc")]
        [Required]
        public DateTime DueAtUtc { get; set; }

        [Column("state")]
        [Required]
        public CharacterMissionDeadlineState State { get; set; }

        public CharacterMissionEntry Mission { get; set; }
    }
}
