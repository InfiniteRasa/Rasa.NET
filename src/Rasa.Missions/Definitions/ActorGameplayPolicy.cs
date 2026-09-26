using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rasa.Missions.Definitions
{
    public sealed record LootDrop(uint TemplateId, int ChancePercent, int Minimum, int Maximum);

    public sealed class AuthoredLootProfile
    {
        public IReadOnlyList<LootDrop> Drops { get; }
        public AuthoredLootProfile(IReadOnlyList<LootDrop> drops)
        {
            if (drops == null || drops.Any(drop => drop == null || drop.TemplateId == 0 || drop.ChancePercent < 0 ||
                drop.ChancePercent > 100 || drop.Minimum < 1 || drop.Maximum < drop.Minimum || drop.Maximum == int.MaxValue))
                throw new ArgumentException("Invalid authored loot distribution.", nameof(drops));
            Drops = Array.AsReadOnly(drops.ToArray());
        }
        public IEnumerable<(uint TemplateId, uint Quantity)> Roll(Func<int, int, int> next)
        {
            foreach (var drop in Drops)
                if (drop.ChancePercent == 100 || next(0, 100) < drop.ChancePercent)
                    yield return (drop.TemplateId, (uint)(drop.Minimum == drop.Maximum
                        ? drop.Minimum : next(drop.Minimum, drop.Maximum + 1)));
        }
    }

    public sealed class ActorGameplayPolicy
    {
        public static ActorGameplayPolicy Ordinary { get; } = new();
        public bool Invulnerable { get; init; }
        public float DefenseRadius { get; init; }
        public string DefenseTargetTag { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
        public bool RewardScenarioKills { get; init; }
        public bool TrackParticipation { get; init; }
        public AuthoredLootProfile Loot { get; init; }
        [JsonIgnore] public bool Defends => DefenseRadius > 0;

        public ActorGameplayPolicy Snapshot()
        {
            if (ValidationError() is string error)
                throw new ArgumentException($"Invalid actor gameplay policy: {error}.");
            return new ActorGameplayPolicy
            {
                Invulnerable = Invulnerable,
                DefenseRadius = DefenseRadius,
                DefenseTargetTag = DefenseTargetTag,
                Tags = Array.AsReadOnly(Tags.ToArray()),
                RewardScenarioKills = RewardScenarioKills,
                TrackParticipation = TrackParticipation,
                Loot = Loot
            };
        }

        public string ValidationError()
        {
            if (!float.IsFinite(DefenseRadius) || DefenseRadius < 0)
                return "defense radius must be finite and nonnegative";
            if (Tags == null || Tags.Any(tag => !IsTag(tag)) ||
                Tags.Distinct(StringComparer.Ordinal).Count() != Tags.Count)
                return "tags must be nonempty, unique tokens without whitespace";
            if (DefenseTargetTag != null && !IsTag(DefenseTargetTag) || Defends && DefenseTargetTag == null)
                return "defense requires a valid target tag";
            return null;
        }

        public bool EquivalentTo(ActorGameplayPolicy other) => other != null &&
            Invulnerable == other.Invulnerable && DefenseRadius == other.DefenseRadius &&
            DefenseTargetTag == other.DefenseTargetTag && RewardScenarioKills == other.RewardScenarioKills &&
            TrackParticipation == other.TrackParticipation &&
            Tags.OrderBy(tag => tag, StringComparer.Ordinal).SequenceEqual(
                other.Tags.OrderBy(tag => tag, StringComparer.Ordinal), StringComparer.Ordinal) &&
            (Loot == null ? other.Loot == null : other.Loot != null && Loot.Drops.SequenceEqual(other.Loot.Drops));

        private static bool IsTag(string tag) => !string.IsNullOrEmpty(tag) && !tag.Any(char.IsWhiteSpace);
    }

    public sealed class ActorPolicyCatalog
    {
        private readonly Dictionary<(uint Map, uint Creature), ActorGameplayPolicy> _policies = new();
        public void Clear() => _policies.Clear();
        public void Add(uint map, uint creature, ActorGameplayPolicy policy)
        {
            if (map == 0 || creature == 0 || policy == null || policy.ValidationError() != null)
                throw new ArgumentException("Invalid actor gameplay policy.");
            _policies.Add((map, creature), policy.Snapshot());
        }
        public ActorGameplayPolicy Get(uint map, uint creature) =>
            _policies.GetValueOrDefault((map, creature), ActorGameplayPolicy.Ordinary);
    }
}
