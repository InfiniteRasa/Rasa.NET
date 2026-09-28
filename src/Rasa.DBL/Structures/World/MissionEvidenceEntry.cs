using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionEvidenceSourceKind : byte
    {
        Client = 1,
        Server = 2,
        Reconstruction = 3,
        Documentation = 4
    }

    public enum MissionEvidenceOwnerKind : byte
    {
        Mission = 1,
        Objective = 2,
        Transition = 3,
        Reward = 4,
        Scenario = 5
    }

    [Table(TableName)]
    public class MissionEvidenceEntry
    {
        public const string TableName = "mission_evidence";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("evidence_id")]
        [Required]
        public uint EvidenceId { get; set; }

        [Column("owner_kind")]
        [Required]
        public MissionEvidenceOwnerKind OwnerKind { get; set; }

        [Column("owner_id")]
        [Required]
        public uint OwnerId { get; set; }

        [Column("source_kind")]
        [Required]
        public MissionEvidenceSourceKind SourceKind { get; set; }

        [Column("source_uri", TypeName = "varchar(256)")]
        public string SourceUri { get; set; }

        [Column("local_client_path", TypeName = "varchar(256)")]
        public string LocalClientPath { get; set; }

        [Column("confidence")]
        [Required]
        public double Confidence { get; set; }

        [Column("reconstruction_note", TypeName = "varchar(256)")]
        [Required]
        public string ReconstructionNote { get; set; } = string.Empty;

        public MissionContentDefinitionEntry Content { get; set; }
    }
}
