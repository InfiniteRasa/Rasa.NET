using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Models;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    public class BehaviorManager
    {
        /// <summary>
        /// Radius around home a stroll may end in. The C++ server used 20; it was raised to 40 while
        /// destinations were random offsets that mostly failed the distance check, so creatures
        /// hardly moved. With navmesh destinations every draw succeeds, and 40 m strolls at the
        /// database's 5 m/s "walk" had whole camps sprinting about.
        /// </summary>
        public const byte WanderDistance = 20;

        /// <summary>
        /// Wander pace, metres per second. creature.walk_speed is 5 for every row in the database -
        /// a jog, and the client shows it as one. Strolling creatures are capped at a walk; chases
        /// still use run_speed.
        /// </summary>
        public const float WanderWalkSpeed = 1.6f;

        /// <summary>Idle time between strolls: RestTimeMin plus up to RestTimeSpread, drawn per stop so a camp does not move in step.</summary>
        private const long RestTimeMin = 12000;
        private const long RestTimeSpread = 28000;
        public const byte PathLengthLimit = 72;

        private const byte PathModeOneShot  = 0; // creature will walk along the path once
        private const byte PathModeCycle    = 1; // creature will walk along the path, then go to the very first node again and repeat
        private const byte PathModeReturn   = 2; // creature will walk along the path, then go the the whole path back and repeat

        public const byte BehaviorActionIdle = 0;
        public const byte BehaviorActionFollowingPath = 1;  // will automatically be triggered by wander if there is an active ai path
        public const byte BehaviorActionFighting = 2;
        public const byte BehaviorActionWander = 3;
        public const byte BehaviorActionPatrol = 4;

        /// <summary>
        /// Trailing a master, or holding an anchor point. Only minions are ever in this state;
        /// an ordinary creature has a spawn point to wander around instead.
        /// </summary>
        public const byte BehaviorActionFollow = 5;
        internal const byte BehaviorActionScriptedMove = 6;

        public const byte WanderIdle = 0;
        public const byte WanderMoving = 1;

        /// <summary>
        /// How often creature AI runs per map. The original server ran controller_mapChannelThink
        /// every 250 ms (MapChannel.cpp:1052); this port ran it every 100 ms, and because the step
        /// size was a fixed quarter of the creature's speed, creatures covered 2.5x the ground
        /// they should while the movement packet still reported their real speed.
        /// </summary>
        private const long CreatureThinkInterval = 250;

        /// <summary>
        /// How long a creature that has left combat ignores everything before looking for a new
        /// target. This was 30 seconds, which also applied to a freshly spawned creature, so a
        /// creature that had just fought - or that the server had only just spawned - stood there
        /// while a player walked past it.
        /// </summary>
        private const long AggroScanDelayMs = 3000;

        /// <summary>How often a chasing creature may recalculate its path to a moving target.</summary>
        private const long ChasePathUpdateMs = 500;

        /// <summary>How far the target may drift from the point the current chase path aims at.</summary>
        private const float ChaseRepathDistance = 2.0f;

        private static BehaviorManager _instance;
        private static readonly object InstanceLock = new object();
        public static BehaviorManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new BehaviorManager();
                    }
                }

                return _instance;
            }
        }

        private BehaviorManager()
        {
        }

        /// <CheckForAttackableEntityInRange>
        /// Checks for enemy creatures and players within the given range
        /// </CheckForAttackableEntityInRange>
        private bool CheckForAttackableEntityInRange(MapChannel mapChannel, Creature creature, float range)
        {
            var foundEntity_distance = range + 100.0f; // value that is guaranteed to be higher than the found creature
            var foundEntity_entityId = 0ul;

            // AFS do not attack AFS
            var attacksPlayers = !IsMissionEscort(creature) && creature.Faction != Factions.AFS;

            foreach (var cell in CellManager.CellsIn(mapChannel, creature.Cells))
            {
                foreach (var client in cell.ClientList)
                {
                    // Cell lists can hold a client whose character is already gone.
                    if (!attacksPlayers || client.Player == null)
                        continue;

                    if (client.Player.GmFlagAlwaysFriendly)
                        continue;

                    if (client.Player.Attributes[Attributes.Health].Current <= 0)
                        continue;

                    // check distance so creature attack closes target
                    var dist = Vector3.Distance(creature.Position, client.Player.Position);

                    if (dist <= range)
                    {
                        // set target and change state
                        if (dist < foundEntity_distance)
                        {
                            foundEntity_entityId = client.Player.EntityId;
                            foundEntity_distance = dist;
                        }
                    }
                }

                foreach (var tCreature in cell.CreatureList)
                {
                    if (IsMissionEscort(creature) &&
                        !CreatureManager.IsHostileTarget(mapChannel, creature, tCreature))
                        continue;

                    if (tCreature.Attributes[Attributes.Health].Current <= 0)
                        continue;

                    if (tCreature == creature)
                        continue;

                    if (tCreature.Faction == creature.Faction)
                        continue;

                    // check distance
                    var dist = Vector3.Distance(creature.Position, tCreature.Position);

                    if (dist <= range)
                    {
                        // set target and change state
                        if (dist < foundEntity_distance)
                        {
                            foundEntity_entityId = tCreature.EntityId;
                            foundEntity_distance = dist;
                        }
                    }
                }
            }

            if (foundEntity_entityId != 0)
            {
                SetActionFighting(creature, foundEntity_entityId);
                return true;
            }

            return false;
        }

        /// <CreatureThink>
        /// Called every 250 milliseconds for every creature on the map
        /// The tick parameter stores the amount of ms since the last call(should be 250 usually, but can go up on heavy load)
        /// </CreatureThink>
        private void CreatureThink(MapChannel mapChannel, Creature creature, long delta, out bool needDeletion, out bool needCellUpdate)
        {
            needDeletion = false; // set to true if the creature should be removed (for whatever reason)
            needCellUpdate = false; // set to true in case the creature moved across cells (doesnt need to be instant)

            // update all creature timers befor continue
            UpdateCreatureTimers(creature, delta);

            if (creature.Attributes[Attributes.Health].Current <= 0)
            {
                // A corpse with loot still on it, or with someone's window open on it, stays
                // longer than one that has been cleared: twenty seconds from the kill is about
                // one more fight, and bodies were going before anyone could loot them.
                if (LootDispenserManager.Instance.AdvanceCorpseLifetime(
                        mapChannel, creature, delta))
                    needDeletion = true;

                return; // creature dead
            }

            if (creature.Controller.CurrentAction == BehaviorActionScriptedMove)
            {
                AdvanceScriptedMove(mapChannel, creature, delta);
                return;
            }

            if (IsMissionEscort(creature) && AdvanceEscort(mapChannel, creature, delta))
                return;
            if (Game.Missions.World.CreatureGameplayRules.IsDefender(creature) && AdvanceBaseDefender(mapChannel, creature, delta))
                return;

            // calculate new cell position
            var cellX = (uint)((creature.Position.X / CellManager.CellSize) + CellManager.CellBias);
            var cellZ = (uint)((creature.Position.Z / CellManager.CellSize) + CellManager.CellBias);
            // creature keep apart (we use a stupid trick for this, but it works rather well)
            // pick a random creature from the same cell

            var tCell = CellManager.Instance.GetCell(mapChannel, cellX, cellZ);
            if (tCell != null && tCell.CreatureList.Count > 1)
            {
                var randomCreatureIndex = new Random().Next(tCell.CreatureList.Count);
                // get the creature
                var tCreature = tCell.CreatureList[randomCreatureIndex];
                // Scripted actors keep their authored path and final pose.
                if (creature != tCreature && tCreature.Attributes[Attributes.Health].Current > 0 &&
                    !IsMissionEscort(creature) && !Game.Missions.World.CreatureGameplayRules.IsDefender(creature) &&
                    tCreature.Controller.CurrentAction != BehaviorActionScriptedMove &&
                    !IsMissionEscort(tCreature) && !Game.Missions.World.CreatureGameplayRules.IsDefender(tCreature))
                {
                    var difX = creature.Position.X - tCreature.Position.X;
                    var difY = creature.Position.Y - tCreature.Position.Y;
                    var difZ = creature.Position.Z - tCreature.Position.Z;
                    difY /= 4.0f; // y position has low importance
                    var tDist = difX * difX + difY * difY + difZ * difZ;
                    if (tDist < 1.0f)
                    {
                        // creatures are too close together
                        // push them apart! (But only along x/z axis)
                        // get direction vector
                        var tLength = Math.Sqrt(difX * difX + difZ * difZ);
                        if (tLength >= 0.05f)
                        {
                            difX /= (float)tLength;
                            difZ /= (float)tLength;
                            // decrease strength of push vector
                            difX *= 0.3f;
                            difZ *= 0.3f;

                            var creatureX = creature.Position.X + difX;
                            var creatureY = creature.Position.Y + difY;
                            var creatureZ = creature.Position.Z + difZ;
                            var tCreatureX = tCreature.Position.X - difX;
                            var tCreatureY = tCreature.Position.Y - difY;
                            var tCreatureZ = tCreature.Position.Z - difZ;

                            // push
                            creature.Position = new Vector3(creatureX, creatureY, creatureZ);
                            tCreature.Position = new Vector3(tCreatureX, tCreatureY, tCreatureZ);
                        }
                    }
                }
            }

            // do we need to check for updated cell position?
            creature.UpdatePositionCounter -= delta;

            if (creature.UpdatePositionCounter <= 0)
            {
                // check for changed cell
                var cellSeed = CellManager.Instance.GetCellSeed(creature.Position);

                // calculate initial cell
                if (cellSeed != creature.Cells[2, 2])
                    needCellUpdate = true;

                creature.UpdatePositionCounter = CreatureManager.CreatureLocationUpdateTime;
            }

            if (creature.Controller.CurrentAction == BehaviorActionWander)
            {
                // scan for enemy
                if (creature.LastAgression >= AggroScanDelayMs && ScansForEnemies(creature))
                    if (CheckForAttackableEntityInRange(mapChannel, creature, creature.AggroRange))
                    {
                        // enemy found!
                        return;
                    }

                if (creature.Controller.ActionWander.State == WanderIdle)
                {
                    if (creature.Controller.ActionWander.RestDuration <= 0)
                        creature.Controller.ActionWander.RestDuration = RestTimeMin + new Random().Next((int)RestTimeSpread);

                    //--- idle for a while before the next stroll
                    if (creature.LastRestTime > creature.Controller.ActionWander.RestDuration)
                    {
                        // does creature have a path?
                        if (creature.Controller.AiPathFollowing.GeneralPath != null)
                        {
                            // has path -> don't wander aimlessly, go path walking
                            SetActionPathFollowing(creature);
                            return;
                        }

                        if (creature.WalkSpeed < 0.01f || creature.RunSpeed < 0.01f)
                            return; // creature doesn't wander

                        // set destination
                        creature.Controller.ActionWander.WanderDestination = GetDestination(mapChannel, creature);

                        // next step approaching
                        creature.Controller.ActionWander.State = WanderMoving;
                        creature.LastRestTime = 0;
                    }
                }

                if (creature.Controller.ActionWander.State == WanderMoving)
                {
                    // following path (short path)
                    if (creature.Controller.Path.Count == 0)
                        BuildPath(mapChannel, creature, creature.Controller.ActionWander.WanderDestination);

                    if (FollowPath(mapChannel, creature, Math.Min(creature.WalkSpeed, WanderWalkSpeed), delta))
                    {
                        creature.Controller.ActionWander.State = WanderIdle;
                        creature.Controller.ActionWander.RestDuration = 0;
                        creature.LastRestTime = 0;
                        return;
                    }
                }
            }
            else if (creature.Controller.CurrentAction == BehaviorActionFollow)
            {
                // A minion, either trailing someone or holding an anchor point.
                if (ScansForEnemies(creature) && creature.LastAgression >= AggroScanDelayMs)
                    if (CheckForAttackableEntityInRange(mapChannel, creature, creature.AggroRange))
                        return;

                // Assist: copy whatever the assisted player is shooting at. "For subordinates that
                // are primarily offensive, assist mode will automatically issue a Target command
                // whenever the player attacks an enemy so that the subordinate is targeting the
                // same enemy."
                if (creature.Controller.ActionFollow.AssistTargetId != 0 && creature.Stance != MinionStance.Passive)
                {
                    var assisted = EntityManager.Instance.GetActor(creature.Controller.ActionFollow.AssistTargetId);

                    if (assisted != null && assisted.Target != 0 && assisted.Target != creature.EntityId)
                    {
                        creature.Target = assisted.Target;
                        SetActionFighting(creature, assisted.Target);
                        return;
                    }
                }

                var destination = creature.Controller.ActionFollow.Anchor;

                if (!creature.Controller.ActionFollow.HasAnchor)
                {
                    Actor followed = null;
                    if (creature.Controller.ActionFollow.FollowTargetId != 0)
                        EntityManager.Instance.Actors.TryGetValue(
                            creature.Controller.ActionFollow.FollowTargetId,
                            out followed);
                    if (followed == null &&
                        creature.SpawnPool?.FollowOwnerCharacterId > 0)
                    {
                        followed = mapChannel.ClientList
                            .Select(client => client?.Player)
                            .FirstOrDefault(player =>
                                player?.Id == creature.SpawnPool.FollowOwnerCharacterId);
                        if (followed != null)
                            creature.Controller.ActionFollow.FollowTargetId = followed.EntityId;
                    }

                    // Nothing left to follow - the master logged out, or the followed player has
                    // gone. Stand still rather than walking to the origin; MinionManager's worker
                    // is what decides whether this minion should still exist at all.
                    if (followed == null)
                        return;

                    destination = followed.Position;
                }

                creature.HomePos.Position = destination;

                var gap = Vector3.Distance(creature.Position, destination);

                if (gap <= MinionManager.FollowDistance)
                {
                    creature.Controller.Path.Clear();
                    creature.Controller.PathIndex = 0;
                    return;
                }

                // A followed player moves, so the path goes stale. Rebuild it on a timer rather
                // than every frame, the same way chasing does.
                creature.Controller.ActionFollow.PathUpdateTime -= delta;

                if (creature.Controller.ActionFollow.PathUpdateTime <= 0)
                {
                    creature.Controller.ActionFollow.PathUpdateTime = ChasePathUpdateMs;

                    if (creature.Controller.Path.Count == 0 || !creature.Controller.ActionFollow.HasAnchor)
                    {
                        creature.Controller.Path.Clear();
                        creature.Controller.PathIndex = 0;
                        BuildPath(mapChannel, creature, destination);
                    }
                }

                // Run when it has fallen a long way behind, walk when it is just catching up.
                var speed = gap > MinionManager.MaxFollowTargetDistance ? creature.RunSpeed : creature.WalkSpeed;

                FollowPath(mapChannel, creature, speed, delta);
            }
            else if (creature.Controller.CurrentAction == BehaviorActionFollowingPath)
            {
                // following predefined path (long path)
                // scan for enemy
                if (ScansForEnemies(creature) && CheckForAttackableEntityInRange(mapChannel, creature, creature.AggroRange))
                {
                    // enemy found!
                    return;
                }

                // following path (to next node)
                if (creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex >= creature.Controller.AiPathFollowing.GeneralPath.NumberOfPathNodes)
                    return; // no more nodes in the path

                var realCurrentNodeIndex = creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex;

                if (realCurrentNodeIndex < 0)
                    realCurrentNodeIndex = -realCurrentNodeIndex;

                var currentTargetNodePos = new float[3];
                currentTargetNodePos[0] = creature.Controller.AiPathFollowing.GeneralPath.PathNodeList[realCurrentNodeIndex].Pos[0];
                currentTargetNodePos[1] = creature.Controller.AiPathFollowing.GeneralPath.PathNodeList[realCurrentNodeIndex].Pos[1];
                currentTargetNodePos[2] = creature.Controller.AiPathFollowing.GeneralPath.PathNodeList[realCurrentNodeIndex].Pos[2];
                currentTargetNodePos[0] += creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0];
                currentTargetNodePos[2] += creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1];

                // The route to the node is walked corner by corner; a map with no navmesh gets
                // the straight line it always had.
                if (creature.Controller.Path.Count == 0)
                    BuildPath(mapChannel, creature, new Vector3(currentTargetNodePos[0], currentTargetNodePos[1], currentTargetNodePos[2]));

                if (FollowPath(mapChannel, creature, creature.WalkSpeed, delta))
                {
                    creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex++; // goto next node

                    if (creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex >= creature.Controller.AiPathFollowing.GeneralPath.NumberOfPathNodes)
                    {
                        // path end reached
                        if (creature.Controller.AiPathFollowing.GeneralPath.Mode == PathModeCycle)
                            creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex = 0;
                        else if (creature.Controller.AiPathFollowing.GeneralPath.Mode == PathModeReturn)
                            creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex = -(creature.Controller.AiPathFollowing.GeneralPath.NumberOfPathNodes - 1) + 1; // a negative number indicates reversed path walking
                        else if (creature.Controller.AiPathFollowing.GeneralPath.Mode == PathModeOneShot)
                        {
                            // no more path
                            // reset path and enter wander mode
                            creature.Controller.AiPathFollowing.GeneralPath = null;
                            creature.Controller.AiPathFollowing.GeneralPathCurrentNodeIndex = 0;
                            SetActionWander(creature);
                        }
                        return;
                    }
                    else
                    {
                        // random position bias added to every node (to make groups look like they do not run on the same path)
                        creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0] = ((new Random().Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
                        creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1] = ((new Random().Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
                        // update home position to be at the (old) current node
                        creature.HomePos.Position = new Vector3(currentTargetNodePos[0], currentTargetNodePos[1], currentTargetNodePos[2]);
                    }
                }
            }
            else if (creature.Controller.CurrentAction == BehaviorActionFighting)
            {
                // get target
                var target = EntityManager.Instance.GetEntityType(creature.Controller.ActionFighting.TargetEntityId);

                if (target == 0)
                {
                    // target disappeared (player logout or deleted for some reason) - leave combat mode
                    RestorePassiveAction(creature);
                    return;
                }

                // leave combat after 
                if (creature.LastAgression > creature.AggressionTime)
                {
                    RestorePassiveAction(creature);
                    return;
                }

                // get position of target
                var targetPosition = new Vector3();

                if (target == EntityType.Character)
                {
                    var player = EntityManager.Instance.GetPlayer(creature.Controller.ActionFighting.TargetEntityId);

                    // if target dead, set wander state
                    if (player.Attributes[Attributes.Health].Current <= 0 || player.State == CharacterState.Dead)
                    {
                        RestorePassiveAction(creature);
                        return;
                    }

                    targetPosition = player.Position;
                }

                else if (target == EntityType.Creature)
                {
                    var targetCreature = EntityManager.Instance.GetCreature(creature.Controller.ActionFighting.TargetEntityId);

                    if (targetCreature.Attributes[Attributes.Health].Current <= 0 || targetCreature.State == CharacterState.Dead)
                    {
                        // exit visual combat mode
                        CellManager.Instance.CellCallMethod(mapChannel, creature, new RequestVisualCombatModePacket(false));

                        RestorePassiveAction(creature);
                        return;
                    }

                    targetPosition = targetCreature.Position;
                }
                else
                    Logger.WriteLog(LogType.Error, $"CreatureThink: unsuported Traget type {target}"); // todo

                var targetDistX = (targetPosition.X - creature.Position.X);
                var targetDistY = (targetPosition.Y - creature.Position.Y);
                var targetDistZ = (targetPosition.Z - creature.Position.Z);
                var targetDistSqr = (targetDistX * targetDistX + targetDistY * targetDistY + targetDistZ * targetDistZ);
                // stop tracking target after target exceeds a certain distance to home pos
                // Note: For patrolling creatures the homePos is the last arrived path node 
                var homeLocDistX = (creature.HomePos.Position.X - targetPosition.X);
                var homeLocDistZ = (creature.HomePos.Position.Z - targetPosition.Z);
                var homeLocDist = homeLocDistX * homeLocDistX + homeLocDistZ * homeLocDistZ;

                if (homeLocDist >= 60.0f * 60.0f)
                {
                    creature.LastRestTime = 0; // forces AI to immediately calculate new wander position
                    RestorePassiveAction(creature);
                    return;
                }
                creature.LastAgression = 0; // update aggression time if we found our target

                var needToMove = true;

                foreach (var action in creature.Actions)
                {
                    // check if we can execute action
                    if (targetDistSqr < action.RangeMin * action.RangeMin || targetDistSqr >= action.RangeMax * action.RangeMax)
                        continue;

                    needToMove = false;

                    if (action.CooldownTimer > 0)
                        continue;   // action on cooldown

                    // rotate
                    UpdateEntityMovement(targetDistX, targetDistY, targetDistZ, creature, mapChannel, 0.0f, false, delta);

                    // execute action and quit
                    var dmg = (int)(action.MinDamage + (new Random().Next() % (action.MaxDamage - action.MinDamage + 1)));

                    var actionData = new ActionData(creature, action.ActionId, action.ActionArgId, creature.Controller.ActionFighting.TargetEntityId, 0);
                    // do damage
                    MissileManager.Instance.MissileLaunch(mapChannel, actionData, dmg);

                    // set cooldown
                    action.CooldownTimer = action.Cooldown;

                    // creature used action, break loop
                    break;
                }

                if (needToMove == false)
                    return;

                if (targetDistSqr <= 3.0f * 3.0f)
                    return;// near enough, dont move

                // After checking for melee and ranged attacks without success, chase.
                //
                // The path was only ever built when there was none, and nothing ever emptied it:
                // a creature walked to the spot its target stood in when the fight started and
                // then held that node forever, standing still while the player moved around it
                // and shot at it. The path is now rebuilt whenever the target has moved away from
                // the point it aims at, at most every ChasePathUpdateMs.
                var targetDrift = Vector3.Distance(targetPosition, creature.Controller.ActionFighting.LockedTargetPosition);

                if (creature.Controller.Path.Count == 0
                    || (targetDrift > ChaseRepathDistance && creature.Controller.TimerPathUpdateLock <= 0))
                {
                    creature.Controller.TimerPathUpdateLock = ChasePathUpdateMs;

                    var pathTarget = new Vector3();

                    if (targetDistSqr < 0.1f)
                    {
                        // if too near, move out of enemy by running to random point somewhere x units around the creature
                        var angle = (new Random().Next() / 32767.0f) * 6.28318f; // random angle
                        var distance = 2.5f; // keep 2.5 meter distance
                        pathTarget.X = targetPosition.X + (float)Math.Cos(angle) * distance;
                        pathTarget.Y = targetPosition.Y;
                        pathTarget.Z = targetPosition.Z + (float)Math.Sin(angle) * distance;
                    }
                    else
                    {
                        // run to nearest point that maintains distance to creature
                        var vecV2A = new float[2]; // vector2D victim->attacker
                        vecV2A[0] = -targetDistX;
                        vecV2A[1] = -targetDistZ;
                        // normalize
                        var vecV2ALen = (float)Math.Sqrt(targetDistSqr);
                        vecV2A[0] /= vecV2ALen;
                        vecV2A[1] /= vecV2ALen;
                        // use vector to calculate nearest melee point from our current position
                        var distance = 2.5f; // keep 2.5 meter distance
                        pathTarget.X = targetPosition.X + vecV2A[0] * distance;
                        pathTarget.Y = targetPosition.Y;
                        pathTarget.Z = targetPosition.Z + vecV2A[1] * distance;
                    }

                    BuildPath(mapChannel, creature, pathTarget);

                    // where the target was when this path was built
                    creature.Controller.ActionFighting.LockedTargetPosition = targetPosition;
                }

                // follow path; a walked path is dropped so the next think builds one for
                // wherever the target is now, instead of holding this node for good
                FollowPath(mapChannel, creature, creature.RunSpeed, delta);
            }//---fighting
        }

        /// <summary>
        /// A wander destination around the creature's home, far enough from where it stands to be
        /// worth walking to. On a map with a navmesh the point is drawn from the walkable surface
        /// around home, so it is never inside a rock or off a cliff. Every candidate sits within
        /// WanderDistance of home, so a creature that ended a chase further from home than that can
        /// never draw one - the loop used to run forever, on the MainLoop thread. It gives up after
        /// a fixed number of tries and walks home instead.
        /// </summary>
        private Vector3 GetDestination(MapChannel mapChannel, Creature creature)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var dest = NavMeshManager.RandomPointAround(mapChannel, creature.HomePos.Position, WanderDistance)
                           ?? creature.HomePos.Position + GetRandomVector();
                var distance = GetDistanceSqr(creature.Position, dest);

                if (distance > WanderDistance / 3 && distance < WanderDistance)
                    return dest;
            }

            return creature.HomePos.Position;
        }

        private static bool IsMissionEscort(Creature creature) =>
            creature.MasterEntityId == 0 && creature.SpawnPool?.FollowOwnerCharacterId > 0;

        private bool AdvanceBaseDefender(MapChannel map, Creature creature, long delta)
        {
            var home = creature.HomePos.Position;
            var target = map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(candidate => Game.Missions.World.CreatureGameplayRules.Policy(candidate).Tags.Contains(
                        Game.Missions.World.CreatureGameplayRules.Policy(creature).DefenseTargetTag) &&
                    CreatureManager.IsHostileTarget(map, creature, candidate) &&
                    Vector3.Distance(candidate.Position, home) <= Game.Missions.World.CreatureGameplayRules.Policy(creature).DefenseRadius)
                .OrderBy(candidate => Vector3.DistanceSquared(candidate.Position, creature.Position))
                .FirstOrDefault();
            if (target != null && Vector3.Distance(creature.Position, home) <= Game.Missions.World.CreatureGameplayRules.Policy(creature).DefenseRadius)
            {
                if (creature.Controller.CurrentAction != BehaviorActionFighting ||
                    creature.Controller.ActionFighting.TargetEntityId != target.EntityId)
                    SetActionFighting(creature, target.EntityId);
                return false;
            }

            if (creature.Controller.CurrentAction != BehaviorActionFollow)
            {
                creature.Controller.CurrentAction = BehaviorActionFollow;
                creature.Controller.ActionFighting.TargetEntityId = 0;
                creature.Controller.ActionFollow.PathUpdateTime = 0;
            }
            if (Vector3.Distance(creature.Position, home) <= 0.75f)
                StopFollowing(map, creature);
            else
                FollowGrounded(map, creature, home, creature.RunSpeed, delta, 0.5f);
            return true;
        }

        private bool AdvanceEscort(MapChannel map, Creature creature, long delta)
        {
            var owner = CreatureManager.FindEscortOwner(map, creature);
            if (owner == null)
            {
                creature.Controller.ActionFollow.OwnerAttackTarget = null;
                creature.Controller.CurrentAction = BehaviorActionFollow;
                creature.Controller.ActionFighting.TargetEntityId = 0;
                StopFollowing(map, creature);
                return true;
            }

            var destination = owner.Player.Position;
            creature.HomePos.Position = destination;
            var follow = creature.Controller.ActionFollow;
            var gap = Vector3.Distance(creature.Position, destination);
            var target = follow.OwnerAttackTarget;
            var hadOwnerTarget = target != null;
            var wasFighting = creature.Controller.CurrentAction == BehaviorActionFighting;
            if (target != null &&
                (!CreatureManager.IsHostileTarget(map, creature, target) ||
                 gap > 20 || Vector3.Distance(target.Position, destination) > 35))
                follow.OwnerAttackTarget = target = null;

            if (!hadOwnerTarget && wasFighting && gap <= 20 &&
                EntityManager.Instance.Creatures.TryGetValue(
                    creature.Controller.ActionFighting.TargetEntityId, out var currentTarget) &&
                CreatureManager.IsHostileTarget(map, creature, currentTarget) &&
                Vector3.Distance(currentTarget.Position, destination) <= 35)
                target = currentTarget;

            if (target != null)
            {
                if (creature.Controller.CurrentAction != BehaviorActionFighting ||
                    creature.Controller.ActionFighting.TargetEntityId != target.EntityId)
                    SetActionFighting(creature, target.EntityId);
                return false;
            }

            if (!hadOwnerTarget && !wasFighting && gap <= 20 && !follow.CatchUpRunning &&
                creature.LastAgression >= AggroScanDelayMs &&
                CheckForAttackableEntityInRange(map, creature, creature.AggroRange))
                return false;

            if (wasFighting)
            {
                creature.Controller.Path.Clear();
                creature.Controller.PathIndex = 0;
                follow.PathUpdateTime = 0;
                creature.Controller.ActionFighting.TargetEntityId = 0;
                creature.LastAgression = 0;
            }
            creature.Controller.CurrentAction = BehaviorActionFollow;
            follow.FollowTargetId = owner.Player.EntityId;
            if (gap <= 4)
            {
                follow.CatchUpRunning = false;
                StopFollowing(map, creature);
                return true;
            }

            if (gap > 10)
                follow.CatchUpRunning = true;
            else if (gap <= 6)
                follow.CatchUpRunning = false;

            // Player run is 6.5 m/s; the shipped Sprint ranks scale it by 120-160%.
            // Bounded 220% catch-up beats even rank five, without copying GM speed.
            var speed = gap > 20 ? 6.5f * 2.2f :
                follow.CatchUpRunning ? Math.Clamp(creature.RunSpeed, 6.5f, 6.5f * 2.2f) : creature.WalkSpeed;
            FollowGrounded(map, creature, destination, speed, delta, 4);
            return true;
        }

        private static void StopFollowing(MapChannel map, Creature creature)
        {
            var wasRunning = creature.IsRunning;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
            creature.Controller.ActionFollow.PathUpdateTime = 0;
            if (creature.IsRunning)
            {
                creature.IsRunning = false;
                CellManager.Instance.CellCallMethod(map, creature, new IsRunningPacket(false));
            }
            var direction = new Vector2((float)creature.Rotation, 0);
            var previous = creature.Controller.LastMovement;
            if (!wasRunning && previous?.Velocity == 0 &&
                previous.Position == creature.Position && previous.ViewDirection == direction)
                return;
            PublishMovement(creature, new Movement(creature.Position, 0, 0x08, direction));
        }

        private static void PublishMovement(Creature creature, Movement movement)
        {
            creature.Controller.LastMovement = movement;
            CellManager.Instance.CellMoveObject(creature, movement);
        }

        private static void FollowGrounded(MapChannel map, Creature creature, Vector3 destination,
            float speed, long delta, float stopDistance)
        {
            var controller = creature.Controller;
            controller.ActionFollow.PathUpdateTime -= delta;
            if (controller.ActionFollow.PathUpdateTime <= 0)
            {
                controller.ActionFollow.PathUpdateTime = ChasePathUpdateMs;
                BuildPath(map, creature, destination);
            }

            var previous = creature.Position;
            var budget = speed * delta / 1000f;
            var approachRemaining = Math.Max(0, Vector3.Distance(previous, destination) - stopDistance);
            var travelled = 0f;
            while (controller.PathIndex < controller.Path.Count && budget > 0 && approachRemaining > 0)
            {
                var node = controller.Path[controller.PathIndex];
                var distance = Vector3.Distance(creature.Position, node);
                if (distance < 0.01f)
                {
                    controller.PathIndex++;
                    continue;
                }
                var step = Math.Min(distance, Math.Min(budget, approachRemaining));
                var next = NavMeshManager.SnapToGround(map,
                    creature.Position + (node - creature.Position) * (step / distance));
                var actual = Vector3.Distance(creature.Position, next);
                for (var attempt = 0; actual > budget && attempt < 8; attempt++)
                {
                    step *= budget / actual * 0.999f;
                    next = NavMeshManager.SnapToGround(map,
                        creature.Position + (node - creature.Position) * (step / distance));
                    actual = Vector3.Distance(creature.Position, next);
                }
                if (actual > budget)
                {
                    Logger.WriteLog(LogType.Error,
                        $"Mission actor {creature.EntityId} cannot take a grounded step within its movement budget.");
                    break;
                }
                creature.Position = next;
                budget -= actual;
                approachRemaining -= step;
                travelled += actual;
                if (step >= distance)
                    controller.PathIndex++;
                else
                    break;
            }
            var direction = creature.Position - previous;
            if (direction.LengthSquared() > 0.0001f)
                creature.Rotation = Math.Atan2(-direction.X, -direction.Z);
            var running = travelled > 0 && speed > creature.WalkSpeed;
            if (creature.IsRunning != running)
            {
                creature.IsRunning = running;
                CellManager.Instance.CellCallMethod(map, creature, new IsRunningPacket(running));
            }
            SynchronizeMovementCell(map, creature);
            PublishMovement(creature,
                new Movement(creature.Position, travelled * 1000 / delta, 0x08,
                    new Vector2((float)creature.Rotation, 0)));
        }

        /// <summary>
        /// Sets the creature's path to <paramref name="destination"/>: the navmesh corners when the
        /// map has one and both ends are on it. Mission escorts and the base defender never use
        /// the straight-line fallback that ordinary creatures retain on maps without a navmesh.
        /// </summary>
        private static void BuildPath(MapChannel mapChannel, Creature creature, Vector3 destination)
        {
            var path = NavMeshManager.FindPath(mapChannel, creature.Position, destination);

            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;

            if (path != null && path.Count > 0)
                creature.Controller.Path.AddRange(path);
            else if (mapChannel.NavMesh == null &&
                     !IsMissionEscort(creature) && !Game.Missions.World.CreatureGameplayRules.IsDefender(creature))
                creature.Controller.Path.Add(destination);
            else
                Logger.WriteLog(LogType.Error,
                    $"Actor {creature.EntityId} has no navmesh route from {creature.Position} to {destination} " +
                    $"on map {mapChannel.MapInfo?.MapContextId} (mesh loaded: {mapChannel.NavMesh != null}).");
        }

        /// <summary>
        /// Walks the creature one tick along its path at <paramref name="speed"/>, advancing to the
        /// next corner when the current one is reached. Returns true once the whole path has been
        /// walked; the path is cleared then, so the next think builds a fresh one.
        /// </summary>
        private bool FollowPath(
            MapChannel mapChannel, Creature creature, float speed, long delta,
            float arrivalDistance = 0.8f, bool synchronizeVisibility = false)
        {
            var controller = creature.Controller;

            if (controller.PathIndex >= controller.Path.Count)
            {
                controller.Path.Clear();
                controller.PathIndex = 0;
                return true;
            }

            var node = controller.Path[controller.PathIndex];
            var difX = node.X - creature.Position.X;
            var difY = node.Y - creature.Position.Y;
            var difZ = node.Z - creature.Position.Z;
            var distSqr = difX * difX + difZ * difZ;
            if (synchronizeVisibility)
                distSqr += difY * difY;

            if (distSqr > Math.Min(0.01f, arrivalDistance * arrivalDistance))
            {
                var moved = UpdateEntityMovement(
                    difX, difY, difZ, creature, mapChannel, speed, true, delta, synchronizeVisibility);

                // the step is clamped to the distance left, so covering it means the corner is reached
                if (moved * moved >= distSqr)
                    distSqr = 0.0f;
            }

            if (distSqr <= arrivalDistance * arrivalDistance)
            {
                controller.PathIndex++;

                if (controller.PathIndex >= controller.Path.Count)
                {
                    controller.Path.Clear();
                    controller.PathIndex = 0;
                    return true;
                }
            }

            return false;
        }

        private double GetDistanceSqr(Vector3 p1, Vector3 p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            float dz = p2.Z - p1.Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private Vector3 GetRandomVector()
        {
            var rnd1 = new Random().Next(1, WanderDistance);
            var rnd2 = new Random().Next(1, WanderDistance);
            var rndX = rnd1 * Math.Cos(Math.PI * 2 * rnd1 / rnd2);
            var rndY = rnd2 * Math.Sin(Math.PI * 2 * rnd1 / rnd2);
            var rndVector = new Vector3((float)rndX, 0.0f, (float)rndY);

            return rndVector;
        }
        
        public void MapChannelThink(MapChannel mapChannel, long delta)
        {
            // Accumulated per map. This used to be one field on the singleton shared by every
            // map channel, so with two maps active each one's creatures ran on the other's time.
            mapChannel.ControllerElapsed += delta;

            if (mapChannel.ControllerElapsed < CreatureThinkInterval)
                return;

            var elapsed = mapChannel.ControllerElapsed;
            mapChannel.ControllerElapsed = 0;

            // creature deletion and update queue
            var queue_creatureDeletion = new List<Creature>();
            var queue_creatureCellUpdate = new List<Creature>();
            var processedCreatures = new HashSet<Creature>(ReferenceEqualityComparer.Instance);
            // todo: When on heavy load, the server should increase the time between calls to
            //       this function. (check player updating as a reference)

            // mapChannel.MapCellInfo.Cells can be modified, so we create temp list;
            var tempCells = mapChannel.MapCellInfo.Cells.ToList();

            foreach (var entry in tempCells)
            {
                var mapCell = entry.Value;

                if (mapCell == null) // should never happen, but still do a check for safety
                    continue;
                // creatures
                if (mapCell.CreatureList.Count > 0)
                {

                    foreach (var creature in mapCell.CreatureList.ToArray())
                    {
                        if (creature == null || !processedCreatures.Add(creature))
                            continue;

                        CreatureThink(mapChannel, creature, elapsed, out var needDeletion, out var needCellUpdate);

                        if (needDeletion)
                            queue_creatureDeletion.Add(creature);

                        if (needCellUpdate) // update cell (even when creature is also deleted)
                            queue_creatureCellUpdate.Add(creature);

                        // need to delete creature & we still have a free space in the deletion queue
                        // not so nice hack to remove creatures from the map cell when creature_cellUpdateLocation is called
                        /*std::swap(mapCell->ht_creatureList.at(f), mapCell->ht_creatureList.at(creatureCount-1));
                        mapCell->ht_creatureList.pop_back();
                        creatureCount = mapCell->ht_creatureList.size();
                        if( creatureCount == 0 )
                            break;
                        creatureList = &mapCell->ht_creatureList[0];
                        f--;*/
                    }
                }
            }
            
            //update logic for creatures (same like for deletion below, moving creatures to different vectors is not good)
            if (queue_creatureCellUpdate.Count > 0)
            {
                var creatureList = queue_creatureCellUpdate;
                var creatureCount = queue_creatureCellUpdate.Count;
                for (var f = 0; f < creatureCount; f++)
                {
                    // calculate new cell position
                    var newLocX = (uint)((creatureList[f].Position.X / CellManager.CellSize) + CellManager.CellBias);
                    var newLocZ = (uint)((creatureList[f].Position.Z / CellManager.CellSize) + CellManager.CellBias);

                    CreatureManager.Instance.CellUpdateLocation(mapChannel, creatureList[f], newLocX, newLocZ);
                }
            }

            // deletion logic for creatures (we have to do it here, since deleting creatures while iterating them is not so nice...)
            // do we need to delete some creatures?
            if (queue_creatureDeletion.Count > 0)
            {
                var creatureList = queue_creatureDeletion;
                var creatureCount = queue_creatureDeletion.Count;

                for (var f = 0; f < creatureCount; f++)
                {
                    // did the creature have an active loot dispenser?
                    if (creatureList[f].LootDispenserObjectEntityId != 0)
                    {
                        var lootDispenserObject = EntityManager.Instance.GetObject(creatureList[f].LootDispenserObjectEntityId);

                        if (lootDispenserObject != null)
                            DynamicObjectManager.Instance.DynamicObjectDestroy(mapChannel, lootDispenserObject);

                        creatureList[f].LootDispenserObjectEntityId = 0;
                    }
                    // remove creature from world
                    CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creatureList[f]);
                }
            }
        }
        
        public void SetActionFighting(Creature creature, ulong targetEntityId)
        {
            // A passive minion does not fight, and this is the one place worth saying so: it
            // covers both the aggro scan and being shot at (MissileManager calls straight in
            // here), so there is no second path where passive quietly stops meaning passive.
            if (creature.MasterEntityId != 0 && creature.Stance == MinionStance.Passive)
                return;

            creature.Controller.CurrentAction = BehaviorActionFighting;
            // Whatever the creature was walking towards is not where the fight is: without this
            // it chased its last wander node before ever heading for its target.
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
            creature.Controller.TimerPathUpdateLock = 0;
            creature.Controller.ActionFighting.TargetEntityId = targetEntityId;
            creature.LastAgression = 0;
        }
        
        private void SetActionPathFollowing(Creature creature)
        {
            creature.Controller.CurrentAction = BehaviorActionFollowingPath;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
            // random position bias added to every node (to make groups look like they do not run on the same path)
            creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0] =  ((new Random().Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
            creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1] = ((new Random().Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;

            Logger.WriteLog(LogType.AI, $"path 0{creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0]}");
            Logger.WriteLog(LogType.AI, $"path 1{creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1]}");
        }

        private void SetActionWander(Creature creature)
        {
            creature.Controller.CurrentAction = BehaviorActionWander;
            creature.Controller.ActionWander.State = WanderIdle;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
        }

        private void RestorePassiveAction(Creature creature)
        {
            if (creature.Controller.ActionFollow.HasAnchor ||
                creature.Controller.ActionFollow.FollowTargetId != 0)
            {
                creature.Controller.CurrentAction = BehaviorActionFollow;
                creature.Controller.ActionFollow.PathUpdateTime = 0;
                creature.Controller.Path.Clear();
                creature.Controller.PathIndex = 0;
                return;
            }

            SetActionWander(creature);
        }

        /// <summary>
        /// Trails an entity - normally the minion's master, or another player after a Follow
        /// Target order. Clears any anchor, which is what Follow Me is for.
        /// </summary>
        public void SetActionFollow(Creature creature, ulong followTargetId)
        {
            creature.Controller.CurrentAction = BehaviorActionFollow;
            if (creature.Controller.ActionFollow.FollowTargetId != followTargetId)
            {
                creature.Controller.ActionFollow.OwnerAttackTarget = null;
                creature.Controller.ActionFollow.CatchUpRunning = false;
            }
            creature.Controller.ActionFollow.FollowTargetId = followTargetId;
            creature.Controller.ActionFollow.HasAnchor = false;
            creature.Controller.ActionFollow.PathUpdateTime = 0;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
        }

        /// <summary>
        /// Plants the minion at a spot and leaves it there: Go sets the picked location, Stay the
        /// minion's own. It may still leave to act, and returns here when it is done.
        /// </summary>
        public void SetActionAnchor(Creature creature, Vector3 anchor)
        {
            creature.Controller.CurrentAction = BehaviorActionFollow;
            creature.Controller.ActionFollow.OwnerAttackTarget = null;
            creature.Controller.ActionFollow.CatchUpRunning = false;
            creature.Controller.ActionFollow.HasAnchor = true;
            creature.Controller.ActionFollow.Anchor = anchor;
            creature.Controller.ActionFollow.PathUpdateTime = 0;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
        }

        internal bool SetActionScriptedMove(
            MapChannel mapChannel, Creature creature, Vector3 destination, double orientation)
        {
            if (!MapInstanceScope.Contains(mapChannel, creature) ||
                !EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) ||
                !ReferenceEquals(registered, creature) ||
                !CellManager.TryGetCellCoordinates(destination, out _, out _) ||
                !double.IsFinite(orientation) || !float.IsFinite(creature.RunSpeed) || creature.RunSpeed <= 0)
            {
                Logger.WriteLog(LogType.Error, "Cannot start scripted movement without a registered actor, valid destination and run speed.");
                return false;
            }

            var current = creature.Controller.ScriptedMove;
            if (creature.Controller.CurrentAction == BehaviorActionScriptedMove &&
                current?.Destination == destination && current.Orientation == orientation)
                return true;

            if (mapChannel.NavMesh == null)
            {
                Logger.WriteLog(LogType.Error,
                    $"Cannot move creature {creature.DbId} to {destination}: map {mapChannel.MapInfo?.MapContextId} has no navmesh loaded.");
                return false;
            }
            var path = mapChannel.NavMesh.FindPath(creature.Position, destination, out var complete);
            if (!complete || path == null || path.Count == 0 ||
                Vector3.Distance(path[^1], destination) > 1)
            {
                Logger.WriteLog(LogType.Error,
                    $"Cannot move creature {creature.DbId} to {destination}: no complete walkable route.");
                return false;
            }

            creature.Controller.ScriptedMove = new ScriptedMove
            {
                Destination = destination,
                Orientation = orientation
            };
            creature.Controller.CurrentAction = BehaviorActionScriptedMove;
            creature.Controller.Path.Clear();
            creature.Controller.Path.AddRange(path);
            creature.Controller.PathIndex = 0;
            creature.HomePos.Position = destination;
            creature.IsRunning = true;
            CellManager.Instance.CellCallMethod(creature, new IsRunningPacket(true));
            return true;
        }

        private void AdvanceScriptedMove(MapChannel mapChannel, Creature creature, long delta)
        {
            var move = creature.Controller.ScriptedMove;
            if (move.Arrived || !FollowPath(mapChannel, creature, creature.RunSpeed, delta, 0.01f, true))
                return;

            creature.Position = move.Destination;
            creature.Rotation = move.Orientation;
            creature.HomePos.Position = move.Destination;
            creature.IsRunning = false;
            move.Arrived = true;
            SynchronizeMovementCell(mapChannel, creature);
            PublishMovement(creature,
                new Movement(creature.Position, 0, 0x08, new Vector2((float)creature.Rotation, 0)));
            CellManager.Instance.CellCallMethod(creature, new IsRunningPacket(false));
        }

        internal bool RestoreScriptedPose(MapChannel map, Creature creature, Vector3 position, double orientation)
        {
            if (!MapInstanceScope.Contains(map, creature) ||
                !CellManager.TryGetCellCoordinates(position, out _, out _) || !double.IsFinite(orientation))
                return false;
            creature.Position = position;
            creature.Rotation = orientation;
            creature.HomePos.Position = position;
            creature.IsRunning = false;
            creature.Controller.Path.Clear();
            creature.Controller.ScriptedMove = new ScriptedMove
                { Destination = position, Orientation = orientation, Arrived = true };
            creature.Controller.CurrentAction = BehaviorActionScriptedMove;
            SynchronizeMovementCell(map, creature);
            PublishMovement(creature, new Movement(position, 0, 0x08, new Vector2((float)orientation, 0)));
            CellManager.Instance.CellCallMethod(creature, new IsRunningPacket(false));
            return true;
        }

        private static void SynchronizeMovementCell(MapChannel mapChannel, Creature creature)
        {
            if (CellManager.Instance.GetCellSeed(creature.Position) == creature.Cells[2, 2])
                return;

            CreatureManager.Instance.CellUpdateLocation(mapChannel, creature,
                (uint)(creature.Position.X / CellManager.CellSize + CellManager.CellBias),
                (uint)(creature.Position.Z / CellManager.CellSize + CellManager.CellBias));
        }

        /// <summary>
        /// Whether this creature goes looking for a fight. An ordinary creature always does; a
        /// minion does only when its master has set it Aggressive. Defensive still fights back,
        /// because retaliation comes through SetActionFighting rather than through a scan.
        /// </summary>
        private static bool ScansForEnemies(Creature creature)
        {
            return creature.MasterEntityId == 0 || creature.Stance == MinionStance.Aggressive;
        }

        private void UpdateCreatureTimers(Creature creature, long delta)
        {
            creature.LastAgression += delta;
            creature.LastRestTime += delta + new Random().Next(1, 100);
            creature.Controller.TimerPathUpdateLock -= delta;

            // update cooldown timer of all actions
            foreach (var action in creature.Actions)
                action.CooldownTimer -= delta;
        }
        
        /// <summary>
        /// Steps a creature toward (difX, difY, difZ) and broadcasts the movement.
        /// </summary>
        /// <param name="speed">Units per second; also what the client is told to extrapolate at.</param>
        /// <param name="elapsedMs">Time since the creature last moved.</param>
        /// <returns>The distance actually moved.</returns>
        float UpdateEntityMovement(double difX, double difY, double difZ, Creature creature, MapChannel mapChannel, float speed, bool isMoved, long elapsedMs,
            bool synchronizeVisibility = false)
        {
            var remaining = Math.Sqrt(difX * difX + difY * difY + difZ * difZ);
            if (remaining < 0.0001d)
                return 0;
            var length = 1.0d / remaining;
            difX *= length;
            difY *= length;
            difZ *= length;
            var vX = (float)Math.Atan2(-difX, -difZ);

            var velocity = isMoved ? speed : 0.0f;

            // Distance from elapsed time rather than a fixed speed/4 per call. The fixed step was
            // calibrated for the original 250 ms think rate; tying it to real time keeps creatures
            // at their stated speed however the MainLoop ticks land. Clamped to the distance left
            // so a large step cannot carry the creature past its node.
            var step = (float)Math.Min(velocity * elapsedMs / 1000.0d, remaining);

            if (isMoved)
            {
                creature.Position += new Vector3((float)(difX * step), (float)(difY * step), (float)(difZ * step));

                // Path corners carry the navmesh height; between them the ground is not a
                // straight line, so keep the feet on it.
                creature.Position = NavMeshManager.SnapToGround(mapChannel, creature.Position);
            }

            if (synchronizeVisibility)
            {
                creature.Rotation = vX;
                SynchronizeMovementCell(mapChannel, creature);
            }

            // send movement update
            var movement = new Movement(new Vector3(creature.Position.X, creature.Position.Y, creature.Position.Z), velocity, 0x08, new Vector2(vX, 0f));

            PublishMovement(creature, movement);

            return step;
        }
    }
}
