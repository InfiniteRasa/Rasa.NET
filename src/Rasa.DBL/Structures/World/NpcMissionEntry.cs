using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class NpcMissionEntry :IHasId
    {
        public const string TableName = "npc_mission";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("giver_id")]
        [Required]
        public uint GiverId { get; set; }

        [Column("reciver_id")]
        [Required]
        public uint ReciverId { get; set; }

        [Column("level")]
        [Required]
        public uint Level { get; set; }

        [Column("group_type")]
        [Required]
        public byte GroupType { get; set; }

        [Column("category_id")]
        [Required]
        public byte CategoryId { get; set; }

        [Column("shareable")]
        [Required]
        public bool Shareable { get; set; }

        [Column("radio_completeable")]
        [Required]
        public bool RadioCompleteable { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; }
    }
}
