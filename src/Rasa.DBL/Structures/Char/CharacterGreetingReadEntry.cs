using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// An NPC's important line that the character has read: the NPC by its creature row, and
    /// the line it was - an id of the client's npcgreetinglanguage. One row per NPC, keyed on
    /// the character and the creature (CharContext); reading another line of the same NPC
    /// replaces the one before.
    ///
    /// What the speech bubble goes by (the game server's NpcGreetings): an NPC whose line is
    /// marked important has it over its head for a character until that character has read
    /// the line, and not again. The line is kept so that an NPC given a new one is unread once
    /// more. The world database says which NPCs are marked (npc_greeting.important); this says
    /// who has been to them.
    ///
    /// Rows are not taken out with a character, as its boss kills and titles are not: a
    /// character's id is never given to another.
    /// </summary>
    [Table(TableName)]
    public class CharacterGreetingReadEntry
    {
        public const string TableName = "character_greeting_read";

        public CharacterGreetingReadEntry()
        {
        }

        public CharacterGreetingReadEntry(uint characterId, uint creatureId, uint greetingId)
        {
            CharacterId = characterId;
            CreatureId = creatureId;
            GreetingId = greetingId;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The NPC: its creature row in the world database.</summary>
        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        /// <summary>The line that was read.</summary>
        [Column("greeting_id")]
        [Required]
        public uint GreetingId { get; set; }
    }
}
