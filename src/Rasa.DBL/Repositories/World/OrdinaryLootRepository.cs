using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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

        public OrdinaryLootContentSnapshot Get()
        {
            var groups = _worldContext.LootGroupEntries.LongCount();
            var items = _worldContext.LootGroupItemEntries.LongCount();
            var bindings = _worldContext.CreatureLootGroupEntries.LongCount();
            if (groups != 0 || items != 0 || bindings != 0)
                throw new LegacyFlatLootArchiveException(
                    $"Inactive flat loot archives are nonempty: {LootGroupEntry.TableName}={groups}, " +
                    $"{LootGroupItemEntry.TableName}={items}, {CreatureLootGroupEntry.TableName}={bindings}. " +
                    "Export and review these rows, explicitly author weighted profiles and bindings, then " +
                    "clear the archives only after an approved authoring decision. A flat percent is not a weight; " +
                    "no rows have been converted, activated or deleted.");
            return new OrdinaryLootContentSnapshot(
            _worldContext.CreateNoTrackingQuery(_worldContext.LootProfileEntries).ToList()
                .OrderBy(entry => entry.ProfileKey, StringComparer.Ordinal),
            _worldContext.CreateNoTrackingQuery(_worldContext.OrdinaryLootGroupEntries).ToList()
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

        public void Replace(OrdinaryLootContentSnapshot content)
        {
            if (_worldContext.Database.CurrentTransaction == null)
                throw new InvalidOperationException("Canonical loot replacement requires a caller-owned World transaction.");
            ArgumentNullException.ThrowIfNull(content);
            // Repeat the archive check at the write boundary, including orphan archive rows.
            Get();
            RemoveMissing(_worldContext.CreatureLootBindingEntries, content.Bindings, row => (row.MapContextId, row.CreatureId));
            RemoveMissing(_worldContext.LootItemEntries, content.Items, row => (row.ProfileKey, row.GroupKey, row.ItemTemplateId));
            RemoveMissing(_worldContext.OrdinaryLootGroupEntries, content.Groups, row => (row.ProfileKey, row.GroupKey));
            RemoveMissing(_worldContext.LootProfileEntries, content.Profiles, row => row.ProfileKey);
            RemoveMissing(_worldContext.LootMapPolicyEntries, content.MapPolicies, row => row.MapContextId);
            Upsert(_worldContext.LootProfileEntries, content.Profiles, row => row.ProfileKey);
            Upsert(_worldContext.OrdinaryLootGroupEntries, content.Groups, row => (row.ProfileKey, row.GroupKey));
            Upsert(_worldContext.LootItemEntries, content.Items, row => (row.ProfileKey, row.GroupKey, row.ItemTemplateId));
            Upsert(_worldContext.CreatureLootBindingEntries, content.Bindings, row => (row.MapContextId, row.CreatureId));
            Upsert(_worldContext.LootMapPolicyEntries, content.MapPolicies, row => row.MapContextId);
        }

        private void RemoveMissing<T, TKey>(DbSet<T> table, IEnumerable<T> rows, Func<T, TKey> key) where T : class
        {
            var keys = rows.Select(key).ToHashSet();
            table.RemoveRange(table.ToList().Where(row => !keys.Contains(key(row))));
            SaveChanges();
        }

        private void Upsert<T, TKey>(DbSet<T> table, IEnumerable<T> rows, Func<T, TKey> key) where T : class
        {
            var stored = table.ToList().ToDictionary(key);
            foreach (var row in rows)
                if (stored.TryGetValue(key(row), out var existing))
                    _worldContext.Entry(existing).CurrentValues.SetValues(row);
                else
                    table.Add(row);
            SaveChanges();
        }

        private void SaveChanges()
        {
            if (_worldContext.ChangeTracker.HasChanges())
                _worldContext.SaveChanges();
        }
    }
}
