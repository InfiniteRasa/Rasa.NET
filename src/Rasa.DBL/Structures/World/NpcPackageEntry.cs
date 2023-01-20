using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class NpcPackageEntry : IHasId
    {
        public const string TableName = "npc_package";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("package_id")]
        [Required]
        public uint PackageId { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; }
    }
}
