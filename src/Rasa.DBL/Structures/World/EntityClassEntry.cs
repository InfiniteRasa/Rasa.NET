using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;
    [Table(TableName)]
    public class EntityClassEntry : IHasId
    {
        public const string TableName = "entityclass";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("class_name", TypeName = "varchar(60)")]
        [Required]
        public string ClassName { get; set; }

        [Column("mesh_id")]
        [Required]
        public uint MeshId { get; set; }

        [Column("class_collision_role")]
        [Required]
        public byte ClassCollisionRole { get; set; }

        [Column("target_flag")]
        [Required]
        public byte TargetFlag { get; set; }

        [Column("aug_list", TypeName = "varchar(60)")]
        [Required]
        public string AugList { get; set; }
    }
}
