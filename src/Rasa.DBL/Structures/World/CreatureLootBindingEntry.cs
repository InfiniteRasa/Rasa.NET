using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class CreatureLootBindingEntry
    {
        public const string TableName = "creature_loot_binding";

        [Column("map_context_id")]
        public uint MapContextId { get; set; }

        [Column("creature_id")]
        public uint CreatureId { get; set; }

        [Column("profile_key", TypeName = "varchar(64)")]
        [MaxLength(64)]
        [Required]
        public string ProfileKey { get; set; } = string.Empty;
    }
}
