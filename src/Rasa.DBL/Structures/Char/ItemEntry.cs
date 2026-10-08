using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    using Repositories.Char.Items;

    [Table(TableName)]
    public class ItemEntry
    {
        public const string TableName = "items";

        /// <summary>The module slots an item has: the client's MODULE_SLOTS.</summary>
        public const int ModuleSlots = 4;

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
            SetModules(item.ModuleIds);
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

        /// <summary>
        /// The module in each of the item's four module slots: a module_class id of the world
        /// database, 0 for an empty slot. The slots are places, not a list - the crafting
        /// station puts a module in the one the player picks and takes one out of the one
        /// picked - so an empty slot can come before a full one.
        /// </summary>
        [Column("module_1")]
        [Required]
        public uint Module1 { get; set; }

        [Column("module_2")]
        [Required]
        public uint Module2 { get; set; }

        [Column("module_3")]
        [Required]
        public uint Module3 { get; set; }

        [Column("module_4")]
        [Required]
        public uint Module4 { get; set; }

        /// <summary>The four slots, in order.</summary>
        [NotMapped]
        public uint[] Modules => new[] { Module1, Module2, Module3, Module4 };

        /// <summary>Fills the four slots from the first four given; the rest are emptied.</summary>
        public void SetModules(IReadOnlyList<uint> moduleIds)
        {
            Module1 = moduleIds != null && moduleIds.Count > 0 ? moduleIds[0] : 0;
            Module2 = moduleIds != null && moduleIds.Count > 1 ? moduleIds[1] : 0;
            Module3 = moduleIds != null && moduleIds.Count > 2 ? moduleIds[2] : 0;
            Module4 = moduleIds != null && moduleIds.Count > 3 ? moduleIds[3] : 0;
        }

        [Column("crafter_name", TypeName = "varchar(64)")]
        [Required]
        public string CrafterName { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
