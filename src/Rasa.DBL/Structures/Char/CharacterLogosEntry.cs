using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterLogosEntry
    {
        public const string TableName = "character_logos";

        public CharacterLogosEntry()
        {
        }

        public CharacterLogosEntry(uint characterId, uint logosId)
        {
            CharacterId = characterId;
            LogosId = logosId;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("logos_id")]
        [Required]
        public uint LogosId { get; set; }
    }
}
