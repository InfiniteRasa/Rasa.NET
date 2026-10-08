using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// One of the client's ambient figures standing on a map: a soldier at a firing range, two
    /// men talking on chairs, an officer reading a tablet. Each is an entity class of the
    /// client's with the STATELESSSWITCH augmentation (UsableStatelessNPC*: 43 of them) - a
    /// model of its own with its own looping animations, no name, no target and nothing to use.
    /// It is not a creature: nothing can be done to it and it does nothing but its animation.
    ///
    /// No .map places one; they were server world data, so these are rows.
    /// </summary>
    [Table(TableName)]
    public class AmbientNpcEntry : IHasId, IHasPosition
    {
        public const string TableName = "ambient_npc";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The figure: an entity class with the STATELESSSWITCH augmentation.</summary>
        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        /// <summary>The ground it stands on: the model's origin is at its feet, seated or not.</summary>
        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>Yaw in radians, as a player's rotation: the way the figure faces.</summary>
        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        [Column("comment", TypeName = "varchar(96)")]
        [Required]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
