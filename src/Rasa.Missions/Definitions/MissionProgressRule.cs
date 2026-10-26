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
        /// <summary>
        /// A scene set split over the client's counters: entry N is the events counter N counts.
        /// Null for one counter (counter 0, as <see cref="CounterId"/>).
        /// </summary>
        public IReadOnlyList<IReadOnlyCollection<uint>> CounterGroups { get; }
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
            uint? durationSeconds = null,
            IReadOnlyList<IReadOnlyCollection<uint>> counterGroups = null)
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
            var distinctCounter = ruleType == MissionProgressRuleType.CompleteDistinctSet && counterId.HasValue;
            InitialValue = distinctCounter ? 0U : initialValue;
            // A counted set completes at its target: all its subjects, or as many of them as the
            // target names ("any 16 of the 22 bodies").
            if (distinctCounter && (targetValue == 0 || targetValue > (uint)_subjectSet.Count))
                throw new ArgumentOutOfRangeException(nameof(targetValue), "A distinct-set target counts 1..N of its subjects.");
            TargetValue = distinctCounter ? targetValue ?? (uint)_subjectSet.Count : targetValue;
            SourceSpawnResolved = sourceSpawnResolved;
            ScopeId = scopeId;
            DetailId = detailId;
            DurationSeconds = durationSeconds;
            CounterGroups = counterGroups;
        }

        /// <summary>
        /// This scene set with its events split over the client's counters, one group each
        /// ("Vital Bane Equipment Destroyed 0/6", "Bane Equipment Locker Destroyed 0/10"):
        /// group N is counter N, its target all its events. The objective completes when every
        /// group is full, so the set must count all its events. Null if the groups do not part
        /// the events exactly or name more counters than the client has (3).
        /// </summary>
        internal MissionProgressRule SplitIntoCounters(IReadOnlyList<IReadOnlyList<uint>> groups)
        {
            if (RuleType != MissionProgressRuleType.CompleteDistinctSet || Kind != MissionProgressEventKind.ScenarioEvent ||
                CounterId != 0 || TargetValue != (uint)_subjectSet.Count ||
                groups == null || groups.Count < 2 || groups.Count > 3 ||
                groups.Any(group => group == null || group.Count == 0 || group.Distinct().Count() != group.Count) ||
                groups.Sum(group => group.Count) != _subjectSet.Count ||
                !_subjectSet.SetEquals(groups.SelectMany(group => group)))
                return null;
            return new MissionProgressRule(RuleType, Kind, _subjectSet, CounterId, InitialValue, TargetValue,
                SourceSpawnResolved, ScopeId, DetailId, DurationSeconds,
                Array.AsReadOnly(groups.Select(group => (IReadOnlyCollection<uint>)Array.AsReadOnly(
                    group.OrderBy(value => value).ToArray())).ToArray()));
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

        internal static MissionProgressRule CompleteOnDistinctScenarioEvents(
            uint missionId, uint scenarioId, IReadOnlySet<uint> subjects, uint? targetValue = null) =>
            new(
                MissionProgressRuleType.CompleteDistinctSet,
                MissionProgressEventKind.ScenarioEvent,
                subjects,
                counterId: 0,
                targetValue: targetValue,
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
            IReadOnlySet<uint> subjects,
            uint? counterId = null)
        {
            if (!IsDistinctFact(kind))
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    "Distinct completion supports waypoint, Logos or cleared spawn pool events.");
            if (kind == MissionProgressEventKind.SpawnPoolCleared && counterId.HasValue)
                throw new ArgumentException(
                    "A cleared spawn pool set has no counter: a pool that comes back would make it go down.",
                    nameof(counterId));
            return new MissionProgressRule(
                MissionProgressRuleType.CompleteDistinctSet, kind, subjects, counterId);
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

        /// <summary>
        /// One counter fed by any of several subjects - "Destroy 5 Bane Vehicles" counts Stalkers,
        /// Predators and Juggernauts alike. Kills only: a creature, a creature flag or a creature
        /// class.
        /// </summary>
        public static MissionProgressRule IncrementCounterOnAnySubject(
            MissionProgressEventKind kind,
            IEnumerable<uint> subjects,
            uint counterId,
            uint initialValue,
            uint targetValue)
        {
            if (!IsKill(kind))
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    "A counter over several subjects supports kill events.");
            return new MissionProgressRule(
                MissionProgressRuleType.IncrementExactCounter,
                kind,
                subjects,
                counterId,
                initialValue,
                targetValue);
        }

        /// <summary>
        /// The facts a distinct-set rule is complete on, all of them held at once: waypoints and
        /// Logos the character has, and the spawn pools that are dead in the squad's instance the
        /// character stands in.
        /// </summary>
        public static bool IsDistinctFact(MissionProgressEventKind kind) =>
            kind is MissionProgressEventKind.WaypointAcquired or MissionProgressEventKind.LogosAcquired or
                MissionProgressEventKind.SpawnPoolCleared;

        /// <summary>The three ways a kill is named: the creature, a flag of its class, its class.</summary>
        public static bool IsKill(MissionProgressEventKind kind) =>
            kind == MissionProgressEventKind.CreatureKilled ||
            kind == MissionProgressEventKind.CreatureFlagKilled ||
            kind == MissionProgressEventKind.CreatureClassKilled ||
            kind == MissionProgressEventKind.ObjectDestroyed;

        /// <summary>
        /// The happenings a squad shares (GroupCreditService): a kill, a scene's signal, and the
        /// player detected - one of a squad seen is the squad seen.
        /// </summary>
        public static bool IsShared(MissionProgressEventKind kind) =>
            IsKill(kind) ||
            kind == MissionProgressEventKind.ScenarioEvent ||
            kind == MissionProgressEventKind.PlayerDetected;

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
            if (CounterGroups != null)
                return CounterGroups.Select((group, index) => (group, index)).All(entry =>
                    objective.Counters.TryGetValue((uint)entry.index, out var groupCounter) &&
                    groupCounter.CounterId == (uint)entry.index &&
                    groupCounter.InitialValue == 0 &&
                    groupCounter.TargetValue == (uint)entry.group.Count) &&
                    objective.Counters.Count == CounterGroups.Count;
            if (RuleType != MissionProgressRuleType.IncrementExactCounter &&
                (RuleType != MissionProgressRuleType.CompleteDistinctSet || !CounterId.HasValue))
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
