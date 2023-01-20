using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class TeleporterEntry : IHasId, IHasPosition
    {
        public const string TableName = "teleporter";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

        [Column("type")]
        [Required]
        public byte Type { get; set; }

        [Column("description", TypeName = "varchar(64)")]
        [Required]
        public string Description { get; set; }

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

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
