namespace Rasa.Structures
{
    using Managers;
    using Repositories.Char.Items;

    public class Item : IItemChange
    {
        public Item()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public Item(uint itemTemplateId, uint stackSize, int currentHitPoints, uint color)
        {
            ItemTemplateId = itemTemplateId;
            StackSize = stackSize;
            CurrentHitPoints = currentHitPoints;
            Crafter = "";
            Color = color;
        }

        public ulong EntityId { get; }
        public ItemTemplate ItemTemplate { get; set; }
        public uint ItemTemplateId { get; set; }
        // uniqe id stored in db
        public uint Id { get; set; }
        // location info
        public uint OwnerId { get; set; }
        public uint OwnerSlotId { get; set; }
        // item instance specific
        public uint Color { get; set; }
        public string Crafter { get; set; }
        public int CurrentHitPoints { get; set; }
        public uint StackSize { get; set; }
        // weapon specific
        public uint CurrentAmmo { get; set; }
        public bool IsJammed { get; set; }
        public int CammeraProfile { get; set; }
    }
}
