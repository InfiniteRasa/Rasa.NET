namespace Rasa.Structures
{
    using Managers;

    public class Item
    {
        public Item()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }
        
        public uint EntityId { get; }
        // uniqe id stored in db
        public uint ItemId { get; set; }
        // location info
        public uint OwnerId { get; set; }
        public int OwnerSlotId { get; set; }
        // template
        public ItemTemplate ItemTemplate { get; set; }
        // item instance specific
        public int Stacksize { get; set; }
        public int Color { get; set; }
    }
}
