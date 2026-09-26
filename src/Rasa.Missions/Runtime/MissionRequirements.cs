using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Reflection;
using Rasa.Data;

namespace Rasa.Missions.Runtime
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(AllRequirements), "all")]
    [JsonDerivedType(typeof(AnyRequirement), "any")]
    [JsonDerivedType(typeof(NotRequirement), "not")]
    [JsonDerivedType(typeof(LevelRequirement), "level")]
    [JsonDerivedType(typeof(MissionStateRequirement), "mission")]
    [JsonDerivedType(typeof(FlagRequirement), "flag")]
    [JsonDerivedType(typeof(CustomRequirement), "custom")]
    public abstract record MissionRequirement;
    public sealed record AllRequirements(IReadOnlyList<MissionRequirement> Items) : MissionRequirement;
    public sealed record AnyRequirement(IReadOnlyList<MissionRequirement> Items) : MissionRequirement;
    public sealed record NotRequirement(MissionRequirement Item) : MissionRequirement;
    public sealed record LevelRequirement(uint Minimum) : MissionRequirement;
    public sealed record MissionStateRequirement(uint MissionId, MissionState? State = null, bool Accepted = false) : MissionRequirement;
    public sealed record FlagRequirement(uint FlagId, uint Value) : MissionRequirement;
    public sealed record CustomRequirement(string Key) : MissionRequirement;
    public sealed record MissionRequirementFacts(uint Level,
        IReadOnlyDictionary<uint, MissionState> Journal, IReadOnlyDictionary<uint, MissionState> History,
        IReadOnlyDictionary<uint, uint> Flags, IReadOnlyDictionary<string, bool> Custom = null,
        IReadOnlySet<uint> EverSucceeded = null, IReadOnlySet<uint> EverRewarded = null);

    public interface IMissionRequirementHandler
    {
        IReadOnlyCollection<string> RequiredFacts { get; }
        bool Evaluate(IReadOnlyDictionary<string, bool> facts);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MissionRequirementHandlerAttribute : Attribute
    {
        public string Key { get; }
        public MissionRequirementHandlerAttribute(string key) => Key = key;
    }

    public sealed class MissionRequirementEvaluator
    {
        private readonly Dictionary<string, IMissionRequirementHandler> _handlers = new(StringComparer.Ordinal);
        public MissionRequirementEvaluator()
        {
            foreach (var type in typeof(MissionRequirementEvaluator).Assembly.GetTypes())
            {
                var registration = type.GetCustomAttribute<MissionRequirementHandlerAttribute>();
                if (registration != null)
                    Register(registration.Key, (IMissionRequirementHandler)Activator.CreateInstance(type));
            }
        }
        public void Register(string key, IMissionRequirementHandler handler) => _handlers.Add(key, handler);
        public IReadOnlyCollection<string> RequiredFacts(MissionRequirement requirement, int depth = 0)
        {
            if (depth > 32)
                throw new MissionRuleException("Mission requirement nesting exceeds 32 levels.");
            return requirement switch
            {
                null => Array.Empty<string>(),
                AllRequirements all when all.Items != null && all.Items.All(item => item != null) =>
                    all.Items.SelectMany(item => RequiredFacts(item, depth + 1)).Distinct().ToArray(),
                AnyRequirement any when any.Items != null && any.Items.All(item => item != null) =>
                    any.Items.SelectMany(item => RequiredFacts(item, depth + 1)).Distinct().ToArray(),
                NotRequirement not when not.Item != null => RequiredFacts(not.Item, depth + 1),
                CustomRequirement custom => !string.IsNullOrWhiteSpace(custom.Key) &&
                    _handlers.TryGetValue(custom.Key, out var handler) ? handler.RequiredFacts :
                    throw new MissionRuleException($"Missing pure requirement handler {custom.Key}."),
                LevelRequirement or MissionStateRequirement or FlagRequirement => Array.Empty<string>(),
                _ => throw new MissionRuleException("Malformed or unsupported mission requirement.")
            };
        }
        public bool Evaluate(MissionRequirement requirement, MissionRequirementFacts facts) =>
            Evaluate(requirement, facts, 0);

        private bool Evaluate(MissionRequirement requirement, MissionRequirementFacts facts, int depth)
        {
            if (depth > 32)
                throw new MissionRuleException("Mission requirement nesting exceeds 32 levels.");
            return requirement switch
            {
                null => true,
                AllRequirements all => all.Items.All(item => Evaluate(item, facts, depth + 1)),
                AnyRequirement any => any.Items.Any(item => Evaluate(item, facts, depth + 1)),
                NotRequirement not => !Evaluate(not.Item, facts, depth + 1),
                LevelRequirement level => facts.Level >= level.Minimum,
                FlagRequirement flag => facts.Flags.TryGetValue(flag.FlagId, out var value) && value == flag.Value,
                MissionStateRequirement mission => HasState(mission, facts),
                CustomRequirement custom => EvaluateCustom(custom, facts),
                _ => throw new MissionRuleException("Unsupported mission requirement.")
            };
        }
        private static bool HasState(MissionStateRequirement requirement, MissionRequirementFacts facts)
        {
            bool Matches(MissionState state) => requirement.State.HasValue ? state == requirement.State :
                requirement.Accepted || state is MissionState.Success or MissionState.Completed;
            if (facts.Journal.TryGetValue(requirement.MissionId, out var state))
            {
                if (Matches(state))
                    return true;
                if (requirement.State is not (null or MissionState.Success or MissionState.Completed))
                    return false;
            }
            if (requirement.Accepted)
                return false;
            return facts.History.TryGetValue(requirement.MissionId, out state) && Matches(state) ||
                requirement.State is null or MissionState.Success && facts.EverSucceeded?.Contains(requirement.MissionId) == true ||
                requirement.State == MissionState.Completed && facts.EverRewarded?.Contains(requirement.MissionId) == true;
        }
        private bool EvaluateCustom(CustomRequirement requirement, MissionRequirementFacts facts)
        {
            if (string.IsNullOrWhiteSpace(requirement.Key) || !_handlers.TryGetValue(requirement.Key, out var handler))
                throw new MissionRuleException($"Missing pure requirement handler {requirement.Key}.");
            if (facts.Custom == null || handler.RequiredFacts.Any(key => !facts.Custom.ContainsKey(key)))
                throw new MissionRuleException($"Required facts for {requirement.Key} were not supplied.");
            return handler.Evaluate(facts.Custom);
        }

        [MissionRequirementHandler("example.even-level")]
        public sealed class EvenLevelRequirement : IMissionRequirementHandler
        {
            public IReadOnlyCollection<string> RequiredFacts { get; } = new[] { "character.level-is-even" };
            public bool Evaluate(IReadOnlyDictionary<string, bool> facts) => facts["character.level-is-even"];
        }

        [MissionRequirementHandler("account.starting-experience-entitlement")]
        public sealed class StartingExperienceEntitlementRequirement : IMissionRequirementHandler
        {
            public IReadOnlyCollection<string> RequiredFacts { get; } = new[] { "account.starting-experience-entitled" };
            public bool Evaluate(IReadOnlyDictionary<string, bool> facts) => facts["account.starting-experience-entitled"];
        }

        [MissionRequirementHandler("character.starting-experience-completed")]
        public sealed class StartingExperienceCompletedRequirement : IMissionRequirementHandler
        {
            public IReadOnlyCollection<string> RequiredFacts { get; } = new[] { "character.starting-experience-completed" };
            public bool Evaluate(IReadOnlyDictionary<string, bool> facts) => facts["character.starting-experience-completed"];
        }

        [MissionRequirementHandler("character.starting-experience-active")]
        public sealed class StartingExperienceActiveRequirement : IMissionRequirementHandler
        {
            public IReadOnlyCollection<string> RequiredFacts { get; } = new[] { "character.starting-experience-active" };
            public bool Evaluate(IReadOnlyDictionary<string, bool> facts) => facts["character.starting-experience-active"];
        }
    }
}
