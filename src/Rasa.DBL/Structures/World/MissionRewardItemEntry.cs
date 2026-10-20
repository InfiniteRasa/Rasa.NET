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

        /// <summary>
        /// The modules the item is given with, a module_class id in each of its four slots or 0
        /// for an empty one, as items.module_1 to module_4 hold them. Every one of the reward's
        /// items gets them; a template that takes none is left with all four 0.
        /// </summary>
        [Column("module_1")]
        [Required]
        public uint Module1 { get; set; }

        [Column("module_2")]
        [Required]
        public uint Module2 { get; set; }

        [Column("module_3")]
        [Required]
        public uint Module3 { get; set; }

        [Column("module_4")]
        [Required]
        public uint Module4 { get; set; }

        /// <summary>The four slots, in order.</summary>
        [NotMapped]
        public uint[] Modules => new[] { Module1, Module2, Module3, Module4 };

        public MissionRewardDefinitionEntry Reward { get; set; }
    }
}
