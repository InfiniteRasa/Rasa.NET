using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A squad's instance of a map (the game server's squad instances): which map, whose it is,
    /// and since when. Written when the instance is made and deleted when it is closed, so an
    /// instance lasts through a restart. One per owner per map; the server keeps that so.
    /// </summary>
    [Table(TableName)]
    public class SquadInstanceEntry
    {
        public const string TableName = "squad_instance";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        /// <summary>The character of the squad's leader, or of a player in no squad.</summary>
        [Column("owner_character_id")]
        [Required]
        public uint OwnerCharacterId { get; set; }

        /// <summary>
        /// When it was made, or when a weekly reset last passed it over for having players in it:
        /// Unix time in milliseconds, UTC. The weekly reset it missed while the server was down is
        /// told by this.
        /// </summary>
        [Column("created_at")]
        [Required]
        public long CreatedAt { get; set; }
    }

    /// <summary>
    /// A spawn pool of a squad's instance whose creatures are all dead (the world database's
    /// spawnpool row with this id), and when the last of them died. Deleted when the pool comes
    /// back. A pool with no row has its creatures, or is due them.
    /// </summary>
    [Table(TableName)]
    public class SquadInstancePoolEntry
    {
        public const string TableName = "squad_instance_pool";

        [Column("instance_id")]
        [Required]
        public uint InstanceId { get; set; }

        [Column("spawnpool_id")]
        [Required]
        public uint SpawnpoolId { get; set; }

        /// <summary>Unix time in milliseconds, UTC.</summary>
        [Column("cleared_at")]
        [Required]
        public long ClearedAt { get; set; }
    }

    /// <summary>
    /// The squad instance a character last went into: where they are put back when they next
    /// enter the world on its map. One row a character, written over each time they go into one.
    /// </summary>
    [Table(TableName)]
    public class SquadInstanceVisitorEntry
    {
        public const string TableName = "squad_instance_visitor";

        [Key]
        [Column("character_id")]
        public uint CharacterId { get; set; }

        [Column("instance_id")]
        [Required]
        public uint InstanceId { get; set; }
    }
}
