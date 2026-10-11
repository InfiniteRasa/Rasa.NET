using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Structures.World;

namespace Rasa.Loot
{
    internal sealed class OrdinaryLootProfile
    {
        public string ProfileKey { get; }
        public IReadOnlyList<OrdinaryLootGroup> Groups { get; }

        public OrdinaryLootProfile(string profileKey, IReadOnlyList<OrdinaryLootGroup> groups)
        {
            OrdinaryLootGroup.ValidateKey(profileKey, 64, nameof(profileKey));
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));

            var snapshot = groups.ToArray();
            var maximumRolls = 0;
            var hasJunk = false;
            foreach (var group in snapshot)
            {
                if (group == null)
                    throw new ArgumentException($"Profile {profileKey} contains a null group.", nameof(groups));
                if (group.Kind == LootGroupKind.Junk)
                {
                    if (hasJunk)
                        throw new ArgumentException($"Profile {profileKey} contains multiple junk groups.", nameof(groups));
                    hasJunk = true;
                }
                if (group.MaximumRolls > 32 - maximumRolls)
                    throw new ArgumentException($"Profile {profileKey} exceeds the 32-roll limit.", nameof(groups));
                maximumRolls += group.MaximumRolls;
            }

            ProfileKey = profileKey;
            Groups = Array.AsReadOnly(snapshot);
        }

        public IEnumerable<(uint TemplateId, uint Quantity)> Roll(Func<int, int, int> next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            var drops = new List<(uint TemplateId, uint Quantity)>();
            foreach (var group in Groups)
            {
                if (next(0, 10000) >= group.ChanceBasisPoints)
                    continue;

                var count = next(group.MinimumRolls, group.MaximumRolls + 1);
                for (var index = 0; index < count; index++)
                {
                    var draw = next(0, group.TotalWeight);
                    var cumulativeWeight = 0;
                    foreach (var candidate in group.Candidates)
                    {
                        cumulativeWeight += candidate.Weight;
                        if (draw >= cumulativeWeight)
                            continue;

                        var quantity = next((int)candidate.MinimumQuantity, (int)candidate.MaximumQuantity + 1);
                        drops.Add((candidate.TemplateId, (uint)quantity));
                        break;
                    }
                }
            }
            return drops.AsReadOnly();
        }
    }
}
