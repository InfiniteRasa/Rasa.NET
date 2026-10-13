using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    public class SpawnPoolManager
    {
        private static SpawnPoolManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public readonly Dictionary<uint, SpawnPool> LoadedSpawnPools = new Dictionary<uint, SpawnPool>();

        /// <summary>spawnpool.mode: the pool spawns on its own timer. Every pool before mode was used.</summary>
        public const short ModeAutomatic = 0;

        /// <summary>
        /// spawnpool.mode: the pool is a control point's Bane garrison. At a control point the
        /// Bane camp is the control point itself, and the hospital, token banker and vendors the
        /// client labels "(Control Point)" are what stands there once AFS has taken it; the two
        /// never stand together. The pool runs while the Bane hold the point it is linked to
        /// (ControlPoints, control_point_link) and is dormant otherwise - as is one linked to no
        /// point at all.
        /// </summary>
        public const short ModeControlPoint = 1;

        /// <summary>
        /// spawnpool.mode: spawned only when something asks for it - a script, a GM. Also where a
        /// mined pool goes that stood on a place players revive at and could not be moved blind.
        /// </summary>
        public const short ModeScripted = 2;

        /// <summary>How close a hostile pool's area may come to a friendly NPC, a hospital or a waypoint.</summary>
        public const float SafeClearance = 15f;

        /// <summary>
        /// How close a hostile pool's ground may come to a turret: a creature's scan
        /// (Creature.AggroRange). Nearer, the turret and the camp find each other and fight for
        /// as long as the server runs.
        /// </summary>
        public const float TurretScan = 18f;

        public static SpawnPoolManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new SpawnPoolManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        internal SpawnPoolManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        /// <summary>
        /// A pool has nothing alive, queued or on its way: its respawn time starts. In a squad's
        /// instance it is dead from this moment, and comes back by the instance's clock instead
        /// (SquadInstanceState).
        /// </summary>
        private static void Emptied(SpawnPool spawnPool)
        {
            spawnPool.UpdateTimer = 0;

            if (spawnPool.Mode == ModeAutomatic && spawnPool.SpawnPolicy != Structures.World.MissionSpawnGroupPolicy.ScenarioControlled)
                spawnPool.RuntimeMapChannel?.SquadState?.PoolCleared(spawnPool);
        }

        /// <summary>
        /// A pool has something alive, queued or on its way again - its respawn, or a creature
        /// of it put back on its feet: in a squad's instance it is no longer dead.
        /// </summary>
        private static void Filled(SpawnPool spawnPool)
        {
            if (spawnPool.ClearedAtUtcMs != 0)
                spawnPool.RuntimeMapChannel?.SquadState?.PoolRespawned(spawnPool);
        }

        public void IncreaseQueueCount(SpawnPool spawnPool)
        {
            spawnPool.DropshipQueue++;
            Filled(spawnPool);
        }

        public void DecreaseQueueCount(SpawnPool spawnPool)
        {
            spawnPool.DropshipQueue--;

            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                Emptied(spawnPool);
        }

        public void IncreaseQueuedCreatureCount(SpawnPool spawnPool, int count)
        {
            spawnPool.QueuedCreatures += count;

            if (count > 0)
                Filled(spawnPool);
        }

        internal void DecreaseQueuedCreatureCount(SpawnPool spawnPool, int count)
        {
            spawnPool.QueuedCreatures -= count;

            if (spawnPool.QueuedCreatures == 0)
                spawnPool.QueuedCreatureList = null;

            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                Emptied(spawnPool);
        }

        public void IncreaseAliveCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.AliveCreatures++;
            Filled(spawnPool);
        }

        internal void DecreaseAliveCreatureCount(MapChannel mapChannel, SpawnPool spawnPool)
        {
            spawnPool.AliveCreatures--;
            if ((spawnPool.DropshipQueue + spawnPool.QueuedCreatures + spawnPool.AliveCreatures) == 0)
                Emptied(spawnPool);
        }

        public void IncreaseDeadCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.DeadCreatures++;
        }

        internal void DecreaseDeadCreatureCount(SpawnPool spawnPool)
        {
            spawnPool.DeadCreatures--;
        }

        public void SpawnPoolInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var spawnPoolList = unitOfWork.Spawnpools.Get();

            foreach (var data in spawnPoolList)
            {
                var spawnPoolSlots = new List<SpawnPoolSlot>();

                if (data.Creature1Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature1Id, data.Creature1MinCount, data.Creature1MaxCount));
                if (data.Creature2Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature2Id, data.Creature2MinCount, data.Creature2MaxCount));
                if (data.Creature3Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature3Id, data.Creature3MinCount, data.Creature3MaxCount));
                if (data.Creature4Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature4Id, data.Creature4MinCount, data.Creature4MaxCount));
                if (data.Creature5Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature5Id, data.Creature5MinCount, data.Creature5MaxCount));
                if (data.Creature6Id > 0)
                    spawnPoolSlots.Add(new SpawnPoolSlot(data.Creature6Id, data.Creature6MinCount, data.Creature6MaxCount));

                // spawnpool.respawn_time is in tenths of a second: 900 is a minute and a half.
                var respawnMilliseconds = data.RespawnTime * 100L;
                var spawnPool = new SpawnPool
                {
                    AnimType = data.AnimType,
                    MapContextId = data.MapContextId,
                    DbId = data.Id,
                    Position = data.Position,
                    Rotation = (float)data.Rotation,
                    Radius = (float)data.Radius,
                    Mode = data.Mode,
                    RespawnTime = respawnMilliseconds,
                    UpdateTimer = respawnMilliseconds,
                    SpawnSlot = spawnPoolSlots
                };

                LoadedSpawnPools.Add(data.Id, spawnPool);
            }

            var arrivals = 0;

            foreach (var arrival in unitOfWork.SpawnPoolArrivals.GetArrivals())
            {
                if (!LoadedSpawnPools.TryGetValue(arrival.PoolId, out var pool))
                {
                    Logger.WriteLog(LogType.Error, $"spawnpool_arrival {arrival.Id} names spawnpool {arrival.PoolId}, which is not loaded.");
                    continue;
                }

                pool.Arrivals.Add(arrival);
                arrivals++;
            }

            // How a pool's creatures stand at their post (spawnpool_pose; NpcPoses).
            var poses = 0;

            foreach (var row in unitOfWork.Spawnpools.GetPoses())
            {
                if (!LoadedSpawnPools.TryGetValue(row.Id, out var pool))
                {
                    Logger.WriteLog(LogType.Error, $"spawnpool_pose {row.Id} names a spawnpool that is not loaded.");
                    continue;
                }

                pool.Pose = NpcPoses.FromRow(row.Id, row.Pose);

                if (pool.Pose != NpcPose.None)
                    poses++;
            }

            // The beat a pool's creature walks (spawnpool_patrol; Patrols).
            var patrols = 0;

            foreach (var (poolId, steps) in Patrols.FromRows(unitOfWork.Spawnpools.GetPatrols()))
            {
                if (!LoadedSpawnPools.TryGetValue(poolId, out var pool))
                {
                    Logger.WriteLog(LogType.Error, $"spawnpool_patrol {poolId} names a spawnpool that is not loaded.");
                    continue;
                }

                // A pose is for standing at a post.
                if (pool.Pose != NpcPose.None)
                {
                    Logger.WriteLog(LogType.Error, $"SpawnPool {poolId} has a patrol and a pose ({pool.Pose}). It patrols, and has no pose.");
                    pool.Pose = NpcPose.None;
                    poses--;
                }

                pool.Patrol = steps;
                patrols++;
            }

            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
                InitializeMapChannel(mapChannel);

            Logger.WriteLog(LogType.Initialize, $"Loaded {LoadedSpawnPools.Count} SpawnPools, {arrivals} arrival points, {poses} posed, {patrols} patrolling");
        }

        internal void InitializeMapChannel(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            mapChannel.SpawnPools.Clear();
            foreach (var template in LoadedSpawnPools.Values.Where(pool => pool.MapContextId == mapChannel.MapInfo.MapContextId))
                mapChannel.SpawnPools.Add(CloneSpawnPool(template, mapChannel));
        }

        internal void CloneTemplateMap(MapChannel template, MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            mapChannel.SpawnPools.Clear();
            var source = template?.SpawnPools?.Count > 0
                ? template.SpawnPools
                : LoadedSpawnPools.Values.Where(pool => pool.MapContextId == mapChannel.MapInfo.MapContextId);

            foreach (var spawnPool in source)
                if (spawnPool != null)
                    mapChannel.SpawnPools.Add(CloneSpawnPool(spawnPool, mapChannel));
        }

        // timePassed is elapsed milliseconds since this map's previous spawn-pool update.
        public void SpawnPoolWorker(MapChannel mapChannel, long timePassed)
        {
            BaneArrivals.TeleportWorker(mapChannel, timePassed);

            var spawnPools = mapChannel.SpawnPools.Count > 0
                ? mapChannel.SpawnPools
                : LoadedSpawnPools.Values.Where(pool => pool.MapContextId == mapChannel.MapInfo.MapContextId).ToList();

            foreach (var spawnPool in spawnPools)
            {
                if (spawnPool.SpawnPolicy == Structures.World.MissionSpawnGroupPolicy.ScenarioControlled)
                    continue;

                // A scripted pool is not on a timer; nor is a pool set aside for a control point
                // that no control point has (ControlPoints). A garrison is, while its side holds
                // the point - and not while the other does, whatever its mode.
                if (spawnPool.Suspended
                    || spawnPool.Mode != ModeAutomatic && !(spawnPool.Mode == ModeControlPoint && spawnPool.IsGarrison)
                    || spawnPool.AnimType < 0 || spawnPool.AnimType > 2)
                    continue;

                if (spawnPool.AliveCreatures > 0 || spawnPool.QueuedCreatures > 0 || spawnPool.DropshipQueue > 0)
                    continue;

                // A Bane garrison comes back together: a pool of it that has been killed waits
                // until the whole garrison has been down long enough (ControlPoints.HoldsBack).
                if (spawnPool.IsGarrison && ControlPoints.Instance.HoldsBack(spawnPool))
                    continue;

                // In a squad's instance a pool that has been cleared comes back a set time after
                // the last of it died, by the clock on the wall, and its own respawn time is not
                // waited for (SquadInstanceState). It is cleared no longer once something of it
                // is queued or alive (Filled).
                var squadState = spawnPool.Mode == ModeAutomatic ? mapChannel.SquadState : null;

                if (squadState != null)
                {
                    if (!squadState.RespawnDue(spawnPool))
                        continue;

                    spawnPool.UpdateTimer = spawnPool.RespawnTime;
                }

                if (spawnPool.UpdateTimer < spawnPool.RespawnTime)
                    spawnPool.UpdateTimer += Math.Min(Math.Max(0, timePassed), spawnPool.RespawnTime - spawnPool.UpdateTimer);

                if (spawnPool.UpdateTimer < spawnPool.RespawnTime)
                    continue; // spawnpool is still on cooldown

                // A turret's wreck is still on its mount: it is put back in service where it
                // stands rather than a second one set down in the wreckage (AlternateMesh).
                if (AlternateMesh.ReviveWrecks(mapChannel, spawnPool))
                    continue;

                // A pool of an area with no walkable ground in it spawns nothing (Anchor).
                if (!HasWalkableGround(mapChannel, spawnPool))
                    continue;

                // create list of creatures to spawn
                var creatureList = CreateListOfCreatures(spawnPool);

                if (creatureList.Count == 0)
                    continue; // nothing to spawn

                // An arrival point: through a teleporter, or off a dropship on a pad or in a bay.
                var arrival = BaneArrivals.Pick(spawnPool);

                spawnPool.HasSpawned = true;

                if (arrival?.Kind == SpawnPoolArrivalEntry.KindTeleporter)
                    BaneArrivals.BeginTeleport(mapChannel, spawnPool, arrival, creatureList.Count);
                else if (arrival?.Kind == SpawnPoolArrivalEntry.KindDropship)
                    EnqueueDropship(mapChannel, spawnPool, creatureList, arrival);
                else if (spawnPool.AnimType == 0)    // animType==0; spawn without animation
                {
                    IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);

                    try
                    {
                        SpawnCreatures(spawnPool, creatureList);
                    }
                    finally
                    {
                        DecreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                    }
                }
                else
                {
                    EnqueueDropship(mapChannel, spawnPool, creatureList);
                }
            }
        }

        /// <param name="arrival">An arrival point the dropship lands on (a pad, a bay); null: the pool's own ground.</param>
        private void EnqueueDropship(MapChannel mapChannel, SpawnPool spawnPool, List<Creature> creatureList, SpawnPoolArrivalEntry arrival = null)
        {
            Dropship dropship = null;
            var reserved = false;
            try
            {
                dropship = new Dropship(arrival != null || spawnPool.AnimType == 1 ? TargetCategory.Hostile : TargetCategory.Friendly,
                    DropshipType.Spawner, spawnPool);

                if (arrival != null)
                {
                    dropship.Position = arrival.Position;
                    dropship.Rotation = arrival.Rotation;
                    dropship.Arrival = arrival;
                }

                spawnPool.QueuedCreatureList = creatureList;
                IncreaseQueueCount(spawnPool);
                IncreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                reserved = true;
                CellManager.Instance.AddToWorld(mapChannel, dropship);
                DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);
            }
            catch
            {
                try
                {
                    if (dropship != null)
                        RollBackDropship(mapChannel, dropship);
                }
                finally
                {
                    if (reserved)
                    {
                        DecreaseQueuedCreatureCount(spawnPool, creatureList.Count);
                        DecreaseQueueCount(spawnPool);
                    }
                }
                throw;
            }
        }

        private void RollBackDropship(MapChannel mapChannel, Dropship dropship)
        {
            var entities = EntityManager.Instance;
            var wasRegistered = entities.RegisteredEntities.TryGetValue(dropship.EntityId, out var entityType) &&
                entityType == EntityType.Object &&
                entities.DynamicObjects.TryGetValue(dropship.EntityId, out var registered) && registered == dropship;
            var workers = DynamicObjectManager.Instance.Dropships;
            if (workers.TryGetValue(dropship.EntityId, out var worker) && worker == dropship)
                workers.Remove(dropship.EntityId);
            try
            {
                if (wasRegistered)
                    CellManager.Instance.RemoveFromWorld(mapChannel, dropship);
            }
            catch (Exception exception)
            {
                Logger.WriteLog(LogType.Error, $"Failed to remove rejected dropship {dropship.EntityId}: {exception.Message}");
            }
            finally
            {
                if (mapChannel.MapCellInfo?.Cells != null)
                    foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                        cell?.DynamicObjectList?.RemoveAll(candidate => candidate == dropship);

                var hasObject = entities.DynamicObjects.TryGetValue(dropship.EntityId, out var current);
                var ownsObject = hasObject && current == dropship;
                if (ownsObject)
                    entities.UnregisterDynamicObject(dropship.EntityId);

                if (ownsObject || (!wasRegistered && !hasObject))
                {
                    if (entities.RegisteredEntities.ContainsKey(dropship.EntityId))
                        entities.ReleaseEntity(dropship.EntityId, EntityType.Object);
                    else
                        entities.FreeEntity(dropship.EntityId);
                }
            }
        }

        /// <param name="arrival">Where they arrive, when they come by an arrival point: they step out there and walk to the pool's ground.</param>
        internal void SpawnCreatures(SpawnPool spawnPool, List<Creature> creatureList, SpawnPoolArrivalEntry arrival = null)
        {
            var mapChannel = spawnPool.RuntimeMapChannel ??
                MapChannelManager.Instance.FindByContextId(spawnPool.MapContextId);

            foreach (var spawnSlot in creatureList)
            {
                var creature = CreatureManager.Instance.CreateCreature(spawnSlot.DbId, spawnPool);

                if (creature == null)
                    continue;

                try
                {
                    if (arrival == null)
                        RandomizePosition(creature, creatureList.Count);
                    else
                        CreatureManager.Instance.SetLocation(creature, BaneArrivals.StepOut(mapChannel, arrival), arrival.Rotation, spawnPool.MapContextId);

                    // Its pose at its post (NpcPoses). One that arrives somewhere else takes it up when it gets there.
                    if (spawnPool.Pose != NpcPose.None)
                        NpcPoses.Assign(creature, spawnPool.Pose, (float)spawnPool.Rotation, show: arrival == null);

                    // Its beat (Patrols), which it sets off on at its first think: from here,
                    // facing the way it was placed, or from wherever it walks in to.
                    if (spawnPool.Patrol != null)
                    {
                        creature.Patrol = spawnPool.Patrol;
                        creature.LastYaw = (float)creature.Rotation;
                    }

                    if (spawnPool.FollowOwnerCharacterId != 0 ||
                        spawnPool.FollowTargetEntityId != 0)
                        BehaviorManager.Instance.SetActionFollow(
                            creature,
                            spawnPool.FollowTargetEntityId);
                    CellManager.Instance.AddToWorld(mapChannel, creature);

                    if (arrival != null)
                        BehaviorManager.Instance.WalkIn(creature, SpawnPoint(mapChannel, spawnPool, creatureList.Count));

                    MissionApplication.Instance.Scenes.ActorAvailable(mapChannel, spawnPool.DbId);
                }
                catch
                {
                    RollBackSpawn(mapChannel, creature);
                    throw;
                }
            }
        }

        private void RollBackSpawn(MapChannel mapChannel, Creature creature)
        {
            var removed = false;
            try
            {
                removed = CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creature);
            }
            catch (Exception exception)
            {
                Logger.WriteLog(LogType.Error, $"Failed to remove rejected spawn {creature.EntityId}: {exception.Message}");
            }
            finally
            {
                if (!removed)
                {
                    if (mapChannel.MapCellInfo?.Cells != null)
                        foreach (var cell in mapChannel.MapCellInfo.Cells.Values)
                            cell?.CreatureList?.RemoveAll(candidate => candidate == creature);

                    var entities = EntityManager.Instance;
                    if (entities.RegisteredEntities.ContainsKey(creature.EntityId))
                        entities.ReleaseEntity(creature.EntityId, EntityType.Creature);
                    else
                        entities.FreeEntity(creature.EntityId);
                }
                DecreaseAliveCreatureCount(mapChannel, creature.SpawnPool);
            }
        }

        internal List<Creature> CreateListOfCreatures(SpawnPool spawnPool)
        {
            return spawnPool.QueuedCreatureList ??
                CreateListOfCreatures(spawnPool, CreatureManager.Instance.LoadedCreatures, Random.Shared);
        }

        public static List<Creature> CreateListOfCreatures(SpawnPool spawnPool,
            IReadOnlyDictionary<uint, Creature> definitions, Random random)
        {
            var creatureList = new List<Creature>();

            if (spawnPool.SpawnSlot == null)
                return creatureList;

            foreach (var spawnSlot in spawnPool.SpawnSlot)
            {
                if (creatureList.Count == 64)
                    break;

                if (spawnSlot == null || spawnSlot.CreatureId == 0)
                    continue;

                if (spawnSlot.CountMin < 0 || spawnSlot.CountMax < spawnSlot.CountMin)
                {
                    Logger.WriteLog(LogType.Error, $"SpawnPool {spawnPool.DbId}: invalid counts for creature {spawnSlot.CreatureId}");
                    continue;
                }

                if (spawnSlot.CountMax == 0)
                    continue;

                if (!definitions.TryGetValue(spawnSlot.CreatureId, out var definition) || definition == null)
                {
                    Logger.WriteLog(LogType.Error, $"SpawnPool {spawnPool.DbId}: missing creature {spawnSlot.CreatureId}");
                    continue;
                }

                var spawnCreatureCount = random.Next(spawnSlot.CountMin, spawnSlot.CountMax + 1);

                // The loaded template itself, once per creature to make: SpawnCreatures reads its
                // DbId and CreateCreature makes the real one. A new Creature per entry took an
                // entity id that nothing ever freed.
                for (var i = 0; i < spawnCreatureCount && creatureList.Count < 64; i++)
                    creatureList.Add(definition);
            }

            return creatureList;
        }

        internal void RandomizePosition(Creature creature, int count)
        {
            var pool = creature.SpawnPool;
            var mapChannel = pool.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(pool.MapContextId);

            // An emplacement stands on its mount, which is exactly where its pool is.
            var pos = Emplacements.Is(creature) ? pool.Position : SpawnPoint(mapChannel, pool, count, creature);

            // A mission scene's recovered pose, in the private instance it belongs to.
            var map = pool.RuntimeMapChannel;
            var pose = pool.ScenePose;
            if (pose != null && map?.IsPrivateInstance == true &&
                pose.OwnerCharacterId == map.OwnerCharacterId && pose.Handle.MapEpoch == map.MissionEpoch)
            {
                var authored = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
                if (Vector3.Distance(pos, authored) >= 0.5f)
                    throw new GameplayRejectionException("Authored recovered pose is not on its spawn's grounded surface.");
                pos = authored;
                creature.Controller.ScriptedMove = new ScriptedMove
                    { Destination = pos, Orientation = pose.Orientation, Arrived = true };
                creature.Controller.CurrentAction = BehaviorManager.BehaviorActionScriptedMove;
                creature.IsRunning = false;
            }
            CreatureManager.Instance.SetLocation(creature, pos, pool.Rotation, pool.MapContextId);
        }

        /// <summary>
        /// How far from a pool's point, sideways, walkable ground is looked for when the point
        /// itself is off the navmesh: a camp authored on a rock the mesh did not take, beside
        /// ground it did. Further than this is somewhere else.
        /// </summary>
        public const float AnchorReach = 64f;

        /// <summary>
        /// How far above or below the edge of a pool's area walkable ground is looked for: a
        /// pool whose area overlaps a slope or a ledge anchors there. The point itself is looked
        /// under and over for 200 m (NavMeshManager.NearestInColumn), for a height that is only a
        /// guess; the edge for less, so that a floor far below a cliff top is not taken for it.
        /// </summary>
        public const float AnchorEdgeHeight = 32f;

        private const int AnchorEdgeSamples = 8;

        /// <summary>
        /// Walkable ground for a pool whose point is off the navmesh: the nearest within
        /// <see cref="AnchorReach"/> sideways or 200 m up or down from the point, or within
        /// <see cref="AnchorEdgeHeight"/> of the edge of its area - whichever is nearest the
        /// point. Null when there is none, or no navmesh.
        /// </summary>
        internal static Vector3? Anchor(MapChannel mapChannel, SpawnPool pool)
        {
            if (mapChannel?.NavMesh == null)
                return null;

            var pos = pool.Position;
            var best = NavMeshManager.NearestWalkable(mapChannel, pos, Math.Max(AnchorReach, pool.Radius))
                       ?? NavMeshManager.NearestInColumn(mapChannel, pos);
            var bestDistance = best.HasValue ? Vector3.Distance(pos, best.Value) : float.MaxValue;

            if (pool.Radius > 0)
                for (var step = 0; step < AnchorEdgeSamples; step++)
                {
                    var angle = step * 2.0 * Math.PI / AnchorEdgeSamples;
                    var probe = new Vector3(pos.X + (float)(Math.Cos(angle) * pool.Radius), pos.Y, pos.Z + (float)(Math.Sin(angle) * pool.Radius));
                    var candidate = NavMeshManager.NearestInColumn(mapChannel, probe, AnchorEdgeHeight);

                    if (!candidate.HasValue)
                        continue;

                    var distance = Vector3.Distance(pos, candidate.Value);

                    if (distance < bestDistance)
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }

            return best;
        }

        /// <summary>
        /// Whether an automatic pool with an area has walkable ground to put its creatures on.
        /// One that has none is marked (SpawnPool.NoWalkableGround) and logged, once: what it
        /// put down went to the raw point plus a throw of the area, in the air or under the
        /// ground - eleven such pools on a fresh database - and stood there, unable to path and
        /// still shooting at whoever passed. A map without a navmesh has nothing to say.
        /// </summary>
        internal static bool HasWalkableGround(MapChannel mapChannel, SpawnPool pool)
        {
            if (pool.NoWalkableGround)
                return false;

            if (pool.Radius <= 0 || mapChannel?.NavMesh == null
                || NavMeshManager.RandomPointAround(mapChannel, pool.Position, pool.Radius).HasValue
                || Anchor(mapChannel, pool).HasValue)
                return true;

            pool.NoWalkableGround = true;

            Logger.WriteLog(LogType.Error,
                $"SpawnPool {pool.DbId} on map {mapChannel.MapInfo.MapContextId} at {pool.Position} (radius {pool.Radius}) has no walkable ground within {AnchorReach} m of its point or {AnchorEdgeHeight} m of its edge; it spawns nothing. Move it onto the navmesh.");

            return false;
        }

        /// <summary>
        /// Where one of the pool's creatures stands. A pool with a radius is an area - a camp,
        /// a nest - and its creatures are spread across it: a walkable point anywhere inside the
        /// radius when the map has a navmesh, otherwise a point in the disc snapped to whatever
        /// ground there is. A pool without one is the old point: two units of scatter when more
        /// than one creature shares it, then snapped to the ground so members on a slope neither
        /// hang in the air nor start in it.
        ///
        /// A mined area's centre is the middle of the props it was built from, and its height
        /// their average: it can be inside a pillbox, or a few metres above or below the floor,
        /// where the navmesh's point query (4 m across, 8 m up and down) finds nothing. Then the
        /// nearest walkable point to the centre, within the pool's own radius, stands in for it -
        /// or, for a centre whose height is a map label's guess, the nearest walkable point in
        /// the column above and below it.
        ///
        /// One creature alone on a point that can go nowhere (BehaviorManager.NeverMoves) is not
        /// put on the mesh at all: it stands on the point, as an emplacement stands on its mount.
        /// What moves is on the mesh from its first step, so it starts there; what cannot is
        /// wherever this puts it for as long as it lives, and the navmesh is not the floor.
        /// Its height is the top of a 0.2 m voxel, sampled every 2.4 m and joined up with
        /// straight lines, and anything an agent can step onto - 0.9 m - is ground to it: it is
        /// 0.17 m over the floor at the median, and beside a low wall, a cot or a crate it is a
        /// ramp up to the top of it. Lt Col Cimoch stands on open ground 1.5 m from a line of
        /// sandbags in Alia Das, and the mesh under him is 0.70 m over the terrain.
        ///
        /// So the pool's height has to be the floor's, and StandingHeights (the migration
        /// Stand_npcs_on_their_floors) made it so: measured against the client's terrain and the
        /// collision meshes of each map's entities.
        ///
        /// The one creature of a pool with a beat (Patrols) stands on the point too: its steps
        /// are floor heights it walks between in straight lines, and the pool's point is where
        /// it starts.
        /// </summary>
        internal static Vector3 SpawnPoint(MapChannel mapChannel, SpawnPool pool, int count, Creature creature = null)
        {
            var pos = pool.Position;

            if (pool.Radius > 0)
            {
                var walkable = NavMeshManager.RandomPointAround(mapChannel, pos, pool.Radius);

                if (!walkable.HasValue && Anchor(mapChannel, pool) is Vector3 anchor)
                    walkable = NavMeshManager.RandomPointAround(mapChannel, anchor, pool.Radius) ?? anchor;

                if (walkable.HasValue)
                    return walkable.Value;

                pos += InDisc(pool.Radius);
            }
            else if (count != 1)
            {
                pos.X += Random.Shared.Next() % 5 - 2;
                pos.Z += Random.Shared.Next() % 5 - 2;
            }
            else if (BehaviorManager.NeverMoves(creature) || pool.Patrol != null)
            {
                return pos;
            }

            return NavMeshManager.SnapToGround(mapChannel, pos);
        }

        /// <summary>
        /// Whether a mission stages what stands at this pool: the pool is the spawn of a
        /// mission's public encounter (PublicEncounterBinding.SpawnId) - an escort waiting to
        /// be led off, a captive held in a Bane camp, an overseer come to parley beside the
        /// officer he threatens. Replaceable for tests.
        /// </summary>
        internal Func<uint, bool> IsStaged { get; set; } = poolId => MissionApplication.Instance.PublicActors.StagesSpawn(poolId);

        /// <summary>
        /// Every automatic pool of hostile creatures whose area comes within SafeClearance of a
        /// friendly NPC's pool, a hospital or a waypoint pad: a player reviving or arriving there
        /// would stand in a fight. A pool of friendly soldiers (see IsSafeGround) is not safe
        /// ground: a skirmish set up on purpose, like boot camp's bridge, is not reported. And
        /// every one whose area comes within TurretScan of an AFS turret (a friendly
        /// emplacement's pool, which is not safe ground): the two would fight for good.
        ///
        /// A Bane emplacement is no turret here. It is on the camp's side, so a camp may stand
        /// around it; and its pool is a hostile one like any other, checked against the AFS
        /// turrets and the safe ground - never against itself.
        ///
        /// A pool a mission stages (IsStaged) is on neither side of the check. Its actor stands
        /// where the mission happens, which is not where players revive or arrive: a captive in
        /// a Bane camp makes the camp no safer. And a hostile one is one creature its scene
        /// directs, not a camp.
        ///
        /// Logged and recorded for the map; nothing is changed. Run once the missions, the
        /// creatures, the pools and the teleporters are all loaded.
        /// </summary>
        public void ValidatePools()
        {
            var places = new List<(uint Map, Vector3 Position, string What)>();

            foreach (var teleporter in DynamicObjectManager.Instance.Teleporters.Values)
                if (teleporter.ObjectData is WaypointInfo info && (info.WaypointType == WaypointType.Hospital || info.WaypointType == WaypointType.Waypoint))
                    places.Add((teleporter.MapContextId, teleporter.Position, $"{info.WaypointType} {info.WaypointId}"));

            var found = PoolsTooClose(places);

            foreach (var (map, message) in found)
            {
                Logger.WriteLog(LogType.Error, message);
                MapErrorManager.Instance.Record(map, message);
            }

            Logger.WriteLog(LogType.Initialize, $"SpawnPools checked against safe ground and turrets: {found.Count} too close.");
        }

        /// <summary>
        /// What ValidatePools reports, a line a pool with the map it is on: the loaded pools
        /// against the friendly NPCs' pools among them, these places besides (the hospitals and
        /// waypoint pads), and the AFS turrets.
        /// </summary>
        internal List<(uint Map, string Message)> PoolsTooClose(IEnumerable<(uint Map, Vector3 Position, string What)> places)
        {
            var found = new List<(uint Map, string Message)>();
            var safe = new Dictionary<uint, List<(Vector3 Position, string What)>>();

            void Add(uint map, Vector3 position, string what)
            {
                if (!safe.TryGetValue(map, out var list))
                    safe[map] = list = new List<(Vector3, string)>();

                list.Add((position, what));
            }

            // An AFS turret is friendly, but no hospital: its ground is where the fighting is.
            var turrets = new List<SpawnPool>();

            foreach (var pool in LoadedSpawnPools.Values)
            {
                if (pool.SpawnSlot.Count > 0 && pool.SpawnSlot.TrueForAll(s => IsEmplacement(s.CreatureId)))
                {
                    // A Bane one is the camp's own gun, and takes its turn as a hostile pool below.
                    if (pool.SpawnSlot.TrueForAll(s => Side(s.CreatureId) == TargetCategory.Friendly))
                        turrets.Add(pool);
                }
                else if (pool.SpawnSlot.Exists(s => IsSafeGround(s.CreatureId)) && !IsStaged(pool.DbId))
                    Add(pool.MapContextId, pool.Position, $"the NPCs of pool {pool.DbId}");
            }

            foreach (var (map, position, what) in places)
                Add(map, position, what);

            foreach (var pool in LoadedSpawnPools.Values)
            {
                // Not on a timer, never brings anything (min and max 0), or not all hostile.
                if (pool.Mode != ModeAutomatic || !pool.SpawnSlot.Exists(s => s.CountMax > 0)
                    || !pool.SpawnSlot.TrueForAll(s => Side(s.CreatureId) == TargetCategory.Hostile))
                    continue;

                // A mission's actor, where its scene has it stand.
                if (IsStaged(pool.DbId))
                    continue;

                // A turret that can see the camp from its mount: the two fight for as long as the
                // server runs, and the camp is never there for a player.
                var fought = false;

                foreach (var turret in turrets)
                {
                    if (turret.MapContextId != pool.MapContextId || turret.Mode != ModeAutomatic)
                        continue;

                    var reach = Vector2.Distance(new Vector2(turret.Position.X, turret.Position.Z), new Vector2(pool.Position.X, pool.Position.Z)) - pool.Radius;

                    if (reach >= TurretScan)
                        continue;

                    found.Add((pool.MapContextId, $"spawnpool {pool.DbId}: its creatures can stand {Math.Max(0, reach):0} m from the turret of pool {turret.DbId}, inside its scan; they will fight for good."));
                    fought = true;
                    break;
                }

                if (fought || !safe.TryGetValue(pool.MapContextId, out var points))
                    continue;

                foreach (var (position, what) in points)
                {
                    var gap = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(pool.Position.X, pool.Position.Z)) - pool.Radius;

                    if (gap >= SafeClearance)
                        continue;

                    found.Add((pool.MapContextId, $"spawnpool {pool.DbId}: its creatures can stand {Math.Max(0, gap):0} m from {what}, which should be safe ground."));
                    break;
                }
            }

            return found;
        }

        private static bool IsEmplacement(uint creatureId) =>
            CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var creature) && Emplacements.Classes.Contains(creature.EntityClass);

        /// <summary>
        /// A friendly creature a player can stand beside: one with the NPC augmentation (a vendor,
        /// trainer, mission giver...), or one with no attack. A friendly with attacks and no NPC
        /// augmentation is a soldier, there to fight - boot camp's AFS bridge squad (pools 510216,
        /// 510217) holds its line 14 m from the Thrax initiates it faces (510218-510220). The old
        /// data's named base staff on soldier classes carry no attack, so they stay safe ground.
        /// </summary>
        private static bool IsSafeGround(uint creatureId) =>
            CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var creature)
            && creature.TargetCategory == TargetCategory.Friendly
            && (creature.Npc != null || creature.Actions.Count == 0);

        private static TargetCategory Side(uint creatureId) =>
            CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var creature) ? creature.TargetCategory : TargetCategory.Hostile;

        /// <summary>A point uniformly inside a disc of this radius, on the ground plane.</summary>
        internal static Vector3 InDisc(float radius)
        {
            var random = new Random();
            var angle = random.NextDouble() * Math.PI * 2;
            var distance = radius * Math.Sqrt(random.NextDouble());

            return new Vector3((float)(Math.Cos(angle) * distance), 0, (float)(Math.Sin(angle) * distance));
        }

        private static SpawnPool CloneSpawnPool(SpawnPool template, MapChannel mapChannel)
        {
            var clone = new SpawnPool
            {
                DbId = template.DbId,
                Position = template.Position,
                Rotation = template.Rotation,
                Radius = template.Radius,
                Pose = template.Pose,
                Patrol = template.Patrol,
                SpawnSlot = template.SpawnSlot?.Select(slot =>
                    new SpawnPoolSlot(slot.CreatureId, slot.CountMin, slot.CountMax)).ToList() ?? new List<SpawnPoolSlot>(),
                Mode = template.Mode,
                AnimType = template.AnimType,
                MapContextId = mapChannel.MapInfo.MapContextId,
                RuntimeMapChannel = mapChannel,
                SpawnPolicy = template.SpawnPolicy,
                RespawnTime = template.RespawnTime,
                UpdateTimer = template.RespawnTime,
                ScenarioMissionId = template.ScenarioMissionId,
                ScenarioGroupId = template.ScenarioGroupId,
                ScenarioAttemptKey = template.ScenarioAttemptKey,
                ScenarioOwnerCharacterId = template.ScenarioOwnerCharacterId,
                FollowOwnerCharacterId = template.FollowOwnerCharacterId,
                FollowTargetEntityId = template.FollowTargetEntityId
            };

            // Where its creatures arrive is the pool's, in every copy of the map.
            clone.Arrivals.AddRange(template.Arrivals);

            return clone;
        }
    }
}
