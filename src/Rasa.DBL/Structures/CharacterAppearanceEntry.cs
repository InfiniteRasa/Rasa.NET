using System.ComponentModel.DataAnnotations;

namespace Rasa.Structures
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class CharacterAppearanceEntry
    {
        [Key]
        [Column("character_id")]
        public uint CharacterId { get; set; }

        [Key]
        public uint Slot { get; set; }

        [ForeignKey(nameof(CharacterId))]
        public CharacterEntry Character { get; set; }

        public uint Class { get; set; }

        public uint Color { get; set; }
    }
}
