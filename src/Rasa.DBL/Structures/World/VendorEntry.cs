using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class VendorEntry : IHasId
    {
        public const string TableName = "vendor";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("package_id")]
        [Required]
        public uint PackageId { get; set; }
    }
}
