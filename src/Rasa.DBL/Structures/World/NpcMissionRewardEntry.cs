using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    [Keyless]
    public class NpcMissionRewardEntry : IHasId
    {
        public const string TableName = "npc_mission_reward";

        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("type")]
        [Required]
        public byte Type { get; set; }

        [Column("credits")]
        [Required]
        public int Credits { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("quantity")]
        [Required]
        public uint Quantity { get; set; }
    }
}
