using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterTitleEntry
    {
        public const string TableName = "character_title";

        public CharacterTitleEntry()
        {
        }

        public CharacterTitleEntry(uint characterId, uint titleId)
        {
            CharacterId = characterId;
            TitleId = titleId;
        }

        [Key]
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("title_id")]
        [Required]
        public uint TitleId { get; set; }
    }
}
