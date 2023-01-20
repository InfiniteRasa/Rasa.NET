using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterTeleporterEntry
    {
        public const string TableName = "character_teleporter";
        
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("waypointId")]
        [Required]
        public uint WaypointId { get; set; }

        public CharacterTeleporterEntry(uint characterId, uint waypointId)
        {
            CharacterId = characterId;
            WaypointId = waypointId;
        }

        public CharacterTeleporterEntry()
        {
        }
    }
}
