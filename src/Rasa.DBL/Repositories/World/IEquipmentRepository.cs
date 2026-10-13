using Rasa.Structures.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Repositories.World
{
    public interface IEquipmentRepository
    {
        uint GetItemClass(uint itemTemplateId);
        /// <summary>Update existing item rows in the caller's World transaction.</summary>
        void UpdateItemEditor(
            ItemTemplateEntry template, ItemClassEntry itemClass,
            ItemTemplateArmorEntry armor, ItemTemplateWeaponEntry weapon,
            WeaponClassEntry weaponClass, ItemTemplateRequirementSkillEntry skill);

        List<ArmorClassEntry> GetArmorClasses();
        List<WeaponClassEntry> GetWeaponClasses();

        List<ItemClassEntry> GetItemClasses();

        List<EquipableClassEntry> GetEquipableClasses();
        List<ItemTemplateItemClassEntry> GetItemTemplateClasses();

        List<ItemTemplateRequirementRaceEntry> GetRequirementsRace();
        List<ItemTemplateRequirementSkillEntry> GetRequirementsSkill();
        List<ItemTemplateRequirementEntry> GetRequirementsGeneric();


        List<ItemTemplateArmorEntry> GetArmorItems();
        List<ItemTemplateWeaponEntry> GetWeaponItems();
        List<ItemTemplateEntry> GetItemTemplates();
        List<ItemTemplateResistanceEntry> GetItemResistances();
    }
}
