using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Game.Missions.World
{
    using Data;
    using Managers;
    using global::Rasa.Missions.Scenes;
    using Structures;
    using Structures.World;

    internal enum WorldEffectState { Applied, Running, Failed, Suppressed, Deferred }
    internal sealed record WorldEffectResult(WorldEffectState State, string Failure = null)
    {
        internal static WorldEffectResult Applied() => new(WorldEffectState.Applied);
        internal static WorldEffectResult Running() => new(WorldEffectState.Running);
        internal static WorldEffectResult Failed(string failure) => new(WorldEffectState.Failed, failure);
        internal static WorldEffectResult Suppressed(string reason) => new(WorldEffectState.Suppressed, reason);
        internal static WorldEffectResult Deferred() => new(WorldEffectState.Deferred);
    }

    internal interface ISceneWorld
    {
        void Attach(SceneRun run, SceneBindings bindings, Client owner, MapChannel map);
        WorldEffectResult Apply(SceneRun run, WorldIntent intent);
        void Tick(MapChannel map, DateTime utcNow);
        void Detach(string runId);
        void Pause(string runId);
        void CancelOperation(string runId, uint generation, string operationKey);
        void Terminate(SceneRun run, MapChannel map);
    }

    internal sealed class SceneWorldAdapter : ISceneWorld
    {
        private readonly PublicActorLeaseService _leases;
        private readonly Dictionary<string, WorldRun> _runs = new(StringComparer.Ordinal);
        private readonly SceneRouteController _routes;
        internal SceneWorldAdapter(PublicActorLeaseService leases, Action<string, SceneObservation> observe)
        {
            _leases = leases;
            _routes = new SceneRouteController(ResolveCreature, observe);
        }

        public void Attach(SceneRun run, SceneBindings bindings, Client owner, MapChannel map)
        {
            if (map.IsPrivateInstance && map.OwnerCharacterId != run.OwnerCharacterId ||
                owner != null && !ReferenceEquals(owner.Player.MapChannel, map))
                throw new GameplayRejectionException($"Run {run.Id} cannot attach to this map.");
            if (_runs.TryGetValue(run.Id, out var existing) &&
                existing.Map == map && existing.Run.Generation == run.Generation)
            {
                existing.Owner = owner;
                existing.Run = run;
                return;
            }
            if (existing != null && run.Generation < existing.Run.Generation)
                throw new GameplayRejectionException($"Run {run.Id} cannot attach an older actor generation.");
            if (existing != null)
                Terminate(existing.Run, existing.Map);
            _runs[run.Id] = new WorldRun(run, bindings, owner, map);
        }

        public WorldEffectResult Apply(SceneRun run, WorldIntent intent)
        {
            if (!_runs.TryGetValue(run.Id, out var world) || world.Run.Generation != run.Generation)
                return WorldEffectResult.Failed("World run is not attached at the current generation.");
            if (intent is PresentationIntent presentation)
            {
                if (world.Owner?.State != ClientState.Ingame || world.Owner.Player.MapChannel != world.Map)
                    return WorldEffectResult.Failed("Presentation recipient is detached.");
                switch (presentation.Kind)
                {
                    case PresentationKind.Tutorial:
                        CommunicatorManager.Instance.DisplayPlayerTutorial(world.Owner, (TutorialId)presentation.Value);
                        break;
                    case PresentationKind.Audio:
                        CommunicatorManager.Instance.PlayTutorialAudio(world.Owner,
                            presentation.Value == 0 ? null : presentation.Value);
                        break;
                    case PresentationKind.Greeting:
                        world.Owner.CallMethod(world.Owner.Player.EntityId,
                            new Packets.MapChannel.Server.ForceConversePacket((int)presentation.Value));
                        break;
                }
                return WorldEffectResult.Applied();
            }
            if (intent is TransferIntent transfer)
            {
                var destination = MapChannelManager.Instance.FindOwnedPrivateInstance(
                    transfer.MapContextId, run.OwnerCharacterId) ??
                    MapChannelManager.Instance.FindByContextId(transfer.MapContextId);
                if (destination == null || world.Owner?.Player.MapChannel != world.Map)
                    return WorldEffectResult.Failed("Transfer source or destination is unavailable.");
                MapChannelManager.Instance.ChangeMap(world.Owner, destination,
                    SceneRouteController.Position(transfer.Position), (float)transfer.Orientation);
                return WorldEffectResult.Applied();
            }
            var definition = world.Bindings.Actors[intent.Role];
            if (intent is RestoreActorPoseIntent pose)
            {
                if (!world.Map.IsPrivateInstance || world.Map.OwnerCharacterId != run.OwnerCharacterId ||
                    definition.Kind != SceneActorKind.PublicSpawn)
                    return WorldEffectResult.Failed("Pose recovery is restricted to an owned private-map static actor.");
                var pool = world.Map.SpawnPools.SingleOrDefault(candidate => candidate.DbId == definition.TemplateId &&
                    candidate.ScenarioKey == null);
                if (pool == null)
                    return WorldEffectResult.Failed($"Static spawn {definition.TemplateId} is unavailable for pose recovery.");
                pool.Position = SceneRouteController.Position(pose.Position);
                pool.Rotation = pose.Orientation;
                pool.ScenePose = new SceneSpawnPose(
                    new ActorHandle(run.Id, pose.Role, run.Generation, world.Map.MissionEpoch),
                    run.OwnerCharacterId, pose.Position, pose.Orientation);
                var creature = world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Distinct().SingleOrDefault(candidate => ReferenceEquals(candidate.SpawnPool, pool));
                if (creature != null && !BehaviorManager.Instance.RestoreScriptedPose(
                        world.Map, creature, pool.Position, pool.Rotation))
                    return WorldEffectResult.Failed("Static actor pose could not be restored.");
                return WorldEffectResult.Applied();
            }
            if (intent is EnsureActorIntent ensure)
            {
                if (ensure.RestorePosition != null)
                {
                    if (!world.Map.IsPrivateInstance || definition.Kind != SceneActorKind.Creature)
                        return WorldEffectResult.Failed("Follower checkpoint placement requires an owned private-map creature.");
                    definition = definition with { Position = ensure.RestorePosition };
                }
                return Ensure(world, definition);
            }
            if (!world.Actors.TryGetValue(intent.Role, out var actor) || !IsCurrent(world, actor))
                TryBindExisting(world, definition, out actor);
            if (actor == null || !IsCurrent(world, actor))
                return intent is RemoveActorIntent ? WorldEffectResult.Applied() :
                    intent is SetInteractionIntent { IfPresent: true } ? WorldEffectResult.Suppressed("Optional actor is absent.") :
                    IsAwaitingPrivateSpawn(world, definition) ? WorldEffectResult.Deferred() :
                    WorldEffectResult.Failed($"Actor role {intent.Role} is unavailable.");
            if (intent is RemoveActorIntent)
            {
                _routes.Cancel(run.Id, intent.Role);
                if (definition.Kind == SceneActorKind.PublicSpawn)
                {
                    if (!world.Map.IsPrivateInstance)
                        _leases.BeginReset(world.Map, run.Id, "SceneRelease");
                    else
                        CreatureGameplayRules.ClearRole(actor.Creature, actor.Handle);
                }
                else if (actor.Creature != null)
                {
                    CellManager.Instance.RemoveCreatureFromWorld(world.Map, actor.Creature);
                    world.Map.SpawnPools.Remove(actor.Creature.SpawnPool);
                }
                else
                {
                    CellManager.Instance.RemoveFromWorld(world.Map, actor.Object);
                    world.Map.DynamicObjects.Remove(actor.Object);
                }
                world.Actors.Remove(intent.Role);
                world.Follows.Remove(intent.Role);
                world.Attacks.Remove(intent.Role);
                return WorldEffectResult.Applied();
            }
            if (intent is SetInteractionIntent interaction)
            {
                if (actor.Creature != null)
                    CreatureManager.Instance.SetScenarioInteractionEnabled(world.Map, actor.Creature, interaction.Enabled);
                else
                {
                    if (interaction.ObjectState.HasValue)
                        actor.Object.StateId = (UseObjectState)interaction.ObjectState.Value;
                    DynamicObjectManager.Instance.SetScenarioInteractionEnabled(world.Map, actor.Object, interaction.Enabled);
                }
                return WorldEffectResult.Applied();
            }
            if (intent is TransitionObjectStateIntent transition)
            {
                if (actor.Object == null || !Enum.IsDefined(typeof(UseObjectState), (int)transition.State))
                    return WorldEffectResult.Failed("Object-state transition requires a world object and supported native state.");
                var state = (UseObjectState)transition.State;
                if (actor.Object.StateId != state)
                {
                    actor.Object.StateId = state;
                    CellManager.Instance.CellCallMethod(world.Map, actor.Object,
                        new Packets.MapChannel.Server.UsePacket(world.Owner?.Player?.EntityId ?? 0, state,
                            checked((int)transition.WindupMilliseconds)));
                }
                return WorldEffectResult.Applied();
            }
            if (intent is RunRouteIntent route)
                return actor.Creature == null ? WorldEffectResult.Failed("Only a creature can follow a route.") :
                    _routes.Start(world.Map, actor.Handle, actor.Creature, route, world.Bindings.Routes[route.Route]);
            if (intent is AttackActorIntent attack)
            {
                Actor target;
                if (attack.TargetRole == null)
                {
                    if (world.Owner?.State != ClientState.Ingame || world.Owner.PendingTransfer != null ||
                        !CreatureManager.IsLivingOnMap(world.Map, world.Owner.Player))
                        return WorldEffectResult.Deferred();
                    target = world.Owner.Player;
                    if (world.Owner.Player.GmFlagAlwaysFriendly)
                        return WorldEffectResult.Suppressed("The owner is protected from hostile targeting.");
                }
                else
                {
                    if (!world.Actors.TryGetValue(attack.TargetRole, out var targetActor) ||
                        !IsCurrent(world, targetActor))
                        TryBindExisting(world, world.Bindings.Actors[attack.TargetRole], out targetActor);
                    target = targetActor?.Creature;
                }
                if (actor.Creature == null || !CreatureManager.IsLivingOnMap(world.Map, actor.Creature) ||
                    !CreatureManager.IsLivingOnMap(world.Map, target) ||
                    actor.Creature.Faction == (target is Creature enemy ? enemy.Faction : Factions.AFS))
                    return WorldEffectResult.Failed("Combat requires living hostile actors in the same runtime map.");
                BehaviorManager.Instance.SetActionFighting(actor.Creature, target.EntityId);
                world.Attacks[intent.Role] = (intent.OperationKey, target.EntityId);
                return WorldEffectResult.Applied();
            }
            if (intent is FollowActorIntent follow)
            {
                if (actor.Creature?.SpawnPool == null)
                    return WorldEffectResult.Failed("Escort actor has no stable spawn binding.");
                var pool = actor.Creature.SpawnPool;
                var targetCharacter = follow.CharacterId == 0 ? world.Run.OwnerCharacterId : follow.CharacterId;
                pool.FollowOwnerCharacterId = follow.Enabled ? targetCharacter : 0;
                pool.ScenarioOwnerCharacterId = targetCharacter;
                pool.ScenarioMissionId = definition.MissionId == 0 ? run.MissionId : definition.MissionId;
                if (follow.Enabled)
                {
                    BehaviorManager.Instance.SetActionFollow(actor.Creature, world.Owner?.Player.EntityId ?? 0);
                    world.Follows[intent.Role] = (intent.OperationKey, targetCharacter, world.Owner?.Player.EntityId ?? 0);
                }
                else
                {
                    BehaviorManager.Instance.SetActionAnchor(actor.Creature, actor.Creature.Position);
                    world.Follows.Remove(intent.Role);
                }
                CreatureManager.PublishEscortStatus(world.Map, actor.Creature, follow.Enabled);
                return WorldEffectResult.Applied();
            }
            return WorldEffectResult.Failed($"Unsupported world intent {intent.GetType().Name}.");
        }

        private WorldEffectResult Ensure(WorldRun world, SceneActorDefinition definition)
        {
            DynamicObject replaced = null;
            if (world.Actors.TryGetValue(definition.Role, out var existing) && IsCurrent(world, existing))
            {
                if (existing.Object == null || ObjectShapeMatches(existing.Object, definition))
                {
                    if (existing.Object?.StateId == UseObjectState.IdStateActive)
                        existing.Object.StateId = (UseObjectState)definition.InitialObjectState;
                    return WorldEffectResult.Applied();
                }
                if (existing.Object.MissionLootSource != null || existing.Object.LootDispenserEntityId != 0)
                    return WorldEffectResult.Failed("Cannot replace an actor that owns reward loot.");
                replaced = existing.Object;
                world.Actors.Remove(definition.Role);
            }
            var handle = new ActorHandle(world.Run.Id, definition.Role, world.Run.Generation, world.Map.MissionEpoch);
            if (definition.Kind == SceneActorKind.PublicSpawn)
            {
                Creature creature;
                if (world.Map.IsPrivateInstance)
                    creature = world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                        .Distinct().SingleOrDefault(candidate => candidate.SpawnPool?.DbId == definition.TemplateId);
                else
                {
                    handle = _leases.Handle(world.Map, definition.TemplateId);
                    if (handle?.RunId != world.Run.Id || handle.Role != definition.Role ||
                        handle.Generation != world.Run.Generation ||
                        !_leases.TryResolve(world.Map, handle, out creature) ||
                        !_leases.AttachPolicy(world.Map, handle, definition.GameplayPolicy))
                        return WorldEffectResult.Failed($"Actor {definition.Role} is not leased to this run.");
                }
                if (creature == null)
                    return IsAwaitingPrivateSpawn(world, definition) ? WorldEffectResult.Deferred() :
                        WorldEffectResult.Failed($"Public actor spawn {definition.TemplateId} is unavailable.");
                world.Actors[definition.Role] = new BoundActor(handle, creature, null);
                BindCreaturePolicy(world, definition, world.Actors[definition.Role]);
                return WorldEffectResult.Applied();
            }
            if (definition.Position == null)
                return WorldEffectResult.Failed($"Spawn role {definition.Role} has no position.");
            if (definition.Conversation != null &&
                EntityClassManager.Instance.GetClassInfo((EntityClasses)definition.TemplateId)?.Augmentations.Contains(AugmentationType.NPC) != true)
                return WorldEffectResult.Failed($"Conversation role {definition.Role} requires a native NPC augmentation.");
            if (definition.SharedKey != null && !world.Map.IsPrivateInstance)
                return WorldEffectResult.Failed("Shared experience roles require their actual private map.");
            var runtimeKey = definition.SharedKey == null
                ? $"run:{world.Run.Id}:generation:{world.Run.Generation}:actor:{definition.Role}"
                : $"experience:{world.Map.MissionEpoch:N}:actor:{definition.SharedKey}";
            if (definition.Kind == SceneActorKind.Creature)
            {
                var creature = world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Distinct().SingleOrDefault(candidate => candidate.SpawnPool?.ScenarioKey == runtimeKey);
                var created = creature == null;
                if (created)
                {
                    var pool = new SpawnPool
                    {
                        MapContextId = world.Map.MapInfo.MapContextId, RuntimeMapChannel = world.Map,
                        Position = SceneRouteController.Position(definition.Position), Rotation = definition.Orientation,
                        ScenarioKey = runtimeKey, ScenarioOwnerCharacterId = world.Run.OwnerCharacterId,
                        SceneRunId = world.Run.Id, SceneActorRole = definition.Role,
                        SceneGeneration = world.Run.Generation, SceneSharedKey = definition.SharedKey,
                        ScenarioMissionId = definition.MissionId == 0 ? world.Run.MissionId : definition.MissionId,
                        ScenarioGroupId = definition.GroupId, DbId = definition.SpawnId,
                        SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                        SpawnSlot = new List<SpawnPoolSlot> { new(definition.TemplateId, 1, 1) }
                    };
                    creature = CreatureManager.Instance.CreateScenarioCreature(pool, definition.TemplateId, pool.Position, pool.Rotation);
                    if (creature == null)
                        return WorldEffectResult.Failed($"Creature template {definition.TemplateId} could not spawn.");
                }
                world.Actors[definition.Role] = new BoundActor(handle, creature, null);
                BindCreaturePolicy(world, definition, world.Actors[definition.Role]);
                if (created)
                {
                    world.Map.SpawnPools.Add(creature.SpawnPool);
                    CellManager.Instance.AddToWorld(world.Map, creature);
                }
            }
            else
            {
                var obj = world.Map.DynamicObjects.SingleOrDefault(candidate => candidate.ScenarioKey == runtimeKey);
                if (obj != null && !ObjectShapeMatches(obj, definition))
                {
                    if (obj.MissionLootSource != null || obj.LootDispenserEntityId != 0)
                        return WorldEffectResult.Failed("Cannot replace an actor that owns reward loot.");
                    replaced = obj;
                    obj = null;
                }
                if (obj == null)
                {
                    obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(world.Map,
                        (EntityClasses)definition.TemplateId, SceneRouteController.Position(definition.Position),
                        definition.Orientation, runtimeKey, definition.InitiallyInteractable, definition.WindupMilliseconds);
                    obj.StateId = (UseObjectState)definition.InitialObjectState;
                    obj.SceneRunId = world.Run.Id;
                    obj.SceneOwnerCharacterId = world.Run.OwnerCharacterId;
                    obj.SceneMissionId = world.Run.MissionId;
                    obj.SceneActorRole = definition.Role;
                    obj.SceneGeneration = world.Run.Generation;
                    obj.MissionConversation = definition.Conversation;
                    if (definition.Kind == SceneActorKind.PracticeTarget)
                        obj.DynamicObjectType = DynamicObjectType.PracticeDummy;
                    if (definition.LootMissionId.HasValue && definition.LootRewardId.HasValue && definition.LootObjectiveId.HasValue)
                        obj.MissionLootSource = new MissionLootSource(definition.LootMissionId.Value,
                            definition.LootObjectiveId.Value, definition.LootRewardId.Value);
                    if (replaced != null)
                    {
                        CellManager.Instance.RemoveFromWorld(world.Map, replaced);
                        world.Map.DynamicObjects.Remove(replaced);
                    }
                    world.Map.DynamicObjects.Add(obj);
                    CellManager.Instance.AddToWorld(world.Map, obj);
                    obj.IsInWorld = true;
                }
                else if (obj.StateId == UseObjectState.IdStateActive)
                    obj.StateId = (UseObjectState)definition.InitialObjectState;
                world.Actors[definition.Role] = new BoundActor(handle, null, obj);
            }
            return WorldEffectResult.Applied();
        }

        private static bool IsAwaitingPrivateSpawn(WorldRun world, SceneActorDefinition definition)
        {
            if (!world.Map.IsPrivateInstance || definition.Kind != SceneActorKind.PublicSpawn)
                return false;
            var pool = world.Map.SpawnPools.SingleOrDefault(candidate =>
                candidate.DbId == definition.TemplateId && candidate.ScenarioKey == null);
            // Scene recovery precedes the normal spawn worker; an automatic pool can be
            // waiting on its first tick, a respawn cooldown, or a dropship delivery.
            return pool != null && ReferenceEquals(pool.RuntimeMapChannel, world.Map) &&
                pool.MapContextId == world.Map.MapInfo.MapContextId &&
                pool.SpawnPolicy != MissionSpawnGroupPolicy.ScenarioControlled && pool.Mode == 0 &&
                pool.AnimType is >= 0 and <= 2 && pool.AliveCreatures == 0 &&
                pool.SpawnSlot?.Any(slot => slot != null && slot.CreatureId != 0 &&
                    slot.CountMin >= 0 && slot.CountMax > 0 && slot.CountMax >= slot.CountMin &&
                    CreatureManager.Instance.LoadedCreatures.TryGetValue(slot.CreatureId, out var creature) &&
                    creature != null) == true;
        }

        private Creature ResolveCreature(MapChannel map, ActorHandle handle) =>
            _runs.TryGetValue(handle.RunId, out var world) && world.Map == map &&
            world.Actors.TryGetValue(handle.Role, out var actor) && actor.Handle == handle && IsCurrent(world, actor)
                ? actor.Creature : null;

        private bool TryBindExisting(WorldRun world, SceneActorDefinition definition, out BoundActor actor)
        {
            actor = null;
            if (definition.Kind == SceneActorKind.PublicSpawn)
            {
                if (Ensure(world, definition).State == WorldEffectState.Applied)
                    return world.Actors.TryGetValue(definition.Role, out actor);
                return false;
            }
            var key = definition.SharedKey == null
                ? $"run:{world.Run.Id}:generation:{world.Run.Generation}:actor:{definition.Role}"
                : $"experience:{world.Map.MissionEpoch:N}:actor:{definition.SharedKey}";
            if (definition.SharedKey != null && !world.Map.IsPrivateInstance)
                return false;
            var handle = new ActorHandle(world.Run.Id, definition.Role, world.Run.Generation, world.Map.MissionEpoch);
            if (definition.Kind == SceneActorKind.Creature)
            {
                var creature = world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Distinct().SingleOrDefault(candidate => candidate.SpawnPool?.ScenarioKey == key);
                if (creature != null)
                    actor = new BoundActor(handle, creature, null);
            }
            else
            {
                var obj = world.Map.DynamicObjects.SingleOrDefault(candidate => candidate.ScenarioKey == key);
                if (obj != null && ObjectShapeMatches(obj, definition))
                    actor = new BoundActor(handle, null, obj);
            }
            if (actor == null)
                return false;
            world.Actors[definition.Role] = actor;
            BindCreaturePolicy(world, definition, actor);
            return true;
        }

        private void BindCreaturePolicy(WorldRun world, SceneActorDefinition definition, BoundActor actor)
        {
            if (actor.Creature == null || definition.Kind == SceneActorKind.PublicSpawn && !world.Map.IsPrivateInstance)
                return;
            var pool = actor.Creature.SpawnPool;
            var ownership = (pool.SceneRunId, pool.SceneActorRole, pool.SceneGeneration, pool.SceneSharedKey, pool.ScenarioKey);
            CreatureGameplayRules.BindRole(actor.Creature, world.Map, actor.Handle, definition.GameplayPolicy,
                () => _runs.GetValueOrDefault(world.Run.Id) == world &&
                    world.Actors.GetValueOrDefault(definition.Role) == actor && IsCurrent(world, actor) &&
                    (pool.SceneRunId, pool.SceneActorRole, pool.SceneGeneration, pool.SceneSharedKey, pool.ScenarioKey) == ownership);
        }

        private static bool ObjectShapeMatches(DynamicObject obj, SceneActorDefinition definition) =>
            (uint)obj.EntityClassId == definition.TemplateId && definition.Position != null &&
            obj.Position == SceneRouteController.Position(definition.Position) && obj.Rotation == definition.Orientation &&
            obj.MissionConversation == definition.Conversation;

        private bool IsCurrent(WorldRun world, BoundActor actor) =>
            actor.Handle.Generation == world.Run.Generation && actor.Handle.MapEpoch == world.Map.MissionEpoch &&
            (actor.Creature != null
                ? MapInstanceScope.TryGetCreature(world.Map, actor.Creature.EntityId, out var current) &&
                  ReferenceEquals(current, actor.Creature) &&
                  (world.Map.IsPrivateInstance || world.Bindings.Actors[actor.Handle.Role].Kind != SceneActorKind.PublicSpawn ||
                   _leases.TryResolve(world.Map, actor.Handle, out _))
                : MapInstanceScope.Contains(world.Map, actor.Object));
        public void Tick(MapChannel map, DateTime utcNow) => _routes.Tick(map, utcNow);
        public void Detach(string runId)
        {
            _routes.Cancel(runId);
            if (_runs.Remove(runId, out var world))
                foreach (var actor in world.Actors.Values.Where(actor => actor.Creature != null &&
                    (world.Map.IsPrivateInstance || world.Bindings.Actors[actor.Handle.Role].Kind != SceneActorKind.PublicSpawn)))
                {
                    CreatureGameplayRules.ClearRole(actor.Creature, actor.Handle);
                    if (world.Bindings.Actors[actor.Handle.Role].SharedKey == null || actor.Creature.GameplayBinding != null)
                        continue;
                    foreach (var remaining in _runs.Values.Where(run => run.Map == world.Map))
                    {
                        var reference = remaining.Actors.Values.FirstOrDefault(candidate =>
                            candidate.Creature == actor.Creature && IsCurrent(remaining, candidate));
                        if (reference == null)
                            continue;
                        BindCreaturePolicy(remaining, remaining.Bindings.Actors[reference.Handle.Role], reference);
                        break;
                    }
                }
        }
        public void Pause(string runId) => _routes.Cancel(runId);
        public void CancelOperation(string runId, uint generation, string operationKey)
        {
            _routes.Cancel(runId, operationKey: operationKey, generation: generation);
            if (!_runs.TryGetValue(runId, out var world) || world.Run.Generation != generation)
                return;
            foreach (var control in world.Follows.Where(entry => entry.Value.OperationKey == operationKey).ToArray())
            {
                world.Follows.Remove(control.Key);
                if (!world.Actors.TryGetValue(control.Key, out var actor) || !IsCurrent(world, actor) ||
                    actor.Creature?.SpawnPool is not { } pool || pool.FollowOwnerCharacterId != control.Value.CharacterId)
                    continue;
                var creature = actor.Creature;
                var follow = creature.Controller.ActionFollow;
                var assisting = creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                    follow.OwnerAttackTarget?.EntityId == creature.Controller.ActionFighting.TargetEntityId &&
                    (!world.Attacks.TryGetValue(control.Key, out var attack) ||
                        attack.TargetId != creature.Controller.ActionFighting.TargetEntityId);
                pool.FollowOwnerCharacterId = 0;
                pool.FollowTargetEntityId = 0;
                follow.FollowTargetId = 0;
                follow.OwnerAttackTarget = null;
                follow.CatchUpRunning = false;
                follow.HasAnchor = true;
                follow.Anchor = creature.Position;
                if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFollow || assisting)
                {
                    creature.Controller.ActionFighting.TargetEntityId = 0;
                    creature.Target = 0;
                    BehaviorManager.Instance.SetActionAnchor(creature, creature.Position);
                }
                CreatureManager.PublishEscortStatus(world.Map, creature, false);
            }
            foreach (var control in world.Attacks.Where(entry => entry.Value.OperationKey == operationKey).ToArray())
            {
                world.Attacks.Remove(control.Key);
                if (!world.Actors.TryGetValue(control.Key, out var actor) || !IsCurrent(world, actor) ||
                    actor.Creature is not { } creature ||
                    creature.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting ||
                    creature.Controller.ActionFighting.TargetEntityId != control.Value.TargetId)
                    continue;
                creature.Controller.ActionFighting.TargetEntityId = 0;
                creature.Target = 0;
                if (world.Follows.TryGetValue(control.Key, out var follow))
                    BehaviorManager.Instance.SetActionFollow(creature, follow.TargetId);
                else
                    BehaviorManager.Instance.SetActionAnchor(creature, creature.Position);
            }
        }

        public void Terminate(SceneRun run, MapChannel map)
        {
            if (_runs.TryGetValue(run.Id, out var attached) && attached.Map == map &&
                attached.Run.Generation == run.Generation)
                Detach(run.Id);
            foreach (var creature in map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.SpawnPool?.SceneRunId == run.Id &&
                    creature.SpawnPool.SceneGeneration == run.Generation).ToArray())
            {
                CreatureGameplayRules.ClearRole(creature);
                if (map.IsPrivateInstance || creature.State != CharacterState.Dead || creature.CorpseLootEntityId == 0)
                    CellManager.Instance.RemoveCreatureFromWorld(map, creature);
            }
            map.SpawnPools.RemoveAll(pool => pool.SceneRunId == run.Id && pool.SceneGeneration == run.Generation);
            foreach (var obj in map.DynamicObjects.Where(obj => obj.SceneRunId == run.Id &&
                obj.SceneGeneration == run.Generation).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(map, obj);
                map.DynamicObjects.Remove(obj);
                obj.IsInWorld = false;
            }
        }

        private sealed record BoundActor(ActorHandle Handle, Creature Creature, DynamicObject Object);
        private sealed class WorldRun
        {
            internal SceneRun Run { get; set; }
            internal SceneBindings Bindings { get; }
            internal Client Owner { get; set; }
            internal MapChannel Map { get; }
            internal Dictionary<string, BoundActor> Actors { get; } = new(StringComparer.Ordinal);
            internal Dictionary<string, (string OperationKey, uint CharacterId, ulong TargetId)> Follows { get; } = new(StringComparer.Ordinal);
            internal Dictionary<string, (string OperationKey, ulong TargetId)> Attacks { get; } = new(StringComparer.Ordinal);
            internal WorldRun(SceneRun run, SceneBindings bindings, Client owner, MapChannel map)
            { Run = run; Bindings = bindings; Owner = owner; Map = map; }
        }
    }
}
