using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ICreatureAppearanceRepository
    {
        void CreateOrUpdate(uint dbId, uint slotId, uint classId, uint hue);
        List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId);
    }

    public class CreatureAppearanceRepository : ICreatureAppearanceRepository
    {
        private readonly WorldContext _worldContext;

        public CreatureAppearanceRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public void CreateOrUpdate(uint creatureId, uint slotId, uint classId, uint hue)
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
    }
}
