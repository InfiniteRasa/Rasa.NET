using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class EquipableClassEntry : IHasId
    {
        public const string TableName = "equipableclass";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("slot_id")]
        [Required]
        public uint SlotId { get; set; }
    }
}
