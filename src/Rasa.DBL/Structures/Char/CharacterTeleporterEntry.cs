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

        [Column("waypoint_type")]
        [Required]
        public byte WaypointType { get; set; }

        public CharacterTeleporterEntry(uint characterId, uint waypointId, byte waypointType)
        {
            CharacterId = characterId;
            WaypointId = waypointId;
            WaypointType = waypointType;
        }

        public CharacterTeleporterEntry()
        {
        }
    }
}
