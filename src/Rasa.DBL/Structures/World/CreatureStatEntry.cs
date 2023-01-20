using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;
    
    [Table(TableName)]
    public class CreatureStatEntry : IHasId
    {
        public const string TableName = "creature_stat";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("body")]
        [Required]
        public int Body { get; set; }

        [Column("mind")]
        [Required]
        public int Mind { get; set; }

        [Column("spirit")]
        [Required]
        public int Spirit { get; set; }

        [Column("health")]
        [Required]
        public int Health { get; set; }

        [Column("armor")]
        [Required]
        public int Armor { get; set; }
    }
}
