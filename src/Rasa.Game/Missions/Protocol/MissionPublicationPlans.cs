using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rasa.Missions.Runtime;
using ProgressCandidate = Rasa.Missions.Runtime.MissionProgressCandidate;
using System.Text.Json;
using Rasa.Game.Missions.Content;
using Rasa.Repositories.World;
using Rasa.Game.Missions.Persistence;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Packets.Mission.Server;
    using Repositories.Char;
    using Repositories.Char.CharacterMissionProgress;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.Missions;
    using Structures.World;

    internal static class MissionStatePublication
    {
        internal static MissionLog Capture(CharacterMissionEntry mission,
            IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> objectives) =>
            new(mission.MissionId, (MissionState)mission.MissionState, mission.Completeable,
                objectives.ToDictionary(entry => entry.Key, entry => new MissionObjectiveLog(
                    entry.Key, (MissionObjectiveState)entry.Value.ObjectiveState,
                    entry.Value.Counters.ToDictionary(counter => counter.CounterId, counter => counter.CounterValue),
                    entry.Value.ItemCounters.ToDictionary(counter => counter.ItemClassId, counter => counter.CounterValue))),
                mission.AssignmentId, mission.Generation, mission.ContentRevision);

        internal static bool Converge(Client client, IEnumerable<MissionLog> snapshots, bool apply = true)
        {
            var committed = snapshots.ToArray();
            foreach (var snapshot in committed)
                if (!client.Player.Missions.TryGetValue(snapshot.MissionId, out var current) ||
                    !snapshot.MatchesAssignment(current.AssignmentId, current.Generation, current.ContentRevision))
                {
                    Logger.WriteLog(LogType.Error, $"Discarded mission publication for retired assignment {snapshot.AssignmentId}.");
                    return false;
                }
            if (!apply)
                return true;

            foreach (var snapshot in committed.GroupBy(mission => mission.MissionId).Select(group => group.Last()))
            {
                var current = client.Player.Missions[snapshot.MissionId];
                foreach (var saved in snapshot.Objectives.Values)
                {
                    var objective = current.Objectives[saved.ObjectiveId];
                    objective.State = saved.State;
                    foreach (var counter in saved.Counters)
                        objective.SetCounter(counter.Key, counter.Value);
                    foreach (var counter in saved.ItemCounters)
                        objective.SetItemCounter(counter.Key, counter.Value);
                }
                if (current.State == MissionState.Active)
                {
                    current.State = snapshot.State;
                    current.Completeable = snapshot.Completeable;
                }
            }
            return true;
        }
    }

    internal sealed class MissionFailurePublicationPlan
    {
        internal static readonly MissionFailurePublicationPlan Empty =
            new(0, 0, MissionState.Active, false, false);

        private readonly uint _missionId;
        private readonly uint _objectiveId;
        private readonly MissionState _missionState;
        private readonly bool _completeable;
        private readonly bool _completeableChanged;
        private readonly bool _publishMissionStatus;
        private readonly Action<Client> _inventoryPublication;
        private readonly IReadOnlyDictionary<uint, uint> _flags;
        private readonly MissionLog _assignment;
        internal IReadOnlyDictionary<uint, uint> FlagSnapshot => _flags;
        internal uint MissionId => _missionId;
        internal bool ChangesFlags { get; }
        internal IReadOnlyList<uint> StartScenarioIds { get; }

        internal MissionFailurePublicationPlan(
            uint missionId,
            uint objectiveId,
            MissionState missionState,
            bool completeable,
            bool completeableChanged,
            IEnumerable<uint> startScenarioIds = null,
            bool publishMissionStatus = false,
            Action<Client> inventoryPublication = null,
            bool changesFlags = false,
            IReadOnlyDictionary<uint, uint> flags = null,
            MissionLog assignment = null)
        {
            _missionId = missionId;
            _objectiveId = objectiveId;
            _missionState = missionState;
            _completeable = completeable;
            _completeableChanged = completeableChanged;
            _publishMissionStatus = publishMissionStatus;
            _inventoryPublication = inventoryPublication;
            _flags = flags;
            _assignment = assignment;
            ChangesFlags = changesFlags;
            StartScenarioIds = Array.AsReadOnly(
                (startScenarioIds ?? Array.Empty<uint>()).ToArray());
        }

        internal void Publish(
            Client client,
            MissionApplication manager,
            Func<Client, uint, uint, bool> startScenario = null,
            Func<Client, uint, uint, bool> startFailureScenario = null,
            bool convergeFlags = true,
            bool convergeMission = true)
        {
            if (!client.Player.Missions.TryGetValue(_missionId, out var mission) ||
                !mission.Objectives.TryGetValue(_objectiveId, out var objective))
                return;
            if (_assignment != null && !_assignment.MatchesAssignment(
                mission.AssignmentId, mission.Generation, mission.ContentRevision))
            {
                Logger.WriteLog(LogType.Error, $"Discarded failure publication for retired assignment {_assignment.AssignmentId}.");
                return;
            }

            if (convergeMission)
            {
                objective.State = MissionObjectiveState.Failed;
                mission.State = _missionState;
                mission.Completeable = _completeable;
            }
            if (_missionState == MissionState.Failed)
                client.Player.MissionHistory[_missionId] = MissionState.Failed;
            if (convergeFlags && _flags != null)
            {
                client.FlagProjection.ApplyCommitted(client, _flags);
                manager.PublishCharacterFlags(client);
            }
            _inventoryPublication?.Invoke(client);

            manager.PublishMissionPacket(
                client,
                new ObjectiveFailedPacket(_missionId, _objectiveId),
                $"mission {_missionId} objective {_objectiveId} failed");
            if (_completeableChanged)
                manager.PublishMissionPacket(
                    client,
                    new MissionCompleteablePacket(_missionId, _completeable),
                    $"mission {_missionId} completable after objective failure");
            if (_missionState == MissionState.Failed)
                manager.PublishMissionPacket(
                    client,
                    new MissionFailedPacket(_missionId),
                    $"mission {_missionId} failed after objective failure");
            if (_publishMissionStatus)
                manager.PublishMissionStatus(
                    client,
                    _missionId,
                    $"mission {_missionId} status after deadline change");

            manager.PublishStartedScenarios(
                client,
                _missionId,
                StartScenarioIds,
                _missionState == MissionState.Failed
                    ? startFailureScenario
                    : startScenario);
        }
    }

    internal sealed class MissionProgressPublicationPlan
    {
        internal static readonly MissionProgressPublicationPlan Empty =
            new(
                Array.Empty<ProgressPublication>(),
                Array.Empty<MissionFailurePublicationPlan>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                null,
                null,
                null,
                null);

        private readonly ProgressPublication[] _publications;
        private readonly MissionFailurePublicationPlan[] _failurePlans;
        private readonly uint[] _completableMissions;
        private readonly uint[] _missionStatusMissionIds;
        private readonly Func<Client, uint, uint, bool> _startScenario;
        private readonly Func<Client, uint, uint, bool> _startFailureScenario;
        private readonly Func<Client, uint, uint, bool> _activateSpawnGroup;
        private readonly MissionApplication _manager;
        private readonly Action<Client> _inventoryPublication;
        private readonly IReadOnlyDictionary<uint, uint> _flags;
        private readonly IReadOnlyDictionary<uint, MissionLog> _committedMissions;
        internal IReadOnlyDictionary<uint, uint> FlagSnapshot => _flags;
        internal IReadOnlyDictionary<uint, MissionLog> CommittedMissions => _committedMissions;

        internal bool HasChanges => _publications.Length > 0 || _failurePlans.Length > 0;

        internal MissionProgressPublicationPlan(
            IEnumerable<ProgressPublication> publications,
            IEnumerable<MissionFailurePublicationPlan> failurePlans,
            IEnumerable<uint> completableMissions,
            IEnumerable<uint> missionStatusMissionIds,
            Func<Client, uint, uint, bool> startScenario,
            Func<Client, uint, uint, bool> startFailureScenario,
            MissionApplication manager,
            Func<Client, uint, uint, bool> activateSpawnGroup = null,
            Action<Client> inventoryPublication = null,
            IReadOnlyDictionary<uint, uint> flags = null,
            IReadOnlyDictionary<uint, MissionLog> committedMissions = null)
        {
            _publications = publications.ToArray();
            _failurePlans = failurePlans.ToArray();
            _completableMissions = completableMissions.ToArray();
            _missionStatusMissionIds = missionStatusMissionIds.ToArray();
            _startScenario = startScenario;
            _startFailureScenario = startFailureScenario;
            _activateSpawnGroup = activateSpawnGroup;
            _manager = manager;
            _inventoryPublication = inventoryPublication;
            _flags = flags;
            _committedMissions = committedMissions ?? new Dictionary<uint, MissionLog>();
        }

        internal void Publish(Client client, bool convergeFlags = true, bool convergeMissions = true)
        {
            if (!MissionStatePublication.Converge(client, _committedMissions.Values, convergeMissions))
                return;
            if (convergeFlags && _flags != null)
            {
                client.FlagProjection.ApplyCommitted(client, _flags);
                _manager.PublishCharacterFlags(client);
            }
            _inventoryPublication?.Invoke(client);

            foreach (var publication in _publications.Where(
                publication =>
                    publication.CounterId.HasValue &&
                    !publication.IsItemCounter))
                MissionApplication.TryPublish(
                    () => client.CallMethod(
                        client.Player.EntityId,
                        new UpdateObjectiveCounterPacket(
                            publication.MissionId,
                            publication.ObjectiveId,
                            publication.CounterId.Value,
                            publication.CounterValue.Value,
                            publication.InitialValue.Value,
                            publication.TargetValue.Value)),
                    $"mission {publication.MissionId} objective counter");

            foreach (var publication in _publications.Where(
                publication => publication.IsItemCounter))
                MissionApplication.TryPublish(
                    () => client.CallMethod(
                        client.Player.EntityId,
                        new UpdateObjectiveItemCounterPacket(
                            publication.MissionId,
                            publication.ObjectiveId,
                            publication.CounterId.Value,
                            publication.CounterValue.Value,
                            publication.TargetValue.Value)),
                    $"mission {publication.MissionId} objective item counter");

            foreach (var publication in _publications.Where(
                publication => publication.Completed))
            {
                MissionApplication.TryPublish(
                    () => client.CallMethod(
                        client.Player.EntityId,
                        new ObjectiveCompletedPacket(
                            publication.MissionId,
                            publication.ObjectiveId)),
                    $"mission {publication.MissionId} objective completion");
            }

            foreach (var publication in _publications)
            {
                foreach (var objectiveId in publication.RevealedObjectiveIds)
                    MissionApplication.TryPublish(
                        () => client.CallMethod(
                            client.Player.EntityId,
                            new ObjectiveRevealedPacket(
                                publication.MissionId,
                                objectiveId,
                                _manager.BuildPublishedMissionInfo(
                                    client.Player,
                                    publication.Definition,
                                    client.Player.Missions[publication.MissionId]))),
                        $"mission {publication.MissionId} objective {objectiveId} revealed");
                foreach (var objectiveId in publication.ActivatedObjectiveIds)
                    MissionApplication.TryPublish(
                        () => client.CallMethod(
                            client.Player.EntityId,
                            new ObjectiveActivatedPacket(
                                publication.MissionId,
                                objectiveId)),
                        $"mission {publication.MissionId} objective {objectiveId} activated");
                foreach (var request in publication.AmbientConversationRequests)
                {
                    MissionApplication.TryPublish(
                        () => client.CallMethod(
                            client.Player.EntityId,
                            new ForceConversePacket((int)request.GreetingId)),
                        $"mission {publication.MissionId} objective {publication.ObjectiveId} ambient conversation");
                    _manager.PublishAnnouncementAudio(client, publication.MissionId, request.GreetingId);
                }
            }

            var indicatorRefreshMissionIds = new SortedSet<uint>(_missionStatusMissionIds);
            foreach (var publication in _publications)
                if (publication.ShownIndicatorIds.Count > 0)
                    indicatorRefreshMissionIds.Add(publication.MissionId);

            foreach (var missionId in indicatorRefreshMissionIds)
                _manager.PublishMissionStatus(
                    client,
                    missionId,
                    $"mission {missionId} status after deadline start or indicator reveal");

            foreach (var missionId in _completableMissions)
            {
                MissionApplication.TryPublish(
                    () => client.CallMethod(
                        client.Player.EntityId,
                        new MissionCompleteablePacket(missionId, true)),
                    $"mission {missionId} completable");
                MissionApplication.TryPublish(
                    () => _manager.Scenes.MissionChanged(client, missionId, "Completeable"),
                    $"mission {missionId} experience ready state");
            }

            foreach (var failurePlan in _failurePlans)
                failurePlan.Publish(
                    client,
                    _manager,
                    _startScenario,
                    _startFailureScenario,
                    convergeFlags: false,
                    convergeMission: false);

            foreach (var publication in _publications)
                _manager.PublishStartedScenarios(
                    client,
                    publication.MissionId,
                    publication.StartScenarioIds,
                    _startScenario);

            foreach (var publication in _publications)
                foreach (var spawnGroupId in publication.ActivateSpawnGroupIds)
                    MissionApplication.TryPublish(
                        () => _activateSpawnGroup?.Invoke(client, publication.MissionId, spawnGroupId),
                        $"mission {publication.MissionId} activate spawn group {spawnGroupId}");

            // ObjectiveState triggers have no other way to fire: nothing re-evaluates a
            // transition just because a *different* objective's state changed underneath it.
            // Recording a synthetic progress event per touched objective, after this plan's
            // own packets are sent, lets any ObjectiveState-gated transition elsewhere in the
            // same mission react in a follow-up, independently-committed RecordProgress call.
            // client.SyncRoot is a plain lock (Monitor), which is re-entrant on the same
            // thread, so this nested call is safe; a well-formed mission can't loop forever
            // here since each objective can only leave Incomplete once.
            foreach (var publication in _publications)
            {
                if (publication.ObjectiveState.HasValue)
                    _manager.RecordProgress(
                        client,
                        MissionProgressEvent.ObjectiveState(
                            publication.MissionId,
                            publication.ObjectiveId,
                            (byte)publication.ObjectiveState.Value));
                foreach (var finalState in publication.FinalObjectiveStates)
                    _manager.RecordProgress(
                        client,
                        MissionProgressEvent.ObjectiveState(
                            publication.MissionId,
                            finalState.Key,
                            (byte)finalState.Value));
            }

            if (HasChanges)
            {
                _manager.Scenes.RefreshTimers(client);
                _manager.RefreshNpcConversationStatuses(client);
            }
        }

    }


    internal readonly struct ProgressPublication
    {
        internal Mission Definition { get; }
        internal MissionLog RuntimeMission { get; }
        internal uint MissionId { get; }
        internal uint ObjectiveId { get; }
        internal MissionObjectiveState? ObjectiveState { get; }
        internal uint? CounterId { get; }
        internal uint? CounterValue { get; }
        internal uint? InitialValue { get; }
        internal uint? TargetValue { get; }
        internal bool Completed { get; }
        internal bool IsItemCounter { get; }
        internal IReadOnlyDictionary<uint, MissionObjectiveState> FinalObjectiveStates { get; }
        internal IReadOnlyList<uint> RevealedObjectiveIds { get; }
        internal IReadOnlyList<uint> ActivatedObjectiveIds { get; }
        internal IReadOnlyList<uint> StartScenarioIds { get; }
        internal IReadOnlyList<MissionApplication.AmbientConversationRequest> AmbientConversationRequests { get; }
        internal IReadOnlyList<uint> ShownIndicatorIds { get; }
        internal IReadOnlyList<MissionApplication.PlayerFlagChange> PlayerFlagChanges { get; }
        internal IReadOnlyList<uint> ActivateSpawnGroupIds { get; }

        private ProgressPublication(
            Mission definition,
            MissionLog runtimeMission,
            uint missionId,
            uint objectiveId,
            MissionObjectiveState? objectiveState,
            uint? counterId,
            uint? counterValue,
            uint? initialValue,
            uint? targetValue,
            bool completed,
            MissionApplication.TransitionActionApplication actionApplication,
            bool isItemCounter = false)
        {
            Definition = definition;
            RuntimeMission = runtimeMission;
            MissionId = missionId;
            ObjectiveId = objectiveId;
            ObjectiveState = objectiveState;
            CounterId = counterId;
            CounterValue = counterValue;
            InitialValue = initialValue;
            TargetValue = targetValue;
            Completed = completed;
            IsItemCounter = isItemCounter;
            FinalObjectiveStates = new ReadOnlyDictionary<uint, MissionObjectiveState>(
                new Dictionary<uint, MissionObjectiveState>(
                    actionApplication.FinalObjectiveStates));
            RevealedObjectiveIds = Array.AsReadOnly(actionApplication.RevealedObjectiveIds.ToArray());
            ActivatedObjectiveIds = Array.AsReadOnly(actionApplication.ActivatedObjectiveIds.ToArray());
            StartScenarioIds = Array.AsReadOnly(actionApplication.StartScenarioIds.ToArray());
            AmbientConversationRequests = Array.AsReadOnly(actionApplication.AmbientConversationRequests.ToArray());
            ShownIndicatorIds = Array.AsReadOnly(actionApplication.ShownIndicatorIds.ToArray());
            PlayerFlagChanges = Array.AsReadOnly(actionApplication.PlayerFlagChanges.ToArray());
            ActivateSpawnGroupIds = Array.AsReadOnly(actionApplication.ActivateSpawnGroupIds.ToArray());
        }

        internal static ProgressPublication ForCompleted(
            ProgressCandidate candidate,
            MissionApplication.TransitionActionApplication actionApplication) =>
            new(
                candidate.Definition,
                candidate.RuntimeMission,
                candidate.Definition.MissionId,
                candidate.ObjectiveDefinition.ObjectiveId,
                MissionObjectiveState.Completed,
                null,
                null,
                null,
                null,
                true,
                actionApplication);

        internal static ProgressPublication ForTransition(
            ProgressCandidate candidate,
            MissionObjectiveState objectiveState,
            MissionApplication.TransitionActionApplication actionApplication) =>
            new(
                candidate.Definition,
                candidate.RuntimeMission,
                candidate.Definition.MissionId,
                candidate.ObjectiveDefinition.ObjectiveId,
                objectiveState,
                null,
                null,
                null,
                null,
                false,
                actionApplication);

        internal static ProgressPublication Counter(
            ProgressCandidate candidate,
            uint counterId,
            uint counterValue,
            bool completed,
            MissionApplication.TransitionActionApplication actionApplication) =>
            new(
                candidate.Definition,
                candidate.RuntimeMission,
                candidate.Definition.MissionId,
                candidate.ObjectiveDefinition.ObjectiveId,
                completed ? MissionObjectiveState.Completed : null,
                counterId,
                counterValue,
                candidate.ExecutableTransition.ProgressRule.InitialValue,
                candidate.ExecutableTransition.ProgressRule.TargetValue,
                completed,
                actionApplication);

        internal static ProgressPublication ItemCounter(
            ProgressCandidate candidate,
            uint itemClassId,
            uint counterValue,
            bool completed,
            MissionApplication.TransitionActionApplication actionApplication) =>
            new(
                candidate.Definition,
                candidate.RuntimeMission,
                candidate.Definition.MissionId,
                candidate.ObjectiveDefinition.ObjectiveId,
                completed ? MissionObjectiveState.Completed : null,
                itemClassId,
                counterValue,
                candidate.ExecutableTransition.ProgressRule.InitialValue,
                candidate.ExecutableTransition.ProgressRule.TargetValue,
                completed,
                actionApplication,
                isItemCounter: true);
    }
}
