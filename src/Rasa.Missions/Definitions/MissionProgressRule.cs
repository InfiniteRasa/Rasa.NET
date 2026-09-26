using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rasa.Structures
{
    using Data;

    internal enum MissionProgressRuleType
    {
        CompleteExact,
        CompleteDistinctSet,
        IncrementExactCounter,
        IncrementExactItemCounter
    }

    public sealed class MissionProgressRule
    {
        private readonly HashSet<uint> _subjectSet;

        public MissionProgressEventKind Kind { get; }
        public IReadOnlyCollection<uint> Subjects { get; }
        public uint? CounterId { get; }
        public uint? InitialValue { get; }
        public uint? TargetValue { get; }
        public bool? SourceSpawnResolved { get; }
        public uint? ScopeId { get; }
        public uint? DetailId { get; }
        public uint? DurationSeconds { get; }
        internal MissionProgressRuleType RuleType { get; }

        private MissionProgressRule(
            MissionProgressRuleType ruleType,
            MissionProgressEventKind kind,
            IEnumerable<uint> subjects,
            uint? counterId = null,
            uint? initialValue = null,
            uint? targetValue = null,
            bool? sourceSpawnResolved = null,
            uint? scopeId = null,
            uint? detailId = null,
            uint? durationSeconds = null)
        {
            if (!Enum.IsDefined(typeof(MissionProgressEventKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            _subjectSet = new HashSet<uint>(subjects ?? Array.Empty<uint>());
            if (_subjectSet.Count == 0 || _subjectSet.Contains(0))
                throw new ArgumentException("Progress rules require non-zero subjects.", nameof(subjects));

            RuleType = ruleType;
            Kind = kind;
            Subjects = new ReadOnlyCollection<uint>(_subjectSet.OrderBy(value => value).ToArray());
            CounterId = counterId;
            InitialValue = initialValue;
            TargetValue = targetValue;
            SourceSpawnResolved = sourceSpawnResolved;
            ScopeId = scopeId;
            DetailId = detailId;
            DurationSeconds = durationSeconds;
        }

        public static MissionProgressRule CompleteOnExactSubject(
            MissionProgressEventKind kind,
            uint subjectId)
        {
            if (kind == MissionProgressEventKind.WaypointAcquired)
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    "Waypoint progress requires a distinct-set rule.");
            return CompleteOnExactSubject(kind, subjectId, null);
        }

        internal static MissionProgressRule CompleteOnExactSubject(
            MissionProgressEventKind kind,
            uint subjectId,
            bool? sourceSpawnResolved) =>
            new(
                MissionProgressRuleType.CompleteExact,
                kind,
                new[] { subjectId },
                sourceSpawnResolved: sourceSpawnResolved);

        internal static MissionProgressRule CompleteOnScopedSubject(
            MissionProgressEventKind kind,
            uint subjectId,
            uint? scopeId = null,
            uint? detailId = null,
            bool? sourceSpawnResolved = null,
            uint? durationSeconds = null) =>
            new(
                MissionProgressRuleType.CompleteExact,
                kind,
                new[] { subjectId },
                sourceSpawnResolved: sourceSpawnResolved,
                scopeId: scopeId,
                detailId: detailId,
                durationSeconds: durationSeconds);

        internal static MissionProgressRule CompleteOnAreaEntered(
            uint missionId,
            uint areaId) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.AreaEntered,
                areaId,
                scopeId: missionId);

        internal static MissionProgressRule CompleteOnItemEquippedClass(
            uint itemClassId) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.ItemEquipped,
                itemClassId,
                sourceSpawnResolved: false);

        internal static MissionProgressRule CompleteOnItemEquippedTemplate(
            uint itemTemplateId) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.ItemEquipped,
                itemTemplateId,
                sourceSpawnResolved: true);

        internal static MissionProgressRule CompleteOnAbilityHit(
            uint actionId,
            uint targetCreatureId) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.AbilityHit,
                actionId,
                detailId: targetCreatureId);

        internal static MissionProgressRule CompleteOnObjectHit(uint entityClassId, uint actionId) =>
            CompleteOnScopedSubject(MissionProgressEventKind.ObjectHit, entityClassId, detailId: actionId);

        internal static MissionProgressRule CompleteOnScenarioEvent(
            uint missionId,
            uint scenarioId,
            uint scenarioEventId) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.ScenarioEvent,
                scenarioEventId,
                scopeId: missionId,
                detailId: scenarioId);

        internal static MissionProgressRule CompleteOnDeadlineElapsed(
            uint missionId,
            uint objectiveId,
            uint durationSeconds) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.DeadlineElapsed,
                objectiveId,
                scopeId: missionId,
                durationSeconds: durationSeconds);

        internal static MissionProgressRule CompleteOnObjectiveState(
            uint missionId,
            uint relatedObjectiveId,
            byte relatedState) =>
            CompleteOnScopedSubject(
                MissionProgressEventKind.ObjectiveStateReached,
                relatedObjectiveId,
                scopeId: missionId,
                detailId: relatedState);

        public static MissionProgressRule CompleteWhenAllDistinctSubjectsObserved(
            MissionProgressEventKind kind,
            IReadOnlySet<uint> subjects)
        {
            if (kind != MissionProgressEventKind.WaypointAcquired &&
                kind != MissionProgressEventKind.LogosAcquired)
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    "Distinct completion supports waypoint or Logos events.");
            return new MissionProgressRule(
                MissionProgressRuleType.CompleteDistinctSet, kind, subjects);
        }

        public static MissionProgressRule IncrementCounterOnExactSubject(
            MissionProgressEventKind kind,
            uint subjectId,
            uint counterId,
            uint initialValue,
            uint targetValue) =>
            new(
                MissionProgressRuleType.IncrementExactCounter,
                kind,
                new[] { subjectId },
                counterId,
                initialValue,
                targetValue);

        public static MissionProgressRule IncrementItemCounterOnExactSubject(
            MissionProgressEventKind kind,
            uint itemClassId,
            uint initialValue,
            uint targetValue)
        {
            if (kind != MissionProgressEventKind.ItemAcquired &&
                kind != MissionProgressEventKind.ItemConsumed)
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    "Item counters support item acquisition or consumption events.");
            return new MissionProgressRule(
                MissionProgressRuleType.IncrementExactItemCounter,
                kind,
                new[] { itemClassId },
                counterId: itemClassId,
                initialValue: initialValue,
                targetValue: targetValue);
        }

        internal bool Matches(MissionProgressEvent progress)
        {
            if (progress.Kind != Kind)
                return false;
            if (ScopeId.HasValue && progress.ScopeId != ScopeId)
                return false;
            if (DetailId.HasValue && progress.DetailId != DetailId)
                return false;

            if (Kind == MissionProgressEventKind.ItemEquipped &&
                SourceSpawnResolved == true)
                return progress.DetailId.HasValue &&
                    _subjectSet.Contains(progress.DetailId.Value);

            return _subjectSet.Contains(progress.SubjectId);
        }

        internal bool IsCompatible(MissionObjectiveDefinition objective)
        {
            if (RuleType == MissionProgressRuleType.IncrementExactItemCounter)
                return CounterId.HasValue &&
                    InitialValue.HasValue &&
                    TargetValue.HasValue &&
                    objective.ItemCounters.TryGetValue(
                        CounterId.Value, out var itemCounter) &&
                    itemCounter.ItemClassId == CounterId.Value &&
                    itemCounter.InitialValue == InitialValue.Value &&
                    itemCounter.TargetValue == TargetValue.Value;
            if (RuleType != MissionProgressRuleType.IncrementExactCounter)
                return true;
            return CounterId.HasValue &&
                InitialValue.HasValue &&
                TargetValue.HasValue &&
                objective.Counters.TryGetValue(CounterId.Value, out var counter) &&
                counter.CounterId == CounterId.Value &&
                counter.InitialValue == InitialValue.Value &&
                counter.TargetValue == TargetValue.Value;
        }

        internal bool AreAllSubjectsObserved(IReadOnlySet<uint> observed) =>
            _subjectSet.IsSubsetOf(observed);
    }
}
