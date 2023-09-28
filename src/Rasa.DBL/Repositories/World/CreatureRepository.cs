using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public class CreatureRepository : ICreatureRepository
    {
        private readonly WorldContext _worldContext;

        public CreatureRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<CreatureEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureEntries);
            var creatureEntries = query.ToList();

            return creatureEntries;
        }

        public CreatureStatEntry GetCreatureStats(uint creatureId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureStatEntries);
            var creatureStat = query.FirstOrDefault(e => e.Id == creatureId);

            return creatureStat;
        }

        public CreatureActionEntry GetCreatureActionById(uint id)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureActionEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            return entry;
        }

        public Dictionary<uint, CreatureActionEntry> GetCreatureActions()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureActionEntries);
            var entres = query.ToDictionary(e => e.Id, e => e);

            return entres;
        }

        public void CreateOrUpdateAppearance(uint creatureId, uint slotId, uint classId, uint hue)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureAppearanceEntries);
            var appearanceEntry = query.FirstOrDefault(e => e.Id == creatureId && e.SlotId == slotId);

            if (appearanceEntry != null)
            {
                appearanceEntry.ClassId = classId;
                appearanceEntry.Color = hue;

                _worldContext.CreatureAppearanceEntries.Update(appearanceEntry);
            }
            else
            {
                var newEntry = new CreatureAppearanceEntry()
                {
                    Id = creatureId,
                    ClassId = classId,
                    SlotId = slotId,
                    Color = hue
                };

                _worldContext.CreatureAppearanceEntries.Add(newEntry);
            }

            _worldContext.SaveChanges();
        }

        public List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureAppearanceEntries);
            var appearance = query.Where(e => e.Id == creatureId).ToList();

            return appearance;
        }

        public List<VendorItemEntry> GetVendorItems()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.VendorItemEntries);
            var vendorItemEntries = query.ToList();

            return vendorItemEntries;
        }

        public List<VendorEntry> GetVendors()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.VendorEntries);
            var vendorEntries = query.ToList();

            return vendorEntries;
        }
    }
}
