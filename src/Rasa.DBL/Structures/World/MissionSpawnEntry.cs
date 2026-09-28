using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class MissionSpawnEntry
    {
        public const string TableName = "mission_spawn";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("spawn_group_id")]
        [Required]
        public uint SpawnGroupId { get; set; }

        [Column("spawn_id")]
        [Required]
        public uint SpawnId { get; set; }

        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        [Column("quantity")]
        [Required]
        public uint Quantity { get; set; }

        public MissionSpawnGroupEntry SpawnGroup { get; set; }
    }
}
