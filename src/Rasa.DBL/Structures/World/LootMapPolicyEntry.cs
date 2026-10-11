using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class LootMapPolicyEntry
    {
        public const string TableName = "loot_map_policy";

        [Key]
        [Column("map_context_id")]
        public uint MapContextId { get; set; }

        [Column("require_profiles")]
        public bool RequireProfiles { get; set; }
    }
}
