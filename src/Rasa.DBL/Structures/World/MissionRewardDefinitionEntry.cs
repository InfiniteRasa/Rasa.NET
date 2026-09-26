using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class MissionRewardDefinitionEntry
    {
        public const string TableName = "mission_reward_definition";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("reward_id")]
        [Required]
        public uint RewardId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("experience")]
        [Required]
        public uint Experience { get; set; }

        [Column("credits")]
        [Required]
        public uint Credits { get; set; }

        [Column("prestige")]
        [Required]
        public uint Prestige { get; set; }

        [Column("selection_count")]
        [Required]
        public byte SelectionCount { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionContentDefinitionEntry Content { get; set; }
        public ICollection<MissionRewardItemEntry> Items { get; set; } =
            new List<MissionRewardItemEntry>();
    }
}
