using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterAppearanceEntry
    {
        public const string TableName = "character_appearance";

        public CharacterAppearanceEntry()
        {
        }

        public CharacterAppearanceEntry(uint slot, uint classId, uint color)
        {
            Slot = slot;
            Class = classId;
            Color = color;
        }

        [Column("character_id")]
        public uint CharacterId { get; set; }

        [Column("slot")]
        public uint Slot { get; set; }

        public CharacterEntry Character { get; set; }

        [Column("class")]
        public uint Class { get; set; }

        [Column("color")]
        public uint Color { get; set; }
    }
}
