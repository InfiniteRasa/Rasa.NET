using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// An FX package played at a spot on a map: an FXPackageEmitter (entity class 2873, the one
    /// class carrying the client's FXPACKAGEEMITTER augmentation). The client attaches the
    /// package - a smoke column, a fire, a looping ambient sound - to an invisible proxy there
    /// while it is on and stops it when it is off. No .map places one; they were server world
    /// data, so these are authored in game with the .emitter commands.
    /// </summary>
    [Table(TableName)]
    public class MapEmitterEntry : IHasId, IHasPosition
    {
        public const string TableName = "map_emitter";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>Yaw in radians, as a player's rotation; a directional package points this way.</summary>
        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        /// <summary>The client's string table id of the FX package asset (a .pkg).</summary>
        [Column("package_id")]
        [Required]
        public uint PackageId { get; set; }

        /// <summary>1: the package plays from when the map loads; 0: placed but off until turned on.</summary>
        [Column("is_on")]
        [Required]
        public byte IsOn { get; set; }

        [Column("comment", TypeName = "varchar(96)")]
        [Required]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
