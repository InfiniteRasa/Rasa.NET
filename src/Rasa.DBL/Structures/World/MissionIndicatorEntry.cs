using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class MissionIndicatorEntry
    {
        public const string TableName = "mission_indicator";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("indicator_id")]
        [Required]
        public uint IndicatorId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

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
        [Required]
        public double Radius { get; set; }

        [Column("show_3d_effect")]
        [Required]
        public bool Show3DEffect { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        public MissionObjectiveDefinitionEntry Objective { get; set; }
    }
}
