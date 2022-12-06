using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Managers;
    public class LootDispenser
    {
        public LootDispenser()
        {
            EntityId = EntityManager.Instance.GetEntityId;
            EntityClassId = (EntityClassId)10000035;
        }

        public ulong EntityId { get; set; }
        public EntityClassId EntityClassId { get; set; }
        public List<LootItem> LootItems = new List<LootItem>();
        public int Credits { get; set; }
        public ulong Owner { get; set; }
        public ulong AttachedTo { get; set; }
        public bool FullyLooted { get; set; }
        public bool IsLootable { get; set; }
        public LootQuality LootQuality { get; set; }
    }
}
