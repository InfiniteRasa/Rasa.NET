using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace Rasa.Game.Missions
{
    using Data;
    using Managers;
    using global::Rasa.Missions.Runtime;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    internal sealed class GroupCreditService
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly MissionApplication _missions;
        private readonly Dictionary<uint, DateTime> _pending = new();
        internal GroupCreditService(IGameUnitOfWorkFactory factory, MissionApplication missions)
        { _factory = factory; _missions = missions; }

        internal bool Record(Client source, MissionProgressEvent progress, Vector3 position, string eventId = null)
        {
            if (progress.Kind is not (MissionProgressEventKind.CreatureKilled or MissionProgressEventKind.ScenarioEvent))
                return _missions.RecordProgress(source, progress);
            if (source?.Player?.MapChannel == null || source.State != ClientState.Ingame)
                return false;
            if (source.Player.MapChannel.IsPrivateInstance || PartyManager.Instance.PartyOf(source) == null)
                return _missions.RecordProgress(source, progress);
            var candidates = Select(source, progress, position, null);
            if (candidates.Count == 0)
                return false;
            eventId ??= Guid.NewGuid().ToString("N");
            IReadOnlyList<uint> recipients = Array.Empty<uint>();
            try
            {
                using var unit = _factory.CreateChar();
                unit.ExecuteTransaction(() => recipients = Freeze(unit, source, progress, eventId, "", 0, candidates));
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                Logger.WriteLog(LogType.Error, $"Mission event {eventId} could not freeze its recipients: {error}");
                return false;
            }
            Schedule(recipients);
            // Other clients are drained by the map loop, never under this client's lock.
            Deliver(source);
            return true;
        }

        internal IReadOnlyList<uint> FreezeScene(ICharUnitOfWork unit, Client source, MissionProgressEvent progress,
            Vector3 position, string eventId, string runId, uint generation)
        {
            var participants = unit.CharacterMissions.Runtime.Participants(runId)
                .Where(participant => participant.Active).ToDictionary(participant => participant.CharacterId);
            return Freeze(unit, source, progress, eventId, runId, generation,
                Select(source, progress, position, participants));
        }

        internal IReadOnlyList<uint> FreezeWorld(ICharUnitOfWork unit, Client source, MissionProgressEvent progress,
            Vector3 position, string eventId, string runId, uint generation) =>
            FreezeScene(unit, source, progress, position, eventId, runId, generation);

        internal bool HasGroupRule(Client source, MissionProgressEvent progress) =>
            _missions.ProgressCandidates(source, progress).Any(candidate =>
                candidate.ObjectiveDefinition.CreditPolicy.Mode != MissionCreditMode.Personal);

        internal void CaptureParticipants(ICharUnitOfWork unit, MapChannel map, uint ownerId,
            uint missionId, string runId, Vector3 position)
        {
            var owner = map.ClientList.SingleOrDefault(client => client.Player?.Id == ownerId);
            var party = owner == null ? null : PartyManager.Instance.PartyOf(owner);
            if (party == null || !_missions.LoadedMissions.TryGetValue(missionId, out var definition))
                return;
            foreach (var client in map.ClientList.Where(client => client.Player?.Id != ownerId &&
                client.Player?.MapChannel == map && client.State == ClientState.Ingame && client.PendingTransfer == null &&
                client.Player.State != CharacterState.Dead))
            {
                if (!ReferenceEquals(party, PartyManager.Instance.PartyOf(client)) ||
                    party.Find(client.AccountEntry.Id)?.EntityId != client.Player.EntityId ||
                    !client.Player.Missions.TryGetValue(missionId, out var journal) || journal.State != MissionState.Active)
                    continue;
                var eligible = definition.Objectives.Values.Any(objective =>
                    objective.CreditPolicy.Mode == MissionCreditMode.EncounterParticipants &&
                    journal.Objectives.TryGetValue(objective.ObjectiveId, out var state) &&
                    state.State == MissionObjectiveState.Incomplete &&
                    Vector3.Distance(client.Player.Position, position) <= objective.CreditPolicy.Radius);
                if (!eligible)
                    continue;
                var assignment = unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, missionId);
                if (assignment?.MissionState != (uint)MissionState.Active ||
                    !journal.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision))
                    continue;
                unit.CharacterMissions.Runtime.Add(new MissionSceneParticipantEntry
                {
                    RunId = runId, CharacterId = client.Player.Id, AssignmentId = assignment.AssignmentId,
                    AssignmentGeneration = assignment.Generation
                });
            }
        }

        private IReadOnlyList<Recipient> Select(Client source, MissionProgressEvent progress, Vector3 position,
            IReadOnlyDictionary<uint, MissionSceneParticipantEntry> participants)
        {
            var result = new List<Recipient>();
            var map = source.Player.MapChannel;
            var party = PartyManager.Instance.PartyOf(source);
            foreach (var client in map.ClientList.Append(source).Distinct().ToArray())
            {
                if (client?.Player?.MapChannel != map || client.State != ClientState.Ingame ||
                    client.PendingTransfer != null || client.AccountEntry == null || client.Player.State == CharacterState.Dead)
                    continue;
                var sameParty = party != null && ReferenceEquals(party, PartyManager.Instance.PartyOf(client)) &&
                    party.Find(client.AccountEntry.Id)?.EntityId == client.Player.EntityId;
                foreach (var group in _missions.ProgressCandidates(client, progress)
                    .GroupBy(candidate => candidate.Definition.MissionId))
                {
                    var assignment = group.First().RuntimeMission;
                    var participant = participants?.GetValueOrDefault(client.Player.Id);
                    if (participant != null && (participant.AssignmentId != assignment.AssignmentId ||
                        participant.AssignmentGeneration != assignment.Generation))
                        participant = null;
                    var objectives = group.Where(candidate =>
                        candidate.ObjectiveDefinition.CreditPolicy.Eligible(ReferenceEquals(source, client), true,
                            sameParty, participant != null, Vector3.Distance(client.Player.Position, position)))
                        .Select(candidate => candidate.ObjectiveDefinition.ObjectiveId).ToArray();
                    if (objectives.Length > 0)
                        result.Add(new Recipient(client, group.Key, objectives, participant, assignment));
                }
            }
            return result;
        }

        private IReadOnlyList<uint> Freeze(ICharUnitOfWork unit, Client source, MissionProgressEvent progress, string eventId,
            string runId, uint generation, IReadOnlyList<Recipient> candidates)
        {
            var store = unit.CharacterMissions.Runtime;
            if (store.HasOutcome(eventId))
                return Array.Empty<uint>();
            var recipients = new List<uint>();
            store.Add(new MissionOutcomeEntry
            { EventId = eventId, RunId = runId, Generation = generation, CreatedAtUtc = DateTime.UtcNow });
            foreach (var candidate in candidates)
            {
                var assignment = unit.CharacterMissions.GetByCharacterAndMission(
                    candidate.Client.Player.Id, candidate.MissionId);
                if (assignment?.MissionState != (uint)MissionState.Active ||
                    !candidate.Assignment.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision) ||
                    candidate.Participant != null && (candidate.Participant.AssignmentId != assignment.AssignmentId ||
                        candidate.Participant.AssignmentGeneration != assignment.Generation))
                    continue;
                if (unit.Characters.Get(assignment.CharacterId)?.AccountId != candidate.Client.AccountEntry?.Id)
                    throw new GameplayRejectionException("Credit recipient character ownership changed.");
                var objectives = unit.CharacterMissionProgress.GetTracked(assignment.CharacterId, assignment.MissionId);
                var eligible = candidate.Objectives.Where(id => objectives.TryGetValue(id, out var objective) &&
                    objective.ObjectiveState == (byte)MissionObjectiveState.Incomplete &&
                    _missions.IsObjectiveEligibleAtEvent(candidate.Client, assignment.MissionId, id, unit)).ToArray();
                if (eligible.Length == 0)
                    continue;
                store.Add(new MissionCreditDeliveryEntry
                {
                    EventId = eventId, AssignmentId = assignment.AssignmentId,
                    AssignmentGeneration = assignment.Generation, CharacterId = assignment.CharacterId,
                    MissionId = assignment.MissionId, ObjectiveId = eligible[0],
                    Payload = JsonSerializer.Serialize(new CreditPayload(progress.Kind, progress.SubjectId,
                        progress.Quantity, progress.ScopeId, progress.DetailId, eligible))
                });
                recipients.Add(assignment.CharacterId);
            }
            return recipients;
        }

        internal void Schedule(IEnumerable<uint> recipients)
        {
            foreach (var characterId in recipients)
                _pending[characterId] = DateTime.MinValue;
        }

        internal bool Deliver(Client client)
        {
            if (client?.Player?.MapChannel == null || client.State != ClientState.Ingame || client.PendingTransfer != null)
                return false;
            lock (client.SyncRoot)
            {
                var changed = false;
                try
                {
                    MissionCreditDeliveryEntry[] pending;
                    using (var unit = _factory.CreateChar())
                        pending = unit.CharacterMissions.Runtime.Deliveries(client.Player.Id).ToArray();
                    foreach (var delivery in pending)
                    {
                        var plan = MissionProgressPublicationPlan.Empty;
                        using var unit = _factory.CreateChar();
                        unit.ExecuteTransaction(() =>
                        {
                            var store = unit.CharacterMissions.Runtime;
                            var current = store.Deliveries(client.Player.Id).SingleOrDefault(entry =>
                                entry.EventId == delivery.EventId && entry.AssignmentId == delivery.AssignmentId);
                            if (current == null)
                                return;
                            var assignment = unit.CharacterMissions.GetByCharacterAndMission(client.Player.Id, delivery.MissionId);
                            if (assignment?.AssignmentId != delivery.AssignmentId ||
                                assignment.Generation != delivery.AssignmentGeneration ||
                                assignment.MissionState != (uint)MissionState.Active)
                            {
                                current.Status = "Expired"; current.Version++;
                                return;
                            }
                            if (!store.HasReceipt(assignment.AssignmentId, assignment.Generation, delivery.EventId))
                            {
                                var payload = JsonSerializer.Deserialize<CreditPayload>(current.Payload)
                                    ?? throw new GameplayRejectionException("Credit payload is missing.");
                                var objectives = unit.CharacterMissionProgress.GetTracked(client.Player.Id, delivery.MissionId);
                                var eligible = payload.Objectives.Where(id => objectives.TryGetValue(id, out var objective) &&
                                    objective.ObjectiveState == (byte)MissionObjectiveState.Incomplete)
                                    .Select(id => (delivery.MissionId, id)).ToHashSet();
                                if (eligible.Count == 0)
                                {
                                    current.Status = "Expired"; current.Version++;
                                    return;
                                }
                                plan = _missions.PlanFrozenProgress(client,
                                    new[] { MissionProgressEvent.Restore(payload.Kind, payload.Subject, payload.Quantity,
                                        payload.Scope, payload.Detail) }, unit, eligible);
                                if (!plan.HasChanges)
                                    throw new GameplayRejectionException("Frozen objective credit could not converge with runtime state.");
                                store.Add(new MissionReceiptEntry
                                {
                                    OwnerId = assignment.AssignmentId, Generation = assignment.Generation,
                                    OperationKey = delivery.EventId, Kind = "Credit", CreatedAtUtc = DateTime.UtcNow
                                });
                            }
                            current.Status = "Applied"; current.Version++;
                        });
                        plan.Publish(client);
                        changed |= plan.HasChanges;
                    }
                    _pending.Remove(client.Player.Id);
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error) || error is JsonException)
                {
                    _pending[client.Player.Id] = DateTime.UtcNow.AddSeconds(1);
                    Logger.WriteLog(LogType.Error, $"Mission credit for character {client.Player.Id} remains pending: {error}");
                }
                return changed;
            }
        }

        internal void Tick(MapChannel map)
        {
            foreach (var entry in _pending.Where(entry => entry.Value <= DateTime.UtcNow).ToArray())
            {
                var client = map.ClientList.FirstOrDefault(client => client.Player?.Id == entry.Key);
                if (client != null)
                    Deliver(client);
            }
        }

        internal void Resume(Client client)
        {
            using var unit = _factory.CreateChar();
            if (unit.CharacterMissions.Runtime.Deliveries(client.Player.Id).Count > 0)
                _pending[client.Player.Id] = DateTime.MinValue;
        }
        internal void Detach(uint characterId) => _pending.Remove(characterId);

        private sealed record Recipient(Client Client, uint MissionId, uint[] Objectives,
            MissionSceneParticipantEntry Participant, MissionLog Assignment);
        private sealed record CreditPayload(MissionProgressEventKind Kind, uint Subject, uint Quantity,
            uint? Scope, uint? Detail, uint[] Objectives);
    }
}
