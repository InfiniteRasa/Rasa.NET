using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class LootProfileEntry
    {
        public const string TableName = "loot_profile";

        [Key]
        [Column("profile_key", TypeName = "varchar(64)")]
        [MaxLength(64)]
        [Required]
        public string ProfileKey { get; set; } = string.Empty;

        [Column("description", TypeName = "text")]
        [Required]
        public string Description { get; set; } = string.Empty;

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("provenance", TypeName = "text")]
        [Required]
        public string Provenance { get; set; } = string.Empty;

        [Column("evidence_notes", TypeName = "text")]
        [Required]
        public string EvidenceNotes { get; set; } = string.Empty;
    }
}
