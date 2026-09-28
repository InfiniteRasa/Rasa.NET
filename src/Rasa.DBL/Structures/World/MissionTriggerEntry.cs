using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionTriggerKind : byte
    {
        /// <summary>Uses npc_package_id and player_flag_id.</summary>
        Conversation = 1,

        /// <summary>Uses event_kind, subject_id, counter_id, initial_value, target_value and source_spawn_resolved.</summary>
        ProgressEvent = 2,

        /// <summary>Uses related_objective_id and related_state.</summary>
        ObjectiveState = 3,

        /// <summary>Uses area_id.</summary>
        AreaEntered = 4,

        /// <summary>Uses duration_seconds.</summary>
        TimerElapsed = 5
    }

    /// <summary>
    /// Only the columns documented on <see cref="Kind"/> are populated for a row. The remaining
    /// typed columns stay null.
    /// </summary>
    [Table(TableName)]
    public class MissionTriggerEntry
    {
        public const string TableName = "mission_trigger";

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

        [Column("trigger_id")]
        [Required]
        public uint TriggerId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("kind")]
        [Required]
        public MissionTriggerKind Kind { get; set; }

        [Column("sequence")]
        [Required]
        public uint Sequence { get; set; }

        [Column("related_objective_id")]
        public uint? RelatedObjectiveId { get; set; }

        [Column("related_state")]
        public byte? RelatedState { get; set; }

        [Column("event_kind")]
        public byte? EventKind { get; set; }

        /// <summary>
        /// Progress-event subjects intentionally stay scalar. Depending on EventKind the value is
        /// a mission id, creature id, item class id, waypoint id, logos id, or interaction class
        /// id, so there is no single authored row type to reference here.
        /// </summary>
        [Column("subject_id")]
        public uint? SubjectId { get; set; }

        [Column("counter_id")]
        public uint? CounterId { get; set; }

        [Column("initial_value")]
        public uint? InitialValue { get; set; }

        [Column("target_value")]
        public uint? TargetValue { get; set; }

        [Column("area_id")]
        public uint? AreaId { get; set; }

        [Column("duration_seconds")]
        public uint? DurationSeconds { get; set; }

        [Column("npc_package_id")]
        public uint? NpcPackageId { get; set; }

        [Column("player_flag_id")]
        public uint? PlayerFlagId { get; set; }

        [Column("source_spawn_resolved")]
        public bool? SourceSpawnResolved { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionObjectiveTransitionEntry Transition { get; set; }
    }
}
