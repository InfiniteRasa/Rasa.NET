using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    public enum CharacterStartingExperienceState : byte
    {
        Pending = 1,
        Bootcamp = 2,
        Skipped = 3,
        Completed = 4,
        Legacy = 5
    }

    [Table(TableName)]
    public class CharacterStartingExperienceEntry
    {
        public const string TableName = "character_starting_experience";

        public CharacterStartingExperienceEntry()
        {
        }

        public CharacterStartingExperienceEntry(
            uint characterId,
            string contentRevision,
            CharacterStartingExperienceState state)
        {
            CharacterId = characterId;
            ContentRevision = contentRevision;
            State = state;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("state")]
        [Required]
        public CharacterStartingExperienceState State { get; set; }

        public CharacterEntry Character { get; set; }
    }
}
