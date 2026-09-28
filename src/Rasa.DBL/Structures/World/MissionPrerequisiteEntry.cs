using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionPrerequisiteKind : byte
    {
        MissionCompleted = 1,
        MissionAccepted = 2,
        PlayerLevelAtLeast = 3,
        PlayerFlagValue = 4
    }

    [Table(TableName)]
    public class MissionPrerequisiteEntry
    {
        public const string TableName = "mission_prerequisite";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("prerequisite_id")]
        [Required]
        public uint PrerequisiteId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("kind")]
        [Required]
        public MissionPrerequisiteKind Kind { get; set; }

        [Column("required_mission_id")]
        public uint? RequiredMissionId { get; set; }

        [Column("required_mission_state")]
        public byte? RequiredMissionState { get; set; }

        [Column("required_level")]
        public uint? RequiredLevel { get; set; }

        [Column("player_flag_id")]
        public uint? PlayerFlagId { get; set; }

        [Column("player_flag_value")]
        public uint? PlayerFlagValue { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionContentDefinitionEntry Content { get; set; }
    }
}
