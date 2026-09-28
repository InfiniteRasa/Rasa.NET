using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Missions.Definitions
{
    internal static class MissionDialogueValidation
    {
        internal static IEnumerable<string> Errors(Mission mission)
        {
            var keys = new HashSet<(MissionDialogueKind Kind, uint Objective, uint Package, uint Flag)>();
            foreach (var objective in mission.Objectives.Values)
                foreach (var conversation in objective.Conversations.Where(conversation =>
                    conversation.Type is MissionObjectiveConversationType.Completion or MissionObjectiveConversationType.Reminder))
                    if (!mission.Dialogue.Any(topic => topic != null && topic.ObjectiveId == objective.ObjectiveId &&
                        topic.NpcPackageId == conversation.NpcPackageId && topic.PlayerFlagId == conversation.PlayerFlagId))
                        keys.Add((conversation.Type == MissionObjectiveConversationType.Completion
                            ? MissionDialogueKind.Completion : MissionDialogueKind.Ambient,
                            objective.ObjectiveId, conversation.NpcPackageId, conversation.PlayerFlagId));
            foreach (var topic in mission.Dialogue)
            {
                if (topic == null)
                {
                    yield return "dialogue topic is null";
                    continue;
                }
                var dialogId = topic.DialogObjectiveId ?? topic.ObjectiveId;
                var prefix = $"dialogue objective {topic.ObjectiveId}, package {topic.NpcPackageId}";
                if (!Enum.IsDefined(typeof(MissionDialogueKind), topic.Kind) ||
                    !ValidId(mission.MissionId) || !ValidId(dialogId) || !ValidId(topic.NpcPackageId) ||
                    topic.PlayerFlagId > int.MaxValue ||
                    !mission.Objectives.TryGetValue(topic.ObjectiveId, out var objective))
                {
                    yield return $"{prefix}: invalid native topic binding";
                    continue;
                }
                if (!keys.Add((topic.Kind, dialogId, topic.NpcPackageId, topic.PlayerFlagId)))
                    yield return $"{prefix}: duplicate native topic binding";

                if (topic.Kind is MissionDialogueKind.Reminder or MissionDialogueKind.Ambient)
                {
                    if (topic.TransitionId.HasValue || topic.Choices != null)
                        yield return $"{prefix}: read-only dialogue cannot bind transitions";
                    continue;
                }
                if (topic.Kind == MissionDialogueKind.Choice)
                {
                    if (topic.TransitionId.HasValue || topic.Choices == null || topic.Choices.Count != 3 ||
                        !Enumerable.Range(1, 3).All(topic.Choices.ContainsKey))
                    {
                        yield return $"{prefix}: choice dialogue must map native indices 1, 2 and 3";
                        continue;
                    }
                }
                else if (!topic.TransitionId.HasValue || topic.Choices != null)
                {
                    yield return $"{prefix}: completion dialogue requires one transition";
                    continue;
                }
                foreach (var id in topic.Kind == MissionDialogueKind.Choice
                    ? topic.Choices.Values.Distinct() : new[] { topic.TransitionId.Value })
                {
                    var transitions = objective.GetExecutableTransitionsOrLegacyDefault()
                        .Where(transition => transition.TransitionId == id).ToArray();
                    if (transitions.Length != 1 || !CanSelect(transitions[0], topic))
                        yield return $"{prefix}: transition {id} is not an executable dialogue branch for this source";
                }
            }
        }

        internal static bool CanSelect(MissionObjectiveExecutableTransition transition, MissionDialogueTopicDefinition topic) =>
            transition.ProgressRule == null &&
            transition.ToState is null or MissionObjectiveState.Completed or MissionObjectiveState.Failed &&
            transition.Conversations.Any(conversation => conversation.NpcPackageId == topic.NpcPackageId &&
                conversation.PlayerFlagId == topic.PlayerFlagId &&
                conversation.Type is MissionObjectiveConversationType.Completion or MissionObjectiveConversationType.Choice1 or
                    MissionObjectiveConversationType.Choice2 or MissionObjectiveConversationType.Choice3) &&
            transition.Actions.All(action => Enum.IsDefined(typeof(MissionActionKind), action.Kind));

        private static bool ValidId(uint id) => id > 0 && id <= int.MaxValue;
    }
}
