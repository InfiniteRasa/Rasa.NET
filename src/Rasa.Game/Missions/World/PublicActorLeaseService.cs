using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace Rasa.Game.Missions.World
{
    using Data;
    using Managers;
    using global::Rasa.Missions.Definitions;
    using global::Rasa.Missions.Scenes;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public sealed class PublicActorLeaseService
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly Func<MissionApplication> _missions;
        private readonly Action<string> _runReset;
        private readonly object _gate = new();
        private readonly Dictionary<uint, PublicEncounterBinding> _bindings = new();
        private readonly Dictionary<uint, ActorGameplayPolicy> _policies = new();
        private readonly Dictionary<(MapChannel Map, uint Spawn), Reservation> _reservations = new();
        private readonly Dictionary<(MapChannel Map, uint Spawn), Recovery> _recovery = new();
        private readonly HashSet<MapChannel> _recoveredMaps = new();

        internal PublicActorLeaseService(IGameUnitOfWorkFactory factory, Func<MissionApplication> missions,
            Action<string> runReset = null)
        {
            _factory = factory;
            _missions = missions;
            _runReset = runReset;
        }

        public void Bind(PublicEncounterBinding binding, ActorGameplayPolicy policy = null)
        {
            if (binding == null || binding.MissionId == 0 || binding.SpawnId == 0 ||
                string.IsNullOrWhiteSpace(binding.Role) || string.IsNullOrWhiteSpace(binding.ScriptKey) ||
                binding.OwnerLossPolicy is not ("Reset" or "Wait" or "Continue"))
                throw new ArgumentException("Public encounter binding is incomplete.", nameof(binding));
            var snapshot = policy?.Snapshot();
            _bindings.Add(binding.MissionId, binding);
            _policies.Add(binding.MissionId, snapshot);
        }

        internal bool HasBinding(uint missionId) => _bindings.ContainsKey(missionId);

        internal bool SupportsPartyJoining(uint missionId) =>
            _bindings.TryGetValue(missionId, out var binding) && binding.AllowPartyJoin &&
            _missions().Scenes.UsesScript(missionId, binding.ScriptKey);

        internal bool TryGetJoinRun(MapChannel map, CharacterMissionEntry assignment, ICharUnitOfWork unit,
            out ActorHandle handle)
        {
            handle = null;
            if (map == null || assignment == null || !SupportsPartyJoining(assignment.MissionId))
                return false;
            var binding = _bindings[assignment.MissionId];
            var current = JoinHandle(map, binding);
            if (current == null)
                return false;
            var store = unit.CharacterMissions.Runtime;
            var scene = store.ReadScene(current.RunId);
            var lease = store.ReadLease(MapKey(map), SpawnKey(binding.SpawnId));
            var participant = store.ReadParticipant(current.RunId, assignment.CharacterId);
            if (scene == null || scene.MissionId != assignment.MissionId ||
                scene.Generation != current.Generation || scene.Status is not ("Running" or "Waiting") ||
                scene.ScriptKey != binding.ScriptKey || scene.Release != assignment.ContentRevision ||
                scene.MapKey != MapKey(map) || lease?.RunId != scene.RunId ||
                lease.Generation != scene.Generation || lease.ActorRole != binding.Role || lease.State != "Reserved" ||
                participant?.Active != true || participant.AssignmentId != assignment.AssignmentId ||
                participant.AssignmentGeneration != assignment.Generation)
                return false;
            var owner = store.ReadAssignment(scene.OwnerCharacterId, scene.MissionId);
            var ownerParticipant = store.ReadParticipant(scene.RunId, scene.OwnerCharacterId);
            if (owner?.AssignmentId != scene.AssignmentId || owner.MissionState != (uint)MissionState.Active ||
                owner.ContentRevision != scene.Release || ownerParticipant?.Active != true ||
                ownerParticipant.AssignmentId != owner.AssignmentId || ownerParticipant.AssignmentGeneration != owner.Generation ||
                JoinHandle(map, binding) != current)
                return false;
            handle = current;
            return true;
        }

        private ActorHandle JoinHandle(MapChannel map, PublicEncounterBinding binding)
        {
            lock (_gate)
                return !map.IsPrivateInstance &&
                    _reservations.TryGetValue((map, binding.SpawnId), out var reservation) &&
                    reservation.Committed && !reservation.Resetting && reservation.ResetRequested == null &&
                    reservation.IsCurrent() && reservation.Actor.State is not (CharacterState.Dead or CharacterState.Dying)
                        ? reservation.Handle : null;
        }

        internal bool TryPrepare(Client client, uint missionId, string revision,
            out Reservation reservation, out string failure)
        {
            reservation = null;
            failure = null;
            if (!_bindings.TryGetValue(missionId, out var binding))
                return true;
            var map = client.Player.MapChannel;
            Recover(map);
            lock (_gate)
            {
                var actors = Actors(map, binding.SpawnId).ToArray();
                if (map.IsPrivateInstance || _reservations.ContainsKey((map, binding.SpawnId)) ||
                    _recovery.ContainsKey((map, binding.SpawnId)) ||
                    actors.Length != 1 || !actors[0].IsInteractable || actors[0].State == CharacterState.Dead)
                {
                    failure = $"Public actor spawn {binding.SpawnId} is unavailable or reserved.";
                    return false;
                }
                reservation = new Reservation(this, map, actors[0], binding, client.Player.Id, revision);
                _reservations.Add((map, binding.SpawnId), reservation);
                return true;
            }
        }

        public ActorHandle Handle(MapChannel map, uint spawnId)
        {
            lock (_gate)
                return _reservations.GetValueOrDefault((map, spawnId))?.Handle;
        }

        internal string OwnerLossPolicy(MapChannel map, string runId)
        {
            lock (_gate)
                return _reservations.Values.SingleOrDefault(entry =>
                    entry.Map == map && entry.Handle.RunId == runId && entry.Committed)?.Binding.OwnerLossPolicy;
        }

        public bool TryResolve(MapChannel map, ActorHandle handle, out Creature actor)
        {
            actor = null;
            if (handle == null || map?.MissionEpoch != handle.MapEpoch)
                return false;
            lock (_gate)
            {
                var current = _reservations.Values.SingleOrDefault(entry =>
                    ReferenceEquals(entry.Map, map) && entry.Committed && entry.Handle == handle);
                if (current == null || !current.IsCurrent())
                    return false;
                actor = current.Actor;
                return true;
            }
        }

        internal bool AttachPolicy(MapChannel map, ActorHandle handle, ActorGameplayPolicy policy)
        {
            lock (_gate)
            {
                var current = _reservations.Values.SingleOrDefault(entry =>
                    entry.Map == map && entry.Committed && !entry.Resetting && entry.Handle == handle);
                if (current == null || !current.IsCurrent())
                    return false;
                current.ApplyPolicy(policy);
                return true;
            }
        }

        internal bool BeginReset(string runId, string reason)
        {
            MapChannel map;
            lock (_gate)
                map = _reservations.Values.SingleOrDefault(entry => entry.Handle.RunId == runId && entry.Committed)?.Map;
            return map != null && BeginReset(map, runId, reason);
        }

        public bool BeginReset(MapChannel map, string runId, string reason)
        {
            Reservation reservation;
            lock (_gate)
                reservation = _reservations.Values.SingleOrDefault(entry =>
                    ReferenceEquals(entry.Map, map) && entry.Handle.RunId == runId && entry.Committed);
            if (reservation == null || reservation.Resetting)
                return false;
            reservation.ResetRequested = reason;
            uint generation = 0;
            using var unit = _factory.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                var store = unit.CharacterMissions.Runtime;
                var scene = store.Scene(runId);
                var lease = store.Lease(MapKey(map), SpawnKey(reservation.Binding.SpawnId));
                if (scene == null || lease?.RunId != runId)
                    throw new GameplayRejectionException($"Lease changed before reset of run {runId}.");
                if (scene.Status == "Resetting" && lease.State == "Resetting" && lease.Generation == scene.Generation)
                {
                    generation = scene.Generation;
                }
                else
                {
                    if (lease.Generation != reservation.Handle.Generation)
                        throw new GameplayRejectionException($"Lease generation changed before reset of run {runId}.");
                    scene.Generation++;
                    scene.Version++;
                    scene.Status = "Resetting";
                    scene.Fault = reason;
                    lease.Generation = scene.Generation;
                    lease.Version++;
                    lease.State = "Resetting";
                    generation = scene.Generation;
                }
                foreach (var effect in store.Effects(runId).Where(entry => entry.Status is "Pending" or "Running"))
                { effect.Status = "Cancelled"; effect.Version++; }
                foreach (var timer in store.Timers(runId).Where(timer => timer.Disposition is "Pending" or "Paused"))
                { timer.Disposition = "Cancelled"; timer.Version++; }
                foreach (var message in store.Messages(runId).Where(message => message.Status == "Pending"))
                { message.Status = "Cancelled"; message.Version++; }
            });
            CreatureGameplayRules.ClearRole(reservation.Actor, reservation.Handle);
            reservation.Handle = reservation.Handle with { Generation = generation };
            reservation.Resetting = true;
            reservation.ResetRequested = null;
            _runReset?.Invoke(runId);
            return true;
        }

        public void Tick(MapChannel map)
        {
            RecoverPending(map);
            Reservation[] runs;
            lock (_gate)
                runs = _reservations.Values.Where(entry => entry.Map == map && entry.Committed).ToArray();
            foreach (var run in runs)
            {
                try
                {
                    if (!run.Resetting && _missions().LoadedMissions.TryGetValue(run.Binding.MissionId, out var mission) &&
                        mission.RepeatPolicy.Kind != global::Rasa.Missions.Runtime.MissionRepeatKind.Once)
                    {
                        using var unit = _factory.CreateChar();
                        var assignment = unit.CharacterMissions.GetByCharacterAndMission(run.OwnerCharacterId, run.Binding.MissionId);
                        if (assignment == null || assignment.MissionState is 2 or 4)
                            BeginReset(map, run.Handle.RunId, "RepeatTerminal");
                    }
                    if (!run.Resetting && run.ResetRequested != null)
                        BeginReset(map, run.Handle.RunId, run.ResetRequested);
                    if (!run.Resetting && run.Binding.OwnerLossPolicy == "Reset" &&
                        !map.ClientList.Any(client => client?.Player?.Id == run.OwnerCharacterId &&
                            client.Player.MapChannel == map && client.State == ClientState.Ingame &&
                            client.PendingTransfer == null))
                        BeginReset(map, run.Handle.RunId, "OwnerLost");
                    if (!run.Resetting)
                        continue;
                    _runReset?.Invoke(run.Handle.RunId);
                    if (!run.IsCurrent() || run.Actor.State == CharacterState.Dead)
                    {
                        var replacement = Actors(map, run.Binding.SpawnId)
                            .SingleOrDefault(actor => actor.State != CharacterState.Dead);
                        if (replacement == null)
                            continue;
                        CreatureGameplayRules.ClearRole(run.Actor);
                        run.Actor = replacement;
                        run.Actor.IsInteractable = false;
                        run.AwaitingRespawn = false;
                    }
                    if (Vector3.Distance(run.Actor.Position, run.Home) > 0.5f)
                    {
                        if (!BehaviorManager.Instance.SetActionScriptedMove(map, run.Actor, run.Home, run.Orientation))
                            RespawnAfterBlockedReturn(run);
                        continue;
                    }
                    CompleteReset(run);
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error, $"Public run {run.Handle.RunId} reset failed: {error}");
                }
            }
        }

        internal void Recover(MapChannel map)
        {
            if (map.IsPrivateInstance || _bindings.Count == 0)
                return;
            lock (_gate)
                if (_recoveredMaps.Contains(map))
                    return;
            using var unit = _factory.CreateChar();
            foreach (var scene in unit.CharacterMissions.Runtime.Scenes(MapKey(map)))
            {
                var lease = unit.CharacterMissions.Runtime.Leases(scene.RunId).SingleOrDefault();
                if (lease == null)
                    continue;
                if (!_bindings.TryGetValue(scene.MissionId, out var binding) ||
                    lease.SpawnKey != SpawnKey(binding.SpawnId))
                {
                    Logger.WriteLog(LogType.Error, $"Cannot recover public run {scene.RunId}: missing actor binding.");
                    continue;
                }
                lock (_gate)
                {
                    if (_reservations.ContainsKey((map, binding.SpawnId)))
                        continue;
                    _recovery[(map, binding.SpawnId)] = new Recovery(
                        scene.RunId, scene.OwnerCharacterId, scene.Release, lease.Generation, binding);
                }
            }
            lock (_gate)
                _recoveredMaps.Add(map);
            RecoverPending(map);
        }

        internal bool IsReserved(MapChannel map, uint spawnId)
        {
            lock (_gate)
                return _reservations.ContainsKey((map, spawnId)) || _recovery.ContainsKey((map, spawnId));
        }

        internal void ActorAvailable(MapChannel map) => RecoverPending(map);

        private void RecoverPending(MapChannel map)
        {
            KeyValuePair<(MapChannel Map, uint Spawn), Recovery>[] pending;
            lock (_gate)
                pending = _recovery.Where(entry => entry.Key.Map == map).ToArray();
            foreach (var entry in pending)
            {
                var actor = Actors(map, entry.Key.Spawn).SingleOrDefault();
                if (actor == null)
                    continue;
                try
                {
                    lock (_gate)
                    {
                        if (!_reservations.ContainsKey(entry.Key))
                        {
                            _reservations.Add(entry.Key, new Reservation(this, map, actor, entry.Value.Binding,
                                entry.Value.Owner, entry.Value.Release)
                            {
                                Handle = new ActorHandle(entry.Value.Run, entry.Value.Binding.Role,
                                    entry.Value.Generation, map.MissionEpoch),
                                Committed = true
                            });
                            actor.IsInteractable = false;
                        }
                    }
                    BeginReset(map, entry.Value.Run, "ServerRestart");
                    lock (_gate)
                        _recovery.Remove(entry.Key);
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error, $"Public lease {entry.Value.Run} recovery remains pending: {error}");
                }
            }
        }

        private void RespawnAfterBlockedReturn(Reservation run)
        {
            if (run.AwaitingRespawn)
                return;
            var pool = run.Actor.SpawnPool;
            if (pool?.SpawnSlot == null || pool.SpawnSlot.Sum(slot => slot.CountMax) != 1 ||
                !run.IsCurrent() || run.Actor.CorpseLootEntityId != 0)
                throw new GameplayRejectionException($"Run {run.Handle.RunId} cannot safely respawn its reserved actor.");
            using (var unit = _factory.CreateChar())
            {
                var lease = unit.CharacterMissions.Runtime.Lease(MapKey(run.Map), SpawnKey(run.Binding.SpawnId));
                if (lease?.RunId != run.Handle.RunId || lease.Generation != run.Handle.Generation)
                    throw new GameplayRejectionException("Actor lease changed before reset respawn.");
            }
            if (!CellManager.Instance.RemoveCreatureFromWorld(run.Map, run.Actor))
                throw new GameplayRejectionException("Reserved actor disappeared before reset respawn.");
            SpawnPoolManager.Instance.DecreaseAliveCreatureCount(run.Map, pool);
            pool.UpdateTimer = 0;
            run.AwaitingRespawn = true;
            Logger.WriteLog(LogType.Debug,
                $"Run {run.Handle.RunId} is resetting {run.Binding.Role} through its authored {pool.RespawnTime}ms respawn.");
        }

        private void CompleteReset(Reservation run)
        {
            using var unit = _factory.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                var store = unit.CharacterMissions.Runtime;
                var lease = store.Lease(MapKey(run.Map), SpawnKey(run.Binding.SpawnId));
                if (lease?.RunId != run.Handle.RunId || lease.Generation != run.Handle.Generation ||
                    !run.IsCurrent())
                    throw new GameplayRejectionException("Stale actor reset.");
                store.Remove(lease);
                var scene = store.Scene(run.Handle.RunId);
                scene.Status = "Ended";
                scene.Version++;
            });
            CreatureGameplayRules.ClearRole(run.Actor, run.Handle);
            run.Actor.Controller.ScriptedMove = null;
            BehaviorManager.Instance.SetActionAnchor(run.Actor, run.Home);
            if (run.Actor.SpawnPool != null)
            {
                run.Actor.SpawnPool.FollowOwnerCharacterId = 0;
                run.Actor.SpawnPool.FollowTargetEntityId = 0;
                run.Actor.SpawnPool.ScenarioOwnerCharacterId = 0;
            }
            run.Actor.Rotation = run.Orientation;
            PublishInteraction(run, true);
            lock (_gate)
                _reservations.Remove((run.Map, run.Binding.SpawnId));
        }

        private void PublishInteraction(Reservation run, bool enabled)
        {
            run.Actor.IsInteractable = enabled;
            foreach (var client in run.Map.ClientList.Where(client => client?.Player?.MapChannel == run.Map).ToArray())
                MissionApplication.TryPublish(
                    () => NpcManager.Instance.UpdateConversationStatus(client, run.Actor, _missions()),
                    $"public actor {run.Binding.Role} availability for run {run.Handle.RunId}");
        }

        internal static string MapKey(MapChannel map) => $"{map.MapInfo.MapContextId}:{map.InstanceId}";
        private static string SpawnKey(uint id) => $"spawn:{id}";
        private static IEnumerable<Creature> Actors(MapChannel map, uint spawnId) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(actor => actor.SpawnPool?.DbId == spawnId && actor.SpawnPool.ScenarioKey == null &&
                    MapInstanceScope.Contains(map, actor));

        private sealed record Recovery(string Run, uint Owner, string Release, uint Generation, PublicEncounterBinding Binding);

        internal sealed class Reservation : IDisposable
        {
            private readonly PublicActorLeaseService _service;
            internal MapChannel Map { get; }
            internal Creature Actor { get; set; }
            internal PublicEncounterBinding Binding { get; }
            internal ActorGameplayPolicy Policy { get; }
            internal ActorHandle Handle { get; set; }
            internal uint OwnerCharacterId { get; }
            internal string Revision { get; }
            internal Vector3 Home { get; }
            internal double Orientation { get; }
            internal bool Committed { get; set; }
            internal bool Resetting { get; set; }
            internal string ResetRequested { get; set; }
            internal bool AwaitingRespawn { get; set; }

            internal Reservation(PublicActorLeaseService service, MapChannel map, Creature actor,
                PublicEncounterBinding binding, uint owner, string revision)
            {
                _service = service; Map = map; Actor = actor; Binding = binding;
                Policy = service._policies.GetValueOrDefault(binding.MissionId);
                OwnerCharacterId = owner; Revision = revision;
                Home = actor.SpawnPool.Position; Orientation = actor.SpawnPool.Rotation;
                Handle = new ActorHandle(Guid.NewGuid().ToString("N"), binding.Role, 1, map.MissionEpoch);
            }

            internal bool IsCurrent() => Map.MissionEpoch == Handle.MapEpoch &&
                MapInstanceScope.TryGetCreature(Map, Actor.EntityId, out var current) &&
                ReferenceEquals(Actor, current);

            internal void Persist(ICharUnitOfWork unit, CharacterMissionEntry assignment)
            {
                if (!IsCurrent() || !Actor.IsInteractable)
                    throw new GameplayRejectionException("Public actor changed before reservation.");
                var store = unit.CharacterMissions.Runtime;
                if (store.Lease(MapKey(Map), SpawnKey(Binding.SpawnId)) != null)
                    throw new GameplayRejectionException("Public actor has a durable lease.");
                store.Add(new MissionSceneEntry
                {
                    RunId = Handle.RunId, Release = Revision, ScriptKey = Binding.ScriptKey,
                    StateVersion = 1, OwnerCharacterId = OwnerCharacterId, MissionId = Binding.MissionId,
                    MapKey = MapKey(Map), Generation = Handle.Generation, AssignmentId = assignment.AssignmentId
                });
                store.Add(new MissionActorLeaseEntry
                {
                    MapKey = MapKey(Map), SpawnKey = SpawnKey(Binding.SpawnId),
                    RunId = Handle.RunId, ActorRole = Binding.Role, Generation = Handle.Generation
                });
                store.Add(new MissionSceneParticipantEntry
                {
                    RunId = Handle.RunId, CharacterId = OwnerCharacterId,
                    AssignmentId = assignment.AssignmentId, AssignmentGeneration = assignment.Generation
                });
                if (Binding.IncludeEligibleParty)
                    _service._missions().Credit.CaptureParticipants(unit, Map, OwnerCharacterId,
                        Binding.MissionId, Handle.RunId, Actor.Position);
                store.Add(new MissionWorldEffectEntry
                {
                    RunId = Handle.RunId, Generation = Handle.Generation, OperationKey = "lease-actor",
                    Payload = JsonSerializer.Serialize<WorldIntent>(new EnsureActorIntent("lease-actor", Binding.Role)),
                    Version = 1
                });
                store.Add(new MissionWorldEffectEntry
                {
                    RunId = Handle.RunId, Generation = Handle.Generation, OperationKey = "reserve-actor",
                    Payload = JsonSerializer.Serialize<WorldIntent>(
                        new SetInteractionIntent("reserve-actor", Binding.Role, false)),
                    Version = 2
                });
                store.Flush();
                if (!IsCurrent() || !Actor.IsInteractable)
                    throw new GameplayRejectionException("Public actor changed while saving the reservation.");
            }

            internal void Commit()
            {
                Committed = true;
                ApplyPolicy(Policy);
                _service.PublishInteraction(this, false);
            }

            internal void ApplyPolicy(ActorGameplayPolicy policy)
            {
                var handle = Handle;
                var actor = Actor;
                var pool = actor.SpawnPool;
                CreatureGameplayRules.BindRole(actor, Map, handle, policy,
                    () => !Resetting && pool.DbId == Binding.SpawnId && pool.ScenarioKey == null &&
                        _service.TryResolve(Map, handle, out var current) &&
                        ReferenceEquals(current, actor));
            }

            public void Dispose()
            {
                if (Committed)
                    return;
                lock (_service._gate)
                    if (_service._reservations.GetValueOrDefault((Map, Binding.SpawnId)) == this)
                        _service._reservations.Remove((Map, Binding.SpawnId));
            }
        }
    }
}
