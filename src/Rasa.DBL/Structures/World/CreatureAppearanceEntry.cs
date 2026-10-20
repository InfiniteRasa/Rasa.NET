using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    [Keyless]
    [Index(nameof(Id), nameof(SlotId), Name = "creature_appearance_index_creature_slot")]
    public class CreatureAppearanceEntry
    {
        public const string TableName = "creature_appearance";
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("slot_id")]
        [Required]
        public uint SlotId { get; set; }

        [Column("Class_id")]
        [Required]
        public uint ClassId { get; set; }

        [Column("color")]
        [Required]
        public uint Color { get; set; }

    }
}
