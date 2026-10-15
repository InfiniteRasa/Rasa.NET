using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A bark an NPC says now and then (the game server's NpcBarks): one of the client's barks
    /// (generated/client/bark and barklanguage), a line in a bubble over the NPC and, for some,
    /// a voice. Which NPC barks which was the 2009 server's and the client carries none of it,
    /// so every row here is given. An NPC with several barks says one of them each time; one
    /// with none never barks.
    ///
    /// The key is the creature row and the bark, set in WorldContext: a composite key cannot be
    /// declared with attributes.
    /// </summary>
    [Table(TableName)]
    public class NpcBarkEntry
    {
        public const string TableName = "npc_bark";

        /// <summary>The creature row (creature.id).</summary>
        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        /// <summary>The bark, as in the client's generated/client/bark.</summary>
        [Column("bark_id")]
        [Required]
        public uint BarkId { get; set; }
    }
}
