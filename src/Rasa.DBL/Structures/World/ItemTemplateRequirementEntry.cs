using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemTemplateRequirementEntry : IHasId
    {
        public const string TableName = "itemtemplate_requirement";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("req_type")]
        [Required]
        public byte RequirementType { get; set; }

        [Column("req_value")]
        [Required]
        public byte RequirementValue { get; set; }
    }
}
