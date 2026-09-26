using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;
    using Structures.Missions;
    using Structures.World;

    internal static class MissionProgressRuleAuthoring
    {
        internal static bool TryBuild(
            IReadOnlyList<MissionTriggerDefinition> progressTriggers,
            out MissionProgressRule rule,
            out IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters,
            out IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> itemCounters,
            out string diagnostic)
        {
            counters = new Dictionary<uint, MissionObjectiveCounterDefinition>();
            itemCounters = new Dictionary<uint, MissionObjectiveItemCounterDefinition>();
            rule = null;
            diagnostic = null;

            if (progressTriggers == null || progressTriggers.Count == 0)
                return false;
            if (progressTriggers.Any(trigger => !trigger.TryGetEventKind(out _)))
            {
                diagnostic = "progress event trigger uses an unknown event_kind value.";
                return false;
            }

            if (progressTriggers.Count > 1)
                return TryBuildDistinctSet(
                    progressTriggers,
                    out rule,
                    out counters,
                    out itemCounters,
                    out diagnostic);

            var trigger = progressTriggers[0];
            trigger.TryGetEventKind(out var kind);
            if (!trigger.SubjectId.HasValue || trigger.SubjectId.Value == 0)
            {
                diagnostic = "progress event trigger must declare a non-zero subject_id.";
                return false;
            }

            var hasCounterId = trigger.CounterId.HasValue;
            var hasInitialValue = trigger.InitialValue.HasValue;
            var hasTargetValue = trigger.TargetValue.HasValue;
            var hasSourceSpawnResolved = trigger.SourceSpawnResolved.HasValue;

            if (kind is MissionProgressEventKind.WaypointAcquired or MissionProgressEventKind.LogosAcquired)
            {
                if (hasCounterId || hasInitialValue || hasTargetValue || hasSourceSpawnResolved)
                {
                    diagnostic = $"{kind} progress rules only support distinct subject sets without counter or spawn parameters.";
                    return false;
                }

                rule = MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(
                    kind,
                    new HashSet<uint> { trigger.SubjectId.Value });
                return true;
            }

            if (kind is MissionProgressEventKind.ItemAcquired or MissionProgressEventKind.ItemConsumed)
            {
                if (!hasCounterId && hasInitialValue && hasTargetValue && !hasSourceSpawnResolved)
                {
                    if (!TryValidateMonotonicRange(
                            "item counter progress rule",
                            "subject_id",
                            trigger.SubjectId.Value,
                            trigger.InitialValue.Value,
                            trigger.TargetValue.Value,
                            out diagnostic))
                    {
                        return false;
                    }

                    itemCounters = new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                    {
                        [trigger.SubjectId.Value] = new(
                            trigger.SubjectId.Value,
                            trigger.InitialValue.Value,
                            trigger.TargetValue.Value)
                    };
                    rule = MissionProgressRule.IncrementItemCounterOnExactSubject(
                        kind,
                        trigger.SubjectId.Value,
                        trigger.InitialValue.Value,
                        trigger.TargetValue.Value);
                    return true;
                }

                if (hasCounterId || hasInitialValue || hasTargetValue || hasSourceSpawnResolved)
                {
                    diagnostic = $"{kind} progress rules must either use subject_id only or use initial_value and target_value without counter_id or source_spawn_resolved.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnExactSubject(
                    kind,
                    trigger.SubjectId.Value);
                return true;
            }

            if (kind == MissionProgressEventKind.ItemEquipped)
            {
                if (hasCounterId || hasInitialValue || hasTargetValue)
                {
                    diagnostic = "item equipped progress rules support subject_id only, with optional source_spawn_resolved=true to match item_template_id.";
                    return false;
                }

                rule = trigger.SourceSpawnResolved == true
                    ? MissionProgressRule.CompleteOnItemEquippedTemplate(
                        trigger.SubjectId.Value)
                    : MissionProgressRule.CompleteOnItemEquippedClass(
                        trigger.SubjectId.Value);
                return true;
            }

            if (kind == MissionProgressEventKind.AbilityHit)
            {
                if (!hasCounterId || trigger.CounterId.Value == 0 ||
                    hasInitialValue || hasTargetValue || hasSourceSpawnResolved)
                {
                    diagnostic = "ability hit progress rules must declare subject_id as action_id and counter_id as target creature_id, without counter ranges or source_spawn_resolved.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnAbilityHit(
                    trigger.SubjectId.Value,
                    trigger.CounterId.Value);
                return true;
            }

            if (kind == MissionProgressEventKind.ScenarioEvent)
            {
                if (!hasCounterId || trigger.CounterId.Value == 0 ||
                    hasInitialValue || hasTargetValue || hasSourceSpawnResolved)
                {
                    diagnostic = "scenario event progress rules must declare subject_id as scenario_event_id and counter_id as scenario_id, without counter ranges or source_spawn_resolved.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnScenarioEvent(
                    trigger.MissionId,
                    trigger.CounterId.Value,
                    trigger.SubjectId.Value);
                return true;
            }

            if (kind == MissionProgressEventKind.ObjectHit)
            {
                if (!hasCounterId || trigger.CounterId.Value == 0 ||
                    hasInitialValue || hasTargetValue || hasSourceSpawnResolved)
                {
                    diagnostic = "object hit progress rules require subject_id as entity_class_id and counter_id as action_id, without counter ranges or source_spawn_resolved.";
                    return false;
                }

                rule = MissionProgressRule.CompleteOnObjectHit(
                    trigger.SubjectId.Value, trigger.CounterId.Value);
                return true;
            }

            if (hasCounterId || hasInitialValue || hasTargetValue)
            {
                if (!hasCounterId || !hasInitialValue || !hasTargetValue || hasSourceSpawnResolved)
                {
                    diagnostic = "counter progress rules must declare counter_id, initial_value, and target_value together, without source_spawn_resolved.";
                    return false;
                }

                if (!TryValidateMonotonicRange(
                        "counter progress rule",
                        "counter_id",
                        trigger.CounterId.Value,
                        trigger.InitialValue.Value,
                        trigger.TargetValue.Value,
                        out diagnostic))
                {
                    return false;
                }

                counters = new Dictionary<uint, MissionObjectiveCounterDefinition>
                {
                    [trigger.CounterId.Value] = new(
                        trigger.CounterId.Value,
                        trigger.InitialValue.Value,
                        trigger.TargetValue.Value)
                };
                rule = MissionProgressRule.IncrementCounterOnExactSubject(
                    kind,
                    trigger.SubjectId.Value,
                    trigger.CounterId.Value,
                    trigger.InitialValue.Value,
                    trigger.TargetValue.Value);
                return true;
            }

            if (hasSourceSpawnResolved &&
                kind != MissionProgressEventKind.CreatureKilled &&
                kind != MissionProgressEventKind.ItemEquipped)
            {
                diagnostic = "source_spawn_resolved is only supported for creature kill rules or item equipped template matching.";
                return false;
            }

            rule = MissionProgressRule.CompleteOnExactSubject(
                kind,
                trigger.SubjectId.Value,
                trigger.SourceSpawnResolved);
            return true;
        }

        private static bool TryBuildDistinctSet(
            IReadOnlyList<MissionTriggerDefinition> progressTriggers,
            out MissionProgressRule rule,
            out IReadOnlyDictionary<uint, MissionObjectiveCounterDefinition> counters,
            out IReadOnlyDictionary<uint, MissionObjectiveItemCounterDefinition> itemCounters,
            out string diagnostic)
        {
            counters = new Dictionary<uint, MissionObjectiveCounterDefinition>();
            itemCounters = new Dictionary<uint, MissionObjectiveItemCounterDefinition>();
            rule = null;
            diagnostic = null;

            progressTriggers[0].TryGetEventKind(out var aggregateKind);
            if (aggregateKind is not MissionProgressEventKind.WaypointAcquired and not MissionProgressEventKind.LogosAcquired)
            {
                diagnostic = "multiple progress event triggers only support waypoint or Logos distinct-subject rules.";
                return false;
            }

            if (!progressTriggers.All(trigger =>
                    trigger.TryGetEventKind(out var kind) &&
                    kind == aggregateKind &&
                    trigger.SubjectId.HasValue &&
                    trigger.SubjectId.Value != 0 &&
                    !trigger.CounterId.HasValue &&
                    !trigger.InitialValue.HasValue &&
                    !trigger.TargetValue.HasValue &&
                    !trigger.SourceSpawnResolved.HasValue))
            {
                diagnostic = "distinct progress subject rules require one event_kind, non-zero subject_id values, and no counter or spawn parameters.";
                return false;
            }

            rule = MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(
                aggregateKind,
                new HashSet<uint>(progressTriggers.Select(trigger => trigger.SubjectId.Value)));
            return true;
        }

        private static bool TryValidateMonotonicRange(
            string ruleLabel,
            string identifierLabel,
            uint identifierValue,
            uint initialValue,
            uint targetValue,
            out string diagnostic)
        {
            if (targetValue > initialValue)
            {
                diagnostic = null;
                return true;
            }

            diagnostic =
                $"{ruleLabel} {identifierLabel} {identifierValue} has initial_value {initialValue} and target_value {targetValue}; target_value must be greater than initial_value so runtime monotonic progress can advance.";
            return false;
        }
    }
}
