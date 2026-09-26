using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rasa.Missions.Definitions;

namespace Rasa.Structures.World
{
    [Table("mission_channel_policy")]
    public sealed class MissionChannelPolicyEntry
    {
        [Column("mission_id")] public uint MissionId { get; set; }
        [Required, Column("content_revision", TypeName = "varchar(32)")] public string ContentRevision { get; set; } = "";
        [Column("acceptance_channel")] public MissionChannel AcceptanceChannel { get; set; } = MissionChannel.Npc;
        [Column("completion_channel")] public MissionChannel CompletionChannel { get; set; } = MissionChannel.Npc;
        [Required, Column("radio_sources", TypeName = "text")] public string RadioSources { get; set; } = "[]";
    }
}
