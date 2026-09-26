using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;
    using Structures.Missions;
    using Structures.World;

    internal static class MissionObjectiveRuntimeAnalyzer
    {
        internal static MissionObjectiveRuntimeAnalysis Analyze(
            uint objectiveId,
            IReadOnlyList<MissionObjectiveTransitionDefinition> transitions)
        {
            var orderedTransitions = (transitions ?? Array.Empty<MissionObjectiveTransitionDefinition>())
                .OrderBy(transition => transition.Sequence)
                .ThenBy(transition => transition.TransitionId)
                .ToArray();
            var diagnostics = new List<MissionObjectiveRuntimeDiagnostic>();
            var executableTransitions = new List<MissionObjectiveExecutableTransition>();
            var conversations = new List<MissionObjectiveConversation>();
            var revealedObjectiveIds = new HashSet<uint>();
            var activatedObjectiveIds = new HashSet<uint>();
            var counters = new Dictionary<uint, MissionObjectiveCounterDefinition>();
            var itemCounters = new Dictionary<uint, MissionObjectiveItemCounterDefinition>();

            foreach (var transition in orderedTransitions)
            {
                if (!TryBuildExecutableTransition(
                        transition,
                        out var executableTransition,
                        out var diagnostic))
                {
                    if (!string.IsNullOrWhiteSpace(diagnostic))
                    {
                        diagnostics.Add(new MissionObjectiveRuntimeDiagnostic(
                            "invalid-trigger-shape",
                            diagnostic,
                            transition.TransitionId));
                    }

                    continue;
                }

                executableTransitions.Add(executableTransition);
                conversations.AddRange(executableTransition.Conversations);
                foreach (var revealed in executableTransition.RevealedObjectiveIds)
                    revealedObjectiveIds.Add(revealed);
                foreach (var activated in executableTransition.ActivatedObjectiveIds)
                    activatedObjectiveIds.Add(activated);
                MergeCounters(
                    objectiveId,
                    transition.TransitionId,
                    executableTransition.Counters,
                    counters,
                    diagnostics);
                MergeItemCounters(
                    objectiveId,
                    transition.TransitionId,
                    executableTransition.ItemCounters,
                    itemCounters,
                    diagnostics);
            }

            if (diagnostics.Count > 0)
                return new MissionObjectiveRuntimeAnalysis(
                    diagnostics,
                    Array.Empty<MissionObjectiveConversation>(),
                    Array.Empty<uint>(),
                    Array.Empty<uint>(),
                    null,
                    new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                    Array.Empty<MissionObjectiveExecutableTransition>());

            return new MissionObjectiveRuntimeAnalysis(
                Array.Empty<MissionObjectiveRuntimeDiagnostic>(),
                conversations,
                revealedObjectiveIds.OrderBy(value => value).ToArray(),
                activatedObjectiveIds.OrderBy(value => value).ToArray(),
                executableTransitions.Count(transition => transition.ProgressRule != null) == 1
                    ? executableTransitions.Single(transition => transition.ProgressRule != null).ProgressRule
                    : null,
                counters,
                itemCounters,
                executableTransitions);
        }

        private static bool TryBuildExecutableTransition(
            MissionObjectiveTransitionDefinition transition,
            out MissionObjectiveExecutableTransition executableTransition,
            out string diagnostic)
        {
            executableTransition = null;
            diagnostic = null;

            var conversationTriggers = transition.Triggers
                .Where(trigger =>
                    trigger.Kind == MissionTriggerKind.Conversation &&
                    trigger.NpcPackageId.HasValue &&
                    trigger.PlayerFlagId.HasValue)
                .ToArray();
            if (conversationTriggers.Length > 0)
            {
                if (conversationTriggers.Length != transition.Triggers.Count)
                {
                    diagnostic = $"transition {transition.TransitionId} mixes conversation triggers with other trigger kinds; author one executable trigger shape per transition.";
                    return false;
                }

                executableTransition = new MissionObjectiveExecutableTransition(
                    transition.TransitionId,
                    transition.Sequence,
                    transition.TryGetToState(out var conversationToState) ? conversationToState : null,
                    conversationTriggers.Select(trigger => new MissionObjectiveConversation(
                        trigger.NpcPackageId!.Value,
                        trigger.PlayerFlagId!.Value,
                        MissionObjectiveConversationType.Completion)),
                    null,
                    new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                    transition.Actions);
                return true;
            }

            if (!TryBuildEventRule(
                    transition,
                    out var rule,
                    out var counters,
                    out var itemCounters,
                    out diagnostic))
                return false;
            if (rule == null)
                return false;

            executableTransition = new MissionObjectiveExecutableTransition(
                transition.TransitionId,
                transition.Sequence,
                transition.TryGetToState(out var progressToState) ? progressToState : null,
                Array.Empty<MissionObjectiveConversation>(),
                rule,
                counters,
                itemCounters,
                transition.Actions);
            return true;
        }

        private static bool TryBuildEventRule(
            MissionObjectiveTransitionDefinition transition,
            out MissionProgressRule rule,
            out IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters,
            out IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> itemCounters,
            out string diagnostic)
        {
            rule = null;
            counters = new Dictionary<uint, MissionObjectiveCounterDefinition>();
            itemCounters = new Dictionary<uint, MissionObjectiveItemCounterDefinition>();
            diagnostic = null;

            var progressTriggers = transition.Triggers
                .Where(trigger => trigger.Kind == MissionTriggerKind.ProgressEvent)
                .ToArray();
            if (progressTriggers.Length > 0)
            {
                if (progressTriggers.Length != transition.Triggers.Count)
                {
                    diagnostic = $"transition {transition.TransitionId} mixes progress-event triggers with other trigger kinds; author one executable trigger shape per transition.";
                    return false;
                }

                return MissionProgressRuleAuthoring.TryBuild(
                    progressTriggers,
                    out rule,
                    out counters,
                    out itemCounters,
                    out diagnostic);
            }

            var areaTriggers = transition.Triggers
                .Where(trigger => trigger.Kind == MissionTriggerKind.AreaEntered)
                .ToArray();
            if (areaTriggers.Length > 0)
            {
                if (areaTriggers.Length != 1 || transition.Triggers.Count != 1 ||
                    !areaTriggers[0].AreaId.HasValue || areaTriggers[0].AreaId.Value == 0)
                {
                    diagnostic = $"transition {transition.TransitionId} must use exactly one area trigger with a non-zero area_id.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnAreaEntered(
                    transition.MissionId,
                    areaTriggers[0].AreaId.Value);
                return true;
            }

            var objectiveStateTriggers = transition.Triggers
                .Where(trigger => trigger.Kind == MissionTriggerKind.ObjectiveState)
                .ToArray();
            if (objectiveStateTriggers.Length > 0)
            {
                if (objectiveStateTriggers.Length != 1 || transition.Triggers.Count != 1 ||
                    !objectiveStateTriggers[0].RelatedObjectiveId.HasValue ||
                    objectiveStateTriggers[0].RelatedObjectiveId.Value == 0 ||
                    !objectiveStateTriggers[0].TryGetRelatedState(out var relatedState))
                {
                    diagnostic = $"transition {transition.TransitionId} must use exactly one objective-state trigger with a non-zero related_objective_id and a valid related_state.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnObjectiveState(
                    transition.MissionId,
                    objectiveStateTriggers[0].RelatedObjectiveId.Value,
                    (byte)relatedState);
                return true;
            }

            var timerTriggers = transition.Triggers
                .Where(trigger => trigger.Kind == MissionTriggerKind.TimerElapsed)
                .ToArray();
            if (timerTriggers.Length > 0)
            {
                if (timerTriggers.Length != 1 || transition.Triggers.Count != 1 ||
                    !timerTriggers[0].DurationSeconds.HasValue ||
                    timerTriggers[0].DurationSeconds.Value == 0)
                {
                    diagnostic = $"transition {transition.TransitionId} must use exactly one timer trigger with a positive duration_seconds value.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnDeadlineElapsed(
                    transition.MissionId,
                    transition.ObjectiveId,
                    timerTriggers[0].DurationSeconds.Value);
                return true;
            }

            return false;
        }

        private static void MergeCounters(
            uint objectiveId,
            uint transitionId,
            IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> source,
            IDictionary<uint, MissionObjectiveCounterDefinition> destination,
            ICollection<MissionObjectiveRuntimeDiagnostic> diagnostics)
        {
            foreach (var entry in source)
            {
                if (!destination.TryGetValue(entry.Key, out var existing))
                {
                    destination.Add(entry.Key, entry.Value);
                    continue;
                }

                if (existing.InitialValue == entry.Value.InitialValue &&
                    existing.TargetValue == entry.Value.TargetValue)
                    continue;

                diagnostics.Add(new MissionObjectiveRuntimeDiagnostic(
                    "conflicting-progress-counters",
                    $"objective {objectiveId} transition {transitionId} authors counter {entry.Key} with a different range than another executable transition.",
                    transitionId));
            }
        }

        private static void MergeItemCounters(
            uint objectiveId,
            uint transitionId,
            IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> source,
            IDictionary<uint, MissionObjectiveItemCounterDefinition> destination,
            ICollection<MissionObjectiveRuntimeDiagnostic> diagnostics)
        {
            foreach (var entry in source)
            {
                if (!destination.TryGetValue(entry.Key, out var existing))
                {
                    destination.Add(entry.Key, entry.Value);
                    continue;
                }

                if (existing.InitialValue == entry.Value.InitialValue &&
                    existing.TargetValue == entry.Value.TargetValue)
                    continue;

                diagnostics.Add(new MissionObjectiveRuntimeDiagnostic(
                    "conflicting-progress-item-counters",
                    $"objective {objectiveId} transition {transitionId} authors item counter {entry.Key} with a different range than another executable transition.",
                    transitionId));
            }
        }
    }

    internal sealed class MissionObjectiveRuntimeAnalysis
    {
        public IReadOnlyList<MissionObjectiveRuntimeDiagnostic> Diagnostics { get; }
        public IReadOnlyList<MissionObjectiveConversation> Conversations { get; }
        public IReadOnlyList<uint> RevealedObjectiveIds { get; }
        public IReadOnlyList<uint> ActivatedObjectiveIds { get; }
        public MissionProgressRule ProgressRule { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> Counters { get; }
        public IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> ItemCounters { get; }
        public IReadOnlyList<MissionObjectiveExecutableTransition> ExecutableTransitions { get; }

        internal MissionObjectiveRuntimeAnalysis(
            IReadOnlyList<MissionObjectiveRuntimeDiagnostic> diagnostics,
            IReadOnlyList<MissionObjectiveConversation> conversations,
            IReadOnlyList<uint> revealedObjectiveIds,
            IReadOnlyList<uint> activatedObjectiveIds,
            MissionProgressRule progressRule,
            IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters,
            IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> itemCounters,
            IReadOnlyList<MissionObjectiveExecutableTransition> executableTransitions)
        {
            Diagnostics = diagnostics ?? Array.Empty<MissionObjectiveRuntimeDiagnostic>();
            Conversations = conversations ?? Array.Empty<MissionObjectiveConversation>();
            RevealedObjectiveIds = revealedObjectiveIds ?? Array.Empty<uint>();
            ActivatedObjectiveIds = activatedObjectiveIds ?? Array.Empty<uint>();
            ProgressRule = progressRule;
            Counters = counters ?? new Dictionary<uint, MissionObjectiveCounterDefinition>();
            ItemCounters = itemCounters ?? new Dictionary<uint, MissionObjectiveItemCounterDefinition>();
            ExecutableTransitions = executableTransitions ?? Array.Empty<MissionObjectiveExecutableTransition>();
        }
    }

    internal readonly struct MissionObjectiveRuntimeDiagnostic
    {
        public string Code { get; }
        public string Message { get; }
        public uint? TransitionId { get; }

        internal MissionObjectiveRuntimeDiagnostic(
            string code,
            string message,
            uint? transitionId = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            TransitionId = transitionId;
        }
    }
}
