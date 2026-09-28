using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class MissionObjectiveDefinitionEntry
    {
        public const string TableName = "mission_objective_definition";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("client_name_text_id")]
        [Required]
        public uint ClientNameTextId { get; set; }

        [Column("client_body_text_id")]
        [Required]
        public uint ClientBodyTextId { get; set; }

        [Column("client_counter_0_text_id")]
        public uint? ClientCounter0TextId { get; set; }

        [Column("client_counter_1_text_id")]
        public uint? ClientCounter1TextId { get; set; }

        [Column("client_counter_2_text_id")]
        public uint? ClientCounter2TextId { get; set; }

        [Column("ordinal")]
        [Required]
        public uint Ordinal { get; set; }

        [Column("initial_state")]
        [Required]
        public byte InitialState { get; set; }

        [Column("is_required")]
        [Required]
        public bool IsRequired { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionContentDefinitionEntry Content { get; set; }
        public ICollection<MissionObjectiveTransitionEntry> Transitions { get; set; } =
            new List<MissionObjectiveTransitionEntry>();
        public ICollection<MissionIndicatorEntry> Indicators { get; set; } =
            new List<MissionIndicatorEntry>();
    }
}
