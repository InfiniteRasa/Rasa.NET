using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class SpawnPoolEntry : IHasId, IHasPosition
    {
        public const string TableName = "spawnpool";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("mode")]
        [Required]
        public byte Mode { get; set; }

        [Column("anim_type")]
        [Required]
        public byte AnimType { get; set; }

        [Column("respown_time")]
        [Required]
        public uint RespawnTime { get; set; }

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

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("creature_1_Id")]
        [Required]
        public uint Creature1Id { get; set; }

        [Column("creature_1_min_count")]
        [Required]
        public byte Creature1MinCount { get; set; }

        [Column("creature_1_max_count")]
        [Required]
        public byte Creature1MaxCount { get; set; }

        [Column("creature_2_Id")]
        [Required]
        public uint Creature2Id { get; set; }

        [Column("creature_2_min_count")]
        [Required]
        public byte Creature2MinCount { get; set; }

        [Column("creature_2_max_count")]
        [Required]
        public byte Creature2MaxCount { get; set; }

        [Column("creature_3_Id")]
        [Required]
        public uint Creature3Id { get; set; }

        [Column("creature_3_min_count")]
        [Required]
        public byte Creature3MinCount { get; set; }

        [Column("creature_3_max_count")]
        [Required]
        public byte Creature3MaxCount { get; set; }

        [Column("creature_4_Id")]
        [Required]
        public uint Creature4Id { get; set; }

        [Column("creature_4_min_count")]
        [Required]
        public byte Creature4MinCount { get; set; }

        [Column("creature_4_max_count")]
        [Required]
        public byte Creature4MaxCount { get; set; }

        [Column("creature_5_Id")]
        [Required]
        public uint Creature5Id { get; set; }

        [Column("creature_5_min_count")]
        [Required]
        public byte Creature5MinCount { get; set; }

        [Column("creature_5_max_count")]
        [Required]
        public byte Creature5MaxCount { get; set; }

        [Column("creature_6_Id")]
        [Required]
        public uint Creature6Id { get; set; }

        [Column("creature_6_min_count")]
        [Required]
        public byte Creature6MinCount { get; set; }

        [Column("creature_6_max_count")]
        [Required]
        public byte Creature6MaxCount { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
