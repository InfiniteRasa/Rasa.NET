using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IItemModuleRepository
    {
        List<ModuleClassEntry> GetModuleClasses();

        /// <summary>In the order a module's lines are sent: by id.</summary>
        List<ModuleEffectEntry> GetModuleEffects();

        List<ModuleItemEntry> GetModuleItems();
        List<ModifiableClassEntry> GetModifiableClasses();
    }

    public class ItemModuleRepository : IItemModuleRepository
    {
        private readonly WorldContext _worldContext;

        public ItemModuleRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ModuleClassEntry> GetModuleClasses()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.ModuleClassEntries).ToList();
        }

        public List<ModuleEffectEntry> GetModuleEffects()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.ModuleEffectEntries).OrderBy(entry => entry.Id).ToList();
        }

        public List<ModuleItemEntry> GetModuleItems()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.ModuleItemEntries).ToList();
        }

        public List<ModifiableClassEntry> GetModifiableClasses()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.ModifiableClassEntries).ToList();
        }
    }
}
