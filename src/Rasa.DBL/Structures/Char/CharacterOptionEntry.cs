using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterOptionEntry
    {
        public const string TableName = "character_option";

        public CharacterOptionEntry()
        {
        }

        public CharacterOptionEntry(uint characterId, uint optionId, string value)
        {
            CharacterId = characterId;
            OptionId = optionId;
            Value = value;
        }
        
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("option_id")]
        [Required]
        public uint OptionId { get; set; }

        [Column("value", TypeName = "varchar(50)")]
        [Required]
        public string Value { get; set; }
    }
}
