using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Only the columns documented on <see cref="Kind"/> are populated for a row. The remaining
    /// typed columns stay null.
    /// </summary>
    [Table(TableName)]
    public class MissionActionEntry
    {
        public const string TableName = "mission_action";

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

        [Column("action_id")]
        [Required]
        public uint ActionId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("kind")]
        [Required]
        public MissionActionKind Kind { get; set; }

        [Column("sequence")]
        [Required]
        public uint Sequence { get; set; }

        [Column("target_objective_id")]
        public uint? TargetObjectiveId { get; set; }

        [Column("objective_state")]
        public byte? ObjectiveState { get; set; }

        [Column("reward_id")]
        public uint? RewardId { get; set; }

        [Column("spawn_group_id")]
        public uint? SpawnGroupId { get; set; }

        [Column("scenario_id")]
        public uint? ScenarioId { get; set; }

        [Column("indicator_id")]
        public uint? IndicatorId { get; set; }

        [Column("player_flag_id")]
        public uint? PlayerFlagId { get; set; }

        [Column("player_flag_value")]
        public uint? PlayerFlagValue { get; set; }

        [Column("npc_package_id")]
        public uint? NpcPackageId { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionObjectiveTransitionEntry Transition { get; set; }

        // Fluent-mapped: historical preloader row widths are frozen by ColumnAttribute reflection.
        public string ItemIntentJson { get; set; }
    }
}
