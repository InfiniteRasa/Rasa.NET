using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public sealed class CharacterFlagEntry
    {
        public const string TableName = "character_flag";

        public CharacterFlagEntry() { }
        public CharacterFlagEntry(uint characterId, uint flagId, uint value = 1)
        {
            CharacterId = characterId;
            FlagId = flagId;
            Value = value;
        }

        [Column("character_id"), Required] public uint CharacterId { get; set; }
        [Column("flag_id"), Required] public uint FlagId { get; set; }
        [Column("value"), Required] public uint Value { get; set; }
        public CharacterEntry Character { get; set; }
    }
}
