using System;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public class OrdinaryLootRepository : IOrdinaryLootRepository
    {
        private readonly WorldContext _worldContext;

        public OrdinaryLootRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public OrdinaryLootContentSnapshot Get() => new OrdinaryLootContentSnapshot(
            _worldContext.CreateNoTrackingQuery(_worldContext.LootProfileEntries).ToList()
                .OrderBy(entry => entry.ProfileKey, StringComparer.Ordinal),
            _worldContext.CreateNoTrackingQuery(_worldContext.LootGroupEntries).ToList()
                .OrderBy(entry => entry.ProfileKey, StringComparer.Ordinal)
                .ThenBy(entry => entry.SortOrder)
                .ThenBy(entry => entry.GroupKey, StringComparer.Ordinal),
            _worldContext.CreateNoTrackingQuery(_worldContext.LootItemEntries).ToList()
                .OrderBy(entry => entry.ProfileKey, StringComparer.Ordinal)
                .ThenBy(entry => entry.GroupKey, StringComparer.Ordinal)
                .ThenBy(entry => entry.SortOrder)
                .ThenBy(entry => entry.ItemTemplateId),
            _worldContext.CreateNoTrackingQuery(_worldContext.CreatureLootBindingEntries).ToList()
                .OrderBy(entry => entry.MapContextId).ThenBy(entry => entry.CreatureId),
            _worldContext.CreateNoTrackingQuery(_worldContext.LootMapPolicyEntries).ToList()
                .OrderBy(entry => entry.MapContextId));
    }
}
