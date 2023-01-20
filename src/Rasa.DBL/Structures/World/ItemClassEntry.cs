using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemClassEntry : IHasId
    {
        public const string TableName = "itemclass";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("inventory_icon_string_id")]
        [Required]
        public uint InventoryIconStringId { get; set; }

        [Column("loot_value")]
        [Required]
        public uint LootValue { get; set; }

        [Column("hidden_invenotry_flag", TypeName = "bit")]
        [Required]
        public byte HidenInventoryFlag { get; set; }

        [Column("is_consumable_flag", TypeName = "bit")]
        [Required]
        public byte IsConsumableFlag { get; set; }

        [Column("max_hp")]
        [Required]
        public int MaxHitPoints { get; set; }

        [Column("stack_size")]
        [Required]
        public uint StackSize { get; set; }

        [Column("drag_audio_set_id")]
        [Required]
        public uint DragAudioSetId { get; set; }

        [Column("drop_audio_set_id")]
        [Required]
        public uint DropAudioSetId { get; set; }
    }
}
