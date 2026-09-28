using Rasa.Managers;

namespace Rasa.Structures
{
    public class LootItem
    {
        public LootItem()
            : this(true)
        {
        }

        private LootItem(bool allocateEntityId)
        {
            if (allocateEntityId)
                EntityId = EntityManager.Instance.GetEntityId;
        }
        public ulong EntityId { get; set; }
        public uint ItemTemplateId { get; set; }
        public uint ItemClassId { get; set; }
        public uint ItemQuantity { get; set; }
        public ulong ActorId { get; set; }
        public uint PartyId { get; set; }

        /// <summary>
        /// The real item this row stands for, created when the loot was rolled.
        ///
        /// The corpse window will not draw a row it cannot resolve to an entity - corpselootwindow
        /// does GetEntity(itemId) and skips the row when that comes back None - so an id with no
        /// item behind it is an empty window rather than a missing line. Holding the item also
        /// means looting hands over the thing that was rolled, instead of making a second one from
        /// the template and hoping they match.
        /// </summary>
        public Item Item { get; set; }

        /// <summary>Whether someone has already taken this one.</summary>
        public bool Taken { get; set; }

        public LootItem(uint itemTemplateId, uint itemClassId, uint itemQuantity, ulong actorId, uint partyId)
            : this(true)
        {
            ItemTemplateId = itemTemplateId;
            ItemClassId = itemClassId;
            ItemQuantity = itemQuantity;
            ActorId = actorId;
            PartyId = partyId;
        }

        /// <summary>A row standing for an item that already exists; its entity id is the item's.</summary>
        public LootItem(Item item, ulong actorId, uint partyId)
            : this(false)
        {
            Item = item;
            EntityId = item.EntityId;
            ItemTemplateId = item.ItemTemplate.ItemTemplateId;
            ItemClassId = (uint)item.ItemTemplate.Class;
            ItemQuantity = item.StackSize;
            ActorId = actorId;
            PartyId = partyId;
        }

        internal static LootItem Capture(LootItem source) => new(false)
        {
            EntityId = source.EntityId,
            ItemTemplateId = source.ItemTemplateId,
            ItemClassId = source.ItemClassId,
            ItemQuantity = source.ItemQuantity,
            ActorId = source.ActorId,
            PartyId = source.PartyId,
            Taken = source.Taken
        };
    }
}
