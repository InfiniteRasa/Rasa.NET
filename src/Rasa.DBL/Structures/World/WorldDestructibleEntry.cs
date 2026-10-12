using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A destructible world object the server puts on a map (the game server's
    /// PlacedDestructibles): a Bane barrel, a locker, a sleep pod, a Warnet hive, a force field
    /// across a gate. The client has their destroyed states, explosions and wrecks, and no .map
    /// places them, so they were the live servers' to put down. Players shoot them down and they
    /// come back 45 to 60 seconds later.
    ///
    /// The class decides what it is: an InertDestroyable, a TeslaCoil, a CreatureSpawner or
    /// DestroyableCreatureSpawner, a DestroyableStatelessSwitch, or a force field (FORCEFIELD,
    /// CLANFORCEFIELD, OWNABLEFORCEFIELD). The other columns are 0 for the class's own defaults.
    /// </summary>
    [Table(TableName)]
    public class WorldDestructibleEntry : IHasId, IHasPosition
    {
        public const string TableName = "world_destructible";

        /// <summary><see cref="Side"/>: the class's default - a force field is the Bane's, which players can shoot.</summary>
        public const uint SideDefault = 0;

        /// <summary><see cref="Side"/>: the AFS's force field, which lets players through and stops hostile creatures.</summary>
        public const uint SideAfs = 1;

        /// <summary><see cref="Side"/>: the Bane's force field, which stops players.</summary>
        public const uint SideBane = 2;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The object: an entity class of the client's with a destroyed state.</summary>
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

        /// <summary>Its hit points; 0 for its class's (small, medium or large).</summary>
        [Column("hit_points")]
        [Required]
        public uint HitPoints { get; set; }

        /// <summary>A creature spawner's creature (creature.id); 0 for its class's, if it has one.</summary>
        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        /// <summary>How many of the creature come out at a time; 0 for its class's.</summary>
        [Column("creature_count")]
        [Required]
        public uint CreatureCount { get; set; }

        /// <summary>A force field's side: <see cref="SideDefault"/>, <see cref="SideAfs"/> or <see cref="SideBane"/>.</summary>
        [Column("side")]
        [Required]
        public uint Side { get; set; }

        [Column("comment", TypeName = "varchar(128)")]
        [Required]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
