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

        public List<NpcBarkEntry> GetNpcBarks()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.NpcBarkEntries).ToList();
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


        public void UpdateCreatureEditor(CreatureEntry creature, IReadOnlyList<CreatureAppearanceEntry> appearance)
        {
            using var transaction = _worldContext.Database.BeginTransaction();
            var row = _worldContext.CreateTrackingQuery(_worldContext.CreatureEntries)
                .FirstOrDefault(e => e.Id == creature.Id);
            if (row == null)
                throw new System.InvalidOperationException($"Creature {creature.Id} no longer exists");
            if (row.ClassId != creature.ClassId)
                throw new System.InvalidOperationException("Creature entity class changed concurrently");

            row.Comment = creature.Comment;
            row.Faction = creature.Faction;
            row.Level = creature.Level;
            row.MaxHitPoints = creature.MaxHitPoints;
            row.NameId = creature.NameId;
            row.RunSpeed = creature.RunSpeed;
            row.WalkSpeed = creature.WalkSpeed;
            row.Action1 = creature.Action1;
            row.Action2 = creature.Action2;
            row.Action3 = creature.Action3;
            row.Action4 = creature.Action4;
            row.Action5 = creature.Action5;
            row.Action6 = creature.Action6;
            row.Action7 = creature.Action7;
            row.Action8 = creature.Action8;
            _worldContext.SaveChanges();

            // Appearance has no EF key. Use parameterized SQL rather than tracking keyless rows.
            _worldContext.Database.ExecuteSqlInterpolated($"DELETE FROM creature_appearance WHERE id = {creature.Id}");
            foreach (var item in appearance)
            {
                _worldContext.Database.ExecuteSqlInterpolated(
                    $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({creature.Id}, {item.SlotId}, {item.ClassId}, {item.Color})");
            }
            // Validate the database state while the transaction is still open. The editor
            // must never report success based only on the rows it was asked to write.
            // If the provider truncated, ignored, or otherwise changed a value, throwing
            // here rolls back BOTH the creature and appearance changes.
            var persisted = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureEntries)
                .Single(e => e.Id == creature.Id);
            if (persisted.ClassId != creature.ClassId || persisted.Comment != creature.Comment ||
                persisted.Faction != creature.Faction || persisted.Level != creature.Level ||
                persisted.MaxHitPoints != creature.MaxHitPoints || persisted.NameId != creature.NameId ||
                persisted.RunSpeed != creature.RunSpeed || persisted.WalkSpeed != creature.WalkSpeed ||
                persisted.Action1 != creature.Action1 || persisted.Action2 != creature.Action2 ||
                persisted.Action3 != creature.Action3 || persisted.Action4 != creature.Action4 ||
                persisted.Action5 != creature.Action5 || persisted.Action6 != creature.Action6 ||
                persisted.Action7 != creature.Action7 || persisted.Action8 != creature.Action8)
                throw new System.InvalidOperationException(
                    $"Creature {creature.Id} database verification failed: creature fields differ");

            var persistedAppearance = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureAppearanceEntries)
                .Where(e => e.Id == creature.Id).ToList();
            if (persistedAppearance.Count != appearance.Count ||
                appearance.Any(wanted => !persistedAppearance.Any(saved =>
                    saved.SlotId == wanted.SlotId && saved.ClassId == wanted.ClassId &&
                    saved.Color == wanted.Color)))
                throw new System.InvalidOperationException(
                    $"Creature {creature.Id} database verification failed: appearance colors or slots differ");

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

        /// <summary>
        /// One slot of a creature's look, in creature_appearance: the row of that creature and slot
        /// changed, or a new one. The table has no key - CreatureAppearanceEntry is [Keyless] - so
        /// Entity Framework can neither update nor add a row of it, and every save of this threw.
        /// Written by hand instead, on the creature and the slot, the same on SQLite and MySQL.
        /// </summary>
        public void CreateOrUpdateAppearance(uint creatureId, uint slotId, uint classId, uint hue)
        {
            var updated = _worldContext.Database.ExecuteSqlInterpolated(
                $"UPDATE creature_appearance SET Class_id = {classId}, color = {hue} WHERE id = {creatureId} AND slot_id = {slotId}");

            if (updated == 0)
                _worldContext.Database.ExecuteSqlInterpolated(
                    $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({creatureId}, {slotId}, {classId}, {hue})");
        }

        public List<CreatureAppearanceEntry> GetCreatureAppearances(uint creatureId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureAppearanceEntries);
            var appearance = query.Where(e => e.Id == creatureId).ToList();

            return appearance;
        }

        public List<CreatureAppearanceEntry> GetAllCreatureAppearances()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.CreatureAppearanceEntries).ToList();
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

        public bool UpdateNpcGreetingEditor(uint creatureId, uint expectedId, bool expectedImportant,
            uint greetingId, bool important)
        {
            using var transaction = _worldContext.Database.BeginTransaction();
            var row = _worldContext.CreateTrackingQuery(_worldContext.NpcGreetingEntries)
                .FirstOrDefault(e => e.Id == creatureId);
            if ((row?.GreetingId ?? 0) != expectedId || (row?.Important ?? false) != expectedImportant)
                return false;

            if (greetingId == 0)
            {
                if (row != null) _worldContext.NpcGreetingEntries.Remove(row);
            }
            else if (row == null)
                _worldContext.NpcGreetingEntries.Add(new NpcGreetingEntry
                {
                    Id = creatureId, GreetingId = greetingId, Important = important
                });
            else
            {
                row.GreetingId = greetingId;
                row.Important = important;
            }
            _worldContext.SaveChanges();
            // Query again without using the tracked row to verify the actual DB state.
            _worldContext.ChangeTracker.Clear();
            var persisted = _worldContext.CreateNoTrackingQuery(_worldContext.NpcGreetingEntries)
                .FirstOrDefault(e => e.Id == creatureId);
            if ((persisted?.GreetingId ?? 0) != greetingId ||
                (persisted?.Important ?? false) != (greetingId != 0 && important))
                throw new System.InvalidOperationException($"NPC greeting {creatureId} did not persist");
            transaction.Commit();
            return true;
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
