using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    /// <summary>The loot pools (loot_group, loot_group_item, creature_loot_group).</summary>
    public interface ILootGroupRepository
    {
        List<LootGroupEntry> GetGroups();
        List<LootGroupItemEntry> GetItems();
        List<CreatureLootGroupEntry> GetCreatureGroups();

        /// <summary>
        /// Puts these rows in place of everything the three tables hold, in one transaction:
        /// all of it is there afterwards, or what was there before is.
        /// </summary>
        void ReplaceAll(IReadOnlyCollection<LootGroupEntry> groups, IReadOnlyCollection<LootGroupItemEntry> items, IReadOnlyCollection<CreatureLootGroupEntry> creatureGroups);
    }

    public class LootGroupRepository : ILootGroupRepository
    {
        private readonly WorldContext _worldContext;

        public LootGroupRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<LootGroupEntry> GetGroups() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.LootGroupEntries).OrderBy(e => e.Id).ToList();

        public List<LootGroupItemEntry> GetItems() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.LootGroupItemEntries).OrderBy(e => e.GroupId).ThenBy(e => e.ItemTemplateId).ToList();

        public List<CreatureLootGroupEntry> GetCreatureGroups() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.CreatureLootGroupEntries).OrderBy(e => e.CreatureId).ThenBy(e => e.GroupId).ToList();

        public void ReplaceAll(IReadOnlyCollection<LootGroupEntry> groups, IReadOnlyCollection<LootGroupItemEntry> items, IReadOnlyCollection<CreatureLootGroupEntry> creatureGroups)
        {
            using var transaction = _worldContext.Database.BeginTransaction();

            // The table names are this code's own constants.
            _worldContext.Database.ExecuteSqlRaw("DELETE FROM " + CreatureLootGroupEntry.TableName);
            _worldContext.Database.ExecuteSqlRaw("DELETE FROM " + LootGroupItemEntry.TableName);
            _worldContext.Database.ExecuteSqlRaw("DELETE FROM " + LootGroupEntry.TableName);

            _worldContext.LootGroupEntries.AddRange(groups);
            _worldContext.LootGroupItemEntries.AddRange(items);
            _worldContext.CreatureLootGroupEntries.AddRange(creatureGroups);
            _worldContext.SaveChanges();

            transaction.Commit();

            // The rows are the caller's, and this context is done with them.
            _worldContext.ChangeTracker.Clear();
        }
    }
}
