using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class MapInfoEntry : IHasId
    {
        public const string TableName = "map_info";

        [Key]
        [Column("map_context_id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_name", TypeName = "varchar(50)")]
        [Required]
        public string MapName { get; set; }

        [Column("map_version")]
        [Required]
        public uint MapVersion { get; set; }

        [Column("base_region")]
        [Required]
        public uint BaseRegion { get; set; }
    }
}
