using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class MissionObjectiveTransitionEntry
    {
        public const string TableName = "mission_objective_transition";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("transition_id")]
        [Required]
        public uint TransitionId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("sequence")]
        [Required]
        public uint Sequence { get; set; }

        [Column("from_state")]
        public byte? FromState { get; set; }

        [Column("to_state")]
        public byte? ToState { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionObjectiveDefinitionEntry Objective { get; set; }
        public ICollection<MissionTriggerEntry> Triggers { get; set; } =
            new List<MissionTriggerEntry>();
        public ICollection<MissionActionEntry> Actions { get; set; } =
            new List<MissionActionEntry>();
    }
}
