using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

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

        public List<CreatureBattlecryEntry> GetBattlecries()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.CreatureBattlecryEntries).ToList();
        }

        public void SaveCreatureBattlecry(uint creatureId, uint packageId)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.CreatureBattlecryEntries)
                .FirstOrDefault(e => e.Scope == CreatureBattlecryEntry.ScopeCreature && e.TargetId == creatureId);

            if (row == null)
                _worldContext.CreatureBattlecryEntries.Add(new CreatureBattlecryEntry
                {
                    Scope = CreatureBattlecryEntry.ScopeCreature,
                    TargetId = creatureId,
                    PackageId = packageId
                });
            else
                row.PackageId = packageId;

            _worldContext.SaveChanges();
        }

        /// <summary>
        /// Every creature class's flags, in one read. Keyed by class rather than by creature, so
        /// this is loaded once with the entity classes rather than per spawn.
        /// </summary>
        public List<CreatureClassFlagEntry> GetClassFlags()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureClassFlagEntries);

            return query.ToList();
        }

        public void ReplaceClassFlags(IReadOnlyDictionary<uint, IReadOnlyCollection<uint>> flagsByClass)
        {
            if (flagsByClass == null || flagsByClass.Count == 0)
                return;

            using var transaction = _worldContext.Database.BeginTransaction();

            // A few hundred classes at a time, so the list of them stays a short one.
            foreach (var classes in flagsByClass.Keys.Chunk(400))
                _worldContext.CreatureClassFlagEntries.RemoveRange(
                    _worldContext.CreateTrackingQuery(_worldContext.CreatureClassFlagEntries).Where(e => classes.Contains(e.ClassId)).ToList());

            _worldContext.SaveChanges();

            foreach (var entry in flagsByClass)
                foreach (var flag in entry.Value.Distinct())
                    _worldContext.CreatureClassFlagEntries.Add(new CreatureClassFlagEntry { ClassId = entry.Key, FlagId = flag });

            _worldContext.SaveChanges();
            transaction.Commit();
            _worldContext.ChangeTracker.Clear();
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

        public List<VendorPriceEntry> GetVendorPrices()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.VendorPriceEntries).ToList();
        }

        public List<CreatureActorNameEntry> GetActorNames()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.CreatureActorNameEntries).ToList();
        }

        public List<NpcGreetingEntry> GetNpcGreetings()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.NpcGreetingEntries).ToList();
        }

        public void SaveNpcGreeting(uint creatureId, uint greetingId)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.NpcGreetingEntries).FirstOrDefault(e => e.Id == creatureId);

            if (row == null)
                _worldContext.NpcGreetingEntries.Add(new NpcGreetingEntry { Id = creatureId, GreetingId = greetingId });
            else
                row.GreetingId = greetingId;

            _worldContext.SaveChanges();
        }

        public bool SaveNpcGreetingImportant(uint creatureId, bool important)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.NpcGreetingEntries).FirstOrDefault(e => e.Id == creatureId);

            if (row == null)
                return false;

            row.Important = important;
            _worldContext.SaveChanges();

            return true;
        }

        public bool DeleteNpcGreeting(uint creatureId)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.NpcGreetingEntries).FirstOrDefault(e => e.Id == creatureId);

            if (row == null)
                return false;

            _worldContext.NpcGreetingEntries.Remove(row);
            _worldContext.SaveChanges();

            return true;
        }
    }
}
