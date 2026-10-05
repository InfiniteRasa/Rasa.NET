using System.Collections.Generic;
using System.Linq;

namespace Rasa.Api.Ingame
{
    using Data;
    using Managers;
    using Structures;

    public abstract class IngameItemEndpointBase : IngameAuthenticatedEndpoint
    {
        protected IngameItemEndpointBase(IngameSessionService sessions) : base(sessions)
        {
        }

        protected override string RequiredCommand => ".giveitem";

        protected static bool TryItem(uint templateId, out ItemTemplate template, out EntityClass entityClass)
        {
            template = null;
            entityClass = null;

            return ItemManager.Instance.ItemTemplateItemClass.TryGetValue(templateId, out var classId)
                && EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out entityClass)
                && entityClass.ItemTemplates.TryGetValue(templateId, out template);
        }

        protected static IngameItemSummary ToSummary(ItemTemplate template, EntityClass entityClass) =>
            new IngameItemSummary(
                template.ItemTemplateId,
                entityClass.ClassId,
                entityClass.ClassName ?? string.Empty,
                template.InventoryCategory.ToString(),
                entityClass.ItemClassInfo?.StackSize ?? 0,
                template.QualityId);

        protected static IngameItemDetails ToDetails(ItemTemplate template, EntityClass entityClass)
        {
            var itemClass = entityClass.ItemClassInfo;
            var equipable = entityClass.EquipableClassInfo;
            var weaponClass = entityClass.WeaponClassInfo;
            var weapon = template.WeaponInfo;
            var requirements = template.ItemInfo?.Requirements == null
                ? new Dictionary<string, int>()
                : template.ItemInfo.Requirements.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);

            return new IngameItemDetails
            {
                TemplateId = template.ItemTemplateId,
                ItemClassId = entityClass.ClassId,
                Name = entityClass.ClassName ?? string.Empty,
                Category = template.InventoryCategory.ToString(),
                StackSize = itemClass?.StackSize ?? 0,
                QualityId = template.QualityId,
                InventoryIconStringId = itemClass?.InventoryIconStringId ?? 0,
                LootValue = itemClass?.LootValue ?? 0,
                MaxHitPoints = itemClass?.MaxHitPoints ?? 0,
                IsConsumable = itemClass?.IsConsumableFlag != 0,
                BuyPrice = template.BuyPrice,
                SellPrice = template.SellPrice,
                ArmorValue = template.ArmorValue,
                BoundToCharacter = template.BoundToCharacter,
                BindOnEquip = template.HasBoEFlag,
                Sellable = template.HasSellableFlag,
                CharacterUnique = template.HasCharacterUniqueFlag,
                AccountUnique = template.HasAccountUniqueFlag,
                NotTradable = template.NotTradable,
                NotPlaceableInLockbox = template.NotPlaceableInLockbox,
                EquipmentSlot = equipable?.EquipmentSlotId.ToString(),
                RequiredSkillId = template.EquipableInfo?.SkillId,
                RequiredSkillLevel = template.EquipableInfo?.SkillLevel,
                Requirements = requirements,
                Weapon = weaponClass == null && weapon == null
                    ? null
                    : new IngameWeaponDetails
                    {
                        MinDamage = weaponClass?.MinDamage,
                        MaxDamage = weaponClass?.MaxDamage,
                        DamageType = weaponClass?.DamageType,
                        AmmoClassId = weaponClass == null ? null : (uint?)weaponClass.AmmoClassId,
                        ClipSize = weaponClass?.ClipSize,
                        AmmoPerShot = weapon?.AmmoPerShot,
                        Range = weapon?.Range,
                        Windup = weapon?.Windup,
                        Recovery = weapon?.Recovery,
                        Refire = weapon?.Refire,
                        ReloadTime = weapon?.ReloadTime
                    }
            };
        }
    }
}
