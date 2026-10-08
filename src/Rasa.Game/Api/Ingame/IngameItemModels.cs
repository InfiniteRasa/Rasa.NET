using System.Collections.Generic;

namespace Rasa.Api.Ingame
{
    internal sealed record IngameItemSearchResponse(string Category, string Search, List<IngameItemSummary> Items);
    internal sealed record IngameItemSummary(uint TemplateId, uint ItemClassId, string Name, string Category, uint StackSize, int QualityId);

    internal sealed class IngameItemDetails
    {
        public uint TemplateId { get; set; }
        public uint ItemClassId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public uint StackSize { get; set; }
        public int QualityId { get; set; }
        public uint InventoryIconStringId { get; set; }
        public uint LootValue { get; set; }
        public int MaxHitPoints { get; set; }
        public bool IsConsumable { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
        public int ArmorValue { get; set; }
        public bool BoundToCharacter { get; set; }
        public bool BindOnEquip { get; set; }
        public bool Sellable { get; set; }
        public bool CharacterUnique { get; set; }
        public bool AccountUnique { get; set; }
        public bool NotTradable { get; set; }
        public bool NotPlaceableInLockbox { get; set; }
        public string EquipmentSlot { get; set; }
        public int? RequiredSkillId { get; set; }
        public int? RequiredSkillLevel { get; set; }
        public Dictionary<string, int> Requirements { get; set; }
        public IngameWeaponDetails Weapon { get; set; }
    }

    internal sealed class IngameWeaponDetails
    {
        public int? MinDamage { get; set; }
        public int? MaxDamage { get; set; }
        public byte? DamageType { get; set; }
        public uint? AmmoClassId { get; set; }
        public uint? ClipSize { get; set; }
        public uint? AmmoPerShot { get; set; }
        public uint? Range { get; set; }
        public uint? Windup { get; set; }
        public uint? Recovery { get; set; }
        public uint? Refire { get; set; }
        public uint? ReloadTime { get; set; }
    }
}
