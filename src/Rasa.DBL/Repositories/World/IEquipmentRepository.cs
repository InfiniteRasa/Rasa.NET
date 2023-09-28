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
