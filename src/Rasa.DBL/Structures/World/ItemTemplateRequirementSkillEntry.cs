using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemTemplateRequirementSkillEntry : IHasId
    {
        public const string TableName = "itemtemplate_requirement_skill";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("skill_id")]
        [Required]
        public int SkillId { get; set; }

        [Column("skill_level")]
        [Required]
        public byte SkillLevel { get; set; }
    }
}
