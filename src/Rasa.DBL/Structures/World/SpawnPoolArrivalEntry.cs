using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// Where a spawn pool's creatures arrive: a Bane landing pad or dropship bay the Bane dropship
    /// comes down on, or a Bane teleporter they come through. The arrival points are map scenery
    /// (ArchBaneObjLandingpadv02, ArchBaneOutpostLaunchDropshipBay, UsableTwoStateBaneTeleporterV01);
    /// a pool with one or more arrives at one of them each time, and its creatures walk from there
    /// to their ground.
    /// </summary>
    [Table(TableName)]
    public class SpawnPoolArrivalEntry : IHasId, IHasPosition
    {
        public const string TableName = "spawnpool_arrival";

        /// <summary>The Bane dropship lands at the point.</summary>
        public const byte KindDropship = 1;

        /// <summary>The creatures come through the map's teleporter at the point, which is switched on for it.</summary>
        public const byte KindTeleporter = 2;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("pool_id")]
        [Required]
        public uint PoolId { get; set; }

        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        /// <summary>The walkable surface at the arrival: a pad's deck, a bay's floor, a teleporter's plate.</summary>
        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>Yaw in radians: the pad's or teleporter's own heading.</summary>
        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        /// <summary>The map's own entity id of the teleporter to switch (above 2^32: the client builds it from the .map); 0 for a dropship.</summary>
        [Column("entity_id")]
        [Required]
        public ulong EntityId { get; set; }

        [Column("comment", TypeName = "varchar(96)")]
        [Required]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
