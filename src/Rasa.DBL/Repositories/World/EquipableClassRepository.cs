using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;


    public interface IEquipableClassRepository
    {
        List<EquipableClassEntry> Get();
    }
    public class EquipableClassRepository : IEquipableClassRepository
    {
        private readonly WorldContext _worldContext;

        public EquipableClassRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<EquipableClassEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.EquipableClassEntries);
            var equipableClassEntries = query.ToList();

            return equipableClassEntries;
        }
    }
}
