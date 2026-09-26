using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Missions.Runtime;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;
using Rasa.Game.Missions.Persistence;

namespace Rasa.Game.Missions.Integration
{
    internal sealed class MissionRequirementService
    {
        private readonly MissionRequirementEvaluator _evaluator;
        private readonly MissionRequirementFactsAdapter _facts;
        internal MissionRequirementService(MissionRequirementEvaluator evaluator = null,
            MissionRequirementFactsAdapter facts = null)
        { _evaluator = evaluator ?? new MissionRequirementEvaluator(); _facts = facts ?? new MissionRequirementFactsAdapter(); }

        internal bool Evaluate(Manifestation player, MissionRequirement requirement, ICharUnitOfWork unit = null) =>
            requirement == null || _evaluator.Evaluate(requirement, _facts.Read(player, _evaluator.RequiredFacts(requirement), unit));

        internal CommitValidation Capture(Manifestation player, MissionRequirement requirement, ICharUnitOfWork unit)
        {
            if (requirement == null)
                return null;
            var requested = _evaluator.RequiredFacts(requirement);
            var facts = _facts.Read(player, requested, unit, out var completedStartingExperienceState);
            if (!_evaluator.Evaluate(requirement, facts))
                throw new GameplayRejectionException("Durable mission preconditions are not satisfied.");
            var validation = new CommitValidation(this, player, requirement, requested, facts, unit,
                completedStartingExperienceState);
            unit.Enlist(() => new TransactionFacts()).Captures.Add(validation);
            return validation;
        }

        internal static void ExpectFlag(ICharUnitOfWork unit, uint characterId, uint flagId, uint? value) =>
            Project(unit, characterId, capture => capture.ExpectFlag(flagId, value));

        internal static void ExpectAssignment(ICharUnitOfWork unit, uint characterId, uint missionId,
            Data.MissionState state, bool archived = false) =>
            Project(unit, characterId, capture => capture.ExpectAssignment(missionId, state, archived));

        internal static void ExpectHistory(ICharUnitOfWork unit, uint characterId, uint missionId, Data.MissionState state) =>
            Project(unit, characterId, capture => capture.ExpectHistory(missionId, state));

        internal static void ExpectLevel(ICharUnitOfWork unit, uint characterId, uint level) =>
            Project(unit, characterId, capture => capture.ExpectLevel(level));

        internal static void ExpectEntitlement(ICharUnitOfWork unit, uint characterId, bool enabled) =>
            Project(unit, characterId, capture => capture.ExpectEntitlement(enabled));

        private static void Project(ICharUnitOfWork unit, uint characterId, Action<CommitValidation> project)
        {
            if (!unit.HasEnlisted<TransactionFacts>())
                return;
            foreach (var capture in unit.Enlist(() => new TransactionFacts()).Captures)
                if (capture.CharacterId == characterId)
                    project(capture);
        }

        private sealed class TransactionFacts : ITransactionParticipant
        {
            internal List<CommitValidation> Captures { get; } = new();
            public void Prepare() { }
            public void FinalizePersistence() { }
            public void Validate() { }
            public void ValidateCommitBoundary() { }
            public void Committed() { }
            public void Dispose() => Captures.Clear();
        }

        internal sealed class CommitValidation
        {
            private readonly MissionRequirementService _service;
            private readonly Manifestation _player;
            private readonly MissionRequirement _requirement;
            private readonly IReadOnlyCollection<string> _requested;
            private readonly ICharUnitOfWork _unit;
            private readonly bool _completedStartingExperienceState;
            private MissionRequirementFacts _expected;
            private bool _checkLevel;
            internal uint CharacterId { get; }

            internal CommitValidation(MissionRequirementService service, Manifestation player,
                MissionRequirement requirement, IReadOnlyCollection<string> requested,
                MissionRequirementFacts expected, ICharUnitOfWork unit, bool completedStartingExperienceState)
            {
                _service = service;
                _player = player;
                _requirement = requirement;
                _requested = requested;
                _expected = expected;
                _unit = unit;
                _completedStartingExperienceState = completedStartingExperienceState;
                CharacterId = player.Id;
            }

            internal void ExpectFlag(uint flagId, uint? value) =>
                _expected = MissionRequirementFactsAdapter.WithFlag(_expected, flagId, value, _completedStartingExperienceState);

            internal void ExpectEntitlement(bool enabled) =>
                _expected = MissionRequirementFactsAdapter.WithCustom(_expected, "account.starting-experience-entitled", enabled);

