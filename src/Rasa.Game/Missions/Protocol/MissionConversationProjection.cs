using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures;

namespace Rasa.Game.Missions.Protocol
{
    internal sealed record MissionDialoguePresentation(
        MissionConversationTopicKey Key, uint ProgressionObjectiveId, MissionDialogueTopicDefinition Definition);

    internal static class MissionConversationProjection
    {
        internal static IEnumerable<MissionDialoguePresentation> ForNpc(Mission mission, uint packageId)
        {
            foreach (var topic in mission.Dialogue.Where(topic => topic.NpcPackageId == packageId))
                yield return Project(mission.MissionId, topic);
            foreach (var objective in mission.Objectives.Values)
                foreach (var conversation in objective.Conversations.Where(conversation => conversation.NpcPackageId == packageId)
                    .DistinctBy(conversation => (conversation.Type, conversation.PlayerFlagId)))
                {
                    if (mission.Dialogue.Any(topic => topic.ObjectiveId == objective.ObjectiveId &&
                        topic.NpcPackageId == packageId && topic.PlayerFlagId == conversation.PlayerFlagId))
                        continue;
                    if (conversation.Type == MissionObjectiveConversationType.Completion)
                    {
                        var transition = objective.GetExecutableTransitionsOrLegacyDefault()
                            .FirstOrDefault(candidate => candidate.Conversations.Any(binding =>
                                binding.Type == MissionObjectiveConversationType.Completion &&
                                binding.NpcPackageId == packageId && binding.PlayerFlagId == conversation.PlayerFlagId));
                        if (transition != null)
                            yield return Project(mission.MissionId, new(objective.ObjectiveId, packageId,
                                conversation.PlayerFlagId, transitionId: transition.TransitionId));
                    }
                    else if (conversation.Type == MissionObjectiveConversationType.Reminder)
                        yield return Project(mission.MissionId, new(objective.ObjectiveId, packageId,
                            conversation.PlayerFlagId, MissionDialogueKind.Ambient));
                }
        }

        internal static MissionDialoguePresentation ForObject(Mission mission, SceneObjectConversation binding)
        {
            var topic = mission.Dialogue.SingleOrDefault(topic => topic.ObjectiveId == binding.ObjectiveId &&
                topic.NpcPackageId == binding.NpcPackageId && topic.PlayerFlagId == binding.PlayerFlagId &&
                (topic.DialogObjectiveId ?? topic.ObjectiveId) == binding.DialogObjectiveId && topic.Kind == binding.Kind);
            if (topic != null)
                return Project(mission.MissionId, topic);
            if (binding.Kind != MissionDialogueKind.Completion ||
                mission.Dialogue.Any(candidate => candidate.ObjectiveId == binding.ObjectiveId &&
                    candidate.NpcPackageId == binding.NpcPackageId && candidate.PlayerFlagId == binding.PlayerFlagId))
                return null;
            return Project(mission.MissionId, new(binding.ObjectiveId, binding.NpcPackageId, binding.PlayerFlagId,
                dialogObjectiveId: binding.DialogObjectiveId));
        }

        private static MissionDialoguePresentation Project(uint missionId, MissionDialogueTopicDefinition topic)
        {
            var kind = topic.Kind switch
            {
                MissionDialogueKind.Completion => MissionConversationTopicKind.ObjectiveCompletion,
                MissionDialogueKind.Reminder => MissionConversationTopicKind.MissionReminder,
                MissionDialogueKind.Ambient => MissionConversationTopicKind.ObjectiveAmbient,
                MissionDialogueKind.Choice => MissionConversationTopicKind.ObjectiveChoice,
                _ => throw new InvalidOperationException($"Unknown authored dialogue kind {topic.Kind}.")
            };
            return new(new(kind, missionId, topic.Kind == MissionDialogueKind.Reminder ? 0 :
                topic.DialogObjectiveId ?? topic.ObjectiveId, topic.Kind == MissionDialogueKind.Reminder ? 0 : topic.PlayerFlagId),
                topic.ObjectiveId, topic);
        }

        internal static void AddPayloads(Dictionary<ConversationType, object> data, IEnumerable<MissionDialoguePresentation> topics)
        {
            foreach (var group in topics.GroupBy(topic => topic.Key.Kind))
            {
                var keys = group.Select(topic => topic.Key).Distinct().ToArray();
                switch (group.Key)
                {
                    case MissionConversationTopicKind.ObjectiveCompletion:
                        data.Add(ConversationType.ObjectiveComplete, keys.Select(key =>
                            new CompleteableObjectives((int)key.MissionId, (int)key.ObjectiveId, (int)key.PlayerFlagId)).ToList());
                        break;
                    case MissionConversationTopicKind.MissionReminder:
                        data.Add(ConversationType.MissionReminder, keys.Select(key => key.MissionId).Distinct().ToList());
                        break;
                    case MissionConversationTopicKind.ObjectiveAmbient:
                        data.Add(ConversationType.ObjectiveAmbient, keys.Select(key =>
                            new AmbientObjectives((int)key.MissionId, (int)key.ObjectiveId, (int)key.PlayerFlagId)).ToList());
                        break;
                    case MissionConversationTopicKind.ObjectiveChoice:
                        data.Add(ConversationType.ObjectiveChoice, keys.Select(key =>
                            new ChoiceObjectives((int)key.MissionId, (int)key.ObjectiveId, (int)key.PlayerFlagId)).ToList());
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported projected dialogue topic {group.Key}.");
                }
            }
        }
    }
}
