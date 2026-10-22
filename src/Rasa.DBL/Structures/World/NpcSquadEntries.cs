using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A kind of NPC squad (the game server's NpcSquads): what it is made of (npc_squad_member),
    /// how it stands and walks together, and how far it looks for a fight. An AFS rifle squad,
    /// a Forean war party, a Bane patrol: what side it is on is its creatures'.
    ///
    /// The ids of this table and the squad ones below are given by whoever makes the row - a GM's
    /// .squad command takes the next free one - not by the database.
    /// </summary>
    [Table(TableName)]
    public class NpcSquadTypeEntry : IHasId
    {
        public const string TableName = "npc_squad_type";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        /// <summary>How the members stand about the leader: 0 a wedge, 1 a column, 2 a line abreast.</summary>
        [Column("formation")]
        [Required]
        public byte Formation { get; set; }

        /// <summary>How far apart the members stand, in metres.</summary>
        [Column("spacing")]
        [Required]
        public double Spacing { get; set; }

        /// <summary>How far each member looks for something to fight, in metres; 0 for its creature's own aggro range.</summary>
        [Column("engage_range")]
        [Required]
        public double EngageRange { get; set; }
    }

    /// <summary>
    /// One line of a squad type: a creature row and how many of it come with each squad, from
    /// <see cref="MinCount"/> to <see cref="MaxCount"/>, drawn each time it is sent. The lines
    /// are taken in slot order, so the first line's first creature is the leader.
    ///
    /// The key is the type and the slot, set in WorldContext.
    /// </summary>
    [Table(TableName)]
    public class NpcSquadMemberEntry
    {
        public const string TableName = "npc_squad_member";

        [Column("type_id")]
        [Required]
        public uint TypeId { get; set; }

        [Column("slot")]
        [Required]
        public byte Slot { get; set; }

        /// <summary>The creature, as in creature.id.</summary>
        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("min_count")]
        [Required]
        public byte MinCount { get; set; }

        [Column("max_count")]
        [Required]
        public byte MaxCount { get; set; }
    }

    /// <summary>
    /// A route a squad walks on one map: its points (npc_squad_path_point), and what it does at
    /// the end of them - <see cref="Mode"/> 0 back to the first and round again, 1 back the way it
    /// came, 2 stop there and hold.
    /// </summary>
    [Table(TableName)]
    public class NpcSquadPathEntry : IHasId
    {
        public const string TableName = "npc_squad_path";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        [Column("mode")]
        [Required]
        public byte Mode { get; set; }
    }

    /// <summary>
    /// A point of a squad's route: where the leader goes next, across the navmesh, and how long
    /// the squad stands there once it is. The key is the path and the step, set in WorldContext.
    /// </summary>
    [Table(TableName)]
    public class NpcSquadPathPointEntry
    {
        public const string TableName = "npc_squad_path_point";

        [Column("path_id")]
        [Required]
        public uint PathId { get; set; }

        /// <summary>Its place on the route: walked in this order, from the lowest.</summary>
        [Column("step")]
        [Required]
        public uint Step { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>How long the squad stands there before it goes on; 0 to go straight on.</summary>
        [Column("pause_ms")]
        [Required]
        public uint PauseMs { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }

    /// <summary>
    /// Where a squad of a type comes into the world, facing which way, the route it walks (none:
    /// it holds the spot), and how long after the last of it falls it is sent again.
    /// </summary>
    [Table(TableName)]
    public class NpcSquadSpawnEntry : IHasId
    {
        public const string TableName = "npc_squad_spawn";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The squad type, as in npc_squad_type.id.</summary>
        [Column("type_id")]
        [Required]
        public uint TypeId { get; set; }

        /// <summary>The route, as in npc_squad_path.id; 0 to hold the spot.</summary>
        [Column("path_id")]
        [Required]
        public uint PathId { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        /// <summary>The way the squad faces as it comes, a yaw in radians.</summary>
        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        /// <summary>How long after the last of the squad falls it is sent again, in seconds.</summary>
        [Column("respawn_seconds")]
        [Required]
        public uint RespawnSeconds { get; set; }

        /// <summary>1 to send it, 0 to leave it out.</summary>
        [Column("enabled")]
        [Required]
        public byte Enabled { get; set; }

        [Column("comment", TypeName = "varchar(64)")]
        [Required]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
