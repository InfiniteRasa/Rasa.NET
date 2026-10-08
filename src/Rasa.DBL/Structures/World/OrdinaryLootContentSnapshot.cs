using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures.World
{
    public sealed class OrdinaryLootContentSnapshot
    {
        public OrdinaryLootContentSnapshot(
            IEnumerable<LootProfileEntry> profiles,
            IEnumerable<OrdinaryLootGroupEntry> groups,
            IEnumerable<LootItemEntry> items,
            IEnumerable<CreatureLootBindingEntry> bindings,
            IEnumerable<LootMapPolicyEntry> mapPolicies)
        {
            Profiles = System.Array.AsReadOnly(profiles.ToArray());
            Groups = System.Array.AsReadOnly(groups.ToArray());
            Items = System.Array.AsReadOnly(items.ToArray());
            Bindings = System.Array.AsReadOnly(bindings.ToArray());
            MapPolicies = System.Array.AsReadOnly(mapPolicies.ToArray());
        }

        public IReadOnlyList<LootProfileEntry> Profiles { get; }
        public IReadOnlyList<OrdinaryLootGroupEntry> Groups { get; }
        public IReadOnlyList<LootItemEntry> Items { get; }
        public IReadOnlyList<CreatureLootBindingEntry> Bindings { get; }
        public IReadOnlyList<LootMapPolicyEntry> MapPolicies { get; }

        public OrdinaryLootContentSnapshot Copy() => new(
            Profiles.Select(row => row == null ? null : new LootProfileEntry
            {
                ProfileKey = row.ProfileKey, Description = row.Description, ContentRevision = row.ContentRevision,
                Provenance = row.Provenance, EvidenceNotes = row.EvidenceNotes
            }),
            Groups.Select(row => row == null ? null : new OrdinaryLootGroupEntry
            {
                ProfileKey = row.ProfileKey, GroupKey = row.GroupKey, Kind = row.Kind,
                ChanceBasisPoints = row.ChanceBasisPoints, MinimumRolls = row.MinimumRolls,
                MaximumRolls = row.MaximumRolls, SortOrder = row.SortOrder
            }),
            Items.Select(row => row == null ? null : new LootItemEntry
            {
                ProfileKey = row.ProfileKey, GroupKey = row.GroupKey, ItemTemplateId = row.ItemTemplateId,
                Weight = row.Weight, MinimumQuantity = row.MinimumQuantity,
                MaximumQuantity = row.MaximumQuantity, SortOrder = row.SortOrder
            }),
            Bindings.Select(row => row == null ? null : new CreatureLootBindingEntry
            {
                MapContextId = row.MapContextId, CreatureId = row.CreatureId, ProfileKey = row.ProfileKey
            }),
            MapPolicies.Select(row => row == null ? null : new LootMapPolicyEntry
            {
                MapContextId = row.MapContextId, RequireProfiles = row.RequireProfiles
            }));
    }
}
