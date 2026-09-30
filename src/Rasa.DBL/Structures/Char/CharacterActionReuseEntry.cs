using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A cooldown a character logged out with: the action, and when it is ready again. Written
    /// when the character leaves the world, read back and removed when it is next loaded, so that
    /// logging out and in does not reset an hour-long account reward or a five-minute class wave.
    /// The client keys its reuse timers by action id alone (actor.py SetActionReuseTime), and so
    /// does the server (Actor.ActionReuseUntil).
    /// </summary>
    [Table(TableName)]
    public class CharacterActionReuseEntry
    {
        public const string TableName = "character_action_reuse";

        public CharacterActionReuseEntry()
        {
        }

        public CharacterActionReuseEntry(uint characterId, uint actionId, long readyAt)
        {
            CharacterId = characterId;
            ActionId = actionId;
            ReadyAt = readyAt;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("action_id")]
        [Required]
        public uint ActionId { get; set; }

        /// <summary>When the action is ready again: Unix time in milliseconds, UTC.</summary>
        [Column("ready_at")]
        [Required]
        public long ReadyAt { get; set; }
    }
}
