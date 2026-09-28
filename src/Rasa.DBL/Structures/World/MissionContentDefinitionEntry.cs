using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    [Index(nameof(ContentRevision), Name = "mission_content_definition_index_content_revision")]
    public class MissionContentDefinitionEntry
    {
        public const string TableName = "mission_content_definition";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        // Mapped in the model so historical reflection-based seed column lists stay fixed.
        public bool Enabled { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("abandonment_policy")]
        [Required]
        public MissionAbandonmentPolicy AbandonmentPolicy { get; set; } =
            MissionAbandonmentPolicy.Allowed;

        [Column("client_name_text_id")]
        [Required]
        public uint ClientNameTextId { get; set; }

        [Column("giver_id")]
        public uint? GiverId { get; set; } = 0;

        [Column("receiver_id")]
        public uint? ReceiverId { get; set; } = 0;

        [Column("level")]
        [Required]
        public uint Level { get; set; }

        [Column("group_type")]
        [Required]
        public byte GroupType { get; set; }

        [Column("category_id")]
        [Required]
        public byte CategoryId { get; set; }

        [Column("shareable")]
        [Required]
        public bool Shareable { get; set; }

        [Column("radio_completeable")]
        [Required]
        public bool RadioCompleteable { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public ICollection<MissionPrerequisiteEntry> Prerequisites { get; set; } =
            new List<MissionPrerequisiteEntry>();
        public ICollection<MissionObjectiveDefinitionEntry> Objectives { get; set; } =
            new List<MissionObjectiveDefinitionEntry>();
        public ICollection<MissionRewardDefinitionEntry> Rewards { get; set; } =
            new List<MissionRewardDefinitionEntry>();
        public ICollection<MissionAreaEntry> Areas { get; set; } =
            new List<MissionAreaEntry>();
        public ICollection<MissionSpawnGroupEntry> SpawnGroups { get; set; } =
            new List<MissionSpawnGroupEntry>();
        public ICollection<MissionScenarioEntry> Scenarios { get; set; } =
            new List<MissionScenarioEntry>();
        public ICollection<MissionEvidenceEntry> Evidence { get; set; } =
            new List<MissionEvidenceEntry>();
    }
}
