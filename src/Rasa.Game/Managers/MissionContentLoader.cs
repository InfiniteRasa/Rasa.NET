using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Missions.Definitions;

namespace Rasa.Managers
{
    using Data;
    using Repositories.World;
    using Structures;
    using Structures.Missions;
    using Structures.World;

    internal sealed class MissionContentLoader
    {
        internal MissionContentSnapshot Load(IMissionContentRepository repository,
            IReadOnlyDictionary<uint, string> selectedRevisions = null)
        {
            if (repository == null)
                return new MissionContentSnapshot(
                    new Dictionary<uint, MissionContentDefinition>(),
                    new Dictionary<uint, IReadOnlyList<string>>());

            var definitions = repository.GetDefinitions() ?? new List<MissionContentDefinitionEntry>();
            var repeatPolicies = (repository.GetRepeatPolicies() ?? new List<MissionRepeatPolicyEntry>())
                .ToDictionary(entry => (entry.MissionId, entry.ContentRevision));
            var channelPolicies = (repository.GetChannelPolicies() ?? new List<MissionChannelPolicyEntry>())
                .ToDictionary(entry => (entry.MissionId, entry.ContentRevision));
            if (selectedRevisions != null)
            {
                definitions = definitions.Where(entry => selectedRevisions.TryGetValue(entry.MissionId, out var revision) &&
                    entry.ContentRevision == revision).ToList();
                foreach (var member in selectedRevisions)
                    if (!definitions.Any(entry => entry.MissionId == member.Key))
                        throw new InvalidOperationException(
                            $"Selected mission {member.Key} revision {member.Value} is unavailable; drain/reset or migrate explicitly.");
            }
            var objectives = repository.GetObjectives() ?? new List<MissionObjectiveDefinitionEntry>();
            var prerequisites = repository.GetPrerequisites() ?? new List<MissionPrerequisiteEntry>();
            var transitions = repository.GetTransitions() ?? new List<MissionObjectiveTransitionEntry>();
            var triggers = repository.GetTriggers() ?? new List<MissionTriggerEntry>();
            var actions = repository.GetActions() ?? new List<MissionActionEntry>();
            var rewards = repository.GetRewards() ?? new List<MissionRewardDefinitionEntry>();
            var rewardItems = repository.GetRewardItems() ?? new List<MissionRewardItemEntry>();
            var indicators = repository.GetIndicators() ?? new List<MissionIndicatorEntry>();
            var areas = repository.GetAreas() ?? new List<MissionAreaEntry>();
            var spawnGroups = repository.GetSpawnGroups() ?? new List<MissionSpawnGroupEntry>();
            var spawns = repository.GetSpawns() ?? new List<MissionSpawnEntry>();
            var scenarios = repository.GetScenarios() ?? new List<MissionScenarioEntry>();
            var scenarioSteps = repository.GetScenarioSteps() ?? new List<MissionScenarioStepEntry>();

            var revisionsByMission = definitions
                .GroupBy(entry => entry.MissionId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group
                        .Select(entry => entry.ContentRevision)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray());

            var selectedDefinitions = new Dictionary<uint, MissionContentDefinition>();
            foreach (var missionGroup in definitions.GroupBy(entry => entry.MissionId).OrderBy(group => group.Key))
            {
                var selectedRevision = SelectRevision(missionGroup);
                var definition = missionGroup
                    .OrderBy(entry => entry.ContentRevision, StringComparer.Ordinal)
                    .First(entry => string.Equals(
                        entry.ContentRevision,
                        selectedRevision,
                        StringComparison.Ordinal));

                var missionObjectives = objectives
                    .Where(entry =>
                        entry.MissionId == definition.MissionId &&
                        string.Equals(entry.ContentRevision, selectedRevision, StringComparison.Ordinal))
                    .OrderBy(entry => entry.Ordinal)
                    .ThenBy(entry => entry.ObjectiveId)
                    .ToArray();
                var duplicateObjectiveIds = missionObjectives
                    .GroupBy(entry => entry.ObjectiveId)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .OrderBy(value => value)
                    .ToArray();
                var uniqueObjectives = missionObjectives
                    .GroupBy(entry => entry.ObjectiveId)
                    .ToDictionary(group => group.Key, group => group.First());

                var missionIndicators = indicators
                    .Where(entry =>
                        entry.MissionId == definition.MissionId &&
                        string.Equals(entry.ContentRevision, selectedRevision, StringComparison.Ordinal))
                    .GroupBy(entry => entry.ObjectiveId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderBy(entry => entry.IndicatorId)
                            .Select(entry => new MissionIndicator
                            {
                                IndicatorId = entry.IndicatorId,
                                Position = new System.Numerics.Vector3(
                                    (float)entry.PosX,
                                    (float)entry.PosY,
                                    (float)entry.PosZ),
                                Radius = entry.Radius,
                                Show3DEffect = entry.Show3DEffect
                            })
                            .ToArray());

                var transitionDefinitions = BuildTransitions(
                    definition.MissionId,
                    selectedRevision,
                    transitions,
                    triggers,
                    actions);
                var objectiveDiagnostics = new List<string>();
                var objectiveDefinitions = BuildObjectives(
                    uniqueObjectives,
                    missionIndicators,
                    transitionDefinitions,
                    objectiveDiagnostics);

                channelPolicies.TryGetValue((definition.MissionId, selectedRevision), out var channels);
                var mission = new Mission(
                    definition.MissionId,
                    definition.Comment,
                    NormalizeTextId(definition.ClientNameTextId),
                    definition.GiverId,
                    definition.ReceiverId,
                    definition.Level,
                    definition.GroupType,
                    definition.CategoryId,
                    definition.Shareable,
                    definition.RadioCompleteable,
                    objectiveDefinitions.Values.OrderBy(objective => objective.ObjectiveId).ToArray(),
                    enableOperational: true,
                    operationalDiagnostic: objectiveDiagnostics.Count == 0
                        ? null
                        : string.Join("; ", objectiveDiagnostics),
                    contentRevision: selectedRevision,
                    repeatPolicy: repeatPolicies.TryGetValue((definition.MissionId, selectedRevision), out var repeat)
                        ? new Rasa.Missions.Runtime.MissionRepeatPolicy(repeat.Kind, repeat.CooldownSeconds, repeat.ResetSecondUtc)
                        : null,
                    acceptanceChannel: channels?.AcceptanceChannel ?? MissionChannel.Npc,
                    completionChannel: channels?.CompletionChannel ?? MissionChannel.Npc,
                    radioSources: channels == null ? null :
                        JsonSerializer.Deserialize<MissionOfferSourceDefinition[]>(channels.RadioSources));

                selectedDefinitions.Add(
                    definition.MissionId,
                    new MissionContentDefinition(
                        definition.MissionId,
                        selectedRevision,
                        definition.Requirement,
                        mission,
                        duplicateObjectiveIds,
                        prerequisites
                            .Where(entry =>
                                entry.MissionId == definition.MissionId &&
                                string.Equals(entry.ContentRevision, selectedRevision, StringComparison.Ordinal))
                            .OrderBy(entry => entry.PrerequisiteId)
                            .Select(entry => new MissionPrerequisiteDefinition(entry))
                            .ToArray(),
                        transitionDefinitions,
                        BuildRewards(definition.MissionId, selectedRevision, rewards, rewardItems),
                        areas
                            .Where(entry =>
                                entry.MissionId == definition.MissionId &&
                                string.Equals(entry.ContentRevision, selectedRevision, StringComparison.Ordinal))
                            .GroupBy(entry => entry.AreaId)
                            .ToDictionary(group => group.Key, group => new MissionAreaDefinition(group.First())),
                        BuildSpawnGroups(definition.MissionId, selectedRevision, spawnGroups, spawns),
                        BuildScenarios(definition.MissionId, selectedRevision, scenarios, scenarioSteps),
                        definition.AbandonmentPolicy));
            }

            return new MissionContentSnapshot(selectedDefinitions, revisionsByMission);
        }

