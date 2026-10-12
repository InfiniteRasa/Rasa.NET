using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A usable world object the server puts on a map (the game server's PlacedUsables): a switch,
    /// a door, an item dispenser or acceptor, a random explosion, a bomb. The client has their
    /// states, animations and effects, and no .map places them, so they were the live servers' to
    /// put down. world_destructible has the destructible ones.
    ///
    /// The class decides what it is (its usable augmentation); the columns say how it starts and
    /// what it does. 0 in any of the number columns is the kind's default.
    /// </summary>
    [Table(TableName)]
    public class WorldUsableEntry : IHasId, IHasPosition
    {
        public const string TableName = "world_usable";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The object: an entity class of the client's with a usable augmentation.</summary>
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

        /// <summary>The usable state it starts in (UseObjectState); 0 for its kind's.</summary>
        [Column("state")]
        [Required]
        public uint State { get; set; }

        /// <summary>1 if a player may use it: a switch steps on, a door opens and closes, a dispenser gives, an acceptor takes, a bomb is armed.</summary>
        [Column("usable")]
        [Required]
        public uint Usable { get; set; }

        /// <summary>An item dispenser's item (itemtemplate.id).</summary>
        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>The item an item acceptor takes, or a bomb is armed with (its entity class).</summary>
        [Column("item_class_id")]
        [Required]
        public uint ItemClassId { get; set; }

        /// <summary>How many are given or taken; 0 for one.</summary>
        [Column("item_count")]
        [Required]
        public uint ItemCount { get; set; }

        /// <summary>
        /// Seconds until it is back as it was: a dispenser active again, an acceptor ready again, a
        /// bomb disarmed again, a door closed again, a switch back in its start state (0 for a door or
        /// a switch: it stays as it is left).
        /// </summary>
        [Column("reset_seconds")]
        [Required]
        public uint ResetSeconds { get; set; }

        /// <summary>A random explosion's shortest wait between blasts, or a bomb's fuse, in seconds.</summary>
        [Column("interval_min_seconds")]
        [Required]
        public uint IntervalMinSeconds { get; set; }

        /// <summary>A random explosion's longest wait between blasts, in seconds.</summary>
        [Column("interval_max_seconds")]
        [Required]
        public uint IntervalMaxSeconds { get; set; }

        /// <summary>A blast's least damage to each player it reaches.</summary>
        [Column("damage_min")]
        [Required]
        public uint DamageMin { get; set; }

        /// <summary>A blast's most damage to each player it reaches.</summary>
        [Column("damage_max")]
        [Required]
        public uint DamageMax { get; set; }

        /// <summary>How far a blast reaches, in metres.</summary>
        [Column("radius")]
        [Required]
        public double Radius { get; set; }

        /// <summary>A blast's OBJ_DAMAGE arg: 1 small explosion, 2 large, 3 lightning, 4 mine, 5 bomb, 6 geyser, 7 hornito.</summary>
        [Column("action_arg")]
        [Required]
        public uint ActionArg { get; set; }

        [Column("comment", TypeName = "varchar(128)")]
        [Required]
        public string Comment { get; set; }

        public Vector3 Position => new((float)PosX, (float)PosY, (float)PosZ);
    }
}
