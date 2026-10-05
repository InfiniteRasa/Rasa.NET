using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A boss the character has killed, by the name the client knows it by (creature.name_id):
    /// one row per boss, keyed on both columns (CharContext). What the boss titles are given
    /// from (the game server's BossTitles).
    /// </summary>
    [Table(TableName)]
    public class CharacterBossKillEntry
    {
        public const string TableName = "character_boss_kill";

        public CharacterBossKillEntry()
        {
        }

        public CharacterBossKillEntry(uint characterId, uint creatureNameId)
        {
            CharacterId = characterId;
            CreatureNameId = creatureNameId;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("creature_name_id")]
        [Required]
        public uint CreatureNameId { get; set; }
    }
}
