using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IWeaponClassRepository
    {
        List<WeaponClassEntry> Get();
    }
    public class WeaponClassRepository : IWeaponClassRepository
    {
        private readonly WorldContext _worldContext;

        public WeaponClassRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }
        public List<WeaponClassEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.WeaponClassEntries);
            var weaponClassEntries = query.ToList();

            return weaponClassEntries;
        }
    }
}
