using Rasa.Structures.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class ArmorClassEntry : IHasId
    {
        public const string TableName = "armorclass";

        [Key]
        [Column("class_id")]
        [Required]
        public uint Id { get; set; }

        [Column("min_damage_absorbed")]
        [Required]
        public uint MinDamageAbsorbed { get; set; }

        [Column("max_damage_absorbed")]
        [Required]
        public uint MaxDamageAbsorbed { get; set; }

        [Column("regen_rate")]
        [Required]
        public int RegenRate { get; set; }
    }
}
