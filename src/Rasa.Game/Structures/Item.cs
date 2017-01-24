namespace Rasa.Structures
{
    using Managers;

    public class Item
    {
        public Item()
        {
            EntityId = EntityManager.Instance.NextEntityId;
        }
        
        public uint EntityId { get; }
        // location info
        public uint OwnerId { get; set; }
        public int OwnerSlotId { get; set; }
        // template
        public ItemTemplate ItemTemplate { get; set; }
        // item instance specific
        public int Stacksize { get; set; }
        public int Color { get; set; }
        // item WeaponDrawer specific
        public int WeaponAmmoCount { get; set; }
    }
}
