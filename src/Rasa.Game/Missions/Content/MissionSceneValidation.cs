using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Game.Missions.Content
{
    internal static class MissionSceneValidation
    {
        internal static bool SupportsSharedAssignment(Mission mission)
        {
            var dependencies = MissionContentValidator.BuildObjectiveGraph(mission.Objectives.Values)
                .SelectMany(entry => entry.Value
                    .Where(id => mission.Objectives.TryGetValue(id, out var target) &&
                        target.InitialState is Data.MissionObjectiveState.Inactive or Data.MissionObjectiveState.NotAssigned)
                    .Select(id => (ObjectiveId: id, DependencyId: entry.Key)))
                .Concat(mission.Objectives.Values.SelectMany(objective => objective.GetExecutableTransitionsOrLegacyDefault()
                    .Where(transition => transition.ProgressRule?.Kind == Data.MissionProgressEventKind.ObjectiveStateReached)
                    .SelectMany(transition => transition.ProgressRule.Subjects
                        .Select(id => (ObjectiveId: objective.ObjectiveId, DependencyId: id)))))
                .ToLookup(edge => edge.ObjectiveId, edge => edge.DependencyId);
            var requiredProgress = mission.Objectives.Values.Where(objective => objective.IsRequired == true)
                .Select(objective => objective.ObjectiveId).ToHashSet();
            var pending = new Stack<uint>(requiredProgress);
            // Alternate branches do not prove that an authored owner-only dependency is safe to join.
            while (pending.TryPop(out var objectiveId))
                foreach (var dependency in dependencies[objectiveId])
                    if (requiredProgress.Add(dependency))
                        pending.Push(dependency);

            return !mission.Objectives.Values.Any(objective => objective.GetExecutableTransitionsOrLegacyDefault()
                .Any(transition => transition.ProgressRule?.Kind == Data.MissionProgressEventKind.DeadlineElapsed ||
                    requiredProgress.Contains(objective.ObjectiveId) && objective.CreditPolicy.Mode == MissionCreditMode.Personal &&
                        transition.ProgressRule?.Kind == Data.MissionProgressEventKind.ScenarioEvent ||
                    transition.Actions.Any(action => action.Kind is MissionActionKind.StartScenario or MissionActionKind.ActivateSpawnGroup)));
        }

        internal static void ValidateSharing(Mission mission, MissionSceneDefinition scene)
        {
            if (mission.Shareable == true && scene.Script != null &&
                (scene.PublicEncounter?.AllowPartyJoin != true || !SupportsSharedAssignment(mission)))
                throw new MissionRuleException($"Mission {mission.MissionId}: scripted assignments must be nonshareable " +
                    "unless their public encounter supports party joining without assignment-owned scene clocks, " +
                    "world-command transitions or required owner-only scene events, including optional objective dependencies.");
        }

        internal static void ValidateDialogueActions(Mission mission, MissionSceneDefinition scene)
        {
            foreach (var topic in mission.Dialogue.Where(topic => topic.Kind is MissionDialogueKind.Completion or MissionDialogueKind.Choice))
            {
                var ids = topic.Kind == MissionDialogueKind.Choice ? topic.Choices.Values : new[] { topic.TransitionId.Value };
                foreach (var transition in mission.Objectives[topic.ObjectiveId].GetExecutableTransitionsOrLegacyDefault()
                    .Where(transition => ids.Contains(transition.TransitionId)))
                    foreach (var action in transition.Actions)
                    {
                        if (action.Kind == MissionActionKind.StartScenario &&
                            (scene.Script == null || !action.ScenarioId.HasValue || !scene.Sequences.ContainsKey(action.ScenarioId.Value)))
                            throw new MissionRuleException($"Mission {mission.MissionId}: dialogue transition {transition.TransitionId} has an unbound scene sequence.");
                        if (action.Kind == MissionActionKind.ActivateSpawnGroup &&
                            (scene.Script == null || !action.SpawnGroupId.HasValue ||
                                !scene.Names.TryGetValue($"spawn-group-{action.SpawnGroupId}", out var sequence) ||
                                !scene.Sequences.ContainsKey(sequence)))
                            throw new MissionRuleException($"Mission {mission.MissionId}: dialogue transition {transition.TransitionId} has an unbound spawn-group sequence.");
                    }
            }
        }

        internal static void Validate(uint missionId, string revision, MissionSceneDefinition scene, IEnumerable<uint> objectives)
        {
            var objectiveIds = objectives.ToHashSet();
            if (missionId == 0 && (scene.Items != null || scene.AcceptanceItems != null ||
                scene.Sequences.Values.SelectMany(sequence => sequence.Character).Any(MissionItemValidation.IsItemIntent)))
                throw new MissionRuleException("Assignment item operations require a mission-owned scene, not an experience.");
            if (scene.Items != null && (scene.Items.Any(item => MissionItemValidation.BindingError(item) != null) ||
                scene.Items.Select(item => item.ItemKey).Distinct(StringComparer.Ordinal).Count() != scene.Items.Count))
                throw new MissionRuleException($"Mission {missionId}: invalid or duplicate item bindings.");
            if (scene.Dialogue != null && (missionId == 0 || scene.Dialogue.Any(topic => topic == null ||
                !objectiveIds.Contains(topic.ObjectiveId))))
                throw new MissionRuleException($"Mission {missionId}: dialogue must name this mission's objectives.");
            if (scene.Audio is { } audio && (audio.Events == null || audio.Announcements == null ||
                audio.OfferAudioSetId is 0 or > int.MaxValue ||
                audio.Events.Any(entry => !Enum.IsDefined(typeof(MissionAudioEvent), entry.Key) ||
                    entry.Value == 0 || entry.Value > int.MaxValue) ||
                audio.Announcements.Any(entry => entry.Key == 0 || entry.Value == 0 || entry.Value > int.MaxValue)))
                throw new MissionRuleException($"Mission {missionId}: invalid audio cue binding.");
            new Integration.MissionRequirementService().Validate(missionId, objectiveIds,
                scene.Requirement, scene.TurnInRequirement, scene.ObjectiveRequirements);
            var scripts = new SceneScriptRegistry();
            if (scene.Script != null && !scripts.TryResolve(scene.Script, scene.StateVersion, out _))
                throw new MissionRuleException($"Mission {missionId}: migrated script {scene.Script}/{scene.StateVersion} is unavailable in this server.");
            foreach (var actor in scene.Actors)
            {
                if (actor.Value == null || string.IsNullOrWhiteSpace(actor.Key) ||
                    actor.Key != actor.Value.Role || actor.Value.TemplateId == 0 ||
                    !Enum.IsDefined(typeof(SceneActorKind), actor.Value.Kind) ||
                    actor.Value.Kind != SceneActorKind.PublicSpawn && actor.Value.Position == null ||
                    actor.Value.SharedKey != null && string.IsNullOrWhiteSpace(actor.Value.SharedKey) ||
                    !double.IsFinite(actor.Value.Orientation) ||
                    actor.Value.Position is { } position &&
                        (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z)))
                    throw new MissionRuleException($"Mission {missionId}: invalid actor role {actor.Key}.");
                if (actor.Value.GameplayPolicy is { } policy)
                {
                    if (actor.Value.Kind is not (SceneActorKind.Creature or SceneActorKind.PublicSpawn))
                        throw new MissionRuleException($"Mission {missionId}, actor {actor.Key}: gameplay policy requires a creature role.");
                    if (policy.ValidationError() is string error)
                        throw new MissionRuleException($"Mission {missionId}, actor {actor.Key}: {error}.");
                }
            }
            if (scene.Actors.Values.Where(actor => actor.SharedKey != null)
                .GroupBy(actor => actor.SharedKey, StringComparer.Ordinal).Any(group => group.Count() > 1))
                throw new MissionRuleException($"Mission {missionId}: a shared actor cannot have multiple roles in one scene.");
            foreach (var actor in scene.Actors.Values.Where(actor => actor.Conversation != null))
            {
                var conversation = actor.Conversation;
                if (actor.Kind != SceneActorKind.Object || conversation.MissionId is 0 or > int.MaxValue ||
                    conversation.ObjectiveId == 0 || conversation.NpcPackageId == 0 ||
                    conversation.DialogObjectiveId is 0 or > int.MaxValue || conversation.PlayerFlagId is 0 or > int.MaxValue ||
                    !Enum.IsDefined(typeof(MissionDialogueKind), conversation.Kind) ||
                    missionId != 0 && (conversation.MissionId != missionId || !objectiveIds.Contains(conversation.ObjectiveId)))
                    throw new MissionRuleException($"Mission {missionId}: invalid object conversation for {actor.Role}.");
            }
            if (scene.PublicEncounter is { } encounter &&
                (encounter.MissionId != missionId || encounter.ScriptKey != scene.Script ||
                 !scene.Actors.TryGetValue(encounter.Role, out var publicActor) ||
                 publicActor.Kind != SceneActorKind.PublicSpawn || publicActor.TemplateId != encounter.SpawnId ||
                 encounter.OwnerLossPolicy is not ("Reset" or "Wait" or "Continue")))
                throw new MissionRuleException($"Mission {missionId}: public encounter does not match its actor/script.");
            foreach (var route in scene.Routes)
                if (route.Key != route.Value.Key || route.Value.Points.Count == 0 ||
                    !float.IsFinite(route.Value.Speed) || route.Value.Speed <= 0 ||
                    route.Value.Points.Any(point => point.Position == null ||
                        !float.IsFinite(point.Position.X) || !float.IsFinite(point.Position.Y) ||
                        !float.IsFinite(point.Position.Z) || !double.IsFinite(point.Orientation)))
                    throw new MissionRuleException($"Mission {missionId}: invalid route {route.Key}.");
            if (scene.Credit.Keys.Any(id => !objectiveIds.Contains(id)))
                throw new MissionRuleException($"Mission {missionId}: credit policy names an unknown objective.");
            if (scene.DefeatSequences?.Any(entry =>
                !scene.Actors.TryGetValue(entry.Key, out var actor) ||
                actor.Kind is not (SceneActorKind.Creature or SceneActorKind.PublicSpawn) ||
                !scene.Sequences.ContainsKey(entry.Value)) == true)
                throw new MissionRuleException($"Mission {missionId}: invalid actor defeat sequence.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var runtime = new SceneRuntime(scripts);
            foreach (var sequence in scene.Sequences)
            {
                var run = new SceneRun("validation", revision, "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, 1, missionId);
                var result = runtime.Evaluate(run, scene.Bindings(revision),
                    new SceneObservation(SceneEventKind.Signal, 1, SequenceId: sequence.Key), DateTime.UnixEpoch);
                if (!result.Accepted)
                    throw new MissionRuleException($"Mission {missionId}, sequence {sequence.Key}: {result.Rejection}");
                foreach (var key in sequence.Value.World.Select(intent => intent.OperationKey)
                    .Concat(sequence.Value.Character.Select(intent => intent.OperationKey)))
                    if (!keys.Add(key))
                        throw new MissionRuleException($"Mission {missionId}: duplicate operation {key}.");
                foreach (var timer in sequence.Value.Timers)
                    if (!timer.Cancel && !scene.Sequences.ContainsKey(timer.SequenceId))
                        throw new MissionRuleException($"Mission {missionId}: timer {timer.Name} has no target sequence.");
            }
        }

        internal static void ValidateSharedActors(IEnumerable<MissionSceneDefinition> scenes)
        {
            foreach (var group in scenes.SelectMany(scene => scene.Actors.Values)
                .Where(actor => actor.SharedKey != null).GroupBy(actor => actor.SharedKey, StringComparer.Ordinal))
            {
                var first = group.First();
                if (group.Any(actor => actor.Kind != first.Kind || actor.TemplateId != first.TemplateId ||
                    (first.GameplayPolicy == null ? actor.GameplayPolicy != null :
                        !first.GameplayPolicy.EquivalentTo(actor.GameplayPolicy))))
                    throw new MissionRuleException($"Shared actor {group.Key}: kind, template and gameplay policy must agree.");
            }
        }
    }
}
