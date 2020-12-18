using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class ExperienceForLevelEntry
    {
        public const string TableName = "player_exp_for_level";

        [Key]
        [Column("level")]
        [Required]
        public uint Level { get; set; }

        [Column("experience")]
        [Required]
        public long Experience { get; set; }
    }
}