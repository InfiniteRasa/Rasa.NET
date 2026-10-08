using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// The line an NPC greets a player with: an id of the client's npcgreetinglanguage, by the
    /// creature row. The client has the 1,699 lines and nothing that says whose each is - that
    /// was the server's to know - so a row here is either one the text itself gives away
    /// (NpcGreetingSeed) or one a game master has set (".greeting"). An NPC with no row says the
    /// game server's default, "Greetings.".
    ///
    /// A table of its own rather than a column on creature, as creature_actor_name is: the
    /// preloaders of earlier migrations insert creature by its entity's columns.
    /// </summary>
    [Table(TableName)]
    public class NpcGreetingEntry : IHasId
    {
        public const string TableName = "npc_greeting";

        /// <summary>The creature row.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>The line: an id of the client's npcgreetinglanguage.</summary>
        [Column("greeting_id")]
        [Required]
        public uint GreetingId { get; set; }

        /// <summary>
        /// Whether the NPC is marked as having something to say: the client's important
        /// greeting, a speech bubble over its head while it has nothing else for the player.
        /// Which NPCs were was the server's to know, as the lines were; a game master sets it
        /// (".greeting important"), and one starts with it (NpcGreetingSeed.Marked).
        /// </summary>
        [Column("important")]
        [Required]
        public bool Important { get; set; }
    }
}
