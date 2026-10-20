using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A lootable container the server puts on a map (the game server's WorldContainers): a crate,
    /// a locker, a console, a corpse - a class of the client's with the TREASUREDISPENSER
    /// augmentation (64). No .map places one; the live servers did, and their placements were not
    /// kept, so every row here is given.
    ///
    /// What is in it is its fixed items (world_container_item), always given, and a roll of its
    /// loot profile (loot_profile, the ordinary loot tables) on top, if it names one. A squad
    /// opens one copy of it together and shares it as it shares a corpse; each player or squad
    /// has their own, and it is closed to them again <see cref="ResetSeconds"/> after they opened
    /// it. With a <see cref="CipherLevel"/> it is locked until they decode it with a cipher tool.
    /// </summary>
    [Table(TableName)]
    public class WorldContainerEntry : IHasId, IHasPosition
    {
        public const string TableName = "world_container";

        /// <summary>The row, given: a container's id is what its items and GM commands name it by.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The container: an entity class of the client's with the TREASUREDISPENSER augmentation.</summary>
        [Column("class_id")]
        [Required]
        public uint ClassId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        /// <summary>The ground it stands on: the model's origin.</summary>
        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>Yaw in radians, as a player's rotation: the way it faces.</summary>
        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        /// <summary>A loot_profile rolled on top of the fixed items each time it is opened, or null for the fixed items alone.</summary>
        [Column("loot_profile_key", TypeName = "varchar(64)")]
        public string LootProfileKey { get; set; }

        /// <summary>The Specialist Tools level a cipher needs to open it; 0 for no lock.</summary>
        [Column("cipher_level")]
        [Required]
        public uint CipherLevel { get; set; }

        /// <summary>How long after a player or squad opened it it is closed and full for them again; 0 for the default (WorldContainers.DefaultResetSeconds).</summary>
        [Column("reset_seconds")]
        [Required]
        public uint ResetSeconds { get; set; }

        [Column("comment", TypeName = "varchar(200)")]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
