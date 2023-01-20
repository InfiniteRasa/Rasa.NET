using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;
    using Microsoft.EntityFrameworkCore;

    [Keyless]
    [Table(TableName)]
    public class VendorItemEntry : IHasId
    {
        public const string TableName = "vendor_item";

        
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }
    }
}
