using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Repositories.World;
    using Rasa.Structures.Char;
    using Structures.Missions;
    using Structures.World;

    internal sealed class MissionContentValidator
    {
        private static readonly HashSet<MissionTriggerKind> SupportedTriggerKinds =
            new()
            {
                MissionTriggerKind.Conversation,
                MissionTriggerKind.ProgressEvent,
                MissionTriggerKind.ObjectiveState,
                MissionTriggerKind.AreaEntered,
                MissionTriggerKind.TimerElapsed
            };

        private static readonly HashSet<MissionActionKind> SupportedActionKinds =
            new()
            {
                MissionActionKind.RevealObjective,
                MissionActionKind.ActivateObjective,
                MissionActionKind.CompleteObjective,
                MissionActionKind.StartScenario,
                MissionActionKind.ActivateSpawnGroup,
                MissionActionKind.ShowIndicator,
                MissionActionKind.SetPlayerFlag,
                MissionActionKind.GrantReward,
                MissionActionKind.ShowAmbientConversation,
                MissionActionKind.IssueMissionItem,
                MissionActionKind.ConsumeMissionItem,
                MissionActionKind.RemoveMissionItems
            };

        private static readonly HashSet<MissionSpawnGroupPolicy> SupportedSpawnPolicies =
            new()
            {
                MissionSpawnGroupPolicy.OrdinaryRespawn,
                MissionSpawnGroupPolicy.ScenarioControlled
            };

        private const uint MaxDelayMilliseconds = MissionScenarioStepEntry.MaxDelayMilliseconds;
        private const uint MaxAbilityId = MissionScenarioStepEntry.MaxAbilityId;
        private const byte MaxSkillLevel = MissionScenarioStepEntry.MaxSkillLevel;
        private const byte MaxAbilitySlot = MissionScenarioStepEntry.MaxAbilitySlot;

        internal MissionValidationReport Validate(
            MissionContentSnapshot snapshot,
            IWorldUnitOfWork unitOfWork) => Validate(snapshot, CreateReferences(unitOfWork));

        private MissionValidationReport Validate(MissionContentSnapshot snapshot, MissionContentReferenceSet references)
        {
            var diagnostics = new List<MissionValidationDiagnostic>();
            var requiredMissionIds = snapshot.Definitions.Values
                .Where(definition => definition.Requirement == MissionContentRequirement.Required)
                .Select(definition => definition.MissionId)
                .ToArray();

            foreach (var revisions in snapshot.RevisionsByMission.OrderBy(entry => entry.Key))
            {
                var authoredRevisions = revisions.Value
                    .Where(revision => !string.Equals(revision, "legacy", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                if (authoredRevisions.Length > 1)
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "duplicate-mission-id",
                        $"multiple authored content revisions are present ({string.Join(", ", authoredRevisions)}); keep one active revision per mission.",
                        revisions.Key,
                        snapshot.Definitions.TryGetValue(revisions.Key, out var selected)
                            ? selected.ContentRevision
                            : authoredRevisions[0]));
                }
            }

            foreach (var definition in snapshot.Definitions.Values.OrderBy(definition => definition.MissionId))
            {
                ValidateMission(snapshot, definition, references, diagnostics);
            }

            var report = new MissionValidationReport(diagnostics, requiredMissionIds);
            var inactiveRequired = BuildRequiredChainInactiveDiagnostics(
                snapshot,
                report.Diagnostics);
            if (inactiveRequired.Length == 0)
                return report;
            return new MissionValidationReport(report.Diagnostics.Concat(inactiveRequired), requiredMissionIds);
        }

        private static MissionContentReferenceSet CreateReferences(IWorldUnitOfWork unitOfWork)
        {
            var itemTemplateClasses = (unitOfWork?.Equipment?.GetItemTemplateClasses() ??
                new List<ItemTemplateItemClassEntry>())
                .ToDictionary(entry => entry.ItemTemplateId, entry => entry.ItemClass);
            var creatureClasses = (unitOfWork?.Creatures?.Get() ?? new List<CreatureEntry>())
                .ToDictionary(entry => entry.Id, entry => entry.ClassId);
            return new MissionContentReferenceSet(
                (unitOfWork?.NpcPackages?.Get() ?? new List<NpcPackageEntry>()).Select(entry => entry.PackageId),
                itemTemplateClasses,
                (unitOfWork?.EntityClasses?.Get() ?? new List<EntityClassEntry>()).Select(entry => entry.Id),
                creatureClasses,
                (unitOfWork?.MapInfos?.Get() ?? new List<MapInfoEntry>()).Select(entry => entry.Id));
        }

        private static void ValidateMission(
            MissionContentSnapshot snapshot,
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var channelError in Rasa.Missions.Definitions.MissionChannelValidation.Errors(definition.Mission))
                diagnostics.Add(new MissionValidationDiagnostic("invalid-mission-channel",
                    channelError, definition.MissionId, definition.ContentRevision));
            foreach (var source in definition.Mission.RadioSources)
                if (source?.MapContextId is uint map && !references.MapContextIds.Contains(map))
                    diagnostics.Add(new MissionValidationDiagnostic("missing-radio-source-map",
                        $"Radio source {source.Key} references unavailable map context {map}.",
                        definition.MissionId, definition.ContentRevision));
            if (definition.Mission.RepeatPolicy.ValidationError is { } repeatError)
                diagnostics.Add(new MissionValidationDiagnostic("invalid-repeat-policy", repeatError,
                    definition.MissionId, definition.ContentRevision));
            if (!definition.Mission.ClientNameTextId.HasValue)
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "missing-client-text",
                    "mission client_name_text_id is missing; author a non-zero binding.",
                    definition.MissionId,
                    definition.ContentRevision));
            }

            if (definition.Objectives.Count == 0)
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "missing-objective-id",
                    "mission has no authored objectives; add at least one objective row.",
                    definition.MissionId,
                    definition.ContentRevision));
            }

            foreach (var duplicateObjectiveId in definition.DuplicateObjectiveIds)
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "duplicate-objective-id",
                    $"objective {duplicateObjectiveId} is defined more than once in the selected revision.",
                    definition.MissionId,
                    definition.ContentRevision,
                    duplicateObjectiveId));
            }

            foreach (var objective in definition.Objectives.Values.OrderBy(objective => objective.ObjectiveId))
            {
                if (!objective.ClientNameTextId.HasValue || !objective.ClientBodyTextId.HasValue)
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "missing-client-text",
                        "objective text bindings are incomplete; populate client_name_text_id and client_body_text_id.",
                        definition.MissionId,
                        definition.ContentRevision,
                        objective.ObjectiveId));
                }
            }

            ValidateTransitions(definition, references, diagnostics);
            ValidateReferences(snapshot, definition, references, diagnostics);
            ValidateCounterTextBindings(definition, diagnostics);
            ValidateRewardReferences(definition, diagnostics);
            ValidateObjectiveGraph(definition, diagnostics);
            ValidateRewards(definition, references, diagnostics);
            ValidateAreas(definition, references, diagnostics);
            ValidateSpawnGroups(definition, references, diagnostics);
            ValidateScenarios(definition, references, diagnostics);
            ValidateProgressReferences(definition, references, diagnostics);
        }

        private static void ValidateTransitions(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            var transitionsByObjective = definition.Transitions.Values
                .GroupBy(transition => transition.ObjectiveId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<MissionObjectiveTransitionDefinition>)group
                        .OrderBy(transition => transition.Sequence)
                        .ThenBy(transition => transition.TransitionId)
                        .ToArray());

            foreach (var transition in definition.Transitions.Values
                .OrderBy(transition => transition.ObjectiveId)
                .ThenBy(transition => transition.TransitionId))
            {
                foreach (var trigger in transition.Triggers)
                {
                    if (!trigger.HasDefinedKind())
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "unsupported-trigger",
                            $"trigger kind {(int)trigger.Kind} is unknown; use a supported discriminator.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            trigger.TriggerId));
                        continue;
                    }

                    if (!SupportedTriggerKinds.Contains(trigger.Kind))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "unsupported-trigger",
                            $"trigger kind {trigger.Kind} is not executable in the current runtime.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            trigger.TriggerId));
                    }

                    if (trigger.Kind == MissionTriggerKind.Conversation &&
                        (!trigger.NpcPackageId.HasValue ||
                            !references.NpcPackageIds.Contains(trigger.NpcPackageId.Value)))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-npc-package",
                            $"conversation trigger references missing npc_package.package_id {trigger.NpcPackageId?.ToString() ?? "null"}; " +
                            "restore npc_package.package_id " +
                            $"{trigger.NpcPackageId?.ToString() ?? "null"} or update mission_trigger.npc_package_id to a valid package.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            trigger.TriggerId));
                    }

                    if (trigger.Kind == MissionTriggerKind.AreaEntered &&
                        (!trigger.AreaId.HasValue ||
                            !definition.Areas.ContainsKey(trigger.AreaId.Value)))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-area",
                            $"area trigger references missing mission_area {trigger.AreaId?.ToString() ?? "null"}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            trigger.TriggerId));
                    }

                    if (trigger.Kind == MissionTriggerKind.TimerElapsed &&
                        (!trigger.DurationSeconds.HasValue || trigger.DurationSeconds.Value == 0))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-delay",
                            "timer trigger must declare a positive duration_seconds value.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            trigger.TriggerId));
                    }

                    if (trigger.Kind == MissionTriggerKind.ObjectiveState &&
                        (!trigger.RelatedObjectiveId.HasValue ||
                            !definition.Objectives.ContainsKey(trigger.RelatedObjectiveId.Value) ||
                            !trigger.TryGetRelatedState(out _)))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-related-objective",
                            $"objective-state trigger references missing objective {trigger.RelatedObjectiveId?.ToString() ?? "null"} or an invalid related_state.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            trigger.TriggerId));
                    }
                }

                var progressTriggers = transition.Triggers
                    .Where(trigger => trigger.Kind == MissionTriggerKind.ProgressEvent)
                    .ToArray();
                if (progressTriggers.Length > 0 &&
                    !MissionProgressRuleAuthoring.TryBuild(
                        progressTriggers,
                        out _,
                        out _,
                        out _,
                        out var progressDiagnostic))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-progress-event",
                        progressDiagnostic,
                        definition.MissionId,
                        definition.ContentRevision,
                        transition.ObjectiveId,
                        transition.TransitionId));
                }

                foreach (var action in transition.Actions)
                {
                    if (!Rasa.Missions.Definitions.MissionItemValidation.ActionMatches(action))
                        diagnostics.Add(new MissionValidationDiagnostic("invalid-item-action",
                            "Mission item action kind does not match its typed item intent.", definition.MissionId,
                            definition.ContentRevision, transition.ObjectiveId, transition.TransitionId, actionId: action.ActionId));
                    if (!action.HasDefinedKind())
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "unsupported-action",
                            $"action kind {(int)action.Kind} is unknown; use a supported discriminator.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                        continue;
                    }

                    if (!SupportedActionKinds.Contains(action.Kind))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "unsupported-action",
                            $"action kind {action.Kind} is not executable in the current runtime.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }

                    if (action.Kind is MissionActionKind.RevealObjective or MissionActionKind.ActivateObjective or MissionActionKind.CompleteObjective)
                    {
                        if (!action.TargetObjectiveId.HasValue ||
                            !definition.Objectives.ContainsKey(action.TargetObjectiveId.Value))
                        {
                            diagnostics.Add(new MissionValidationDiagnostic(
                                "missing-target",
                                $"action references missing objective {action.TargetObjectiveId?.ToString() ?? "null"}.",
                                definition.MissionId,
                                definition.ContentRevision,
                                transition.ObjectiveId,
                                transition.TransitionId,
                                actionId: action.ActionId));
                        }
                    }

                    if (action.Kind == MissionActionKind.GrantReward &&
                        (!action.RewardId.HasValue || !definition.Rewards.ContainsKey(action.RewardId.Value)))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-reward",
                            $"grant reward action references missing reward {action.RewardId?.ToString() ?? "null"}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }

                    if (action.Kind == MissionActionKind.ActivateSpawnGroup &&
                        (!action.SpawnGroupId.HasValue || !definition.SpawnGroups.ContainsKey(action.SpawnGroupId.Value)))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-spawn-group",
                            $"activate spawn group action references missing spawn group {action.SpawnGroupId?.ToString() ?? "null"}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }

                    if (action.Kind == MissionActionKind.StartScenario &&
                        (!action.ScenarioId.HasValue || !definition.Scenarios.ContainsKey(action.ScenarioId.Value)))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-scenario",
                            $"start scenario action references missing scenario {action.ScenarioId?.ToString() ?? "null"}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }

                    if (action.Kind == MissionActionKind.ShowAmbientConversation &&
                        (!action.NpcPackageId.HasValue || !action.PlayerFlagId.HasValue))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-ambient-conversation-binding",
                            "ambient conversation action is missing its NPC package or player flag.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }

                    if (action.Kind == MissionActionKind.ShowIndicator &&
                        (!action.IndicatorId.HasValue ||
                            !definition.Objectives.Values.Any(objective =>
                                objective.Indicators.Any(indicator =>
                                    indicator.IndicatorId == action.IndicatorId.Value))))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-indicator",
                            $"show indicator action references missing mission_indicator {action.IndicatorId?.ToString() ?? "null"}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }

                    if (action.Kind == MissionActionKind.SetPlayerFlag &&
                        (!action.PlayerFlagId.HasValue || !action.PlayerFlagValue.HasValue))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-player-flag-binding",
                            "set player flag action is missing its player_flag_id or player_flag_value.",
                            definition.MissionId,
                            definition.ContentRevision,
                            transition.ObjectiveId,
                            transition.TransitionId,
                            actionId: action.ActionId));
                    }
                    else if (action.Kind == MissionActionKind.SetPlayerFlag &&
                        !Structures.Char.CharacterFlagIds.IsMissionFlag(action.PlayerFlagId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "reserved-player-flag-id",
                            "Mission flags must use IDs 1..2147483647; higher IDs are reserved for named server state.",
                            definition.MissionId, definition.ContentRevision, transition.ObjectiveId,
                            transition.TransitionId, actionId: action.ActionId));
                    }
                }
            }

            foreach (var objective in definition.Objectives.Values.OrderBy(objective => objective.ObjectiveId))
            {
                var runtime = MissionObjectiveRuntimeAnalyzer.Analyze(
                    objective.ObjectiveId,
                    transitionsByObjective.TryGetValue(objective.ObjectiveId, out var objectiveTransitions)
                        ? objectiveTransitions
                        : Array.Empty<MissionObjectiveTransitionDefinition>());
                foreach (var diagnostic in runtime.Diagnostics)
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        diagnostic.Code,
                        diagnostic.Message,
                        definition.MissionId,
                        definition.ContentRevision,
                        objective.ObjectiveId,
                        diagnostic.TransitionId));
                }
            }
        }

        private static MissionValidationDiagnostic[] BuildRequiredChainInactiveDiagnostics(
            MissionContentSnapshot snapshot,
            IReadOnlyList<MissionValidationDiagnostic> diagnostics)
        {
            var inactiveMissionIds = diagnostics
                .Where(diagnostic => diagnostic.MissionId.HasValue)
                .Select(diagnostic => diagnostic.MissionId.Value)
                .ToHashSet();
            var propagated = new List<MissionValidationDiagnostic>();
            var requiredDefinitions = snapshot.Definitions.Values
                .Where(definition => definition.Requirement == MissionContentRequirement.Required)
                .OrderBy(definition => definition.MissionId)
                .ToArray();

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var definition in requiredDefinitions)
                {
                    if (inactiveMissionIds.Contains(definition.MissionId))
                        continue;

                    var blockedPrerequisites = definition.Prerequisites
                        .Where(prerequisite =>
                            prerequisite.RequiredMissionId.HasValue &&
                            inactiveMissionIds.Contains(prerequisite.RequiredMissionId.Value))
                        .OrderBy(prerequisite => prerequisite.PrerequisiteId)
                        .ThenBy(prerequisite => prerequisite.RequiredMissionId.Value)
                        .ToArray();
                    if (blockedPrerequisites.Length == 0)
                        continue;

                    foreach (var prerequisite in blockedPrerequisites)
                    {
                        propagated.Add(new MissionValidationDiagnostic(
                            "required-chain-inactive",
                            $"required prerequisite mission {prerequisite.RequiredMissionId.Value} is inactive; fix that mission or mark the chain optional.",
                            definition.MissionId,
                            definition.ContentRevision));
                    }

                    inactiveMissionIds.Add(definition.MissionId);
                    changed = true;
                }
            }

            return propagated.ToArray();
        }

        private static void ValidateCounterTextBindings(
            MissionContentDefinition definition,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var objective in definition.Objectives.Values.OrderBy(objective => objective.ObjectiveId))
            {
                foreach (var counter in objective.Counters.Values.OrderBy(counter => counter.CounterId))
                {
                    if (counter.CounterId >= (uint)objective.ClientCounterTextIds.Count)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-counter-text-binding",
                            $"counter {counter.CounterId} has no authored client counter text slot.",
                            definition.MissionId,
                            definition.ContentRevision,
                            objective.ObjectiveId));
                        continue;
                    }

                    if (!objective.ClientCounterTextIds[(int)counter.CounterId].HasValue)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-counter-text-binding",
                            $"counter {counter.CounterId} is missing its authored client counter text binding.",
                            definition.MissionId,
                            definition.ContentRevision,
                            objective.ObjectiveId));
                    }
                }
            }
        }

        private static void ValidateRewardReferences(
            MissionContentDefinition definition,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            if (definition.Rewards.Count == 0)
                return;

            var referencedRewardIds = definition.Transitions.Values
                .SelectMany(transition => transition.Actions)
                .Where(action => action.Kind == MissionActionKind.GrantReward && action.RewardId.HasValue)
                .Select(action => action.RewardId.Value)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();

            if (referencedRewardIds.Length == 0)
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "missing-reward-reference",
                    "mission authors rewards but no GrantReward action references a reward_id.",
                    definition.MissionId,
                    definition.ContentRevision));
                return;
            }

            if (referencedRewardIds.Length > 1)
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "ambiguous-reward-reference",
                    $"mission references multiple GrantReward reward_ids ({string.Join(", ", referencedRewardIds)}); the current runtime supports exactly one turn-in reward.",
                    definition.MissionId,
                    definition.ContentRevision));
            }
        }

        private static void ValidateReferences(
            MissionContentSnapshot snapshot,
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var prerequisite in definition.Prerequisites)
            {
                if (prerequisite.RequiredMissionId.HasValue &&
                    snapshot.Definitions.TryGetValue(prerequisite.RequiredMissionId.Value, out var target) &&
                    !string.Equals(target.ContentRevision, definition.ContentRevision, StringComparison.Ordinal))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "cross-revision-reference",
                        $"prerequisite mission {prerequisite.RequiredMissionId.Value} resolves to content revision {target.ContentRevision}; keep required chains on one revision.",
                        definition.MissionId,
                        definition.ContentRevision));
                }
            }

            foreach (var objective in definition.Objectives.Values)
            {
                var rule = objective.ProgressRule;
                if (rule != null &&
                    rule.Kind == MissionProgressEventKind.MissionCompleted)
                {
                    var targetMissionId = rule.Subjects.Single();
                    if (snapshot.Definitions.TryGetValue(targetMissionId, out var target) &&
                        !string.Equals(target.ContentRevision, definition.ContentRevision, StringComparison.Ordinal))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "cross-revision-reference",
                            $"mission completed progress rule references mission {targetMissionId} on revision {target.ContentRevision}; keep authored mission references on one revision.",
                            definition.MissionId,
                            definition.ContentRevision,
                            objective.ObjectiveId));
                    }
                }
            }
        }

        private static void ValidateObjectiveGraph(
            MissionContentDefinition definition,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            var revealEdges = definition.Objectives.Keys.ToDictionary(
                objectiveId => objectiveId,
                objectiveId => definition.Transitions.Values
                    .Where(transition => transition.ObjectiveId == objectiveId)
                    .SelectMany(transition => transition.Actions)
                    .Where(action =>
                        action.Kind == MissionActionKind.RevealObjective &&
                        action.TargetObjectiveId.HasValue)
                    .Select(action => action.TargetObjectiveId.Value)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray());
            var activateEdges = definition.Objectives.Keys.ToDictionary(
                objectiveId => objectiveId,
                objectiveId => definition.Transitions.Values
                    .Where(transition => transition.ObjectiveId == objectiveId)
                    .SelectMany(transition => transition.Actions)
                    .Where(action =>
                        action.Kind == MissionActionKind.ActivateObjective &&
                        action.TargetObjectiveId.HasValue)
                    .Select(action => action.TargetObjectiveId.Value)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray());

            var initialObjectives = definition.Objectives.Values
                .Where(objective => objective.InitialState == MissionObjectiveState.Incomplete)
                .Select(objective => objective.ObjectiveId)
                .ToHashSet();
            if (definition.Objectives.Count > 0 && initialObjectives.Count == 0)
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "invalid-objective-graph",
                    "mission has no initially incomplete objective; author one entry point.",
                    definition.MissionId,
                    definition.ContentRevision));
            }

            foreach (var objective in definition.Objectives.Values.OrderBy(objective => objective.ObjectiveId))
            {
                if (objective.InitialState == MissionObjectiveState.Inactive &&
                    activateEdges.Values.SelectMany(ids => ids).Contains(objective.ObjectiveId) &&
                    !revealEdges.Values.SelectMany(ids => ids).Contains(objective.ObjectiveId))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-objective-graph",
                        "inactive objectives must be revealed before they are activated.",
                        definition.MissionId,
                        definition.ContentRevision,
                        objective.ObjectiveId));
                }
            }

            var graph = BuildObjectiveGraph(definition.Objectives.Values);
            var visiting = new HashSet<uint>();
            var visited = new HashSet<uint>();
            foreach (var node in graph.Keys.OrderBy(value => value))
            {
                if (DetectCycle(node, graph, visiting, visited))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "transition-cycle",
                        "reveal/activate actions create a cycle; break the successor loop.",
                        definition.MissionId,
                        definition.ContentRevision,
                        node));
                    break;
                }
            }
        }

        internal static IReadOnlyDictionary<uint, uint[]> BuildObjectiveGraph(
            IEnumerable<Structures.MissionObjectiveDefinition> objectives) =>
            objectives.ToDictionary(objective => objective.ObjectiveId,
                objective => objective.GetExecutableTransitionsOrLegacyDefault()
                    .SelectMany(transition => transition.RevealedObjectiveIds.Concat(transition.ActivatedObjectiveIds))
                    .Distinct().ToArray());

        private static bool DetectCycle(
            uint node,
            IReadOnlyDictionary<uint, uint[]> graph,
            ISet<uint> visiting,
            ISet<uint> visited)
        {
            if (visited.Contains(node))
                return false;
            if (!visiting.Add(node))
                return true;
            if (graph.TryGetValue(node, out var edges))
            {
                foreach (var edge in edges)
                {
                    if (graph.ContainsKey(edge) && DetectCycle(edge, graph, visiting, visited))
                        return true;
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }

        private static void ValidateRewards(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var reward in definition.Rewards.Values.OrderBy(reward => reward.RewardId))
            {
                foreach (var item in reward.FixedItems.Concat(reward.SelectableItems))
                {
                    if (!references.ItemTemplateClasses.TryGetValue(item.ItemTemplateId, out var entityClassId))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-item-template",
                            $"reward item {item.ItemId} references missing item template {item.ItemTemplateId}.",
                            definition.MissionId,
                            definition.ContentRevision));
                        continue;
                    }

                    if (!references.EntityClassIds.Contains(entityClassId))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-entity-class",
                            $"item template {item.ItemTemplateId} resolves to missing entity class {entityClassId}.",
                            definition.MissionId,
                            definition.ContentRevision));
                    }

                    if (item.Quantity == 0)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-quantity",
                            $"reward item {item.ItemId} must have a positive quantity.",
                            definition.MissionId,
                            definition.ContentRevision));
                    }
                }

                var hasSelectable = reward.SelectableItems.Count > 0;
                if ((reward.SelectionCount == 0 && hasSelectable) ||
                    (reward.SelectionCount > 0 && !hasSelectable))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-reward-selection",
                        "reward selection_count must match the presence of selectable reward items.",
                        definition.MissionId,
                        definition.ContentRevision));
                }
            }
        }

        private static void ValidateProgressReferences(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var objective in definition.Objectives.Values.OrderBy(objective => objective.ObjectiveId))
            {
                var rule = objective.ProgressRule;
                if (rule == null)
                    continue;

                foreach (var subjectId in rule.Subjects.OrderBy(value => value))
                {
                    switch (rule.Kind)
                    {
                        case MissionProgressEventKind.CreatureKilled:
                            if (!references.CreatureClasses.ContainsKey(subjectId))
                            {
                                diagnostics.Add(new MissionValidationDiagnostic(
                                    "missing-creature",
                                    $"creature kill progress rule references missing creature {subjectId}.",
                                    definition.MissionId,
                                    definition.ContentRevision,
                                    objective.ObjectiveId));
                            }

                            break;

                        case MissionProgressEventKind.ItemAcquired:
                        case MissionProgressEventKind.ItemConsumed:
                        case MissionProgressEventKind.InteractionUsed:
                        case MissionProgressEventKind.ObjectHit:
                            if (!references.EntityClassIds.Contains(subjectId))
                            {
                                diagnostics.Add(new MissionValidationDiagnostic(
                                    "missing-entity-class",
                                    $"progress rule references missing entity class {subjectId}.",
                                    definition.MissionId,
                                    definition.ContentRevision,
                                    objective.ObjectiveId));
                            }

                            break;

                        case MissionProgressEventKind.ItemEquipped:
                            if (rule.SourceSpawnResolved == true)
                            {
                                if (!references.ItemTemplateClasses.ContainsKey(subjectId))
                                {
                                    diagnostics.Add(new MissionValidationDiagnostic(
                                        "missing-item-template",
                                        $"item equipped progress rule references missing item template {subjectId}.",
                                        definition.MissionId,
                                        definition.ContentRevision,
                                        objective.ObjectiveId));
                                }
                            }
                            else if (!references.EntityClassIds.Contains(subjectId))
                            {
                                diagnostics.Add(new MissionValidationDiagnostic(
                                    "missing-entity-class",
                                    $"item equipped progress rule references missing entity class {subjectId}.",
                                    definition.MissionId,
                                    definition.ContentRevision,
                                    objective.ObjectiveId));
                            }

                            break;

                        case MissionProgressEventKind.AbilityHit:
                            if (!references.CreatureClasses.ContainsKey(rule.DetailId.GetValueOrDefault()))
                            {
                                diagnostics.Add(new MissionValidationDiagnostic(
                                    "missing-creature",
                                    $"ability hit progress rule references missing creature {rule.DetailId?.ToString() ?? "null"}.",
                                    definition.MissionId,
                                    definition.ContentRevision,
                                    objective.ObjectiveId));
                            }

                            break;

                        case MissionProgressEventKind.ScenarioEvent:
                            if (!definition.Scenarios.TryGetValue(
                                    rule.DetailId.GetValueOrDefault(),
                                    out var scenario) ||
                                scenario.Steps.All(step => step.ScenarioEventId != subjectId))
                            {
                                diagnostics.Add(new MissionValidationDiagnostic(
                                    "missing-scenario-step",
                                    $"scenario event progress rule references missing scenario {rule.DetailId?.ToString() ?? "null"} scenario_event_id {subjectId}.",
                                    definition.MissionId,
                                    definition.ContentRevision,
                                    objective.ObjectiveId));
                            }

                            break;
                    }
                }
            }
        }

        private static void ValidateAreas(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var area in definition.Areas.Values.OrderBy(area => area.AreaId))
            {
                if (!references.MapContextIds.Contains(area.MapContextId))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "missing-map-context",
                        $"mission area {area.AreaId} references missing map_context {area.MapContextId}.",
                        definition.MissionId,
                        definition.ContentRevision));
                }

                if (area.Shape == MissionAreaShape.Sphere &&
                    (!area.Radius.HasValue || area.Radius.Value <= 0))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-radius",
                        $"mission area {area.AreaId} must have a positive radius.",
                        definition.MissionId,
                        definition.ContentRevision));
                }
            }

            foreach (var objective in definition.Objectives.Values.OrderBy(objective => objective.ObjectiveId))
            {
                foreach (var indicator in objective.Indicators.Where(indicator => indicator.Radius <= 0))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-radius",
                        $"indicator {indicator.IndicatorId} must have a positive radius.",
                        definition.MissionId,
                        definition.ContentRevision,
                        objective.ObjectiveId));
                }
            }
        }

        private static void ValidateSpawnGroups(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var spawnGroup in definition.SpawnGroups.Values.OrderBy(group => group.SpawnGroupId))
            {
                if (!references.MapContextIds.Contains(spawnGroup.MapContextId))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "missing-map-context",
                        $"spawn group {spawnGroup.SpawnGroupId} references missing map_context {spawnGroup.MapContextId}.",
                        definition.MissionId,
                        definition.ContentRevision));
                }

                if (spawnGroup.AreaId.HasValue &&
                    !definition.Areas.ContainsKey(spawnGroup.AreaId.Value))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "missing-area",
                        $"spawn group {spawnGroup.SpawnGroupId} references missing mission_area {spawnGroup.AreaId.Value}.",
                        definition.MissionId,
                        definition.ContentRevision));
                }

                if (spawnGroup.RespawnSeconds.HasValue &&
                    spawnGroup.RespawnSeconds.Value == 0)
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-delay",
                        $"spawn group {spawnGroup.SpawnGroupId} must use a positive respawn_seconds value when present.",
                        definition.MissionId,
                        definition.ContentRevision));
                }

                if (!SupportedSpawnPolicies.Contains(spawnGroup.SpawnPolicy))
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-spawn-policy",
                        $"spawn group {spawnGroup.SpawnGroupId} uses unsupported spawn_policy {(byte)spawnGroup.SpawnPolicy}.",
                        definition.MissionId,
                        definition.ContentRevision));
                }

                if (spawnGroup.SpawnPolicy == MissionSpawnGroupPolicy.ScenarioControlled &&
                    spawnGroup.RespawnSeconds.HasValue)
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "invalid-spawn-policy",
                        $"scenario-controlled spawn group {spawnGroup.SpawnGroupId} must not declare respawn_seconds.",
                        definition.MissionId,
                        definition.ContentRevision));
                }

                foreach (var spawn in spawnGroup.Spawns)
                {
                    if (spawn.Quantity == 0)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-quantity",
                            $"spawn {spawn.SpawnId} must have a positive quantity.",
                            definition.MissionId,
                            definition.ContentRevision));
                    }

                    if (references.CreatureClasses.TryGetValue(spawn.CreatureId, out var creatureClassId))
                    {
                        if (!references.EntityClassIds.Contains(creatureClassId))
                        {
                            diagnostics.Add(new MissionValidationDiagnostic(
                                "missing-entity-class",
                                $"spawn creature {spawn.CreatureId} resolves to missing entity class {creatureClassId}.",
                                definition.MissionId,
                                definition.ContentRevision));
                        }
                    }
                }
            }
        }

        private static void ValidateScenarios(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            foreach (var scenario in definition.Scenarios.Values)
            {
                if (scenario.Steps.Count == 0)
                {
                    diagnostics.Add(new MissionValidationDiagnostic(
                        "missing-scenario",
                        $"scenario {scenario.ScenarioId} has no steps; author at least one step.",
                        definition.MissionId,
                        definition.ContentRevision));
                    continue;
                }

                foreach (var step in scenario.Steps)
                {
                    ValidateScenarioStep(
                        definition,
                        references,
                        scenario.ScenarioId,
                        step,
                        diagnostics);
                }
            }
        }

        private static void ValidateScenarioStep(
            MissionContentDefinition definition,
            MissionContentReferenceSet references,
            uint scenarioId,
            MissionScenarioStepDefinition step,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            if (!step.HasDefinedKind())
            {
                diagnostics.Add(new MissionValidationDiagnostic(
                    "unsupported-scenario-step",
                    $"scenario step kind {(int)step.Kind} is unknown; use an approved discriminator.",
                    definition.MissionId,
                    definition.ContentRevision,
                    scenarioId: scenarioId,
                    stepId: step.StepId));
                return;
            }

            switch (step.Kind)
            {
                case MissionScenarioStepKind.SpawnGroup:
                case MissionScenarioStepKind.DespawnGroup:
                    if (!step.SpawnGroupId.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "spawn group steps require spawn_group_id.",
                            diagnostics);
                        return;
                    }

                    if (!definition.SpawnGroups.TryGetValue(step.SpawnGroupId.Value, out var scenarioSpawnGroup))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-spawn-group",
                            $"scenario step references missing spawn group {step.SpawnGroupId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                        return;
                    }

                    return;

                case MissionScenarioStepKind.EscortSpawnGroup:
                    if (!step.SpawnGroupId.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "escort spawn group steps require spawn_group_id.",
                            diagnostics);
                        return;
                    }

                    if (!definition.SpawnGroups.TryGetValue(step.SpawnGroupId.Value, out var escortSpawnGroup))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-spawn-group",
                            $"scenario step references missing spawn group {step.SpawnGroupId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                        return;
                    }

                    if (escortSpawnGroup.SpawnPolicy != MissionSpawnGroupPolicy.ScenarioControlled)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-spawn-policy",
                            $"escort step requires spawn group {step.SpawnGroupId.Value} to use spawn_policy ScenarioControlled.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.SpawnDynamicObject:
                    if (string.IsNullOrWhiteSpace(step.DynamicObjectKey) ||
                        !step.EntityClassId.HasValue ||
                        !step.PosX.HasValue ||
                        !step.PosY.HasValue ||
                        !step.PosZ.HasValue ||
                        !step.Orientation.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "spawn dynamic object steps require dynamic_object_key, entity_class_id, pos_x, pos_y, pos_z, and orientation.",
                            diagnostics);
                        return;
                    }

                    if (!references.EntityClassIds.Contains(step.EntityClassId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-entity-class",
                            $"dynamic object step references missing entity class {step.EntityClassId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.DespawnDynamicObject:
                    if (string.IsNullOrWhiteSpace(step.DynamicObjectKey))
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "despawn dynamic object steps require dynamic_object_key.",
                            diagnostics);
                    }

                    return;

                case MissionScenarioStepKind.EnableInteraction:
                case MissionScenarioStepKind.DisableInteraction:
                    if (step.EntityClassId.HasValue)
                    {
                        if (!references.EntityClassIds.Contains(step.EntityClassId.Value))
                        {
                            diagnostics.Add(new MissionValidationDiagnostic(
                                "missing-entity-class",
                                $"interaction step references missing entity class {step.EntityClassId.Value}.",
                                definition.MissionId,
                                definition.ContentRevision,
                                scenarioId: scenarioId,
                                stepId: step.StepId));
                        }

                        return;
                    }

                    if (!step.SpawnGroupId.HasValue || !step.SpawnId.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "interaction steps require either entity_class_id or spawn_group_id plus spawn_id.",
                            diagnostics);
                        return;
                    }

                    if (!definition.SpawnGroups.TryGetValue(step.SpawnGroupId.Value, out var interactionSpawnGroup))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-spawn-group",
                            $"interaction step references missing spawn group {step.SpawnGroupId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                        return;
                    }

                    if (!interactionSpawnGroup.Spawns.Any(spawn => spawn.SpawnId == step.SpawnId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-spawn",
                            $"interaction step references missing spawn {step.SpawnId.Value} in spawn group {step.SpawnGroupId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.RevealObjective:
                case MissionScenarioStepKind.ActivateObjective:
                case MissionScenarioStepKind.CompleteObjective:
                case MissionScenarioStepKind.FailObjective:
                    if (!step.TargetObjectiveId.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "objective steps require target_objective_id.",
                            diagnostics);
                        return;
                    }

                    if (!definition.Objectives.ContainsKey(step.TargetObjectiveId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-target",
                            $"scenario step references missing objective {step.TargetObjectiveId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.StartDeadline:
                    if (!step.DelayMilliseconds.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "deadline start steps require delay_milliseconds.",
                            diagnostics);
                        return;
                    }

                    if (!HasValidDelay(step.DelayMilliseconds.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-delay",
                            $"deadline delay_milliseconds {step.DelayMilliseconds.Value} must be between 1 and {MaxDelayMilliseconds}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.CancelDeadline:
                case MissionScenarioStepKind.SatisfyDeadline:
                    return;

                case MissionScenarioStepKind.GrantRewardPackage:
                    if (!step.RewardId.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "reward steps require reward_id.",
                            diagnostics);
                        return;
                    }

                    if (!definition.Rewards.ContainsKey(step.RewardId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-reward",
                            $"scenario step references missing reward {step.RewardId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                        return;
                    }

                    var reward = definition.Rewards[step.RewardId.Value];
                    if (reward.SelectionCount > 0 || reward.SelectableItems.Count > 0)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-scenario-reward-selection",
                            $"scenario step reward {step.RewardId.Value} must not contain selectable alternatives.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.GrantSkillAbility:
                    if (!step.SkillId.HasValue || !step.AbilityId.HasValue || !step.SkillLevel.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "skill grant steps require skill_id, ability_id, and skill_level.",
                            diagnostics);
                        return;
                    }

                    if (step.SkillId.Value == 0)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "skill_id must be greater than zero.",
                            diagnostics);
                    }

                    if (step.AbilityId.Value == 0 || step.AbilityId.Value > MaxAbilityId)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            $"ability_id must be between 1 and {MaxAbilityId}.",
                            diagnostics);
                    }

                    if (step.SkillLevel.Value is < 1 or > MaxSkillLevel)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            $"skill_level must be between 1 and {MaxSkillLevel}.",
                            diagnostics);
                    }

                    if (step.AbilitySlot.HasValue &&
                        step.AbilitySlot.Value > MaxAbilitySlot)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            $"ability_slot must be between 0 and {MaxAbilitySlot} when it is authored.",
                            diagnostics);
                    }

                    return;

                case MissionScenarioStepKind.PlayTutorial:
                    if (!step.TutorialId.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "tutorial steps require tutorial_id.",
                            diagnostics);
                        return;
                    }

                    if (!step.TryGetTutorialId(out _))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-tutorial",
                            $"tutorial step references unknown tutorial_id {step.TutorialId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    if (step.AudioSetId.HasValue && step.AudioSetId.Value == 0)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "audio_set_id must be greater than zero when it is authored.",
                            diagnostics);
                    }

                    return;

                case MissionScenarioStepKind.ScheduleScenario:
                    if (!step.TargetScenarioId.HasValue || !step.DelayMilliseconds.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "scheduled scenario steps require target_scenario_id and delay_milliseconds.",
                            diagnostics);
                        return;
                    }

                    if (!definition.Scenarios.ContainsKey(step.TargetScenarioId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-scenario",
                            $"scheduled scenario step references missing scenario {step.TargetScenarioId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    if (!HasValidDelay(step.DelayMilliseconds.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-delay",
                            $"scenario delay_milliseconds {step.DelayMilliseconds.Value} must be between 1 and {MaxDelayMilliseconds}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.ResetAttempt:
                    var hasScenarioTarget = step.TargetScenarioId.HasValue;
                    var hasAttemptKey = !string.IsNullOrWhiteSpace(step.AttemptKey);
                    if (hasScenarioTarget == hasAttemptKey)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "reset attempt steps require exactly one of target_scenario_id or attempt_key.",
                            diagnostics);
                        return;
                    }

                    if (hasScenarioTarget && !definition.Scenarios.ContainsKey(step.TargetScenarioId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-scenario",
                            $"reset attempt step references missing scenario {step.TargetScenarioId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.EmitScenarioEvent:
                    if (!step.ScenarioEventId.HasValue || step.ScenarioEventId.Value == 0)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "scenario event steps require scenario_event_id.",
                            diagnostics);
                    }

                    return;

                case MissionScenarioStepKind.TransferPlayer:
                    if (!step.MapContextId.HasValue ||
                        !step.PosX.HasValue ||
                        !step.PosY.HasValue ||
                        !step.PosZ.HasValue ||
                        !step.Orientation.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "transfer steps require map_context_id, pos_x, pos_y, pos_z, and orientation.",
                            diagnostics);
                        return;
                    }

                    if (!references.MapContextIds.Contains(step.MapContextId.Value))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "missing-map-context",
                            $"transfer step references missing map_context {step.MapContextId.Value}.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.SetQualification:
                    if (!step.QualificationKey.HasValue || !step.QualificationValue.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "qualification steps require qualification_key and qualification_value.",
                            diagnostics);
                        return;
                    }

                    if (!step.TryGetQualificationKey(out _))
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-qualification",
                            "qualification step requires a defined CharacterQualificationKey.",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                        return;
                    }

                    if (step.QualificationValue.Value != MissionScenarioStepEntry.RemovedQualificationValue &&
                        step.QualificationValue.Value != MissionScenarioStepEntry.GrantedQualificationValue)
                    {
                        diagnostics.Add(new MissionValidationDiagnostic(
                            "invalid-qualification",
                            $"qualification step requires qualification_value {MissionScenarioStepEntry.RemovedQualificationValue} (remove) or {MissionScenarioStepEntry.GrantedQualificationValue} (set).",
                            definition.MissionId,
                            definition.ContentRevision,
                            scenarioId: scenarioId,
                            stepId: step.StepId));
                    }

                    return;

                case MissionScenarioStepKind.SetAccountSkipEntitlement:
                    if (!step.AccountSkipEntitlement.HasValue)
                    {
                        AddInvalidScenarioStepShape(
                            definition,
                            scenarioId,
                            step,
                            "account skip entitlement steps require account_skip_entitlement.",
                            diagnostics);
                    }

                    return;
            }
        }

        private static bool HasValidDelay(uint delayMilliseconds) =>
            delayMilliseconds >= 1 && delayMilliseconds <= MaxDelayMilliseconds;

        private static void AddInvalidScenarioStepShape(
            MissionContentDefinition definition,
            uint scenarioId,
            MissionScenarioStepDefinition step,
            string message,
            ICollection<MissionValidationDiagnostic> diagnostics)
        {
            (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).Add(
                new MissionValidationDiagnostic(
                    "invalid-scenario-step-shape",
                    message,
                    definition.MissionId,
                    definition.ContentRevision,
                    scenarioId: scenarioId,
                    stepId: step.StepId));
        }

        private sealed class MissionContentReferenceSet
        {
            public HashSet<uint> NpcPackageIds { get; }
            public IReadOnlyDictionary<uint, uint> ItemTemplateClasses { get; }
            public HashSet<uint> EntityClassIds { get; }
            public IReadOnlyDictionary<uint, uint> CreatureClasses { get; }
            public HashSet<uint> MapContextIds { get; }

            public MissionContentReferenceSet(
                IEnumerable<uint> npcPackageIds,
                IReadOnlyDictionary<uint, uint> itemTemplateClasses,
                IEnumerable<uint> entityClassIds,
                IReadOnlyDictionary<uint, uint> creatureClasses,
                IEnumerable<uint> mapContextIds)
            {
                NpcPackageIds = new HashSet<uint>(npcPackageIds ?? Array.Empty<uint>());
                ItemTemplateClasses = itemTemplateClasses ??
                    new Dictionary<uint, uint>();
                EntityClassIds = new HashSet<uint>(entityClassIds ?? Array.Empty<uint>());
                CreatureClasses = creatureClasses ?? new Dictionary<uint, uint>();
                MapContextIds = new HashSet<uint>(mapContextIds ?? Array.Empty<uint>());
            }
        }
    }
}