            internal void ExpectAssignment(uint missionId, Data.MissionState state, bool archived)
            {
                var journal = _expected.Journal.ToDictionary(entry => entry.Key, entry => entry.Value);
                journal[missionId] = state;
                _expected = _expected with { Journal = journal };
                if (archived)
                    ExpectHistory(missionId, state);
            }

            internal void ExpectHistory(uint missionId, Data.MissionState state)
            {
                var history = _expected.History.ToDictionary(entry => entry.Key, entry => entry.Value);
                history[missionId] = state;
                var succeeded = _expected.EverSucceeded.ToHashSet();
                var rewarded = _expected.EverRewarded.ToHashSet();
                if (state is Data.MissionState.Success or Data.MissionState.Completed)
                    succeeded.Add(missionId);
                if (state == Data.MissionState.Completed)
                    rewarded.Add(missionId);
                _expected = _expected with { History = history, EverSucceeded = succeeded, EverRewarded = rewarded };
            }

            internal void Validate()
            {
                var current = _service._facts.Read(_player, _requested, _unit);
                if (_checkLevel && current.Level != _expected.Level ||
                    !_service.InputsMatch(_requirement, _expected, current))
                    throw new GameplayRejectionException("Mission requirement facts changed beyond the planned operation.");
            }

            internal void ExpectLevel(uint level)
            {
                _expected = MissionRequirementFactsAdapter.WithLevel(_expected, level);
                _checkLevel = true;
            }
        }

        private bool InputsMatch(MissionRequirement requirement, MissionRequirementFacts expected,
            MissionRequirementFacts current) =>
            requirement switch
            {
                null => true,
                AllRequirements all => all.Items.All(item => InputsMatch(item, expected, current)),
                AnyRequirement any => any.Items.All(item => InputsMatch(item, expected, current)),
                NotRequirement not => InputsMatch(not.Item, expected, current),
                LevelRequirement => expected.Level == current.Level,
                FlagRequirement flag => SameValue(expected.Flags, current.Flags, flag.FlagId),
                MissionStateRequirement mission =>
                    SameValue(expected.Journal, current.Journal, mission.MissionId) &&
                    SameValue(expected.History, current.History, mission.MissionId) &&
                    expected.EverSucceeded?.Contains(mission.MissionId) == current.EverSucceeded?.Contains(mission.MissionId) &&
                    expected.EverRewarded?.Contains(mission.MissionId) == current.EverRewarded?.Contains(mission.MissionId),
                CustomRequirement custom => _evaluator.RequiredFacts(custom)
                    .All(key => SameValue(expected.Custom, current.Custom, key)),
                _ => throw new MissionRuleException("Unsupported mission requirement.")
            };

        private static bool SameValue<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> expected,
            IReadOnlyDictionary<TKey, TValue> current, TKey key)
        {
            var hadValue = expected.TryGetValue(key, out var before);
            var hasValue = current.TryGetValue(key, out var after);
            return hadValue == hasValue && (!hadValue || EqualityComparer<TValue>.Default.Equals(before, after));
        }

        internal void Validate(Mission mission)
        {
            Validate(mission.MissionId, mission.Objectives.Keys,
                mission.Requirement, mission.TurnInRequirement, mission.ObjectiveRequirements);
            foreach (var source in mission.RadioSources)
                foreach (var fact in _evaluator.RequiredFacts(source.Requirement))
                    if (!MissionRequirementFactsAdapter.Supported.Contains(fact))
                        throw new MissionRuleException($"Mission {mission.MissionId} radio source requires unsupported Game fact {fact}.");
        }

        internal void Validate(uint missionId, IEnumerable<uint> objectiveIds, MissionRequirement admission,
            MissionRequirement turnIn, IReadOnlyDictionary<uint, MissionRequirement> objectiveRequirements)
        {
            if (objectiveRequirements == null)
                throw new MissionRuleException($"Mission {missionId} has no objective requirement bindings.");
            var knownObjectives = objectiveIds.ToHashSet();
            foreach (var pair in objectiveRequirements)
                if (!knownObjectives.Contains(pair.Key))
                    throw new MissionRuleException($"Mission {missionId} binds a requirement to missing objective {pair.Key}.");
            foreach (var requirement in objectiveRequirements.Values.Append(admission).Append(turnIn))
                foreach (var fact in _evaluator.RequiredFacts(requirement))
                    if (!MissionRequirementFactsAdapter.Supported.Contains(fact))
                        throw new MissionRuleException($"Mission {missionId} requires unsupported Game fact {fact}.");
        }
    }
}
