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
        public ItemTemplate ItemTemplate { get; set; }
        // uniqe id stored in db
        public uint ItemId { get; set; }
        // location info
        public uint OwnerId { get; set; }
        public int OwnerSlotId { get; set; }
        // item instance specific
        public uint Color { get; set; }
        public string CrafterName { get; set; }
        public int CurrentHitPoints { get; set; }
        public int Stacksize { get; set; }
        // weapon specific
        public int CurrentAmmo { get; set; }
        public bool IsJammed { get; set; }
        public int CammeraProfile { get; set; }
    }
}
