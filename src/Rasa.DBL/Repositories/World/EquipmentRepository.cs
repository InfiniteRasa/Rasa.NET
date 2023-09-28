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
