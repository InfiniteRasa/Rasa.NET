using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Game.Missions
{
    using Data;
    using Protocol;
    using Managers;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;

    internal sealed class MissionObjectConversations
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly MissionApplication _missions;

        internal MissionObjectConversations(IGameUnitOfWorkFactory factory, MissionApplication missions)
        {
            _factory = factory;
            _missions = missions;
        }

        internal bool IsAvailable(Client client, DynamicObject obj) =>
            obj?.MissionConversation is { } binding && obj.IsEnabled &&
            (obj.SceneOwnerCharacterId == 0 || obj.SceneOwnerCharacterId == client?.Player?.Id) &&
            client?.Player?.Missions.TryGetValue(binding.MissionId, out var mission) == true &&
            mission.State == MissionState.Active &&
            mission.Objectives.TryGetValue(binding.ObjectiveId, out var objective) &&
            objective.State == MissionObjectiveState.Incomplete &&
            Presentation(obj) != null;

        internal NPCConversationStatusPacket Status(Client client, DynamicObject obj)
        {
            if (!IsAvailable(client, obj))
                return new(ConversationStatus.None, new List<uint>());
            var kind = Presentation(obj).Key.Kind;
            var status = kind switch
            {
                MissionConversationTopicKind.ObjectiveCompletion => ConversationStatus.ObjectivComplete,
                MissionConversationTopicKind.ObjectiveChoice => ConversationStatus.ObjectivChoice,
                MissionConversationTopicKind.ObjectiveAmbient => ConversationStatus.ObjectivAMB,
                MissionConversationTopicKind.MissionReminder => ConversationStatus.MissionReminder,
                _ => throw new InvalidOperationException($"Unsupported object dialogue topic {kind}.")
            };
            return new(status, new List<uint> { obj.MissionConversation.MissionId });
        }

        private MissionDialoguePresentation Presentation(DynamicObject obj) =>
            _missions.TryGetOperationalMission(obj.MissionConversation.MissionId, out var definition)
                ? MissionConversationProjection.ForObject(definition, obj.MissionConversation) : null;

        internal bool Open(Client client, ulong entityId)
        {
            if (client == null)
                return false;
            lock (client.SyncRoot)
            {
                client.MissionConversation = null;
                if (!_missions.Interactions.TryResolveOpeningTarget(client, entityId, out var target) ||
                    target.Object is not { } obj)
                    return Reject("Conversation object is unavailable, out of range or belongs to another character.");
                var binding = obj.MissionConversation;
                try
                {
                    using var unit = _factory.CreateChar();
                    if (!_missions.Interactions.CanOpen(client, target, unit))
                        return Reject("The conversation source is not available.");
                    _missions.RefreshConversationAssignments(client, unit, new[] { binding.MissionId });
                    if (!IsAvailable(client, obj))
                        return Reject("The conversation objective is not available.");
                    var presentation = Presentation(obj);
                    if (!_missions.Interactions.CanOpen(client, target, unit) || !DurableAvailable(client, obj, unit) ||
                        !_missions.Interactions.TryCaptureTopic(client, presentation.Key, unit, out var topic, binding.ObjectiveId) ||
                        !_missions.Interactions.CanOpen(client, target, unit))
                        return Reject("The conversation objective is not available.");
                    topic = topic with { Dialogue = presentation.Definition };
                    client.MissionConversation = new MissionConversationSession(client, target, new[] { topic });
                    var data = new Dictionary<ConversationType, object>();
                    MissionConversationProjection.AddPayloads(data, new[] { presentation });
                    client.CallMethod(obj.EntityId, new ConversePacket(data));
                    return true;
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error, $"Unable to open mission object conversation {entityId}: {error}");
                    return false;
                }
            }
        }

        internal bool Complete(Client client, ulong entityId, uint missionId, uint dialogObjectiveId, uint playerFlagId) =>
            _missions.TryCompleteNpcObjective(client, entityId, missionId, dialogObjectiveId, playerFlagId);

        private bool DurableAvailable(Client client, DynamicObject obj, ICharUnitOfWork unit)
        {
            var binding = obj.MissionConversation;
            return unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, binding.MissionId)?.MissionState ==
                    (uint)MissionState.Active &&
                unit.CharacterMissionProgress.GetTracked(client.Player.Id, binding.MissionId)
                    .TryGetValue(binding.ObjectiveId, out var objective) &&
                objective.ObjectiveState == (byte)MissionObjectiveState.Incomplete &&
                _missions.IsObjectiveEligibleAtEvent(client, binding.MissionId, binding.ObjectiveId, unit);
        }

        private static bool Reject(string reason)
        {
            Logger.WriteLog(LogType.Debug, $"Rejected mission object conversation: {reason}");
            return false;
        }
    }
}
