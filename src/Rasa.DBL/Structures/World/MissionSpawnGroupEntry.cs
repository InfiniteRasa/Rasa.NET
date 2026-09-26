using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    public enum MissionSpawnGroupPolicy : byte
    {
        OrdinaryRespawn = 0,
        ScenarioControlled = 1
    }

    [Table(TableName)]
    public class MissionSpawnGroupEntry
    {
        public const string TableName = "mission_spawn_group";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("content_revision", TypeName = "varchar(32)")]
        [Required]
        public string ContentRevision { get; set; } = string.Empty;

        [Column("spawn_group_id")]
        [Required]
        public uint SpawnGroupId { get; set; }

        [Column("requirement")]
        [Required]
        public MissionContentRequirement Requirement { get; set; } =
            MissionContentRequirement.Required;

        [Column("area_id")]
        public uint? AreaId { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("enabled")]
        [Required]
        public bool Enabled { get; set; }

        [Column("respawn_seconds")]
        public uint? RespawnSeconds { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; } = string.Empty;

        [Column("spawn_policy")]
        [Required]
        public MissionSpawnGroupPolicy SpawnPolicy { get; set; } =
            MissionSpawnGroupPolicy.OrdinaryRespawn;

        public MissionContentDefinitionEntry Content { get; set; }
        public ICollection<MissionSpawnEntry> Spawns { get; set; } =
            new List<MissionSpawnEntry>();
    }
}
