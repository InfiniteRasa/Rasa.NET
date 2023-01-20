using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class CreatureEntry : IHasId
    {
        public const string TableName = "creature";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; }

        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

        [Column("faction")]
        [Required]
        public uint Faction { get; set; }

        [Column("level")]
        [Required]
        public uint Level { get; set; }

        [Column("max_hp")]
        [Required]
        public uint MaxHitPoints { get; set; }

        [Column("name_id")]
        [Required]
        public uint NameId { get; set; }

        [Column("run_speed")]
        [Required]
        public uint RunSpeed { get; set; }

        [Column("walk_speed")]
        [Required]
        public uint WalkSpeed { get; set; }

        [Column("action1")]
        [Required]
        public uint Action1 { get; set; }

        [Column("action2")]
        [Required]
        public uint Action2 { get; set; }

        [Column("action3")]
        [Required]
        public uint Action3 { get; set; }

        [Column("action4")]
        [Required]
        public uint Action4 { get; set; }

        [Column("action5")]
        [Required]
        public uint Action5 { get; set; }

        [Column("action6")]
        [Required]
        public uint Action6 { get; set; }

        [Column("action7")]
        [Required]
        public uint Action7 { get; set; }

        [Column("action8")]
        [Required]
        public uint Action8 { get; set; }
    }
}
