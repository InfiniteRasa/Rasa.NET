using Rasa.Missions.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace Rasa.Game.Missions
{
    using Managers;
    using Content;
    using global::Rasa.Missions.Scenes;
    using Persistence;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using World;

    internal enum SceneTickScope { All, Deadlines, Scripts }

    internal sealed class SceneApplication
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly MissionApplication _missions;
        private readonly ManifestationManager _manifestations;
        private readonly SceneRuntime _runtime;
        private readonly SceneCharacterAdapter _characters;
        private readonly ISceneWorld _world;
        private readonly Func<DateTime> _utcNow;
        private readonly Dictionary<string, Resident> _runs = new(StringComparer.Ordinal);
        private readonly Dictionary<uint, (string Script, SceneBindings Bindings)> _bindings = new();
        private readonly Dictionary<(uint Character, uint Mission, string Assignment), string> _assignmentScenes = new();
        private readonly Dictionary<string, WorldRetry> _worldRetries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _messageRetries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _terminationRetries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PendingPause> _pauseRetries = new(StringComparer.Ordinal);
        private readonly Dictionary<uint, MissionExperienceDefinition> _experiences = new();
        private readonly Dictionary<(uint Character, MapChannel Map), Client> _resumed = new();
        private readonly SceneDueQueue _due = new();
        private readonly Queue<(string RunId, SceneObservation Observation)> _observations = new();
        private readonly object _dispatchGate = new();
        private bool _draining;

        internal SceneApplication(IGameUnitOfWorkFactory factory, MissionApplication missions,
            ManifestationManager manifestations, SceneScriptRegistry registry = null,
            Func<Action<string, SceneObservation>, ISceneWorld> worldFactory = null, Func<DateTime> utcNow = null)
        {
            _factory = factory; _missions = missions; _manifestations = manifestations;
            _runtime = new SceneRuntime(registry ?? new SceneScriptRegistry());
            _characters = new SceneCharacterAdapter(missions, manifestations);
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _world = worldFactory?.Invoke(ObserveWorld) ?? new SceneWorldAdapter(missions.PublicActors, ObserveWorld);
        }

        internal void Bind(uint missionId, string script, SceneBindings bindings)
        {
            if (!_runtime.Supports(script, 1))
                throw new GameplayRejectionException($"Missing script {script}/1 for mission {missionId}.");
            _bindings.Add(missionId, (script, bindings));
        }

        internal bool Owns(uint missionId) => _bindings.ContainsKey(missionId);
        internal bool UsesScript(uint missionId, string script) =>
            _bindings.TryGetValue(missionId, out var binding) && binding.Script == script;

        internal void LeaseReset(string runId)
        {
            _pauseRetries.Remove(runId);
            if (_runs.TryGetValue(runId, out var current))
                _world.Terminate(current.Run, current.Map);
            else
                _world.Detach(runId);
            _due.Cancel(runId);
            _worldRetries.Remove(runId);
            _messageRetries.Remove(runId);
            if (_runs.Remove(runId, out var resident))
                _assignmentScenes.Remove((resident.Run.OwnerCharacterId, resident.Run.MissionId, resident.AssignmentId));
        }

        internal bool ExecuteNamed(Client client, uint missionId, string name)
        {
            if (!_bindings.TryGetValue(missionId, out var binding) || !binding.Bindings.Names.TryGetValue(name, out var sequence))
                return Reject($"Mission {missionId} has no authored sequence {name}.");
            return Execute(client, missionId, sequence);
        }
        internal bool OwnsExperience(uint mapContextId) => _experiences.ContainsKey(mapContextId);

        internal void ClearBindings()
        {
            _bindings.Clear();
            _experiences.Clear();
        }

        internal void BindExperience(MissionExperienceDefinition experience)
        {
            if (!experience.PrivatePerCharacter || !_runtime.Supports(experience.Scene.Script, experience.Scene.StateVersion))
                throw new GameplayRejectionException($"Experience {experience.Key} has an unsupported host/script.");
            _experiences.Add(experience.MapContextId, experience);
        }

        internal void StageExperience(uint characterId, MapChannel map)
        {
            if (!_experiences.TryGetValue(map.MapInfo.MapContextId, out var experience))
                return;
            if (!map.IsPrivateInstance || map.OwnerCharacterId != characterId)
                throw new GameplayRejectionException($"Experience {experience.Key} requires its character's private map.");
            var owner = map.ClientList.FirstOrDefault(client => client.Player?.Id == characterId);
            using var unit = _factory.CreateChar();
            var row = unit.CharacterMissions.Runtime.Scenes(characterId, 0)
                .SingleOrDefault(scene => scene.ScriptKey == experience.Scene.Script);
            var bindings = experience.Scene.Bindings(experience.Revision);
            string runId;
            if (row == null)
                runId = Start(characterId, map, owner, experience.Scene.Script, bindings, 0);
            else
            {
                runId = row.RunId;
                Attach(characterId, map, owner, runId, bindings);
                if (row.Version == 0)
                    Submit(runId, new SceneObservation(SceneEventKind.Started, row.Generation));
            }
            var assignments = unit.CharacterMissions.Get(characterId).ToDictionary(mission => mission.MissionId);
            var states = assignments.ToDictionary(entry => entry.Key, entry => entry.Value.MissionState);
            var history = unit.CharacterMissions.Runtime.History(characterId);
            foreach (var outcome in history.OrderByDescending(entry => entry.AssignmentGeneration)
                .ThenByDescending(entry => entry.CompletedAtUtc).ThenByDescending(entry => entry.AssignmentId))
                states.TryAdd(outcome.MissionId, outcome.Outcome);
            foreach (var trigger in experience.MissionTriggers.Where(trigger =>
                states.TryGetValue(trigger.MissionId, out var state) &&
                (trigger.Event == "Accepted" || trigger.Event == "Rewarded" &&
                    (state == (uint)Data.MissionState.Completed || history.Any(entry => entry.MissionId == trigger.MissionId && entry.Rewarded)) ||
                 trigger.Event == "Completeable" && state == (uint)Data.MissionState.Active &&
                    assignments.TryGetValue(trigger.MissionId, out var assignment) && assignment.Completeable)))
                Submit(runId, new SceneObservation(SceneEventKind.Signal, _runs[runId].Run.Generation, SequenceId: trigger.SequenceId));
        }

        internal void Resume(Client client)
        {
            if (client?.Player?.MapChannel == null || client.State != Data.ClientState.Ingame || client.PendingTransfer != null)
                return;
            var map = client.Player.MapChannel;
            if (_resumed.TryGetValue((client.Player.Id, map), out var attached) && ReferenceEquals(attached, client))
                return;
            StageExperience(client.Player.Id, map);
            foreach (var resident in _runs.Values.Where(run => run.Run.OwnerCharacterId == client.Player.Id && run.Map == map))
            {
                resident.Owner = client;
                _world.Attach(resident.Run, resident.Bindings, client, map);
            }
            using var unit = _factory.CreateChar();
            var assignments = unit.CharacterMissions.Get(client.Player.Id).ToDictionary(entry => entry.MissionId);
            foreach (var binding in _bindings)
                foreach (var row in unit.CharacterMissions.Runtime.Scenes(client.Player.Id, binding.Key))
                {
                    var current = assignments.TryGetValue(binding.Key, out var assignment) &&
                        assignment.AssignmentId == row.AssignmentId;
                    if (!map.IsPrivateInstance && row.MapKey != PublicActorLeaseService.MapKey(map))
                        continue;
                    if ((!current && (!map.IsPrivateInstance || row.Version == 0)) || row.Status == "Resetting" ||
                        !map.IsPrivateInstance && row.Status is "Ended" or "Faulted")
                        continue;
                    Attach(client, row.RunId, binding.Value.Bindings);
                    if (current && row.Version == 0 && row.Status is "Running" or "Waiting")
                        Submit(row.RunId, new SceneObservation(SceneEventKind.Started, row.Generation));
                    if (current && row.Status is "Running" or "Waiting")
                        DrainMessages(row.RunId);
                }
            _missions.Credit.Resume(client);
            _resumed[(client.Player.Id, map)] = client;
        }

        internal void Rebuild(uint characterId, MapChannel map)
        {
            StageExperience(characterId, map);
            var owner = map.ClientList.FirstOrDefault(client => client.Player?.Id == characterId);
            using var unit = _factory.CreateChar();
            foreach (var binding in _bindings.OrderBy(entry => entry.Key))
                foreach (var row in unit.CharacterMissions.Runtime.Scenes(characterId, binding.Key))
                    if (row.Status != "Resetting" && (map.IsPrivateInstance || row.MapKey == PublicActorLeaseService.MapKey(map)))
                        Attach(characterId, map, owner, row.RunId, binding.Value.Bindings);
        }

        internal void MissionChanged(Client client, uint missionId, string change)
        {
            if (client?.Player?.MapChannel == null ||
                !_experiences.TryGetValue(client.Player.MapContextId, out var experience))
                return;
            Resume(client);
            var root = _runs.Values.Single(run => run.Run.OwnerCharacterId == client.Player.Id &&
                run.Map == client.Player.MapChannel && run.Run.ScriptKey == experience.Scene.Script);
            foreach (var trigger in experience.MissionTriggers.Where(trigger => trigger.MissionId == missionId && trigger.Event == change))
                Submit(root.Run.Id, new SceneObservation(SceneEventKind.Signal, root.Run.Generation, SequenceId: trigger.SequenceId));
        }

        internal void PrepareAssignment(Repositories.Char.ICharUnitOfWork unit, Client owner, CharacterMissionEntry assignment)
        {
            if (!_bindings.TryGetValue(assignment.MissionId, out var binding))
                return;
            RequireSceneControl(unit, assignment);
            var scene = new MissionSceneEntry
            {
                RunId = Guid.NewGuid().ToString("N"), AssignmentId = assignment.AssignmentId,
                MissionId = assignment.MissionId, OwnerCharacterId = owner.Player.Id,
                ScriptKey = binding.Script, Release = binding.Bindings.Release, StateVersion = 1,
                MapKey = PublicActorLeaseService.MapKey(owner.Player.MapChannel)
            };
            unit.CharacterMissions.Runtime.Add(scene);
            unit.CharacterMissions.Runtime.Add(new MissionSceneParticipantEntry
            {
                RunId = scene.RunId, CharacterId = owner.Player.Id,
                AssignmentId = assignment.AssignmentId, AssignmentGeneration = assignment.Generation
            });
        }

        internal void AttachSharedAssignment(Repositories.Char.ICharUnitOfWork unit, CharacterMissionEntry assignment,
            global::Rasa.Missions.Definitions.MissionOfferSourceIdentity source)
        {
            var store = unit.CharacterMissions.Runtime;
            var scene = store.ReadScene(source.InstanceId);
            if (source.Generation == 0 || scene?.Generation != source.Generation ||
                scene.MissionId != assignment.MissionId || scene.Release != assignment.ContentRevision ||
                scene.Status is not ("Running" or "Waiting") || scene.OwnerCharacterId == assignment.CharacterId)
                throw new GameplayRejectionException("The shared scene is not the offered live run.");
            var participant = store.Participants(scene.RunId).SingleOrDefault(entry => entry.CharacterId == assignment.CharacterId);
            if (participant?.Active == true)
                throw new GameplayRejectionException("A live participant already occupies this character's run slot.");
            if (participant == null)
            {
                participant = new MissionSceneParticipantEntry { RunId = scene.RunId, CharacterId = assignment.CharacterId };
                store.Add(participant);
            }
            participant.AssignmentId = assignment.AssignmentId;
            participant.AssignmentGeneration = assignment.Generation;
            participant.Active = true;
            TransactionValidation.AtCommitBoundary(unit, () =>
            {
                var current = store.ReadParticipant(source.InstanceId, assignment.CharacterId);
                var run = store.ReadScene(source.InstanceId);
                if (current?.Active != true || current.AssignmentId != assignment.AssignmentId ||
                    current.AssignmentGeneration != assignment.Generation || run?.Generation != source.Generation ||
                    run.Status is not ("Running" or "Waiting") || run.AssignmentId != scene.AssignmentId)
                    throw new GameplayRejectionException("Shared participation changed before assignment commit.");
            });
        }

        private static bool IsJoinedAssignment(Repositories.Char.ICharUnitOfWork unit, CharacterMissionEntry assignment) =>
            unit.CharacterMissions.Runtime.Participations(assignment.AssignmentId).Any(participant =>
                participant.CharacterId == assignment.CharacterId && participant.AssignmentGeneration == assignment.Generation &&
                unit.CharacterMissions.Runtime.Scene(participant.RunId)?.OwnerCharacterId != assignment.CharacterId);

        private static void RequireSceneControl(Repositories.Char.ICharUnitOfWork unit, CharacterMissionEntry assignment)
        {
            if (IsJoinedAssignment(unit, assignment))
                throw new GameplayRejectionException("A shared participant cannot control or create the initiator's world scene.");
        }

        internal IReadOnlyList<string> CancelAssignment(Repositories.Char.ICharUnitOfWork unit, CharacterMissionEntry assignment)
        {
            var store = unit.CharacterMissions.Runtime;
            var scenes = store.Scenes(assignment.CharacterId, assignment.MissionId)
                .Where(scene => scene.AssignmentId == assignment.AssignmentId).ToArray();
            foreach (var participant in store.Participations(assignment.AssignmentId).Where(participant =>
                participant.CharacterId == assignment.CharacterId && participant.AssignmentGeneration == assignment.Generation))
                participant.Active = false;
            foreach (var scene in scenes)
            {
                foreach (var effect in store.ForwardedEffects(scene.RunId).Where(effect =>
                    effect.SourceAssignmentId == assignment.AssignmentId &&
                    effect.SourceAssignmentGeneration == assignment.Generation && effect.Status != "Cancelled"))
                { effect.Status = "Cancelled"; effect.Version++; }
                if (scene.Status == "Resetting")
                    continue;
                scene.Generation++;
                scene.Version++;
                scene.Status = "Resetting";
                scene.Fault = "AssignmentAbandoned";
                foreach (var timer in store.Timers(scene.RunId).Where(timer => timer.Disposition is "Pending" or "Paused"))
                { timer.Disposition = "Cancelled"; timer.Version++; }
                foreach (var message in store.Messages(scene.RunId).Where(message => message.Status == "Pending"))
                { message.Status = "Cancelled"; message.Version++; }
                foreach (var effect in store.Effects(scene.RunId).Where(effect => effect.Status is "Pending" or "Running"))
                { effect.Status = "Cancelled"; effect.Version++; }
                foreach (var participant in store.Participants(scene.RunId))
                    participant.Active = false;
                foreach (var lease in store.Leases(scene.RunId))
                {
                    lease.Generation = scene.Generation;
                    lease.State = "Resetting";
                    lease.Version++;
                }
            }
            foreach (var delivery in store.Deliveries(assignment.CharacterId).Where(entry => entry.AssignmentId == assignment.AssignmentId))
            {
                delivery.Status = "Expired";
                delivery.Version++;
            }
            return scenes.Select(scene => scene.RunId).ToArray();
        }

        internal void CompleteAssignmentCancellation(IEnumerable<string> runIds)
        {
            foreach (var runId in runIds)
            {
                _terminationRetries[runId] = _utcNow();
                if (_runs.TryGetValue(runId, out var resident))
                    resident.Suspended = true;
                _due.Cancel(runId);
                _worldRetries.Remove(runId);
                _messageRetries.Remove(runId);
                _pauseRetries.Remove(runId);
                FinishCancellation(runId);
            }
        }

        private void FinishCancellation(string runId)
        {
            try
            {
                using (var unit = _factory.CreateChar())
                    foreach (var effect in unit.CharacterMissions.Runtime.ForwardedEffects(runId)
                        .Where(effect => effect.Status == "Cancelled"))
                        _world.CancelOperation(effect.RunId, effect.Generation, effect.OperationKey);
                _missions.PublicActors.BeginReset(runId, "AssignmentAbandoned");
                LeaseReset(runId);
                _terminationRetries.Remove(runId);
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error) || error is InvalidOperationException)
            {
                _terminationRetries[runId] = _utcNow().AddSeconds(1);
                Reject($"Scene {runId} termination remains pending: {error}");
            }
        }

        private static void RequireCurrentAssignment(Repositories.Char.ICharUnitOfWork unit, MissionSceneEntry scene,
            bool persisted = false)
        {
            if (string.IsNullOrEmpty(scene.AssignmentId))
                return;
            var assignment = persisted ? unit.CharacterMissions.Runtime.ReadAssignment(scene.OwnerCharacterId, scene.MissionId) :
                unit.CharacterMissions.GetByCharacterAndMission(scene.OwnerCharacterId, scene.MissionId);
            var participant = persisted ? unit.CharacterMissions.Runtime.ReadParticipant(scene.RunId, scene.OwnerCharacterId) :
                unit.CharacterMissions.Runtime.Participants(scene.RunId).SingleOrDefault(entry => entry.CharacterId == scene.OwnerCharacterId);
            if (assignment?.AssignmentId != scene.AssignmentId || participant == null ||
                participant.AssignmentId != scene.AssignmentId || participant.AssignmentGeneration != assignment.Generation)
                throw new GameplayRejectionException($"Scene {scene.RunId} no longer owns its mission assignment/generation.");
        }

        private bool HasCurrentEffectSource(Repositories.Char.ICharUnitOfWork unit, MissionSceneEntry target,
            MissionWorldEffectEntry effect)
        {
            if (effect.SourceRunId == null)
                return effect.SourceGeneration == null && effect.SourceAssignmentId == null &&
                    effect.SourceAssignmentGeneration == null;
            var store = unit.CharacterMissions.Runtime;
            var source = store.ReadScene(effect.SourceRunId);
            // Private instance keys can change on recreation; attachment enforces their current owner.
            var samePrivateOwner = _runs.TryGetValue(target.RunId, out var execution) &&
                execution.Map.IsPrivateInstance && execution.Map.OwnerCharacterId == target.OwnerCharacterId;
            if (target.MissionId != 0 || source == null || source.MissionId == 0 ||
                source.OwnerCharacterId != target.OwnerCharacterId || !samePrivateOwner && source.MapKey != target.MapKey ||
                source.Generation != effect.SourceGeneration || source.Status is "Resetting" or "Faulted" ||
                string.IsNullOrEmpty(effect.SourceAssignmentId) || source.AssignmentId != effect.SourceAssignmentId ||
                !effect.SourceAssignmentGeneration.HasValue || effect.SourceAssignmentGeneration == 0)
                return false;
            var participant = store.ReadParticipant(source.RunId, source.OwnerCharacterId);
            if (participant?.Active != true || participant.AssignmentId != effect.SourceAssignmentId ||
                participant.AssignmentGeneration != effect.SourceAssignmentGeneration)
                return false;
            var assignment = store.ReadAssignment(source.OwnerCharacterId, source.MissionId);
            if (assignment != null)
                return assignment.AssignmentId == effect.SourceAssignmentId &&
                    assignment.Generation == effect.SourceAssignmentGeneration;
            // A cleared one-time success still owns its authored experience state.
            var history = store.ReadHistory(effect.SourceAssignmentId);
            return _missions.TryGetOperationalMission(source.MissionId, out var definition) &&
                definition.RepeatPolicy.Kind == global::Rasa.Missions.Runtime.MissionRepeatKind.Once &&
                history?.CharacterId == source.OwnerCharacterId && history.MissionId == source.MissionId &&
                history.AssignmentGeneration == effect.SourceAssignmentGeneration &&
                history.Outcome is (uint)Data.MissionState.Success or (uint)Data.MissionState.Completed;
        }

        private static void CancelStaleEffect(MissionWorldEffectEntry effect)
        {
            effect.Status = "Cancelled";
            effect.Failure = "Source assignment or scene generation is no longer current.";
            effect.Version++;
        }

        internal void SynchronizeDeadline(Repositories.Char.ICharUnitOfWork unit, CharacterMissionEntry assignment,
            CharacterMissionDeadlineEntry deadline, uint? objectiveId)
        {
            if (!Owns(assignment.MissionId))
                return;
            var store = unit.CharacterMissions.Runtime;
            var scene = store.AssignmentScene(assignment.AssignmentId);
            if (scene == null)
                throw new GameplayRejectionException($"Assignment {assignment.AssignmentId} has no scene clock owner.");
            if (deadline?.State == CharacterMissionDeadlineState.Active && objectiveId.HasValue)
            {
                var name = $"objective-{objectiveId.Value}-deadline";
                var timer = store.Timer(scene.RunId, name);
                if (timer == null)
                    store.Add(new MissionTimerEntry
                    {
                        RunId = scene.RunId, Generation = scene.Generation, Name = name,
                        MissionId = assignment.MissionId, ObjectiveId = objectiveId,
                        DueAtUtc = deadline.DueAtUtc, ClockPolicy = "WallClock"
                    });
            }
            else
                foreach (var timer in store.Timers(scene.RunId).Where(timer => timer.MissionId == assignment.MissionId &&
                    timer.ObjectiveId.HasValue && timer.Disposition == "Pending"))
                { timer.Disposition = "Cancelled"; timer.Version++; }
        }

        internal void RefreshTimers(Client client)
        {
            foreach (var resident in _runs.Values.Where(run => run.Run.OwnerCharacterId == client.Player.Id).ToArray())
            {
                using var unit = _factory.CreateChar();
                LoadTimers(resident, unit.CharacterMissions.Runtime.Timers(resident.Run.Id));
            }
        }

        internal void ActorAvailable(MapChannel map, uint spawnId)
        {
            _missions.PublicActors.ActorAvailable(map);
            foreach (var run in _runs.Values.Where(run => run.Map == map &&
                run.Bindings.Actors.Values.Any(actor => actor.Kind == SceneActorKind.PublicSpawn &&
                    actor.TemplateId == spawnId)).ToArray())
                Reconcile(run.Run.Id);
        }

        internal void RecordDefeat(MapChannel map, Creature creature, Client credited)
        {
            var pool = creature.SpawnPool;
            if (pool?.SceneRunId == null || !_runs.TryGetValue(pool.SceneRunId, out var owner) ||
                owner.Map != map || pool.SceneGeneration != owner.Run.Generation ||
                !MapInstanceScope.Contains(map, creature))
                throw new GameplayRejectionException("Rejected a stale or wrong-map scene actor defeat.");
            IReadOnlyList<uint> recipients = Array.Empty<uint>();
            using (var unit = _factory.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var store = unit.CharacterMissions.Runtime;
                    var scene = store.Scene(owner.Run.Id);
                    if (scene == null || scene.Generation != owner.Run.Generation ||
                        scene.Status is "Resetting" or "Ended" or "Faulted")
                        throw new GameplayRejectionException("Rejected a terminated scene actor defeat.");
                    RequireCurrentAssignment(unit, scene);
                    if (store.ActorState(owner.Run.Id, pool.SceneActorRole, owner.Run.Generation) != null)
                        return;
                    store.Add(new MissionActorStateEntry
                    {
                        RunId = owner.Run.Id, ActorRole = pool.SceneActorRole, Generation = owner.Run.Generation,
                        OwnerCharacterId = owner.Run.OwnerCharacterId, MapContextId = map.MapInfo.MapContextId,
                        SharedKey = pool.SceneSharedKey
                    });
                    if (owner.Bindings.DefeatSequences.TryGetValue(pool.SceneActorRole, out var sequence))
                        store.Add(new MissionSceneMessageEntry
                        {
                            RunId = owner.Run.Id, Generation = owner.Run.Generation,
                            OperationKey = $"defeated-{pool.SceneActorRole}", SequenceId = sequence
                        });
                    if (credited != null)
                        recipients = _missions.Credit.FreezeWorld(unit, credited, MissionProgressEvent.Creature(creature.DbId),
                            creature.Position, Guid.NewGuid().ToString("N"), owner.Run.Id, owner.Run.Generation);
                });
            _missions.Credit.Schedule(recipients);
            if (credited != null)
                _missions.Credit.Deliver(credited);
            if (owner.Bindings.DefeatSequences.ContainsKey(pool.SceneActorRole))
                DrainMessages(owner.Run.Id);
        }

        internal void EndDeadline(Repositories.Char.ICharUnitOfWork unit, SceneRun run, uint missionId)
        {
            foreach (var timer in unit.CharacterMissions.Runtime.Timers(run.Id)
                .Where(timer => timer.MissionId == missionId && timer.ObjectiveId.HasValue && timer.Disposition == "Pending"))
            { timer.Disposition = "Cancelled"; timer.Version++; }
        }

        internal bool Execute(Client owner, uint missionId, uint sequenceId, bool started = false)
        {
            if (!_bindings.TryGetValue(missionId, out var binding) ||
                owner?.Player?.MapChannel == null || owner.State != Data.ClientState.Ingame ||
                owner.PendingTransfer != null || !owner.Player.Missions.TryGetValue(missionId, out var mission) ||
                mission.State is not (Data.MissionState.Active or Data.MissionState.Failed))
                return Reject($"Scene admission rejected for mission {missionId}.");
            string assignmentId;
            using (var unit = _factory.CreateChar())
            {
                var assignment = unit.CharacterMissions.GetByCharacterAndMission(owner.Player.Id, missionId);
                if (assignment == null || !mission.MatchesAssignment(
                    assignment.AssignmentId, assignment.Generation, assignment.ContentRevision))
                    return Reject("Scene assignment disappeared or the caller belongs to a different attempt.");
                if (IsJoinedAssignment(unit, assignment))
                    return Reject("A shared participant cannot execute the initiator's scene.");
                assignmentId = assignment.AssignmentId;
            }
            if (!_assignmentScenes.TryGetValue((owner.Player.Id, missionId, assignmentId), out var id))
            {
                using var unit = _factory.CreateChar();
                var assignment = unit.CharacterMissions.GetByCharacterAndMission(owner.Player.Id, missionId);
                if (assignment == null || !mission.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision))
                    return Reject("Scene assignment disappeared.");
                var existing = unit.CharacterMissions.Runtime.Scenes(owner.Player.Id, missionId)
                    .SingleOrDefault(scene => scene.AssignmentId == assignment.AssignmentId &&
                        scene.ScriptKey == binding.Script);
                if (existing == null)
                {
                    id = Start(owner, binding.Script, binding.Bindings, missionId);
                    if (started)
                        return true;
                }
                else
                {
                    id = existing.RunId;
                    if (existing.Status is "Ended" or "Faulted" or "Resetting")
                        return Reject($"Scene {id} is {existing.Status}; follow its authored retry policy.");
                    Attach(owner, id, binding.Bindings);
                }
            }
            if (started)
                return Submit(id, new SceneObservation(SceneEventKind.Started, _runs[id].Run.Generation));
            using (var unit = _factory.CreateChar())
                unit.ExecuteTransaction(() => QueueSequence(unit, owner, missionId, sequenceId));
            return DrainMessages(id);
        }

        internal void QueueNamedSequence(Repositories.Char.ICharUnitOfWork unit, Client owner, uint missionId, string name)
        {
            if (!_bindings.TryGetValue(missionId, out var binding) ||
                !binding.Bindings.Names.TryGetValue(name, out var sequence) ||
                !binding.Bindings.Sequences.ContainsKey(sequence))
                throw new GameplayRejectionException($"Mission {missionId} has no executable sequence {name}.");
            QueueSequence(unit, owner, missionId, sequence);
        }

        internal void QueueSequence(Repositories.Char.ICharUnitOfWork unit, Client owner, uint missionId, uint sequenceId)
        {
            if (!Owns(missionId))
                return;
            var assignment = unit.CharacterMissions.GetByCharacterAndMission(owner.Player.Id, missionId)
                ?? throw new GameplayRejectionException("Scene input has no assignment.");
            if (!owner.Player.Missions.TryGetValue(missionId, out var runtime) ||
                !runtime.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision))
                throw new GameplayRejectionException("Scene input caller belongs to a different attempt.");
            RequireSceneControl(unit, assignment);
            var store = unit.CharacterMissions.Runtime;
            var scene = store.AssignmentScene(assignment.AssignmentId);
            if (scene == null)
            {
                PrepareAssignment(unit, owner, assignment);
                scene = store.AssignmentScene(assignment.AssignmentId);
            }
            var key = $"sequence-{sequenceId}";
            if (!store.Messages(scene.RunId).Any(message => message.Generation == scene.Generation && message.OperationKey == key))
                store.Add(new MissionSceneMessageEntry
                {
                    RunId = scene.RunId, Generation = scene.Generation, OperationKey = key, SequenceId = sequenceId
                });
        }

        internal bool DrainMessages(string runId)
        {
            if (!_runs.TryGetValue(runId, out var resident))
                return false;
            MissionSceneMessageEntry[] messages;
            using (var unit = _factory.CreateChar())
                messages = unit.CharacterMissions.Runtime.Messages(runId)
                    .Where(message => message.Status == "Pending" && message.Generation == resident.Run.Generation).ToArray();
            var changed = false;
            _messageRetries.Remove(runId);
            foreach (var message in messages)
                changed |= Submit(runId, new SceneObservation(SceneEventKind.Signal, message.Generation,
                    SequenceId: message.SequenceId, DeliveryKey: message.OperationKey));
            return changed;
        }

        internal string Start(Client owner, string script, SceneBindings bindings, uint missionId = 0) =>
            Start(owner.Player.Id, owner.Player.MapChannel, owner, script, bindings, missionId);

        private string Start(uint characterId, MapChannel map, Client owner, string script, SceneBindings bindings, uint missionId)
        {
            var run = new SceneRun(Guid.NewGuid().ToString("N"), bindings.Release, script, 1,
                1, 0, "{}", SceneStatus.Running, characterId, missionId);
            var initial = _runtime.Evaluate(run, bindings,
                new SceneObservation(SceneEventKind.Started, run.Generation), _utcNow());
            if (!initial.Accepted)
                throw new GameplayRejectionException(initial.Rejection);
            using (var unit = _factory.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var assignment = missionId == 0 ? null :
                        unit.CharacterMissions.GetByCharacterAndMission(characterId, missionId);
                    if (missionId != 0 && (assignment == null || owner != null &&
                        (!owner.Player.Missions.TryGetValue(missionId, out var log) ||
                         !log.MatchesAssignment(assignment.AssignmentId, assignment.Generation, assignment.ContentRevision))))
                        throw new GameplayRejectionException("Scene start has no matching runtime assignment.");
                    if (assignment != null)
                        RequireSceneControl(unit, assignment);
                    unit.CharacterMissions.Runtime.Add(new MissionSceneEntry
                    {
                        RunId = run.Id, Release = run.Release, ScriptKey = run.ScriptKey, StateVersion = run.StateVersion,
                        OwnerCharacterId = run.OwnerCharacterId, MissionId = missionId,
                        MapKey = PublicActorLeaseService.MapKey(map),
                        AssignmentId = assignment?.AssignmentId ?? ""
                    });
                    if (assignment != null)
                        unit.CharacterMissions.Runtime.Add(new MissionSceneParticipantEntry
                        {
                            RunId = run.Id, CharacterId = characterId,
                            AssignmentId = assignment.AssignmentId, AssignmentGeneration = assignment.Generation
                        });
                });
            Attach(characterId, map, owner, run.Id, bindings);
            Submit(run.Id, new SceneObservation(SceneEventKind.Started, run.Generation));
            return run.Id;
        }

        internal void Attach(Client owner, string runId, SceneBindings bindings) =>
            Attach(owner.Player.Id, owner.Player.MapChannel, owner, runId, bindings);

        private void Attach(uint characterId, MapChannel map, Client owner, string runId, SceneBindings bindings)
        {
            if (_pauseRetries.ContainsKey(runId) && !PersistPause(runId))
                throw new GameplayRejectionException($"Scene {runId} cannot resume before its pending clock pause is persisted.");
            using var unit = _factory.CreateChar();
            var row = unit.CharacterMissions.Runtime.Scene(runId)
                ?? throw new GameplayRejectionException($"Scene run {runId} is unavailable.");
            if (row.OwnerCharacterId != characterId ||
                !map.IsPrivateInstance && row.MapKey != PublicActorLeaseService.MapKey(map))
                throw new GameplayRejectionException($"Scene run {runId} belongs to a different character/map.");
            var run = ReadRun(row);
            if (run.Status == SceneStatus.Resetting ||
                !map.IsPrivateInstance && run.Status is SceneStatus.Ended or SceneStatus.Faulted)
            {
                LeaseReset(runId);
                return;
            }
            if (bindings.Release != run.Release || !_runtime.Supports(run.ScriptKey, run.StateVersion))
                throw new GameplayRejectionException(
                    $"Scene run {runId} cannot load release {run.Release}, script {run.ScriptKey}/{run.StateVersion}.");
            unit.ExecuteTransaction(() =>
            {
                foreach (var timer in unit.CharacterMissions.Runtime.Timers(runId)
                    .Where(timer => timer.ClockPolicy == "ActiveScene" && timer.Disposition == "Paused"))
                {
                    if (!timer.RemainingTicks.HasValue || timer.RemainingTicks < 0)
                        throw new GameplayRejectionException($"Scene {runId} has an invalid paused timer {timer.Name}.");
                    timer.DueAtUtc = _utcNow().AddTicks(timer.RemainingTicks.Value);
                    timer.RemainingTicks = null; timer.Disposition = "Pending"; timer.Version++;
                }
            });
            var resident = new Resident(run, bindings, owner, map, row.AssignmentId);
            _runs[runId] = resident;
            if (run.MissionId != 0)
                _assignmentScenes[(run.OwnerCharacterId, run.MissionId, row.AssignmentId)] = runId;
            _world.Attach(run, bindings, owner, map);
            LoadTimers(resident, unit.CharacterMissions.Runtime.Timers(runId));
            Reconcile(runId, reconstruct: true);
        }

        internal bool Submit(string runId, SceneObservation observation)
        {
            lock (_dispatchGate)
                return SubmitCore(runId, observation);
        }

        private bool SubmitCore(string runId, SceneObservation observation)
        {
            _observations.Enqueue((runId, observation));
            if (_draining)
                return true;
            _draining = true;
            var accepted = false;
            try
            {
                var count = 0;
                while (_observations.Count > 0)
                {
                    var next = _observations.Dequeue();
                    if (++count > 128)
                    {
                        _observations.Clear();
                        throw new GameplayRejectionException($"Scene transition bound exceeded by run {next.RunId}.");
                    }
                    var committed = Commit(next.RunId, next.Observation);
                    if (!committed && next.Observation.DeliveryKey != null)
                        _messageRetries[next.RunId] = _utcNow().AddSeconds(1);
                    accepted |= committed;
                }
            }
            finally { _draining = false; }
            return accepted;
        }

        private bool Commit(string runId, SceneObservation observation)
        {
            if (!_runs.TryGetValue(runId, out var resident))
                return Reject($"Scene run {runId} is not attached.");
            if (resident.Suspended)
                return Reject($"Scene run {runId} is waiting for its owner.");
            var evaluation = _runtime.Evaluate(resident.Run, resident.Bindings, observation, _utcNow());
            if (!evaluation.Accepted)
                return Reject(evaluation.Rejection);
            var decision = evaluation.Decision;
            using var publication = new MissionScenarioPlan();
            var worldTargets = new HashSet<string>(StringComparer.Ordinal) { runId };
            var sharedVersions = new Dictionary<string, long>(StringComparer.Ordinal);
            MissionWorldEffectEntry observedEffect = null;
            var next = resident.Run with
            {
                Checkpoint = decision.Checkpoint, Status = decision.Status, Version = resident.Run.Version + 1,
                Generation = decision.ResetGeneration ? resident.Run.Generation + 1 : resident.Run.Generation
            };
            try
            {
                using var unit = _factory.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var store = unit.CharacterMissions.Runtime;
                    var scene = store.Scene(runId);
                    if (scene?.Version != resident.Run.Version || scene.Generation != observation.Generation)
                        throw new GameplayRejectionException($"Concurrent scene revision for run {runId}.");
                    RequireCurrentAssignment(unit, scene);
                    if (observation.DeliveryKey != null)
                    {
                        var message = store.Messages(runId).SingleOrDefault(message =>
                            message.Generation == observation.Generation && message.OperationKey == observation.DeliveryKey);
                        if (message?.Status != "Pending")
                            throw new GameplayRejectionException("Scene input was already handled or cancelled.");
                        message.Status = "Handled"; message.Version++;
                    }
                    if (observation.OperationKey != null && observation.Kind is
                        SceneEventKind.WaypointReached or SceneEventKind.RouteCompleted or SceneEventKind.Cancelled or SceneEventKind.ActorDied)
                    {
                        var effect = store.Effects(runId).SingleOrDefault(entry =>
                            entry.Generation == observation.Generation && entry.OperationKey == observation.OperationKey &&
                            entry.Status == "Running");
                        if (effect == null)
                            throw new GameplayRejectionException("Unmatched world operation callback.");
                        if (!HasCurrentEffectSource(unit, scene, effect))
                            throw new GameplayRejectionException("World operation callback belongs to a retired source attempt.");
                        observedEffect = effect;
                        if (observation.Kind == SceneEventKind.RouteCompleted)
                            effect.Status = "Applied";
                        else if (observation.Kind == SceneEventKind.WaypointReached &&
                            JsonSerializer.Deserialize<WorldIntent>(effect.Payload) is RunRouteIntent route &&
                            observation.Waypoint + 1 < resident.Bindings.Routes[route.Route].Points.Count)
                            effect.Payload = JsonSerializer.Serialize<WorldIntent>(route with { StartWaypoint = observation.Waypoint + 1 });
                        else if (observation.Kind is SceneEventKind.Cancelled or SceneEventKind.ActorDied)
                        { effect.Status = "Failed"; effect.Failure = observation.Name; }
                        store.Add(new MissionReceiptEntry
                        {
                            OwnerId = runId, Generation = observation.Generation, OperationKey = WorldResultKey(observation),
                            Kind = "WorldResult", CreatedAtUtc = _utcNow()
                        });
                    }
                    var timers = store.Timers(runId).ToDictionary(timer => timer.Name, StringComparer.Ordinal);
                    if (observation.Kind is SceneEventKind.TimerElapsed or SceneEventKind.ObjectiveDeadlineElapsed)
                    {
                        if (!timers.TryGetValue(observation.Name, out var timer) || timer.Generation != observation.Generation ||
                            timer.Disposition != "Pending" || timer.DueAtUtc > _utcNow())
                            throw new GameplayRejectionException("Scene timer is no longer due.");
                        timer.Disposition = "Fired"; timer.Version++;
                        if (timer.ObjectiveId.HasValue && timer.MissionId.HasValue)
                        {
                            var deadline = unit.CharacterMissionDeadlines.Get(resident.Run.OwnerCharacterId, timer.MissionId.Value);
                            if (deadline?.State != CharacterMissionDeadlineState.Active)
                                throw new GameplayRejectionException("The mission deadline is no longer active.");
                            unit.CharacterMissionDeadlines.SetState(resident.Run.OwnerCharacterId,
                                timer.MissionId.Value, CharacterMissionDeadlineState.Expired);
                            publication.AddProgressPlan(_missions.PlanProgress(resident.Owner,
                                new[] { MissionProgressEvent.Deadline(timer.MissionId.Value, timer.ObjectiveId.Value) }, unit));
                        }
                    }
                    scene.Checkpoint = next.Checkpoint; scene.Status = next.Status.ToString();
                    scene.Generation = next.Generation; scene.Version = next.Version; scene.Fault = decision.Fault;
                    if (decision.ResetGeneration || decision.Status is SceneStatus.Ended or SceneStatus.Faulted)
                    {
                        foreach (var timer in timers.Values)
                            timer.Disposition = "Cancelled";
                        foreach (var message in store.Messages(runId).Where(message => message.Status == "Pending"))
                        { message.Status = "Cancelled"; message.Version++; }
                        foreach (var effect in store.Effects(runId).Where(effect => effect.Status is "Pending" or "Running"))
                            effect.Status = "Cancelled";
                    }
                    foreach (var intent in decision.CharacterIntents)
                    {
                        var generation = intent is GrantRewardIntent or GrantAbilityIntent ? 0U : next.Generation;
                        var recorded = store.HasReceipt(runId, generation, intent.OperationKey);
                        if (recorded && !Rasa.Missions.Definitions.MissionItemValidation.IsItemIntent(intent) &&
                            intent is not OfferRadioMissionIntent)
                            continue;
                        _characters.Apply(resident.Owner, next, intent, unit, publication);
                        if (!recorded)
                            store.Add(new MissionReceiptEntry
                            {
                                OwnerId = runId, Generation = generation, OperationKey = intent.OperationKey,
                                Kind = "Grant", CreatedAtUtc = _utcNow()
                            });
                    }
                    var order = 0;
                    CharacterMissionEntry sourceAssignment = null;
                    foreach (var intent in decision.WorldIntents)
                    {
                        var target = resident;
                        var applied = intent;
                        if (intent.Role != null && resident.Run.MissionId != 0 &&
                            resident.Bindings.Actors[intent.Role].SharedKey is string sharedKey)
                        {
                            target = _runs.Values.SingleOrDefault(candidate => candidate.Map == resident.Map &&
                                candidate.Run.OwnerCharacterId == resident.Run.OwnerCharacterId && candidate.Run.MissionId == 0 &&
                                candidate.Bindings.Actors.Values.Any(actor => actor.SharedKey == sharedKey))
                                ?? throw new GameplayRejectionException($"Shared actor {sharedKey} has no experience owner.");
                            var role = target.Bindings.Actors.Values.Single(actor => actor.SharedKey == sharedKey).Role;
                            var key = "shared-" + Convert.ToHexString(SHA256.HashData(
                                Encoding.UTF8.GetBytes($"{runId}:{next.Generation}:{intent.OperationKey}"))).ToLowerInvariant();
                            applied = intent with { OperationKey = key, Role = role };
                            sourceAssignment ??= unit.CharacterMissions.GetByCharacterAndMission(
                                scene.OwnerCharacterId, scene.MissionId)
                                ?? throw new GameplayRejectionException("Forwarded world work has no source assignment.");
                            if (applied is RunRouteIntent route && !target.Bindings.Routes.ContainsKey(route.Route))
                                throw new GameplayRejectionException("Shared actor routes must be owned by their experience.");
                            if (!sharedVersions.ContainsKey(target.Run.Id))
                            {
                                var sharedScene = store.Scene(target.Run.Id);
                                if (sharedScene.Version != target.Run.Version)
                                    throw new GameplayRejectionException("Experience actor state changed concurrently.");
                                sharedScene.Version++;
                                sharedVersions.Add(sharedScene.RunId, sharedScene.Version);
                            }
                        }
                        var targetGeneration = target == resident ? next.Generation : target.Run.Generation;
                        var version = target == resident ? next.Version : sharedVersions[target.Run.Id];
                        worldTargets.Add(target.Run.Id);
                        if (!store.Effects(target.Run.Id).Any(effect =>
                            effect.Generation == targetGeneration && effect.OperationKey == applied.OperationKey))
                            store.Add(new MissionWorldEffectEntry
                            {
                                RunId = target.Run.Id, Generation = targetGeneration, OperationKey = applied.OperationKey,
                                SourceRunId = target == resident ? null : scene.RunId,
                                SourceGeneration = target == resident ? null : next.Generation,
                                SourceAssignmentId = target == resident ? null : sourceAssignment.AssignmentId,
                                SourceAssignmentGeneration = target == resident ? null : sourceAssignment.Generation,
                                Payload = JsonSerializer.Serialize<WorldIntent>(applied), Version = checked(version * 100 + order++)
                            });
                    }
                    foreach (var change in decision.Timers)
                    {
                        if (!timers.TryGetValue(change.Name, out var timer))
                        {
                            timer = new MissionTimerEntry { RunId = runId, Name = change.Name };
                            timers[change.Name] = timer; store.Add(timer);
                        }
                        // Reconciliation never extends an already-running wall clock.
                        if (timer.Disposition == "Pending" && timer.DueAtUtc.HasValue && !change.Cancel &&
                            timer.Generation == next.Generation)
                            continue;
                        timer.Generation = next.Generation; timer.ClockPolicy = change.ClockPolicy.ToString();
                        timer.SequenceId = change.SequenceId;
                        timer.DueAtUtc = change.DueAtUtc; timer.RemainingTicks = null;
                        timer.Disposition = change.Cancel ? "Cancelled" : "Pending"; timer.Version++;
                    }
                    foreach (var signal in decision.Signals)
                    {
                        var signalKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                            $"{runId}:{next.Generation}:{observation.Kind}:{observation.DeliveryKey ?? observation.OperationKey ?? observation.Name}:{observation.SequenceId}:{signal.MissionId}:{signal.SequenceId}:{signal.EventId}"))).ToLowerInvariant();
                        if (store.HasReceipt(runId, next.Generation, signalKey))
                            continue;
                        var progress = MissionProgressEvent.Scenario(signal.MissionId, signal.SequenceId, signal.EventId);
                        var source = SceneCreditSource(resident, unit, progress);
                        if (source != null)
                        {
                            var recipients = _missions.Credit.FreezeScene(unit, source, progress,
                                observation.Position == null
                                    ? resident.Owner?.Player?.MapChannel == resident.Map
                                        ? resident.Owner.Player.Position : resident.OwnerPosition :
                                    new System.Numerics.Vector3(observation.Position.X, observation.Position.Y, observation.Position.Z),
                                signalKey.Substring(0, 32), runId, next.Generation);
                            publication.AddRuntimeConvergence(() => _missions.Credit.Schedule(recipients));
                        }
                        else
                            publication.AddProgressPlan(_missions.PlanProgress(resident.Owner, new[] { progress }, unit));
                        store.Add(new MissionReceiptEntry
                        {
                            OwnerId = runId, Generation = next.Generation, OperationKey = signalKey,
                            Kind = "Signal", CreatedAtUtc = _utcNow()
                        });
                    }
                    {
                        var plannedGeneration = scene.Generation;
                        var plannedVersion = scene.Version;
                        Repositories.UnitOfWork.TransactionValidation.Add(unit, () =>
                        {
                            var current = store.ReadScene(runId);
                            if (current?.Generation != plannedGeneration || current.Version != plannedVersion)
                                throw new GameplayRejectionException("Scene changed during final item persistence.");
                            RequireCurrentAssignment(unit, current, persisted: true);
                            if (observedEffect != null && !HasCurrentEffectSource(unit, current, observedEffect))
                                throw new GameplayRejectionException("World operation source changed during final persistence.");
                        });
                    }
                });
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                return Reject($"Scene {runId} commit failed: {error}");
            }
            if (decision.ResetGeneration)
                _world.Terminate(resident.Run, resident.Map);
            resident.Run = next;
            foreach (var version in sharedVersions)
                _runs[version.Key].Run = _runs[version.Key].Run with { Version = version.Value };
            _world.Attach(next, resident.Bindings, resident.Owner, resident.Map);
            if (decision.Fault != null)
                Logger.WriteLog(LogType.Error, decision.Fault);
            if (publication.HasChanges)
                MissionApplication.TryPublish(() => publication.ApplyRuntime(resident.Owner, _manifestations, _missions),
                    $"scene {runId} committed publication");
            using (var unit = _factory.CreateChar())
                LoadTimers(resident, unit.CharacterMissions.Runtime.Timers(runId));
            foreach (var target in worldTargets)
                Reconcile(target);
            if (next.Status is SceneStatus.Ended or SceneStatus.Faulted)
            {
                _missions.PublicActors.BeginReset(resident.Map, runId, next.Status.ToString());
                _due.Cancel(runId);
                _world.Terminate(next, resident.Map);
            }
            return true;
        }

        private Client SceneCreditSource(Resident resident, Repositories.Char.ICharUnitOfWork unit, MissionProgressEvent progress)
        {
            if (resident.Owner?.Player?.MapChannel == resident.Map &&
                resident.Owner.State == Data.ClientState.Ingame && resident.Owner.PendingTransfer == null &&
                _missions.Credit.HasGroupRule(resident.Owner, progress))
                return resident.Owner;
            var participants = unit.CharacterMissions.Runtime.Participants(resident.Run.Id)
                .Where(participant => participant.Active).Select(participant => participant.CharacterId).ToHashSet();
            return resident.Map.ClientList.FirstOrDefault(client => client?.Player?.MapChannel == resident.Map &&
                client.State == Data.ClientState.Ingame && client.PendingTransfer == null &&
                participants.Contains(client.Player.Id) && _missions.Credit.HasGroupRule(client, progress));
        }

        internal void Reconcile(string runId, bool reconstruct = false)
        {
            if (!_runs.TryGetValue(runId, out var resident) || resident.Suspended)
                return;
            MissionWorldEffectEntry[] effects;
            HashSet<string> defeated;
            CharacterEntry savedCharacter = null;
            using (var unit = _factory.CreateChar())
            {
                var scene = unit.CharacterMissions.Runtime.Scene(runId);
                if (scene == null || scene.Generation != resident.Run.Generation || scene.Status == "Resetting" ||
                    !resident.Map.IsPrivateInstance && scene.Status is "Ended" or "Faulted")
                {
                    _worldRetries.Remove(runId);
                    return;
                }
                try
                {
                    RequireCurrentAssignment(unit, scene);
                }
                catch (GameplayRejectionException error)
                {
                    _worldRetries.Remove(runId);
                    Reject($"Scene {runId} reconciliation rejected: {error.Message}");
                    return;
                }
                effects = unit.CharacterMissions.Runtime.Effects(runId)
                    .Where(effect => effect.Generation == resident.Run.Generation && effect.Status != "Cancelled")
                    .OrderBy(effect => effect.Version).ToArray();
                var stale = effects.Where(effect => !HasCurrentEffectSource(unit, scene, effect)).ToArray();
                if (stale.Length > 0)
                {
                    unit.ExecuteTransaction(() =>
                    {
                        foreach (var effect in stale)
                            CancelStaleEffect(effect);
                    });
                    foreach (var effect in stale)
                    {
                        _world.CancelOperation(effect.RunId, effect.Generation, effect.OperationKey);
                        Reject($"Scene {runId} effect {effect.OperationKey} cancelled: {effect.Failure}");
                    }
                    effects = effects.Where(effect => effect.Status != "Cancelled").ToArray();
                }
                defeated = unit.CharacterMissions.Runtime.ActorStates(runId)
                    .Where(actor => actor.Generation == resident.Run.Generation && actor.Outcome == "Defeated")
                    .Select(actor => actor.ActorRole).ToHashSet(StringComparer.Ordinal);
                var shared = unit.CharacterMissions.Runtime.DefeatedSharedActors(
                    resident.Run.OwnerCharacterId, resident.Map.MapInfo.MapContextId).ToHashSet(StringComparer.Ordinal);
                foreach (var actor in resident.Bindings.Actors.Values.Where(actor => actor.SharedKey != null && shared.Contains(actor.SharedKey)))
                    defeated.Add(actor.Role);
                if (reconstruct && resident.Map.IsPrivateInstance)
                    savedCharacter = unit.Characters.Find(resident.Run.OwnerCharacterId);
            }
            var following = effects.Select(effect => JsonSerializer.Deserialize<WorldIntent>(effect.Payload))
                .OfType<FollowActorIntent>().GroupBy(intent => intent.Role)
                .Where(group => group.Last().Enabled).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
            _worldRetries.Remove(runId, out var retry);
            var acknowledgements = new Dictionary<string, EffectAcknowledgementRetry>(StringComparer.Ordinal);
            if (retry != null)
                foreach (var acknowledgement in retry.Acknowledgements.Values)
                    if (effects.Any(acknowledgement.Matches) && !HasSupersedingWorldEffect(acknowledgement, effects))
                        acknowledgements.Add(acknowledgement.OperationKey, acknowledgement);
                    else
                        Reject($"Scene {runId} effect {acknowledgement.OperationKey} acknowledgement retry retired: durable identity, status or desired command changed.");
            var selected = reconstruct ? DesiredEffects(effects) :
                effects.Where(effect => effect.Status == "Pending" || acknowledgements.ContainsKey(effect.OperationKey));
            foreach (var effect in selected)
            {
                WorldEffectResult result;
                WorldIntent appliedIntent = null;
                acknowledgements.TryGetValue(effect.OperationKey, out var acknowledgement);
                try
                {
                    var intent = acknowledgement?.Intent ?? JsonSerializer.Deserialize<WorldIntent>(effect.Payload);
                    if (intent.Role != null && defeated.Contains(intent.Role))
                        continue;
                    if (acknowledgement?.Intent == null && intent is EnsureActorIntent ensure && following.Contains(ensure.Role) &&
                        savedCharacter?.MapContextId == resident.Map.MapInfo.MapContextId)
                    {
                        var offset = resident.Bindings.Actors[ensure.Role].FollowOffset ?? new ScenePosition(0, 0, 0);
                        var position = new System.Numerics.Vector3((float)savedCharacter.CoordX + offset.X,
                            (float)savedCharacter.CoordY + offset.Y, (float)savedCharacter.CoordZ + offset.Z);
                        position = NavMeshManager.NearestWalkable(resident.Map, position)
                            ?? throw new GameplayRejectionException($"Scene {runId} has no grounded follower checkpoint.");
                        intent = ensure with { RestorePosition = new ScenePosition(position.X, position.Y, position.Z) };
                    }
                    if (reconstruct && acknowledgement?.Intent == null && intent is RunRouteIntent route &&
                        resident.Bindings.Routes[route.Route].ResumeAtDestination)
                    {
                        var destination = resident.Bindings.Routes[route.Route].Points.Last();
                        intent = new RestoreActorPoseIntent(route.OperationKey, route.Role, destination.Position, destination.Orientation);
                    }
                    appliedIntent = intent;
                    result = _world.Apply(resident.Run, intent);
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    result = WorldEffectResult.Failed(error.ToString());
                }
                using var unit = _factory.CreateChar();
                var sourceRetired = false;
                try
                {
                    unit.ExecuteTransaction(() =>
                    {
                        var current = unit.CharacterMissions.Runtime.Effects(runId)
                            .SingleOrDefault(candidate => candidate.Generation == effect.Generation &&
                                candidate.OperationKey == effect.OperationKey);
                        var scene = unit.CharacterMissions.Runtime.Scene(runId);
                        if (current == null || current.Status == "Cancelled" ||
                            acknowledgement != null && !acknowledgement.Matches(current) ||
                            scene == null || scene.Generation != current.Generation || scene.Status == "Resetting")
                        {
                            sourceRetired = true;
                            return;
                        }
                        if (!HasCurrentEffectSource(unit, scene, current))
                        {
                            CancelStaleEffect(current);
                            sourceRetired = true;
                            return;
                        }
                        current.Status = result.State switch
                        {
                            WorldEffectState.Applied => "Applied",
                            WorldEffectState.Running => "Running",
                            WorldEffectState.Suppressed => "Cancelled",
                            _ => "Pending"
                        };
                        current.Failure = result.Failure;
                        if (appliedIntent is RestoreActorPoseIntent)
                            current.Payload = JsonSerializer.Serialize<WorldIntent>(appliedIntent);
                        if (current.SourceRunId != null)
                            TransactionValidation.Add(unit, () =>
                            {
                                var owner = unit.CharacterMissions.Runtime.ReadScene(runId);
                                if (owner == null || owner.Generation != current.Generation || owner.Status == "Resetting" ||
                                    !HasCurrentEffectSource(unit, owner, current))
                                    throw new GameplayRejectionException("Forwarded effect source changed during acknowledgement.");
                            });
                    });
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    _world.CancelOperation(effect.RunId, effect.Generation, effect.OperationKey);
                    ScheduleWorldRetry(runId, effect.Status is "Running" or "Applied"
                        ? EffectAcknowledgementRetry.Capture(effect, appliedIntent) : null);
                    Reject($"Scene {runId} effect {effect.OperationKey} acknowledgement failed: {error}");
                    continue;
                }
                if (sourceRetired)
                {
                    _world.CancelOperation(effect.RunId, effect.Generation, effect.OperationKey);
                    Reject($"Scene {runId} effect {effect.OperationKey} lost its source before acknowledgement.");
                    continue;
                }
                if (result.State is WorldEffectState.Failed or WorldEffectState.Deferred)
                {
                    if (result.State == WorldEffectState.Failed)
                        Reject($"Scene {runId} effect {effect.OperationKey} remains pending: {result.Failure}");
                    ScheduleWorldRetry(runId);
                }
            }
        }

        private void ScheduleWorldRetry(string runId, EffectAcknowledgementRetry acknowledgement = null)
        {
            if (!_worldRetries.TryGetValue(runId, out var retry))
                _worldRetries.Add(runId, retry = new WorldRetry());
            retry.RetryAt = _utcNow().AddSeconds(1);
            if (acknowledgement != null)
                retry.Acknowledgements[acknowledgement.OperationKey] = acknowledgement;
        }

        private static bool HasSupersedingWorldEffect(EffectAcknowledgementRetry retry, IEnumerable<MissionWorldEffectEntry> effects)
        {
            var intent = JsonSerializer.Deserialize<WorldIntent>(retry.Payload);
            return effects.Where(effect => effect.Version > retry.Version)
                .Select(effect => JsonSerializer.Deserialize<WorldIntent>(effect.Payload))
                .Any(later => later.Role == intent.Role &&
                    (later is EnsureActorIntent or RemoveActorIntent || later.GetType() == intent.GetType() ||
                        IsControllerCommand(later) && IsControllerCommand(intent)));
        }

        private static bool IsControllerCommand(WorldIntent intent) =>
            intent is RunRouteIntent or RestoreActorPoseIntent or FollowActorIntent or AttackActorIntent;

        private static IEnumerable<MissionWorldEffectEntry> DesiredEffects(IEnumerable<MissionWorldEffectEntry> effects)
        {
            var decoded = effects.Select(effect => (Effect: effect, Intent: JsonSerializer.Deserialize<WorldIntent>(effect.Payload))).ToArray();
            foreach (var role in decoded.Where(entry => entry.Intent.Role != null).GroupBy(entry => entry.Intent.Role))
            {
                var presence = role.LastOrDefault(entry => entry.Intent is EnsureActorIntent or RemoveActorIntent);
                if (presence.Intent is not EnsureActorIntent)
                    continue;
                yield return presence.Effect;
                foreach (var property in role.Where(entry => entry.Effect.Version >= presence.Effect.Version &&
                        entry.Intent is not (EnsureActorIntent or RemoveActorIntent))
                    .GroupBy(entry => entry.Intent.GetType()))
                    yield return property.Last().Effect;
            }
        }

        private void ObserveWorld(string runId, SceneObservation observation)
        {
            if (!_runs.TryGetValue(runId, out var resident) || resident.Run.Generation != observation.Generation)
                return;
            using (var unit = _factory.CreateChar())
            {
                var store = unit.CharacterMissions.Runtime;
                var scene = store.ReadScene(runId);
                var effect = store.Effects(runId).SingleOrDefault(entry =>
                    entry.Generation == observation.Generation && entry.OperationKey == observation.OperationKey);
                if (scene == null || scene.Generation != observation.Generation || scene.Status == "Resetting" ||
                    effect == null || effect.Status == "Cancelled" || !HasCurrentEffectSource(unit, scene, effect))
                {
                    if (effect != null && effect.Status != "Cancelled")
                        unit.ExecuteTransaction(() => CancelStaleEffect(effect));
                    _world.CancelOperation(runId, observation.Generation, observation.OperationKey);
                    Reject($"Scene {runId} rejected world result {observation.OperationKey} from a retired operation.");
                    return;
                }
                if (store.HasReceipt(runId, observation.Generation, WorldResultKey(observation)))
                {
                    unit.ExecuteTransaction(() =>
                    {
                        if (observation.Kind == SceneEventKind.RouteCompleted)
                            effect.Status = "Applied";
                        if (effect.SourceRunId != null)
                            TransactionValidation.Add(unit, () =>
                            {
                                var owner = store.ReadScene(runId);
                                if (owner == null || owner.Generation != observation.Generation || owner.Status == "Resetting" ||
                                    !HasCurrentEffectSource(unit, owner, effect))
                                    throw new GameplayRejectionException("Forwarded world receipt lost its source before commit.");
                            });
                    });
                    return;
                }
            }
            if (!Submit(runId, observation))
                throw new GameplayRejectionException($"World result for scene {runId} was not committed; movement must retry.");
        }

        private static string WorldResultKey(SceneObservation observation) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"world-result:{observation.OperationKey}:{observation.Kind}:{observation.Waypoint}"))).ToLowerInvariant();

        internal bool Tick(MapChannel map, SceneTickScope scope = SceneTickScope.All)
        {
            var changed = false;
            foreach (var retry in _pauseRetries.Where(entry => entry.Value.RetryAt <= _utcNow()).ToArray())
                PersistPause(retry.Key);
            foreach (var retry in _terminationRetries.Where(entry => entry.Value <= _utcNow()).ToArray())
                FinishCancellation(retry.Key);
            if (scope != SceneTickScope.Deadlines)
                _world.Tick(map, _utcNow());
            foreach (var retry in _worldRetries.Where(entry => entry.Value.RetryAt <= _utcNow() &&
                _runs.TryGetValue(entry.Key, out var run) && run.Map == map).ToArray())
                Reconcile(retry.Key);
            foreach (var retry in _messageRetries.Where(entry => entry.Value <= _utcNow() &&
                _runs.TryGetValue(entry.Key, out var run) && run.Map == map).ToArray())
                DrainMessages(retry.Key);
            foreach (var due in _due.TakeDue(_utcNow()))
            {
                if (!_runs.TryGetValue(due.RunId, out var resident))
                    continue;
                if (resident.Map != map)
                { _due.Schedule(due); continue; }
                if (scope == SceneTickScope.Deadlines && !due.ObjectiveId.HasValue ||
                    scope == SceneTickScope.Scripts && due.ObjectiveId.HasValue)
                { _due.Schedule(due); continue; }
                if (!Submit(due.RunId, new SceneObservation(due.ObjectiveId.HasValue
                        ? SceneEventKind.ObjectiveDeadlineElapsed : SceneEventKind.TimerElapsed,
                    due.Generation, due.Name, SequenceId: due.SequenceId)))
                    _due.Schedule(due with { DueAtUtc = _utcNow().AddSeconds(1) });
                else
                    changed = true;
            }
            if (scope != SceneTickScope.Deadlines)
            {
                _missions.PublicActors.Tick(map);
                _missions.Credit.Tick(map);
            }
            return changed;
        }

        internal void Detach(uint characterId, MapChannel map) => Detach(characterId, map, null);

        internal void Detach(Client client, MapChannel map)
        {
            if (client?.Player != null)
                Detach(client.Player.Id, map, client);
        }

        private void Detach(uint characterId, MapChannel map, Client departing)
        {
            if (departing == null || !_resumed.TryGetValue((characterId, map), out var attached) ||
                ReferenceEquals(attached, departing))
            {
                _missions.Credit.Detach(characterId);
                _resumed.Remove((characterId, map));
            }
            foreach (var resident in _runs.Values.Where(run => run.Run.OwnerCharacterId == characterId && run.Map == map &&
                (departing == null || ReferenceEquals(run.Owner, departing))).ToArray())
            {
                var policy = map.IsPrivateInstance ? null :
                    _missions.PublicActors.OwnerLossPolicy(map, resident.Run.Id) ?? "Wait";
                resident.OwnerPosition = resident.Owner?.Player?.Position ?? resident.OwnerPosition;
                if (policy == "Continue")
                {
                    resident.Owner = null;
                    _world.Attach(resident.Run, resident.Bindings, null, map);
                    continue;
                }
                resident.Owner = null;
                resident.Suspended = true;
                _world.Attach(resident.Run, resident.Bindings, null, map);
                _world.Pause(resident.Run.Id);
                _due.Cancel(resident.Run.Id);
                _worldRetries.Remove(resident.Run.Id);
                _messageRetries.Remove(resident.Run.Id);
                _pauseRetries.TryAdd(resident.Run.Id, new PendingPause(resident.Run.Generation, _utcNow(), _utcNow()));
                PersistPause(resident.Run.Id);
                if (policy != null)
                {
                    if (policy == "Reset")
                        try
                        {
                            _missions.PublicActors.BeginReset(map, resident.Run.Id, "OwnerLost");
                        }
                        catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                        {
                            Reject($"Scene {resident.Run.Id} owner-loss reset remains pending: {error.Message}");
                        }
                    continue;
                }
                _world.Detach(resident.Run.Id);
                _runs.Remove(resident.Run.Id);
                _assignmentScenes.Remove((characterId, resident.Run.MissionId, resident.AssignmentId));
            }
        }

        private bool PersistPause(string runId)
        {
            if (!_pauseRetries.TryGetValue(runId, out var pause))
                return true;
            try
            {
                using var unit = _factory.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    var scene = unit.CharacterMissions.Runtime.Scene(runId);
                    if (scene?.Generation != pause.Generation || scene.Status is "Resetting" or "Ended" or "Faulted")
                        return;
                    foreach (var timer in unit.CharacterMissions.Runtime.Timers(runId).Where(timer =>
                        timer.Generation == pause.Generation && timer.ClockPolicy == "ActiveScene" && timer.Disposition == "Pending"))
                    {
                        timer.RemainingTicks = Math.Max(0, (timer.DueAtUtc.GetValueOrDefault() - pause.Cutoff).Ticks);
                        timer.DueAtUtc = null;
                        timer.Disposition = "Paused";
                        timer.Version++;
                    }
                });
                _pauseRetries.Remove(runId);
                return true;
            }
            catch (Exception error) when (GameplayRejectionException.IsExpected(error))
            {
                _pauseRetries[runId] = pause with { RetryAt = _utcNow().AddSeconds(1) };
                Reject($"Scene {runId} clock pause remains pending: {error.Message}");
                return false;
            }
        }

        private sealed record PendingPause(uint Generation, DateTime Cutoff, DateTime RetryAt);

        private sealed class WorldRetry
        {
            internal DateTime RetryAt { get; set; }
            internal Dictionary<string, EffectAcknowledgementRetry> Acknowledgements { get; } = new(StringComparer.Ordinal);
        }

        private sealed record EffectAcknowledgementRetry(
            string RunId, uint Generation, string OperationKey, string Status, long Version, string Payload,
            string SourceRunId, uint? SourceGeneration, string SourceAssignmentId, uint? SourceAssignmentGeneration,
            WorldIntent Intent)
        {
            internal static EffectAcknowledgementRetry Capture(MissionWorldEffectEntry effect, WorldIntent intent) =>
                new(effect.RunId, effect.Generation, effect.OperationKey, effect.Status, effect.Version, effect.Payload,
                    effect.SourceRunId, effect.SourceGeneration, effect.SourceAssignmentId, effect.SourceAssignmentGeneration, intent);
            internal bool Matches(MissionWorldEffectEntry effect) => this == Capture(effect, Intent);
        }

        private void LoadTimers(Resident resident, IReadOnlyList<MissionTimerEntry> timers)
        {
            _due.Cancel(resident.Run.Id);
            if (resident.Suspended)
                return;
            foreach (var timer in timers.Where(timer => timer.Generation == resident.Run.Generation &&
                timer.Disposition == "Pending" && timer.DueAtUtc.HasValue))
            {
                _due.Schedule(new SceneDueWork(resident.Run.Id, timer.Generation, timer.Name,
                    DateTime.SpecifyKind(timer.DueAtUtc.Value, DateTimeKind.Utc), timer.SequenceId, timer.ObjectiveId));
            }
        }

        private static SceneRun ReadRun(MissionSceneEntry entry) =>
            new(entry.RunId, entry.Release, entry.ScriptKey, entry.StateVersion, entry.Generation,
                entry.Version, entry.Checkpoint, Enum.Parse<SceneStatus>(entry.Status), entry.OwnerCharacterId, entry.MissionId);
        private static bool Reject(string message) { Logger.WriteLog(LogType.Error, message); return false; }
        private sealed class Resident
        {
            internal SceneRun Run { get; set; }
            internal SceneBindings Bindings { get; }
            internal Client Owner { get; set; }
            internal MapChannel Map { get; }
            internal string AssignmentId { get; }
            internal bool Suspended { get; set; }
            internal System.Numerics.Vector3 OwnerPosition { get; set; }
            internal Resident(SceneRun run, SceneBindings bindings, Client owner, MapChannel map, string assignmentId)
            {
                Run = run; Bindings = bindings; Owner = owner; Map = map; AssignmentId = assignmentId;
                OwnerPosition = owner?.Player?.Position ?? System.Numerics.Vector3.Zero;
            }
        }
    }
}
