namespace Rasa.Structures
{
    public class ItemTemplate
    {
        public int ClassId { get; set; }
        public int ItemTemplateId { get; set; }
        public ItemInfo ItemInfo = new ItemInfo();
        public EquipableInfo EquipableInfo { get; set; }
        public WeaponInfo WeaponInfo { get; set; }
        public int ArmorValue { get; set; }      // for now we use only armor value

        // general stuff
        public bool BoundToCharacter { get; set; }
        public bool HasSellableFlag { get; set; }
        public bool HasCharacterUniqueFlag { get; set; }
        public bool HasAccountUniqueFlag { get; set; }
        public int[] ClassModuleIds { get; set; }
        public int[] LootModuleIds { get; set; }
        public bool HasBoEFlag { get; set; }
        public int QualityId { get; set; }
        public bool NotPlaceableInLockbox { get; set; }
        public int InventoryCategory { get; set; }

        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
       
        public ItemTemplate(ItemTemplateItemClassEntry template)
        {
            ItemTemplateId = template.ItemTemplateId;
            ClassId = template.ItemClassId;
        }
    }
}
