using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Missions.Runtime
{
    using Data;
    using Structures;

    public enum MissionRejection
    {
        None,
        InactiveContent,
        AlreadyAssigned,
        AlreadyRewarded,
        JournalFull,
        WrongState
    }

    public readonly struct MissionCommandResult
    {
        public MissionRejection Rejection { get; }
        public bool Accepted => Rejection == MissionRejection.None;
        public MissionCommandResult(MissionRejection rejection) => Rejection = rejection;
    }

    public sealed class MissionRuleException : InvalidOperationException
    {
        public MissionRuleException(string message) : base(message) { }
    }

    public sealed record MissionProgressCandidate(
        Mission Definition,
        MissionLog RuntimeMission,
        MissionObjectiveDefinition ObjectiveDefinition,
        MissionObjectiveLog RuntimeObjective,
        MissionObjectiveExecutableTransition ExecutableTransition,
        MissionProgressEvent Progress,
        IReadOnlySet<uint> DistinctSubjects = null);

    public sealed record ObjectiveDecision(
        MissionObjectiveState? State,
        uint? CounterId = null,
        uint? CounterValue = null,
        bool IsItemCounter = false);

    public sealed class MissionRuntime
    {
        public const int JournalCapacity = 30;
        private readonly Dictionary<(MissionProgressEventKind Kind, uint Subject), List<Binding>> _events = new();
        private readonly Dictionary<uint, List<(Mission Mission, uint ObjectiveId)>> _history = new();
        private readonly Dictionary<uint, Mission[]> _npcs;
        private readonly Dictionary<uint, Mission[]> _conversations;

        public int LastRuleEvaluations { get; private set; }
        public long TotalRuleEvaluations { get; private set; }

        public MissionRuntime(IEnumerable<Mission> definitions)
        {
            var missions = (definitions ?? throw new ArgumentNullException(nameof(definitions)))
                .Where(mission => mission.IsOperational).OrderBy(mission => mission.MissionId).ToArray();
            _npcs = missions.SelectMany(mission => new[]
                {
                    mission.AcceptanceChannel.HasFlag(Definitions.MissionChannel.Npc) ? mission.MissionGiver : null,
                    mission.CompletionChannel.HasFlag(Definitions.MissionChannel.Npc) ? mission.MissionReciver : null
                }.Where(id => id.HasValue).Select(id => id.Value).Distinct().Select(id => (Id: id, Mission: mission)))
                .GroupBy(entry => entry.Id).ToDictionary(group => group.Key,
                    group => group.Select(entry => entry.Mission).ToArray());
            _conversations = missions.SelectMany(mission => mission.Objectives.Values
                    .SelectMany(objective => objective.Conversations)
                    .Select(conversation => conversation.NpcPackageId)
                    .Concat(mission.Dialogue.Select(topic => topic.NpcPackageId)).Distinct()
                    .Select(id => (Id: id, Mission: mission)))
                .GroupBy(entry => entry.Id).ToDictionary(group => group.Key,
                    group => group.Select(entry => entry.Mission).ToArray());
            foreach (var mission in missions)
                foreach (var objective in mission.Objectives.Values)
                    foreach (var transition in objective.GetExecutableTransitionsOrLegacyDefault())
                    {
                        if (transition.ProgressRule == null)
                            continue;
                        foreach (var subject in transition.ProgressRule.Subjects)
                        {
                            var key = (transition.ProgressRule.Kind, subject);
                            if (!_events.TryGetValue(key, out var bindings))
                                _events[key] = bindings = new List<Binding>();
                            bindings.Add(new Binding(mission, objective, transition));
                        }
                    }
            foreach (var mission in missions)
                foreach (var objective in mission.Objectives.Values.Where(value => value.HistoryAggregation != null))
                    foreach (var source in objective.HistoryAggregation.Groups.SelectMany(group => group).Distinct())
                    {
                        if (!_history.TryGetValue(source, out var bindings))
                            _history[source] = bindings = new();
                        bindings.Add((mission, objective.ObjectiveId));
                    }
        }

        public IReadOnlyList<Mission> SelectHistoryAggregates(
            IReadOnlyDictionary<uint, MissionLog> journal, IReadOnlyList<MissionProgressEvent> events) =>
            (events ?? Array.Empty<MissionProgressEvent>())
                .Where(progress => progress.Kind == MissionProgressEventKind.MissionCompleted &&
                    progress.SubjectId != 0 && progress.Quantity != 0)
                .SelectMany(progress => _history.GetValueOrDefault(progress.SubjectId, new()))
                .Where(binding => journal.TryGetValue(binding.Mission.MissionId, out var mission) &&
                    mission.State == MissionState.Active &&
                    mission.Objectives.TryGetValue(binding.ObjectiveId, out var objective) &&
                    objective.State == MissionObjectiveState.Incomplete)
                .Select(binding => binding.Mission).Distinct().OrderBy(mission => mission.MissionId).ToArray();

        public IReadOnlyList<Mission> ForNpc(uint creatureId, uint packageId) =>
            _npcs.GetValueOrDefault(creatureId, Array.Empty<Mission>())
                .Concat(_conversations.GetValueOrDefault(packageId, Array.Empty<Mission>()))
                .Distinct().OrderBy(mission => mission.MissionId).ToArray();

        public IReadOnlyList<MissionProgressCandidate> SelectCandidates(
            IReadOnlyDictionary<uint, MissionLog> journal,
            IReadOnlyList<MissionProgressEvent> events,
            IReadOnlySet<uint> waypoints = null,
            IReadOnlySet<uint> logos = null)
        {
            LastRuleEvaluations = 0;
            var candidates = new Dictionary<(uint Mission, uint Objective), MissionProgressCandidate>();
            foreach (var progress in Aggregate(events))
            {
                var bindings = _events.GetValueOrDefault((progress.Kind, progress.SubjectId),
                    new List<Binding>()).AsEnumerable();
                if (progress.Kind == MissionProgressEventKind.ItemEquipped && progress.DetailId.HasValue &&
                    progress.DetailId != progress.SubjectId)
                    bindings = bindings.Concat(_events.GetValueOrDefault(
                        (progress.Kind, progress.DetailId.Value), new List<Binding>()));
                foreach (var binding in bindings.Distinct())
                {
                    if (!journal.TryGetValue(binding.Mission.MissionId, out var mission) ||
                        mission.State != MissionState.Active ||
                        !mission.Objectives.TryGetValue(binding.Objective.ObjectiveId, out var objective) ||
                        objective.State != MissionObjectiveState.Incomplete)
                        continue;
                    LastRuleEvaluations++;
                    TotalRuleEvaluations++;
                    var rule = binding.Transition.ProgressRule;
                    if (!rule.Matches(progress) || rule.RuleType == MissionProgressRuleType.CompleteDistinctSet &&
                        !rule.CounterId.HasValue &&
                        !rule.AreAllSubjectsObserved(Subjects(rule.Kind, waypoints, logos)))
                        continue;
                    var key = (binding.Mission.MissionId, binding.Objective.ObjectiveId);
                    if (candidates.TryGetValue(key, out var previous))
                    {
                        if (previous.ExecutableTransition == binding.Transition &&
                            rule.RuleType == MissionProgressRuleType.CompleteDistinctSet &&
                            rule.Kind == MissionProgressEventKind.ScenarioEvent)
                        {
                            candidates[key] = previous with
                            {
                                DistinctSubjects = previous.DistinctSubjects.Append(progress.SubjectId).ToHashSet()
                            };
                            continue;
                        }
                        // Several creature rows of one counter in one batch are several kills.
                        // The class and the flags of a kill are names of the one kill
                        // (CreatureManager.KillEvents): an objective that answers to more than
                        // one of them advances once, by the rule below.
                        if (previous.ExecutableTransition == binding.Transition &&
                            rule.RuleType == MissionProgressRuleType.IncrementExactCounter &&
                            rule.Kind == MissionProgressEventKind.CreatureKilled &&
                            rule.Subjects.Count > 1)
                        {
                            var quantity = (ulong)previous.Progress.Quantity + progress.Quantity;
                            if (quantity > uint.MaxValue)
                                throw new MissionRuleException("Mission progress quantity exceeds the supported range.");
                            candidates[key] = previous with
                            {
                                Progress = MissionProgressEvent.Restore(
                                    previous.Progress.Kind, previous.Progress.SubjectId, (uint)quantity,
                                    previous.Progress.ScopeId, previous.Progress.DetailId)
                            };
                            continue;
                        }
                        if (previous.ExecutableTransition.Sequence < binding.Transition.Sequence ||
                            previous.ExecutableTransition.Sequence == binding.Transition.Sequence &&
                            previous.ExecutableTransition.TransitionId <= binding.Transition.TransitionId)
                            continue;
                    }
                    candidates[key] = new MissionProgressCandidate(binding.Mission, mission,
                        binding.Objective, objective, binding.Transition, progress,
                        rule.RuleType == MissionProgressRuleType.CompleteDistinctSet &&
                            rule.Kind == MissionProgressEventKind.ScenarioEvent
                            ? new HashSet<uint> { progress.SubjectId } : null);
                }
            }
            return candidates.Values.OrderBy(candidate => candidate.Definition.MissionId)
                .ThenBy(candidate => candidate.ObjectiveDefinition.ObjectiveId).ToArray();
        }

        public static MissionCommandResult Admit(
            bool operational, bool assigned, bool rewarded, int activeCount) =>
            new(!operational ? MissionRejection.InactiveContent :
                rewarded ? MissionRejection.AlreadyRewarded :
                assigned ? MissionRejection.AlreadyAssigned :
                activeCount >= JournalCapacity ? MissionRejection.JournalFull : MissionRejection.None);

        public static MissionCommandResult CanTurnIn(
            MissionLog mission, MissionState expectedState, bool requireCompleteable) =>
            new(mission == null || mission.State != expectedState ||
                requireCompleteable && !mission.Completeable
                    ? MissionRejection.WrongState : MissionRejection.None);

        public static bool IsComplete(
            Mission definition, IReadOnlyDictionary<uint, MissionObjectiveState> states) =>
            definition.Objectives.Values.Where(objective => objective.IsRequired == true)
                .All(objective => states.TryGetValue(objective.ObjectiveId, out var state) &&
                    state == MissionObjectiveState.Completed);

        public static ObjectiveDecision Evaluate(
            MissionProgressCandidate candidate,
            MissionObjectiveLog durable,
            IReadOnlySet<uint> durableSubjects = null,
            uint? ownedItemQuantity = null)
        {
            if (durable == null || durable.State != MissionObjectiveState.Incomplete ||
                candidate.RuntimeObjective.State != MissionObjectiveState.Incomplete)
                throw new MissionRuleException("Durable mission objective state is stale.");
            var rule = candidate.ExecutableTransition.ProgressRule;
            var state = candidate.ExecutableTransition.ToState ?? MissionObjectiveState.Completed;
            if (rule == null || rule.RuleType == MissionProgressRuleType.CompleteExact)
                return new ObjectiveDecision(state);
            if (state != MissionObjectiveState.Completed)
                throw new MissionRuleException("Counter and distinct-set transitions must complete objectives.");
            if (rule.RuleType == MissionProgressRuleType.CompleteDistinctSet && !rule.CounterId.HasValue)
            {
                if (!rule.AreAllSubjectsObserved(durableSubjects ?? new HashSet<uint>()))
                    throw new MissionRuleException("Distinct progress subjects are incomplete.");
                return new ObjectiveDecision(state);
            }
            var item = rule.RuleType == MissionProgressRuleType.IncrementExactItemCounter;
            if (ownedItemQuantity.HasValue && !item)
                throw new MissionRuleException("Exact inventory facts require an item-counter rule.");
            var runtimeCounters = item ? candidate.RuntimeObjective.ItemCounters : candidate.RuntimeObjective.Counters;
            var durableCounters = item ? durable.ItemCounters : durable.Counters;
            var id = rule.CounterId.Value;
            if (!runtimeCounters.TryGetValue(id, out var value) ||
                !durableCounters.TryGetValue(id, out var stored) || value != stored ||
                value < rule.InitialValue.Value || value >= rule.TargetValue.Value)
                throw new MissionRuleException("Mission counter is stale or cannot advance monotonically.");
            var next = rule.RuleType == MissionProgressRuleType.CompleteDistinctSet
                ? (uint)rule.Subjects.Count(subject => durableSubjects?.Contains(subject) == true)
                : ownedItemQuantity.HasValue ? Math.Min(ownedItemQuantity.Value, rule.TargetValue.Value)
                : value + Math.Min(candidate.Progress.Quantity, rule.TargetValue.Value - value);
            if ((rule.RuleType == MissionProgressRuleType.CompleteDistinctSet || ownedItemQuantity.HasValue) && next <= value)
                return new ObjectiveDecision(null);
            return new ObjectiveDecision(next == rule.TargetValue ? state : null, id, next, item);
        }

        public static ObjectiveDecision EvaluateAggregation(
            MissionObjectiveDefinition definition,
            MissionObjectiveLog durable,
            IReadOnlyDictionary<uint, MissionObjectiveState> states)
        {
            var aggregate = definition.Aggregation ??
                throw new ArgumentException("Objective has no aggregate definition.", nameof(definition));
            if (durable.State != MissionObjectiveState.Incomplete)
                return new ObjectiveDecision(null);
            if (aggregate.ChildObjectiveIds.Any(id => !states.ContainsKey(id)))
                throw new MissionRuleException("Aggregate child state is missing.");
            var count = (uint)aggregate.ChildObjectiveIds.Count(id => states[id] == MissionObjectiveState.Completed);
            var state = count >= aggregate.TargetCount ? MissionObjectiveState.Completed : (MissionObjectiveState?)null;
            if (aggregate.CounterId is not uint counterId)
                return new ObjectiveDecision(state);
            if (!durable.Counters.TryGetValue(counterId, out var current) || current > aggregate.TargetCount)
                throw new MissionRuleException("Aggregate counter state is missing or outside its authored range.");
            var next = Math.Min(count, aggregate.TargetCount);
            return next == current ? new ObjectiveDecision(state) : new ObjectiveDecision(state, counterId, next);
        }

        public static ObjectiveDecision EvaluateHistoryAggregation(
            MissionObjectiveDefinition definition, MissionObjectiveLog durable, IReadOnlySet<uint> completedMissions)
        {
            var aggregate = definition.HistoryAggregation ??
                throw new ArgumentException("Objective has no history aggregate definition.", nameof(definition));
            if (durable.State != MissionObjectiveState.Incomplete)
                return new ObjectiveDecision(null);
            var count = aggregate.CountCompleted(completedMissions);
            var state = count == aggregate.TargetCount ? MissionObjectiveState.Completed : (MissionObjectiveState?)null;
            if (aggregate.CounterId is not uint counterId)
                return new ObjectiveDecision(state);
            if (!durable.Counters.TryGetValue(counterId, out var current) || current > aggregate.TargetCount || count < current)
                throw new MissionRuleException("Completed mission history does not support the saved aggregate counter.");
            return count == current ? new ObjectiveDecision(state) : new ObjectiveDecision(state, counterId, count);
        }

        private static IReadOnlySet<uint> Subjects(
            MissionProgressEventKind kind, IReadOnlySet<uint> waypoints, IReadOnlySet<uint> logos) =>
            (kind == MissionProgressEventKind.WaypointAcquired ? waypoints : logos) ?? new HashSet<uint>();

        private static IReadOnlyList<MissionProgressEvent> Aggregate(IReadOnlyList<MissionProgressEvent> events)
        {
            var result = new List<MissionProgressEvent>();
            foreach (var group in (events ?? Array.Empty<MissionProgressEvent>())
                .Where(progress => Enum.IsDefined(typeof(MissionProgressEventKind), progress.Kind) &&
                    progress.SubjectId != 0 && progress.Quantity != 0)
                .GroupBy(progress => (progress.Kind, progress.SubjectId, progress.ScopeId, progress.DetailId)))
            {
                ulong quantity = 0;
                foreach (var progress in group)
                    quantity += progress.Quantity;
                if (quantity > uint.MaxValue)
                    throw new MissionRuleException("Mission progress quantity exceeds the supported range.");
                result.Add(group.Key.Kind switch
                {
                    MissionProgressEventKind.ItemAcquired =>
                        MissionProgressEvent.ItemAcquired(group.Key.SubjectId, (uint)quantity),
                    MissionProgressEventKind.ItemConsumed =>
                        MissionProgressEvent.ItemConsumed(group.Key.SubjectId, (uint)quantity),
                    _ => group.First()
                });
            }
            return result;
        }

        private sealed record Binding(
            Mission Mission, MissionObjectiveDefinition Objective, MissionObjectiveExecutableTransition Transition);
    }
}
