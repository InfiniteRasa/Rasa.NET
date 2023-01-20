using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    [Table(TableName)]
    public class RandomNameEntry
    {
        public const string TableName = "player_random_name";

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        [Column("type")]
        [Required]
        public byte Type { get; set; }

        [Column("gender")]
        [Required]
        public byte Gender { get; set; }
    }
}