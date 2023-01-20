
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class CreatureActionEntry : IHasId
    {
        public const string TableName = "creature_action";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("description", TypeName = "varchar(50)")]
        [Required]
        public string Description { get; set; }

        [Column("action_id")]
        [Required]
        public uint ActionId { get; set; }

        [Column("action_arg_id")]
        [Required]
        public uint ActionArgId { get; set; }

        [Column("range_min")]
        [Required]
        public double RangeMin { get; set; }

        [Column("range_max")]
        [Required]
        public double RangeMax { get; set; }

        [Column("cooldown")]
        [Required]
        public uint Cooldown { get; set; }

        [Column("windup")]
        [Required]
        public uint Windup { get; set; }

        [Column("min_damage")]
        [Required]
        public uint MinDamage { get; set; }

        [Column("max_damage")]
        [Required]
        public uint MaxDamage { get; set; }
    }
}
