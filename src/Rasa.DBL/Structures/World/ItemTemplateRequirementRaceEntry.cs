using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemTemplateRequirementRaceEntry : IHasId
    {
        public const string TableName = "itemtemplate_requirement_race";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("race_id")]
        [Required]
        public byte RaceId { get; set; }
    }
}
