using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Structures.World;

namespace Rasa.Loot
{
    internal sealed class OrdinaryLootGroup
    {
        public string Key { get; }
        public LootGroupKind Kind { get; }
        public int ChanceBasisPoints { get; }
        public int MinimumRolls { get; }
        public int MaximumRolls { get; }
        public IReadOnlyList<OrdinaryLootCandidate> Candidates { get; }
        internal int TotalWeight { get; }

        public OrdinaryLootGroup(string key, LootGroupKind kind, int chanceBasisPoints,
            int minimumRolls, int maximumRolls, IReadOnlyList<OrdinaryLootCandidate> candidates)
        {
            ValidateKey(key, 32, nameof(key));
            if (!Enum.IsDefined(typeof(LootGroupKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Undefined ordinary loot group kind.");
            if (chanceBasisPoints < 0 || chanceBasisPoints > 10000)
                throw new ArgumentOutOfRangeException(nameof(chanceBasisPoints), "Chance must be in 0..10000.");
            if (minimumRolls < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumRolls), "Roll counts must be nonnegative.");
            if (maximumRolls < minimumRolls || maximumRolls == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maximumRolls), "Roll bounds must be ordered and RNG-safe.");
            if (kind == LootGroupKind.Junk && maximumRolls > 1)
                throw new ArgumentOutOfRangeException(nameof(maximumRolls), "Junk groups may select at most one type.");
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            var snapshot = candidates.ToArray();
            if (snapshot.Length == 0)
                throw new ArgumentException("An ordinary loot group requires candidates.", nameof(candidates));

            var totalWeight = 0;
            foreach (var candidate in snapshot)
            {
                if (candidate == null || candidate.TemplateId == 0 || candidate.Weight <= 0 ||
                    candidate.MinimumQuantity == 0 || candidate.MaximumQuantity < candidate.MinimumQuantity ||
                    candidate.MaximumQuantity >= int.MaxValue)
                    throw new ArgumentException($"Group {key} contains an invalid ordinary loot candidate.", nameof(candidates));
                if (candidate.Weight > int.MaxValue - totalWeight)
                    throw new ArgumentException($"Group {key} total weight exceeds the integer RNG range.", nameof(candidates));
                totalWeight += candidate.Weight;
            }

            Key = key;
            Kind = kind;
            ChanceBasisPoints = chanceBasisPoints;
            MinimumRolls = minimumRolls;
            MaximumRolls = maximumRolls;
            Candidates = Array.AsReadOnly(snapshot);
            TotalWeight = totalWeight;
        }

        internal static void ValidateKey(string key, int maximumLength, string parameterName)
        {
            if (string.IsNullOrEmpty(key) || key.Length > maximumLength ||
                key.Any(character => !(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') && character != '_' && character != '-'))
                throw new ArgumentException(
                    $"Loot keys must contain 1..{maximumLength} lowercase ASCII letters, digits, underscores or hyphens.",
                    parameterName);
        }
    }
}
