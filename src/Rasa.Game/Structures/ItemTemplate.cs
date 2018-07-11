using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class ItemTemplate
    {
        public EntityClassId Class { get; set; }
        public uint ItemTemplateId { get; set; }
        public ItemInfo ItemInfo = new ItemInfo();
        public EquipableInfo EquipableInfo { get; set; }
        public WeaponInfo WeaponInfo { get; set; }
        public int ArmorValue { get; set; }      // for now we use only armor value

        // general stuff
        public bool BoundToCharacter { get; set; }
        public bool HasSellableFlag { get; set; }
        public bool HasCharacterUniqueFlag { get; set; }
        public bool HasAccountUniqueFlag { get; set; }
        public List<int> ModuleIds = new List<int>();
        public int[] LootModuleIds { get; set; }
        public bool HasBoEFlag { get; set; }
        public int QualityId { get; set; }
        public bool NotPlaceableInLockbox { get; set; }
        public InventoryCategory InventoryCategory { get; set; }

        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
       
        public ItemTemplate(ItemTemplateItemClassEntry template)
        {
            ItemTemplateId = template.ItemTemplateId;
            Class = (EntityClassId)template.ItemClass;
        }
    }
}
