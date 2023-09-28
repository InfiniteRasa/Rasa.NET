using Rasa.Structures.World;
using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    public interface ICreatureRepository
    {
        List<CreatureEntry> Get();
        CreatureStatEntry GetCreatureStats(uint creatureId);
        CreatureActionEntry GetCreatureActionById(uint id);
        Dictionary<uint, CreatureActionEntry> GetCreatureActions();
        void CreateOrUpdateAppearance(uint dbId, uint slotId, uint classId, uint hue);
        List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId);

        List<VendorItemEntry> GetVendorItems();

        List<VendorEntry> GetVendors();
    }
}
