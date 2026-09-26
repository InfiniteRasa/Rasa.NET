using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rasa.Missions.Runtime;

namespace Rasa.Structures.World
{
    [Table("mission_repeat_policy")]
    public sealed class MissionRepeatPolicyEntry
    {
        [Column("mission_id")] public uint MissionId { get; set; }
        [Required, Column("content_revision", TypeName = "varchar(32)")] public string ContentRevision { get; set; } = "";
        [Column("repeat_kind")] public MissionRepeatKind Kind { get; set; }
        [Column("cooldown_seconds")] public uint? CooldownSeconds { get; set; }
        [Column("reset_second_utc")] public uint? ResetSecondUtc { get; set; }
    }
}
