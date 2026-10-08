using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures.World
{
    public sealed class OrdinaryLootContentSnapshot
    {
        public OrdinaryLootContentSnapshot(
            IEnumerable<LootProfileEntry> profiles,
            IEnumerable<LootGroupEntry> groups,
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
        public IReadOnlyList<LootGroupEntry> Groups { get; }
        public IReadOnlyList<LootItemEntry> Items { get; }
        public IReadOnlyList<CreatureLootBindingEntry> Bindings { get; }
        public IReadOnlyList<LootMapPolicyEntry> MapPolicies { get; }
    }
}
