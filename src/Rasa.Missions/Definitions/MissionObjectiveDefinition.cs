using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rasa.Structures
{
    using Data;
    using Missions;
    using World;

    public readonly struct MissionObjectiveCounterDefinition
    {
        public uint CounterId { get; }
        public uint InitialValue { get; }
        public uint TargetValue { get; }

        public MissionObjectiveCounterDefinition(uint counterId, uint initialValue, uint targetValue)
        {
            CounterId = counterId;
            InitialValue = initialValue;
            TargetValue = targetValue;
        }
    }

    public readonly struct MissionObjectiveItemCounterDefinition
    {
        public uint ItemClassId { get; }
        public uint InitialValue { get; }
        public uint TargetValue { get; }

        public MissionObjectiveItemCounterDefinition(uint itemClassId, uint initialValue, uint targetValue)
        {
            ItemClassId = itemClassId;
            InitialValue = initialValue;
            TargetValue = targetValue;
        }
    }

    public sealed class MissionObjectiveExecutableTransition
    {
        public uint TransitionId { get; }
        public uint Sequence { get; }
        public MissionObjectiveState? ToState { get; }
        public IReadOnlyList<MissionObjectiveConversation> Conversations { get; }
        public MissionProgressRule ProgressRule { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> Counters { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> ItemCounters { get; }
        public IReadOnlyList<MissionActionDefinition> Actions { get; }
        public IReadOnlyList<uint> RevealedObjectiveIds { get; }
        public IReadOnlyList<uint> ActivatedObjectiveIds { get; }

        public MissionObjectiveExecutableTransition(
            uint transitionId,
            uint sequence,
            MissionObjectiveState? toState,
            IEnumerable<MissionObjectiveConversation> conversations,
            MissionProgressRule progressRule,
            IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters,
            IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> itemCounters,
            IEnumerable<MissionActionDefinition> actions)
        {
            TransitionId = transitionId;
            Sequence = sequence;
            ToState = toState;
            Conversations = Array.AsReadOnly(
                (conversations ?? Array.Empty<MissionObjectiveConversation>())
                .ToArray());
            ProgressRule = progressRule;
            Counters = new ReadOnlyDictionary<uint, MissionObjectiveCounterDefinition>(
                new Dictionary<uint, MissionObjectiveCounterDefinition>(
                    counters ?? new Dictionary<uint, MissionObjectiveCounterDefinition>()));
            ItemCounters = new ReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition>(
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(
                    itemCounters ?? new Dictionary<uint, MissionObjectiveItemCounterDefinition>()));
            Actions = Array.AsReadOnly(
                (actions ?? Array.Empty<MissionActionDefinition>())
                .OrderBy(action => action.Sequence)
                .ThenBy(action => action.ActionId)
                .ToArray());
            RevealedObjectiveIds = Array.AsReadOnly(
                Actions.Where(action =>
                        action.Kind == World.MissionActionKind.RevealObjective &&
                        action.TargetObjectiveId.HasValue)
                    .Select(action => action.TargetObjectiveId!.Value)
                    .Distinct()
                    .ToArray());
            ActivatedObjectiveIds = Array.AsReadOnly(
                Actions.Where(action =>
                        action.Kind == World.MissionActionKind.ActivateObjective &&
                        action.TargetObjectiveId.HasValue)
                    .Select(action => action.TargetObjectiveId!.Value)
                    .Distinct()
                    .ToArray());
        }
    }

    public sealed class MissionObjectiveDefinition
    {
        public uint ObjectiveId { get; }
        public uint? ClientNameTextId { get; }
        public uint? ClientBodyTextId { get; }
        public IReadOnlyList<uint?> ClientCounterTextIds { get; }
        public uint? Ordinal { get; }
        public MissionContentRequirement Requirement { get; }
        public MissionObjectiveState? InitialState { get; }
        public bool? IsRequired { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> Counters { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> ItemCounters { get; }
        public IReadOnlyList<MissionObjectiveConversation> Conversations { get; }
        public IReadOnlyList<uint> RevealedObjectiveIds { get; }
        public IReadOnlyList<uint> ActivatedObjectiveIds { get; }
        public IReadOnlyList<MissionIndicator> Indicators { get; }
        public MissionProgressRule ProgressRule { get; }
        public IReadOnlyList<MissionObjectiveExecutableTransition> ExecutableTransitions { get; }
        public global::Rasa.Missions.Runtime.MissionCreditPolicy CreditPolicy { get; }
        public global::Rasa.Missions.Definitions.MissionObjectiveAggregation Aggregation { get; }
        public global::Rasa.Missions.Definitions.MissionHistoryAggregation HistoryAggregation { get; }
        internal bool IsAggregate => Aggregation != null || HistoryAggregation != null;
        internal uint? AggregateCounterId => Aggregation?.CounterId ?? HistoryAggregation?.CounterId;
        internal uint? AggregateTargetCount => Aggregation?.TargetCount ?? HistoryAggregation?.TargetCount;
        public bool IsVisible { get; }
        public bool RecognizeExistingFacts { get; }
        public bool HasCompleteServerContract { get; }

        public MissionObjectiveDefinition(
            uint objectiveId,
            uint? clientNameTextId,
            uint? clientBodyTextId,
            IEnumerable<uint?> clientCounterTextIds,
            uint? ordinal,
            MissionObjectiveState? initialState,
            bool? isRequired,
            IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters,
            IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> itemCounters,
            IEnumerable<MissionObjectiveConversation> conversations,
            IEnumerable<uint> revealedObjectiveIds = null,
            IEnumerable<uint> activatedObjectiveIds = null,
            IEnumerable<MissionIndicator> indicators = null,
            MissionProgressRule progressRule = null,
            IEnumerable<MissionObjectiveExecutableTransition> executableTransitions = null,
            MissionContentRequirement requirement = MissionContentRequirement.Required,
            global::Rasa.Missions.Runtime.MissionCreditPolicy creditPolicy = null,
            global::Rasa.Missions.Definitions.MissionObjectiveAggregation aggregation = null,
            bool isVisible = true,
            bool recognizeExistingFacts = false,
            global::Rasa.Missions.Definitions.MissionHistoryAggregation historyAggregation = null)
        {
            ObjectiveId = objectiveId;
            ClientNameTextId = clientNameTextId;
            ClientBodyTextId = clientBodyTextId;
            ClientCounterTextIds = Array.AsReadOnly(
                (clientCounterTextIds ?? Array.Empty<uint?>()).ToArray());
            Ordinal = ordinal;
            Requirement = requirement;
            CreditPolicy = creditPolicy ?? global::Rasa.Missions.Runtime.MissionCreditPolicy.Personal;
            Aggregation = aggregation;
            HistoryAggregation = historyAggregation;
            IsVisible = isVisible;
            RecognizeExistingFacts = recognizeExistingFacts;
            InitialState = initialState;
            IsRequired = isRequired;
            var counterDefinitions = new Dictionary<uint, MissionObjectiveCounterDefinition>(
                counters ?? new Dictionary<uint, MissionObjectiveCounterDefinition>());
            if (AggregateCounterId is uint aggregateCounter)
            {
                if (counterDefinitions.TryGetValue(aggregateCounter, out var existing) &&
                    (existing.InitialValue != 0 || existing.TargetValue != AggregateTargetCount.Value))
                    throw new ArgumentException("Aggregate counter range conflicts with the objective counter.", nameof(counters));
                counterDefinitions[aggregateCounter] = new(aggregateCounter, 0, AggregateTargetCount.Value);
            }
            Counters = new ReadOnlyDictionary<uint, MissionObjectiveCounterDefinition>(
                counterDefinitions);
            ItemCounters = new ReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition>(
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(
                    itemCounters ?? new Dictionary<uint, MissionObjectiveItemCounterDefinition>()));
            Conversations = Array.AsReadOnly(
                (conversations ?? Array.Empty<MissionObjectiveConversation>()).ToArray());
            RevealedObjectiveIds = revealedObjectiveIds == null
                ? null
                : Array.AsReadOnly(revealedObjectiveIds.ToArray());
            ActivatedObjectiveIds = activatedObjectiveIds == null
                ? null
                : Array.AsReadOnly(activatedObjectiveIds.ToArray());
            Indicators = indicators == null
                ? null
                : Array.AsReadOnly(indicators.Select(CloneIndicator).ToArray());
            ProgressRule = progressRule;
            ExecutableTransitions = Array.AsReadOnly(
                (executableTransitions ?? Array.Empty<MissionObjectiveExecutableTransition>())
                .OrderBy(transition => transition.Sequence)
                .ThenBy(transition => transition.TransitionId)
                .ToArray());
            HasCompleteServerContract =
                (!IsVisible || ClientNameTextId.HasValue && ClientBodyTextId.HasValue) &&
                Ordinal.HasValue &&
                InitialState.HasValue &&
                IsRequired.HasValue &&
                RevealedObjectiveIds != null &&
                ActivatedObjectiveIds != null &&
                Indicators != null &&
                Counters.All(counter =>
                    counter.Value.CounterId == counter.Key &&
                    (!IsVisible || counter.Key < (uint)ClientCounterTextIds.Count &&
                        ClientCounterTextIds[(int)counter.Key].HasValue)) &&
                ItemCounters.All(counter => counter.Value.ItemClassId == counter.Key) &&
                (ProgressRule == null || ProgressRule.IsCompatible(this)) &&
                (Aggregation == null || HistoryAggregation == null) &&
                (HistoryAggregation == null || CreditPolicy.Mode == global::Rasa.Missions.Runtime.MissionCreditMode.Personal) &&
                (!IsAggregate || ProgressRule == null && ExecutableTransitions.Count == 0 &&
                    Conversations.Count == 0 && ItemCounters.Count == 0 &&
                    RevealedObjectiveIds.Count == 0 && ActivatedObjectiveIds.Count == 0 &&
                    Counters.Count == (AggregateCounterId.HasValue ? 1 : 0)) &&
                (!RecognizeExistingFacts || HasExistingFactContract());
        }

        internal MissionObjective CreateRuntime(
            MissionObjectiveState state,
            IReadOnlyDictionary<uint, uint> counters,
            IReadOnlyDictionary<uint, uint> itemCounters)
        {
            if (!HasCompleteServerContract)
                throw new InvalidOperationException("Mission objective definition is incomplete.");

            var objective = new MissionObjective
            {
                ObjectiveId = ObjectiveId,
                State = state,
                Ordinal = Ordinal.Value,
                IsRequired = IsRequired.Value
            };
            foreach (var counter in Counters)
            {
                if (!counters.TryGetValue(counter.Key, out var value))
                    throw new InvalidOperationException("Mission objective counter state is missing.");
                objective.Counters.Add(counter.Key, new MissionObjectiveCounter
                {
                    CounterValue = value,
                    InitialValue = counter.Value.InitialValue,
                    TargetValue = counter.Value.TargetValue
                });
            }
            foreach (var counter in ItemCounters)
            {
                if (!itemCounters.TryGetValue(counter.Key, out var value))
                    throw new InvalidOperationException("Mission objective item counter state is missing.");
                objective.ItemCounters.Add(counter.Key, new MissionObjectiveItemCounter
                {
                    CounterValue = value,
                    TargetValue = counter.Value.TargetValue
                });
            }
            foreach (var indicator in Indicators)
                objective.IndicatorList.Add(CloneIndicator(indicator));
            return objective;
        }

        private static MissionIndicator CloneIndicator(MissionIndicator indicator) =>
            new()
            {
                Position = indicator.Position,
                Radius = indicator.Radius,
                IndicatorId = indicator.IndicatorId,
                Show3DEffect = indicator.Show3DEffect
            };

        internal MissionObjectiveDefinition WithCreditPolicy(global::Rasa.Missions.Runtime.MissionCreditPolicy policy) =>
            new(ObjectiveId, ClientNameTextId, ClientBodyTextId, ClientCounterTextIds, Ordinal,
                InitialState, IsRequired, Counters, ItemCounters, Conversations, RevealedObjectiveIds,
                ActivatedObjectiveIds, Indicators, ProgressRule, ExecutableTransitions, Requirement, policy,
                Aggregation, IsVisible, RecognizeExistingFacts, HistoryAggregation);

        private bool HasExistingFactContract()
        {
            var transitions = GetExecutableTransitionsOrLegacyDefault();
            if (transitions.Count != 1 || transitions[0].Conversations.Count != 0 ||
                transitions[0].ToState is not (null or MissionObjectiveState.Completed))
                return false;
            var rule = transitions[0].ProgressRule;
            return rule != null && rule.ScopeId == null && rule.DetailId == null &&
                (rule.Kind is MissionProgressEventKind.WaypointAcquired or MissionProgressEventKind.LogosAcquired &&
                    rule.RuleType is MissionProgressRuleType.CompleteDistinctSet or MissionProgressRuleType.CompleteExact ||
                 rule.Kind == MissionProgressEventKind.MissionCompleted && rule.RuleType == MissionProgressRuleType.CompleteExact ||
                 rule.Kind == MissionProgressEventKind.ItemAcquired && rule.RuleType == MissionProgressRuleType.IncrementExactItemCounter);
        }

        internal IReadOnlyList<MissionObjectiveExecutableTransition> GetExecutableTransitionsOrLegacyDefault()
        {
            if (ExecutableTransitions.Count > 0)
                return ExecutableTransitions;
            if (Conversations.Count == 0 && ProgressRule == null)
                return Array.Empty<MissionObjectiveExecutableTransition>();

            var actions = new List<MissionActionDefinition>();
            uint sequence = 1;
            foreach (var objectiveId in RevealedObjectiveIds ?? Array.Empty<uint>())
            {
                actions.Add(new MissionActionDefinition
                {
                    MissionId = 0,
                    ContentRevision = string.Empty,
                    ObjectiveId = ObjectiveId,
                    TransitionId = 0,
                    ActionId = sequence,
                    Kind = World.MissionActionKind.RevealObjective,
                    Sequence = sequence,
                    TargetObjectiveId = objectiveId,
                    Comment = "Legacy reveal"
                });
                sequence++;
            }

            foreach (var objectiveId in ActivatedObjectiveIds ?? Array.Empty<uint>())
            {
                actions.Add(new MissionActionDefinition
                {
                    MissionId = 0,
                    ContentRevision = string.Empty,
                    ObjectiveId = ObjectiveId,
                    TransitionId = 0,
                    ActionId = sequence,
                    Kind = World.MissionActionKind.ActivateObjective,
                    Sequence = sequence,
                    TargetObjectiveId = objectiveId,
                    Comment = "Legacy activate"
                });
                sequence++;
            }

            return new[]
            {
                new MissionObjectiveExecutableTransition(
                    0,
                    0,
                    MissionObjectiveState.Completed,
                    Conversations,
                    ProgressRule,
                    Counters,
                    ItemCounters,
                    actions)
            };
        }
    }
}