        private static string SelectRevision(
            IGrouping<uint, MissionContentDefinitionEntry> missionGroup)
        {
            var revisions = missionGroup
                .Select(entry => entry.ContentRevision)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var nonLegacy = revisions
                .Where(revision => !string.Equals(revision, "legacy", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (nonLegacy.Length > 0)
                return nonLegacy[0];
            return revisions[0];
        }

        private static IReadOnlyDictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition> BuildTransitions(
            uint missionId,
            string contentRevision,
            IEnumerable<MissionObjectiveTransitionEntry> transitions,
            IEnumerable<MissionTriggerEntry> triggers,
            IEnumerable<MissionActionEntry> actions)
        {
            var missionTriggers = triggers
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToLookup(entry => (entry.ObjectiveId, entry.TransitionId));
            var missionActions = actions
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToLookup(entry => (entry.ObjectiveId, entry.TransitionId));

            return transitions
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .GroupBy(entry => (entry.ObjectiveId, entry.TransitionId))
                .ToDictionary(
                    group => group.Key,
                    group => new MissionObjectiveTransitionDefinition(
                        group.First(),
                        missionTriggers[group.Key].Select(entry => new MissionTriggerDefinition(entry)).ToArray(),
                        missionActions[group.Key].Select(entry => new MissionActionDefinition
                        {
                            MissionId = entry.MissionId,
                            ContentRevision = entry.ContentRevision,
                            ObjectiveId = entry.ObjectiveId,
                            TransitionId = entry.TransitionId,
                            ActionId = entry.ActionId,
                            Requirement = entry.Requirement,
                            Kind = entry.Kind,
                            Sequence = entry.Sequence,
                            TargetObjectiveId = entry.TargetObjectiveId,
                            ObjectiveStateValue = entry.ObjectiveState,
                            RewardId = entry.RewardId,
                            SpawnGroupId = entry.SpawnGroupId,
                            ScenarioId = entry.ScenarioId,
                            IndicatorId = entry.IndicatorId,
                            PlayerFlagId = entry.PlayerFlagId,
                            PlayerFlagValue = entry.PlayerFlagValue,
                            NpcPackageId = entry.NpcPackageId,
                            ItemIntent = entry.ItemIntentJson == null ? null :
                                System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Scenes.CharacterIntent>(
                                    entry.ItemIntentJson, Rasa.Missions.Content.MissionContentCodec.Options),
                            Comment = entry.Comment
                        }).ToArray()));
        }

        private static IReadOnlyDictionary<uint, MissionObjectiveDefinition> BuildObjectives(
            IReadOnlyDictionary<uint, MissionObjectiveDefinitionEntry> objectives,
            IReadOnlyDictionary<uint, MissionIndicator[]> indicators,
            IReadOnlyDictionary<(uint ObjectiveId, uint TransitionId), MissionObjectiveTransitionDefinition> transitions,
            ICollection<string> diagnostics)
        {
            var objectiveDefinitions = new Dictionary<uint, MissionObjectiveDefinition>();
            var transitionsByObjective = transitions.Values.ToLookup(transition => transition.ObjectiveId);
            foreach (var objectiveEntry in objectives.Values.OrderBy(entry => entry.Ordinal).ThenBy(entry => entry.ObjectiveId))
            {
                var runtime = MissionObjectiveRuntimeAnalyzer.Analyze(
                    objectiveEntry.ObjectiveId,
                    transitionsByObjective[objectiveEntry.ObjectiveId].ToArray());
                foreach (var diagnostic in runtime.Diagnostics)
                    diagnostics?.Add(diagnostic.Message);

                objectiveDefinitions.Add(
                    objectiveEntry.ObjectiveId,
                    new MissionObjectiveDefinition(
                        objectiveEntry.ObjectiveId,
                        NormalizeTextId(objectiveEntry.ClientNameTextId),
                        NormalizeTextId(objectiveEntry.ClientBodyTextId),
                        new uint?[]
                        {
                            NormalizeTextId(objectiveEntry.ClientCounter0TextId ?? 0),
                            NormalizeTextId(objectiveEntry.ClientCounter1TextId ?? 0),
                            NormalizeTextId(objectiveEntry.ClientCounter2TextId ?? 0)
                        },
                        objectiveEntry.Ordinal,
                        ParseObjectiveState(objectiveEntry.InitialState),
                        objectiveEntry.IsRequired,
                        runtime.Counters,
                        runtime.ItemCounters,
                        runtime.Conversations,
                        runtime.RevealedObjectiveIds,
                        runtime.ActivatedObjectiveIds,
                        indicators.TryGetValue(objectiveEntry.ObjectiveId, out var indicatorList)
                            ? indicatorList
                            : Array.Empty<MissionIndicator>(),
                        runtime.ProgressRule,
                        runtime.ExecutableTransitions,
                        objectiveEntry.Requirement));
            }

            return objectiveDefinitions;
        }

        private static IReadOnlyDictionary<uint, MissionAuthoringRewardDefinition> BuildRewards(
            uint missionId,
            string contentRevision,
            IEnumerable<MissionRewardDefinitionEntry> rewards,
            IEnumerable<MissionRewardItemEntry> rewardItems)
        {
            var itemsLookup = rewardItems
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToLookup(entry => entry.RewardId);
            return rewards
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToDictionary(
                    entry => entry.RewardId,
                    entry => new MissionAuthoringRewardDefinition(
                        entry,
                        itemsLookup[entry.RewardId]
                            .Where(item => item.Kind == MissionRewardItemKind.Fixed)
                            .Select(item => new MissionAuthoringRewardItemDefinition(item))
                            .ToArray(),
                        itemsLookup[entry.RewardId]
                            .Where(item => item.Kind == MissionRewardItemKind.Selectable)
                            .Select(item => new MissionAuthoringRewardItemDefinition(item))
                            .ToArray()));
        }

        private static IReadOnlyDictionary<uint, MissionSpawnGroupDefinition> BuildSpawnGroups(
            uint missionId,
            string contentRevision,
            IEnumerable<MissionSpawnGroupEntry> spawnGroups,
            IEnumerable<MissionSpawnEntry> spawns)
        {
            var spawnLookup = spawns
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToLookup(entry => entry.SpawnGroupId);
            return spawnGroups
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToDictionary(
                    entry => entry.SpawnGroupId,
                    entry => new MissionSpawnGroupDefinition(
                        entry,
                        spawnLookup[entry.SpawnGroupId]
                            .Select(spawn => new MissionSpawnDefinition(spawn))
                            .ToArray()));
        }

        private static IReadOnlyDictionary<uint, MissionScenarioDefinition> BuildScenarios(
            uint missionId,
            string contentRevision,
            IEnumerable<MissionScenarioEntry> scenarios,
            IEnumerable<MissionScenarioStepEntry> scenarioSteps)
        {
            var stepLookup = scenarioSteps
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToLookup(entry => entry.ScenarioId);
            return scenarios
                .Where(entry =>
                    entry.MissionId == missionId &&
                    string.Equals(entry.ContentRevision, contentRevision, StringComparison.Ordinal))
                .ToDictionary(
                    entry => entry.ScenarioId,
                    entry => new MissionScenarioDefinition(
                        entry,
                        stepLookup[entry.ScenarioId]
                            .Select(step => new MissionScenarioStepDefinition(step))
                            .ToArray()));
        }

        private static uint? NormalizeTextId(uint value) =>
            value == 0 ? null : value;

        private static MissionObjectiveState? ParseObjectiveState(byte value) =>
            Enum.IsDefined(typeof(MissionObjectiveState), (int)value)
                ? (MissionObjectiveState)value
                : null;
    }
}
