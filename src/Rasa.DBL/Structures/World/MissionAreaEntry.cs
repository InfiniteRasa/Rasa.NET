using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionAreaShape : byte
    {
        Sphere = 1,
        Cylinder = 2,
        Box = 3
    }

    [Table(TableName)]
    public class MissionAreaEntry
    {
        public const string TableName = "mission_area";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("area_id")]
        [Required]
        public uint AreaId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("shape")]
        [Required]
        public MissionAreaShape Shape { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        [Column("radius")]
        public double? Radius { get; set; }

        [Column("extent_x")]
        public double? ExtentX { get; set; }

        [Column("extent_y")]
        public double? ExtentY { get; set; }

        [Column("extent_z")]
        public double? ExtentZ { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionContentDefinitionEntry Content { get; set; }
    }
}
