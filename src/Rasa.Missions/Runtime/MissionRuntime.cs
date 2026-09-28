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
        MissionProgressEvent Progress);

    public sealed record ObjectiveDecision(
        MissionObjectiveState? State,
        uint? CounterId = null,
        uint? CounterValue = null,
        bool IsItemCounter = false);

    public sealed class MissionRuntime
    {
        public const int JournalCapacity = 30;
        private readonly Dictionary<(MissionProgressEventKind Kind, uint Subject), List<Binding>> _events = new();
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
        }

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
                        !rule.AreAllSubjectsObserved(Subjects(rule.Kind, waypoints, logos)))
                        continue;
                    var key = (binding.Mission.MissionId, binding.Objective.ObjectiveId);
                    if (candidates.TryGetValue(key, out var previous) &&
                        (previous.ExecutableTransition.Sequence < binding.Transition.Sequence ||
                         previous.ExecutableTransition.Sequence == binding.Transition.Sequence &&
                         previous.ExecutableTransition.TransitionId <= binding.Transition.TransitionId))
                        continue;
                    candidates[key] = new MissionProgressCandidate(binding.Mission, mission,
                        binding.Objective, objective, binding.Transition, progress);
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
            IReadOnlySet<uint> durableSubjects = null)
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
            if (rule.RuleType == MissionProgressRuleType.CompleteDistinctSet)
            {
                if (!rule.AreAllSubjectsObserved(durableSubjects ?? new HashSet<uint>()))
                    throw new MissionRuleException("Distinct progress subjects are incomplete.");
                return new ObjectiveDecision(state);
            }
            var item = rule.RuleType == MissionProgressRuleType.IncrementExactItemCounter;
            var runtimeCounters = item ? candidate.RuntimeObjective.ItemCounters : candidate.RuntimeObjective.Counters;
            var durableCounters = item ? durable.ItemCounters : durable.Counters;
            var id = rule.CounterId.Value;
            if (!runtimeCounters.TryGetValue(id, out var value) ||
                !durableCounters.TryGetValue(id, out var stored) || value != stored ||
                value < rule.InitialValue.Value || value >= rule.TargetValue.Value)
                throw new MissionRuleException("Mission counter is stale or cannot advance monotonically.");
            var next = value + Math.Min(candidate.Progress.Quantity, rule.TargetValue.Value - value);
            return new ObjectiveDecision(next == rule.TargetValue ? state : null, id, next, item);
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
