using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Game.Missions.Integration
{
    using Data;
    using Managers;
    using Repositories.Char;
    using Structures;
    using World;

    internal sealed class MissionInteractionPolicy
    {
        // Native body.InRadiusOf uses 5. The server has origins, not native body bounds or LOS.
        internal const float MaxConversationRange = 5;
        private readonly MissionApplication _missions;

        internal MissionInteractionPolicy(MissionApplication missions) => _missions = missions;

        internal static bool IsActivePlayer(Client client)
        {
            var player = client?.Player;
            var map = player?.MapChannel;
            return client?.State == ClientState.Ingame && client.PendingTransfer == null &&
                client.AccountEntry != null && player?.Id > 0 && map != null &&
                !player.Disconected && !player.RemoveFromMap && !player.LogoutActive &&
                player.State is not (CharacterState.Dead or CharacterState.Dying) &&
                player.MapContextId == map.MapInfo.MapContextId &&
                ReferenceEquals(player.RuntimeMapChannel, map) &&
                (!map.IsPrivateInstance || map.OwnerCharacterId == player.Id) &&
                EntityManager.Instance.GetEntityType(player.EntityId) == EntityType.Character &&
                EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registered) &&
                ReferenceEquals(registered, player) && CellManager.Instance.IsInWorld(client);
        }

        internal static bool InRange(Vector3 player, Vector3 target)
        {
            if (!Finite(player) || !Finite(target))
                return false;
            var x = (double)player.X - target.X;
            var y = (double)player.Y - target.Y;
            var z = (double)player.Z - target.Z;
            return x * x + y * y + z * z <= MaxConversationRange * MaxConversationRange;
        }

        private static bool Finite(Vector3 position) =>
            float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);

        internal static bool TryResolveTarget(Client client, ulong entityId, out MissionConversationTarget target)
        {
            target = null;
            if (!IsActivePlayer(client))
                return false;
            var player = client.Player;
            var map = player.MapChannel;
            if (MapInstanceScope.TryGetCreature(map, entityId, out var npc) &&
                ReferenceEquals(npc.RuntimeMapChannel, map) && npc.MapContextId == map.MapInfo.MapContextId &&
                npc.EntityId == entityId && npc.Npc != null && npc.IsInteractable &&
                npc.State is not (CharacterState.Dead or CharacterState.Dying) &&
                (npc.MasterEntityId == 0 || npc.MasterEntityId == player.EntityId) &&
                (npc.SpawnPool?.ScenarioOwnerCharacterId is null or 0 ||
                    npc.SpawnPool.ScenarioOwnerCharacterId == player.Id) &&
                InRange(player.Position, npc.Position))
                target = new MissionConversationTarget(npc);
            else if (EntityManager.Instance.GetEntityType(entityId) == EntityType.Object &&
                EntityManager.Instance.TryGetObject(entityId, out var obj) && obj.EntityId == entityId &&
                ReferenceEquals(obj.RuntimeMapChannel, map) && obj.MapContextId == map.MapInfo.MapContextId &&
                obj.IsInWorld && obj.IsEnabled && obj.MissionConversation != null &&
                EntityClassManager.Instance.GetClassInfo(obj.EntityClassId)?.Augmentations?.Contains(AugmentationType.NPC) == true &&
                (obj.SceneOwnerCharacterId == 0 || obj.SceneOwnerCharacterId == player.Id) &&
                InRange(player.Position, obj.Position))
                target = new MissionConversationTarget(obj);
            return target != null;
        }

        internal bool TryResolveOpeningTarget(Client client, ulong entityId, out MissionConversationTarget target)
        {
            if (!TryResolveTarget(client, entityId, out target))
                return false;
            if (target.Creature is { SpawnPool: { } pool } creature && !client.Player.MapChannel.IsPrivateInstance)
                target = new MissionConversationTarget(creature,
                    _missions.PublicActors.Handle(client.Player.MapChannel, pool.DbId));
            return true;
        }

        internal bool CanOpen(Client client, MissionConversationTarget target, ICharUnitOfWork unit) =>
            TryResolveTarget(client, target.EntityId, out var current) && target.Matches(current) &&
            unit.Characters.Get(client.Player.Id)?.AccountId == client.AccountEntry.Id &&
            SourceIsEligible(client, target, unit) &&
            TryResolveTarget(client, target.EntityId, out current) && target.Matches(current);

        internal bool TryCaptureTopic(Client client, MissionConversationTopicKey key, ICharUnitOfWork unit,
            out MissionConversationTopic topic, uint? progressionObjectiveId = null)
        {
            topic = null;
            if (!_missions.TryGetOperationalMission(key.MissionId, out var definition))
                return false;
            var assignment = unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, key.MissionId);
            if (key.Kind == MissionConversationTopicKind.Acceptance)
            {
                if (!_missions.CanAdmit(client.Player, definition, unit, out _))
                    return false;
            }
            else if (assignment == null || string.IsNullOrWhiteSpace(assignment.AssignmentId) ||
                assignment.Generation == 0 ||
                !IsCompatibleAssignmentRevision(assignment.ContentRevision, definition.ContentRevision) ||
                !client.Player.Missions.TryGetValue(key.MissionId, out var log) ||
                !log.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision) ||
                (uint)log.State != assignment.MissionState)
                return false;
            topic = new MissionConversationTopic(key, definition.ContentRevision,
                assignment?.AssignmentId, assignment?.ContentRevision, assignment?.Generation ?? 0,
                assignment == null ? null : (MissionState)assignment.MissionState,
                progressionObjectiveId ?? key.ObjectiveId,
                AdmissionHistoryId: key.Kind == MissionConversationTopicKind.Acceptance
                    ? unit.CharacterMissions.Runtime.LatestTerminal(client.Player.Id, key.MissionId)?.AssignmentId : null);
            return true;
        }

        internal bool TryGetTopic(Client client, ulong entityId, MissionConversationTopicKey key,
            out MissionConversationSession session, out MissionConversationTopic topic)
        {
            session = client?.MissionConversation;
            topic = null;
            if (!IsCurrent(client, session))
            {
                if (client != null)
                    client.MissionConversation = null;
                return false;
            }
            if (session.Target.EntityId != entityId)
                return false;
            topic = session.Topics.FirstOrDefault(candidate => candidate.Key == key);
            // Success saves historically use the completion presentation with the reward callback.
            if (topic == null && key.Kind == MissionConversationTopicKind.LegacyReward)
                topic = session.Topics.FirstOrDefault(candidate =>
                    candidate.Key == key with { Kind = MissionConversationTopicKind.MissionCompletion } &&
                    candidate.AssignmentState == MissionState.Success);
            if (topic == null)
                return false;
            return _missions.TryGetOperationalMission(key.MissionId, out var definition) &&
                definition.ContentRevision == topic.ContentRevision || Invalidate(client);
        }

        internal bool ValidateDurable(Client client, MissionConversationSession session,
            MissionConversationTopic topic, ICharUnitOfWork unit,
            PublicActorLeaseService.Reservation preparedAdmission = null, bool persisted = false)
        {
            if (!ValidateSession(client, session, topic) ||
                unit.Characters.Get(session.CharacterId)?.AccountId != session.AccountId ||
                !SourceIsEligible(client, session.Target, unit, preparedAdmission, persisted))
                return Invalidate(client);
            var assignment = persisted ? unit.CharacterMissions.Runtime.ReadAssignment(session.CharacterId, topic.Key.MissionId) :
                unit.CharacterMissions.GetByCharacterAndMission(session.CharacterId, topic.Key.MissionId);
            var matches = assignment == null
                ? topic.AssignmentId == null
                : assignment.AssignmentId == topic.AssignmentId &&
                    assignment.ContentRevision == topic.AssignmentRevision && assignment.Generation == topic.Generation;
            if (topic.Key.Kind == MissionConversationTopicKind.Acceptance)
                matches &= (assignment == null || (MissionState)assignment.MissionState == topic.AssignmentState) &&
                    unit.CharacterMissions.Runtime.LatestTerminal(session.CharacterId, topic.Key.MissionId)?.AssignmentId ==
                        topic.AdmissionHistoryId;
            return matches && ValidateSession(client, session, topic) || Invalidate(client);
        }

        internal bool ValidateSession(Client client, MissionConversationSession session, MissionConversationTopic topic) =>
            ReferenceEquals(client.MissionConversation, session) && IsCurrent(client, session) &&
            session.Topics.Contains(topic) &&
            _missions.TryGetOperationalMission(topic.Key.MissionId, out var definition) &&
            definition.ContentRevision == topic.ContentRevision || Invalidate(client);

        internal bool ValidateAfterPlanning(Client client, MissionConversationSession session,
            MissionConversationTopic topic, ICharUnitOfWork unit,
            PublicActorLeaseService.Reservation preparedAdmission = null) =>
            topic.Key.Kind != MissionConversationTopicKind.Acceptance
                ? ValidateDurable(client, session, topic, unit, persisted: true)
                : ValidateSession(client, session, topic) &&
                    unit.Characters.Get(session.CharacterId)?.AccountId == session.AccountId &&
                    SourceIsEligible(client, session.Target, unit, preparedAdmission, persisted: true) &&
                    ValidateSession(client, session, topic) || Invalidate(client);

        private static bool Invalidate(Client client)
        {
            client.MissionConversation = null;
            return false;
        }

        internal static bool IsCurrent(Client client, MissionConversationSession session) =>
            session != null && ReferenceEquals(client?.Player, session.Player) &&
            client.Player.Id == session.CharacterId && client.Player.EntityId == session.PlayerEntityId &&
            client.AccountEntry?.Id == session.AccountId &&
            ReferenceEquals(client.Player.MapChannel, session.Map) && session.Map.MissionEpoch == session.MapEpoch &&
            TryResolveTarget(client, session.Target.EntityId, out var target) && session.Target.Matches(target);

        internal static void InvalidateIfUnavailable(Client client)
        {
            if (client.MissionConversation is { } session && !IsCurrent(client, session))
                client.MissionConversation = null;
        }

        internal static void InvalidateTarget(MapChannel map, ulong entityId)
        {
            if (map?.ClientList == null)
                return;
            foreach (var client in map.ClientList.ToArray())
                if (client?.MissionConversation is { } session &&
                    ReferenceEquals(session.Map, map) && session.Target.EntityId == entityId)
                    client.MissionConversation = null;
        }

        private bool SourceIsEligible(Client client, MissionConversationTarget target, ICharUnitOfWork unit,
            PublicActorLeaseService.Reservation preparedAdmission = null, bool persisted = false)
        {
            var map = client.Player.MapChannel;
            var store = unit.CharacterMissions.Runtime;
            var runId = target.SceneRunId;
            var generation = target.SceneGeneration;
            if (!map.IsPrivateInstance && target.SpawnPool is { DbId: > 0 } pool)
            {
                var handle = _missions.PublicActors.Handle(map, pool.DbId);
                var lease = persisted ? store.ReadLease(PublicActorLeaseService.MapKey(map), $"spawn:{pool.DbId}") :
                    store.Lease(PublicActorLeaseService.MapKey(map), $"spawn:{pool.DbId}");
                var preparingSource = preparedAdmission != null && !preparedAdmission.Committed &&
                    preparedAdmission.OwnerCharacterId == client.Player.Id && preparedAdmission.Handle == handle &&
                    ReferenceEquals(preparedAdmission.Actor, target.Creature) && preparedAdmission.IsCurrent() &&
                    (lease == null ? !persisted :
                        lease.State == "Reserved" && lease.RunId == handle.RunId && lease.Generation == handle.Generation);
                if (preparingSource && lease != null)
                {
                    runId = handle.RunId;
                    generation = handle.Generation;
                }
                if (!preparingSource && handle != target.LeaseHandle)
                    return false;
                if (!preparingSource && (lease != null || handle != null))
                {
                    if (lease?.State != "Reserved" || handle == null || lease.RunId != handle.RunId ||
                        lease.Generation != handle.Generation ||
                        !_missions.PublicActors.TryResolve(map, handle, out var actor) ||
                        !ReferenceEquals(actor, target.Creature))
                        return false;
                    runId = handle.RunId;
                    generation = handle.Generation;
                }
                else if (!preparingSource && _missions.PublicActors.IsReserved(map, pool.DbId))
                    return false;
            }
            if (runId == null)
                return true;
            var scene = persisted ? store.ReadScene(runId) : store.Scene(runId);
            if (scene == null || scene.Generation != generation || scene.Status != "Running" ||
                scene.MapKey != PublicActorLeaseService.MapKey(map))
                return false;
            if (scene.MissionId == 0)
                return map.IsPrivateInstance && scene.OwnerCharacterId == client.Player.Id;
            if (!_missions.TryGetOperationalMission(scene.MissionId, out var definition) ||
                scene.Release != definition.ContentRevision)
                return false;
            var participant = persisted ? store.ReadParticipant(runId, client.Player.Id) :
                store.Participants(runId).SingleOrDefault(entry => entry.CharacterId == client.Player.Id);
            var assignment = persisted ? store.ReadAssignment(client.Player.Id, scene.MissionId) :
                unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, scene.MissionId);
            return participant?.Active == true && assignment != null &&
                participant.AssignmentId == assignment.AssignmentId &&
                participant.AssignmentGeneration == assignment.Generation &&
                IsCompatibleAssignmentRevision(assignment.ContentRevision, definition.ContentRevision);
        }

        private static bool IsCompatibleAssignmentRevision(string storedRevision, string currentRevision) =>
            storedRevision is "legacy" or "unversioned" || storedRevision == currentRevision;
    }
}
