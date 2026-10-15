using Rasa.Context.World;
using Rasa.Structures.World;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly WorldContext _worldContext;

        public EquipmentRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        /// <summary>Tracked updates only; missing records are not silently fabricated.</summary>
        public void UpdateItemEditor(
            ItemTemplateEntry template, ItemClassEntry itemClass,
            ItemTemplateArmorEntry armor, ItemTemplateWeaponEntry weapon,
            WeaponClassEntry weaponClass, ItemTemplateRequirementSkillEntry skill)
        {
            var record = _worldContext.ItemTemplateEntries.Find(template.Id)
                ?? throw new System.InvalidOperationException($"Item template {template.Id} has no itemtemplate row");
            record.QualityId = template.QualityId;
            record.HasSellableFlag = template.HasSellableFlag;
            record.NotTradableFlag = template.NotTradableFlag;
            record.HasCharacterUniqueFlag = template.HasCharacterUniqueFlag;
            record.HasAccountUniqueFlag = template.HasAccountUniqueFlag;
            record.HasBoEFlag = template.HasBoEFlag;
            record.BoundToCharacterFlag = template.BoundToCharacterFlag;
            record.NotPlacableInLockboxFlag = template.NotPlacableInLockboxFlag;
            record.InventoryCategory = template.InventoryCategory;
            record.BuyPrice = template.BuyPrice;
            record.SellPrice = template.SellPrice;

            if (itemClass != null)
            {
                var shared = _worldContext.ItemClassEntries.Find(itemClass.Id)
                    ?? throw new System.InvalidOperationException($"Item class {itemClass.Id} has no itemclass row");
                shared.InventoryIconStringId = itemClass.InventoryIconStringId;
                shared.LootValue = itemClass.LootValue;
                shared.IsConsumableFlag = itemClass.IsConsumableFlag;
                shared.MaxHitPoints = itemClass.MaxHitPoints;
                shared.StackSize = itemClass.StackSize;
            }
            if (armor != null)
            {
                var row = _worldContext.ItemTemplateArmorEntries.Find(armor.Id)
                    ?? throw new System.InvalidOperationException($"Item template {armor.Id} has no armor row");
                row.ArmorValue = armor.ArmorValue;
            }
            if (weapon != null)
            {
                var row = _worldContext.ItemTemplateWeaponEntries.Find(weapon.Id)
                    ?? throw new System.InvalidOperationException($"Item template {weapon.Id} has no weapon row");
                row.AmmoPerShot = weapon.AmmoPerShot;
                row.Range = weapon.Range;
                row.Windup = weapon.Windup;
                row.Recovery = weapon.Recovery;
                row.Refire = weapon.Refire;
                row.ReloadTime = weapon.ReloadTime;
            }
            if (weaponClass != null)
            {
                var row = _worldContext.WeaponClassEntries.Find(weaponClass.Id)
                    ?? throw new System.InvalidOperationException($"Class {weaponClass.Id} has no weaponclass row");
                row.MinDamage = weaponClass.MinDamage;
                row.MaxDamage = weaponClass.MaxDamage;
                row.DamageType = weaponClass.DamageType;
                row.AmmoClassId = weaponClass.AmmoClassId;
                row.ClipSize = weaponClass.ClipSize;
            }
            if (skill != null)
            {
                var row = _worldContext.ItemTemplateRequirementSkillEntries.Find(skill.Id)
                    ?? throw new System.InvalidOperationException($"Item template {skill.Id} has no skill requirement row");
                row.SkillId = skill.SkillId;
                row.SkillLevel = skill.SkillLevel;
            }
        }

        public List<ArmorClassEntry> GetArmorClasses()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ArmorClassEntries);
            var armorClassEntries = query.ToList();

            return armorClassEntries;
        }

        public uint GetItemClass(uint itemTemplateId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateItemClassEntries);
            var result = query.FirstOrDefault(e => e.ItemTemplateId == itemTemplateId);
            if (result == null)
            {
                throw new EntityNotFoundException(nameof(ItemTemplateItemClassEntry), nameof(ItemTemplateItemClassEntry.ItemTemplateId), itemTemplateId);
            }
            return result.ItemClass;
        }

        public List<ItemTemplateItemClassEntry> GetItemTemplateClasses()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateItemClassEntries);
            var itemTemplateItemClasses = query.ToList();

            return itemTemplateItemClasses;
        }

        public List<ItemClassEntry> GetItemClasses()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemClassEntries);
            var itemClassEntries = query.ToList();

            return itemClassEntries;
        }

        public List<ItemTemplateArmorEntry> GetArmorItems()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateArmorEntries);
            var itemTemplateArmorEntries = query.ToList();

            return itemTemplateArmorEntries;
        }

        public List<ItemTemplateEntry> GetItemTemplates()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateEntries);
            var itemTemplateList = query.ToList();

            return itemTemplateList;
        }

        public List<ItemTemplateRequirementRaceEntry> GetRequirementsRace()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateRequirementRaceEntries);
            var requirementRaceEntries = query.ToList();

            return requirementRaceEntries;
        }

        public List<ItemTemplateResistanceEntry> GetItemResistances()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateResistanceEntries);
            var requirementResistanceEntries = query.ToList();

            return requirementResistanceEntries;
        }

        public List<WeaponClassEntry> GetWeaponClasses()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.WeaponClassEntries);
            var weaponClassEntries = query.ToList();

            return weaponClassEntries;
        }

        public List<ItemTemplateWeaponEntry> GetWeaponItems()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateWeaponEntries);
            var weaponEntries = query.ToList();

            return weaponEntries;
        }

        public List<EquipableClassEntry> GetEquipableClasses()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.EquipableClassEntries);
            var equipableClassEntries = query.ToList();

            return equipableClassEntries;
        }

        public List<ItemTemplateRequirementSkillEntry> GetRequirementsSkill()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateRequirementSkillEntries);
            var requirementSkillEntries = query.ToList();

            return requirementSkillEntries;
        }

        public List<ItemTemplateRequirementEntry> GetRequirementsGeneric()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateRequirementEntries);
            var requirementEntries = query.ToList();

            return requirementEntries;
        }
    }
}
