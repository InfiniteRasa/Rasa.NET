using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;
    using Microsoft.EntityFrameworkCore;

    [Table(TableName)]
    [Keyless]
    public class ItemTemplateResistanceEntry : IHasId
    {
        public const string TableName = "itemtemplate_resistance";

        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("resistance_type")]
        [Required]
        public byte ResistanceType { get; set; }

        [Column("resistance_value")]
        [Required]
        public int ResistanceValue { get; set; }
    }
}
