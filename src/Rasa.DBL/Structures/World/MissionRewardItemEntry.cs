using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionRewardItemKind : byte
    {
        Fixed = 1,
        Selectable = 2
    }

    [Table(TableName)]
    public class MissionRewardItemEntry
    {
        public const string TableName = "mission_reward_item";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("reward_id")]
        [Required]
        public uint RewardId { get; set; }

        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }

        [Column("kind")]
        [Required]
        public MissionRewardItemKind Kind { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("quantity")]
        [Required]
        public uint Quantity { get; set; }

        public MissionRewardDefinitionEntry Reward { get; set; }
    }
}
