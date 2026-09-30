using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    using Repositories.Char.Items;

    [Table(TableName)]
    public class ItemEntry
    {
        public const string TableName = "items";

        public ItemEntry()
        {
        }

        public ItemEntry(IItemChange item)
        {
            Color = item.Color;
            CrafterName = item.Crafter;
            CurrentHitPoints = item.CurrentHitPoints;
            ItemTemplateId = item.ItemTemplateId;
            StackSize = item.StackSize;
            BoundCharacterId = item.BoundCharacterId;
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("stack_size")]
        [Required]
        public uint StackSize { get; set; }

        [Column("current_hp")]
        [Required]
        public int CurrentHitPoints { get; set; }

        [Column("color")]
        [Required]
        public uint Color { get; set; }

        [Column("ammo_count")]
        [Required]
        public uint AmmoCount { get; set; }

        /// <summary>
        /// The character this one item is bound to, 0 when it is not. Set when a Bind on Equip
        /// item is first equipped; the template's own bound_to_character_flag is the other way an
        /// item is bound, and does not name a character.
        /// </summary>
        [Column("bound_character_id")]
        [Required]
        public uint BoundCharacterId { get; set; }

        [Column("crafter_name", TypeName = "varchar(64)")]
        [Required]
        public string CrafterName { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
