using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A control point of the open world: the Eloh node the AFS and the Bane fight over (the game
    /// server's ControlPoints). The client has a map marker for each - its id, name and position
    /// are from the client's uimapmarker table - and nothing else: which side starts with it, and
    /// what stands there for each side (ControlPointLinkEntry), were server data.
    ///
    /// The position is the marker's, which is drawn near the object rather than on it; the
    /// server sets the object on the ground beneath (NavMeshManager.SnapToGround), and
    /// ".cp move" puts it where a game master stands.
    /// </summary>
    [Table(TableName)]
    public class ControlPointEntry : IHasId, IHasPosition
    {
        public const string TableName = "control_point";

        /// <summary><see cref="DefaultOwner"/>: the Bane hold it until it is taken.</summary>
        public const byte OwnerBane = 0;

        /// <summary><see cref="DefaultOwner"/>: the AFS hold it until it is lost.</summary>
        public const byte OwnerAfs = 1;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The point's name as the capture messages say it: "Purgas", "Fort Dew".</summary>
        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        /// <summary>The usable's entity class: the Eloh control point (3814) or a Phi Resonator (26486).</summary>
        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

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

        /// <summary>The client's entity id of the point's map marker, which its owner is sent under.</summary>
        [Column("marker_entity_id")]
        [Required]
        public ulong MarkerEntityId { get; set; }

        /// <summary>Who holds the point until it first changes hands: <see cref="OwnerBane"/> or <see cref="OwnerAfs"/>.</summary>
        [Column("default_owner")]
        [Required]
        public byte DefaultOwner { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
