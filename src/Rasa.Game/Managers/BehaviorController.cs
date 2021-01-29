using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Memory;
    using Packets.MapChannel.Client;
    using Structures;

    public class BehaviorManager
    {
        public const byte PathLengthLimit = 72;

        private const byte PathModeOneShot  = 0; // creature will walk along the path once
        private const byte PathModeCycle    = 1; // creature will walk along the path, then go to the very first node again and repeat
        private const byte PathModeReturn   = 2; // creature will walk along the path, then go the the whole path back and repeat

        public const byte BehaviorActionIdle = 0;
        public const byte BehaviorActionFollowingPath = 1;  // will automatically be triggered by wander if there is an active ai path
        public const byte BehaviorActionFighting = 2;
        public const byte BehaviorActionWander = 3;
        public const byte BehaviorActionPatrol = 4;

        public const byte WanderIdle = 0;
        public const byte WanderMoving = 1;

        private long PassedTime = 0;
        private readonly long CreatureRestTime = 15000;

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

        // returns the distance moved
        float UpdateEntityMovement(double difX, double difY, double difZ, Creature creature, MapChannel mapChannel, float speeddiv, bool isMoved)
        {
            var length = 1.0d / Math.Sqrt(difX * difX + difY * difY + difZ * difZ);
            var velocity = 0.0f;
            difX *= length;
            difY *= length;
            difZ *= length;
            var vX = Math.Atan2(-difX, -difZ);
            // multiplicate with speed
            if (isMoved == true)
                velocity = speeddiv;
            else
                velocity = 0.0f;
            velocity /= 4.0f;
            difX *= velocity;
            difY *= velocity;
            difZ *= velocity;

            // move unit
            if (isMoved == true)
                creature.Actor.Position += new Vector3((float)difX, (float)difY, (float)difZ);

            // send movement update
            var movementData = new MovementData(creature.Actor.Position.X, creature.Actor.Position.Y, creature.Actor.Position.Z, (float)vX)
            {
                Flags = 0x08,
                Velocity = velocity * 4.0f
            };

            CellManager.Instance.CellMoveObject(creature, movementData);

            return velocity;
        }

        internal void MapChannelThink(MapChannel mapChannel, long delta)
        {
            PassedTime += delta;

            if (PassedTime < 100)
                return;

            // creature deletion and update queue
            var queue_creatureDeletion = new List<Creature>();
            var queue_creatureCellUpdate = new List<Creature>();
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

                    for (var f = 0; f < mapCell.CreatureList.Count; f++)
                    {
                        CreatureThink(mapChannel, mapCell.CreatureList[f], PassedTime, out var needDeletion, out var needCellUpdate); // update time hardcoded, see todo

                        if (needDeletion)
                            queue_creatureDeletion.Add(mapCell.CreatureList[f]);

                        if (needCellUpdate) // update cell (even when creature is also deleted)
                            queue_creatureCellUpdate.Add(mapCell.CreatureList[f]);

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
                    var newLocX = (uint)((creatureList[f].Actor.Position.X / CellManager.CellSize) + CellManager.CellBias);
                    var newLocZ = (uint)((creatureList[f].Actor.Position.Z / CellManager.CellSize) + CellManager.CellBias);

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

            PassedTime = 0;
        }

        /// <CheckForAttackableEntityInRange>
        /// Checks for enemy creatures and players within the given range
        /// </CheckForAttackableEntityInRange>
        private bool CheckForAttackableEntityInRange(MapChannel mapChannel, Creature creature, float range)
        {
            var foundEntity_distance = range + 100.0f; // value that is guaranteed to be higher than the found creature
            var foundEntity_entityId = 0ul;
            
            foreach (var cellSeed in creature.Actor.Cells)
            {
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                {
                    // AFS do not attack AFS
                    if (creature.Faction == Factions.AFS)
                        continue;

                    if (client.MapClient.GmFlagAlwaysFriendly)
                        continue;

                    if (client.MapClient.Player.Actor.Attributes[Attributes.Health].Current <= 0)
                        continue;

                    // check distance so creature attack closes target
                    var dist = Vector3.Distance(creature.Actor.Position, client.MapClient.Player.Actor.Position);

                    if (dist <= range)
                    {
                        // set target and change state
                        if (dist < foundEntity_distance)
                        {
                            foundEntity_entityId = client.MapClient.Player.Actor.EntityId;
                            foundEntity_distance = dist;
                        }
                    }
                }

                foreach (var tCreature in mapChannel.MapCellInfo.Cells[cellSeed].CreatureList)
                {
                    if (tCreature.Actor.Attributes[Attributes.Health].Current <= 0)
                        continue;

                    if (tCreature == creature)
                        continue;

                    if (tCreature.Faction == creature.Faction)
                        continue;

                    // check distance
                    var dist = Vector3.Distance(creature.Actor.Position, tCreature.Actor.Position);

                    if (dist <= range)
                    {
                        // set target and change state
                        if (dist < foundEntity_distance)
                        {
                            foundEntity_entityId = tCreature.Actor.EntityId;
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

        public void SetActionFighting(Creature creature, ulong targetEntityId)
        {
            creature.Controller.CurrentAction = BehaviorActionFighting;
            creature.Controller.PathIndex = 0;
            creature.Controller.PathLength = 0;
            creature.Controller.ActionFighting.TargetEntityId = targetEntityId;
            creature.LastAgression = 0;
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

            if (creature.Actor.Attributes[Attributes.Health].Current <= 0)
            {
                creature.Controller.DeadTime += delta;
                if (creature.Controller.DeadTime >= 20000)
                {
                    // disappear after 20 seconds
                    needDeletion = true;
                }
                return; // creature dead
            }
            // calculate new cell position
            var cellX = (uint)((creature.Actor.Position.X / CellManager.CellSize) + CellManager.CellBias);
            var cellZ = (uint)((creature.Actor.Position.Z / CellManager.CellSize) + CellManager.CellBias);
            // creature keep apart (we use a stupid trick for this, but it works rather well)
            // pick a random creature from the same cell

            var tCell = CellManager.Instance.GetCell(mapChannel, cellX, cellZ);
            if (tCell != null && tCell.CreatureList.Count > 1)
            {
                var randomCreatureIndex = new Random().Next(tCell.CreatureList.Count);
                // get the creature
                var tCreature = tCell.CreatureList[randomCreatureIndex];
                // is it a different alive creature?
                if (creature != tCreature && tCreature.Actor.Attributes[Attributes.Health].Current > 0)
                {
                    var difX = creature.Actor.Position.X - tCreature.Actor.Position.X;
                    var difY = creature.Actor.Position.Y - tCreature.Actor.Position.Y;
                    var difZ = creature.Actor.Position.Z - tCreature.Actor.Position.Z;
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

                            var creatureX = creature.Actor.Position.X + difX;
                            var creatureY = creature.Actor.Position.Y + difY;
                            var creatureZ = creature.Actor.Position.Z + difZ;
                            var tCreatureX = tCreature.Actor.Position.X - difX;
                            var tCreatureY = tCreature.Actor.Position.Y - difY;
                            var tCreatureZ = tCreature.Actor.Position.Z - difZ;

                            // push
                            creature.Actor.Position = new Vector3(creatureX, creatureY, creatureZ);
                            tCreature.Actor.Position = new Vector3(tCreatureX, tCreatureY, tCreatureZ);
                        }
                    }
                }
            }

            // do we need to check for updated cell position?
            creature.UpdatePositionCounter -= delta;

            if (creature.UpdatePositionCounter <= 0)
            {
                // check for changed cell
                var cellSeed = CellManager.Instance.GetCellSeed(creature.Actor.Position);

                // calculate initial cell
                if (cellSeed != creature.Actor.Cells[2, 2])
                    needCellUpdate = true;

                creature.UpdatePositionCounter = CreatureManager.CreatureLocationUpdateTime;
            }

            if (creature.Controller.CurrentAction == BehaviorActionWander)
            {
                // scan for enemy
                if (creature.LastAgression >= 30000)    // 30 sec
                    if (CheckForAttackableEntityInRange(mapChannel, creature, creature.AggroRange))
                    {
                        // enemy found!
                        return;
                    }

                if (creature.Controller.ActionWander.State == WanderIdle)
                {
                    //--- idle for int time before get new wander position
                    if (creature.LastRestTime > CreatureRestTime)
                    {
                        // does creature have a path?
                        if (creature.Controller.AiPathFollowing.GeneralPath != null)
                        {
                            // has path -> don't wander aimlessly, go path walking
                            SetActionPathFollowing(creature);
                            return;
                        }

                        if (creature.WanderDistance < 0.01f)
                            return; // creature doesn't wander

                        // calc target
                        var srndx = (float)(new Random().Next() % 1000) * 0.001f * creature.WanderDistance;
                        var srndz = (float)(new Random().Next() % 1000) * 0.001f * creature.WanderDistance;

                        char[] operators = { '+', '-' };
                        char op = operators[new Random().Next(operators.Length)];
                        char op1 = operators[new Random().Next(operators.Length)];

                        switch (op)
                        {
                            case '+':
                                creature.Controller.ActionWander.WanderDestination[0] = creature.HomePos.Position.X + srndx;
                                break;

                            case '-':
                                creature.Controller.ActionWander.WanderDestination[0] = creature.HomePos.Position.X - srndx;
                                break;
                        }

                        creature.Controller.ActionWander.WanderDestination[1] = creature.HomePos.Position.Y;

                        switch (op1)
                        {
                            case '+':
                                creature.Controller.ActionWander.WanderDestination[2] = creature.HomePos.Position.Z + srndz;
                                break;

                            case '-':
                                creature.Controller.ActionWander.WanderDestination[2] = creature.HomePos.Position.Z - srndz;
                                break;
                        }
                        
                        // next step approaching
                        creature.Controller.ActionWander.State = WanderMoving;
                        creature.LastRestTime = 0;
                    }
                }
                if (creature.Controller.ActionWander.State == WanderMoving)
                {
                    // following path (short path)
                    if (creature.Controller.PathLength == 0)
                    {
                        // no path, generate new one
                        var startPos = new float[3];
                        var endPos = new float[3];
                        startPos[0] = creature.Actor.Position.X;
                        startPos[1] = creature.Actor.Position.Y;
                        startPos[2] = creature.Actor.Position.Z;
                        endPos[0] = creature.Controller.ActionWander.WanderDestination[0];
                        endPos[1] = creature.Controller.ActionWander.WanderDestination[1];
                        endPos[2] = creature.Controller.ActionWander.WanderDestination[2];
                        creature.Controller.PathIndex = 0;

                        // later we can implement "navmesh" so creature move more acurate on terrain
                        //creature.Controller.PathLength = navmesh_getPath(mapChannel, startPos, endPos, creature.Controller.path, false);
                        creature.Controller.PathLength = 1;
                        creature.Controller.Path = endPos;

                        if (creature.Controller.PathLength == 0)
                        {
                            // path could not be generated or too short
                            // leave state and go idle mode
                            creature.Controller.ActionWander.State = WanderIdle;
                            creature.LastRestTime = 0;
                            return;
                        }
                    }
                    // get distance
                    //var nextPathNodePos = creature.Controller.Path + (creature.Controller.PathIndex * 3);
                    var nextPathNodePos = creature.Controller.Path;
                    var difX = nextPathNodePos[0] - creature.Actor.Position.X;
                    var difY = nextPathNodePos[1] - creature.Actor.Position.Y;
                    var difZ = nextPathNodePos[2] - creature.Actor.Position.Z;
                    var dist = difX * difX + difZ * difZ;
                    // wander target location reached
                    if (dist > 0.01f) // to avoid division by zero
                    {
                        var distanceMoved = UpdateEntityMovement(difX, difY, difZ, creature, mapChannel, creature.WalkSpeed, true);
                        // sometimes it is possible the creature walks past the pathnode a tiny bit,
                        // which will force him to move back a step, it does look ugly so here is a tiny workaround
                        if (distanceMoved > dist) // distance moved greater than distance left?
                            dist = 0.0f; // mark pathnode reached
                    }
                    if (dist < 0.8f)
                    {
                        creature.Controller.PathIndex++; // goto next node
                        if (creature.Controller.PathIndex >= creature.Controller.PathLength)
                        {
                            creature.Controller.PathLength = 0;
                            creature.Controller.ActionWander.State = WanderIdle;
                            return;
                        }
                    }
                }
            }
            else if (creature.Controller.CurrentAction == BehaviorActionFollowingPath)
            {
                // following predefined path (long path)
                // scan for enemy
                if (CheckForAttackableEntityInRange(mapChannel, creature, creature.AggroRange))
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
                // get distance
                var difX = currentTargetNodePos[0] - creature.Actor.Position.X;
                var difY = currentTargetNodePos[1] - creature.Actor.Position.Y;
                var difZ = currentTargetNodePos[2] - creature.Actor.Position.Z;
                var dist = difX * difX + difZ * difZ;
                // wander target location reached
                if (dist > 0.01f) // to avoid division by zero
                    UpdateEntityMovement(difX, difY, difZ, creature, mapChannel, creature.WalkSpeed, true);
                if (dist < 0.8f)
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
                    SetActionWander(creature);
                    return;
                }
                // leave combat after 
                if (creature.LastAgression > creature.AggressionTime)
                {
                    SetActionWander(creature);
                    return;
                }
                // get position of target
                var targetX = 0.0f;
                var targetY = 0.0f;
                var targetZ = 0.0f;
                var targetName = "";
                if (target == EntityType.Player)
                {
                    var mapClient = EntityManager.Instance.GetPlayer(creature.Controller.ActionFighting.TargetEntityId);
                    // if target dead, set wander state
                    if (mapClient.Player.Actor.Attributes[Attributes.Health].Current <= 0 || mapClient.Player.Actor.State == CharacterState.Dead)
                    {
                        SetActionWander(creature);
                        return;
                    }
                    targetX = mapClient.Player.Actor.Position.X;
                    targetY = mapClient.Player.Actor.Position.Y;
                    targetZ = mapClient.Player.Actor.Position.Z;
                    targetName = mapClient.Player.Actor.Name;
                }
                else if (target == EntityType.Creature)
                {
                    var targetCreature = EntityManager.Instance.GetCreature(creature.Controller.ActionFighting.TargetEntityId);

                    if (targetCreature.Actor.Attributes[Attributes.Health].Current <= 0 || targetCreature.Actor.State == CharacterState.Dead)
                    {
                        // exit visual combat mode
                        CellManager.Instance.CellCallMethod(mapChannel, creature.Actor, new RequestVisualCombatModePacket(false));

                        SetActionWander(creature);
                        return;
                    }
                    targetX = targetCreature.Actor.Position.X;
                    targetY = targetCreature.Actor.Position.Y;
                    targetZ = targetCreature.Actor.Position.Z;
                    targetName = targetCreature.Actor.Name;
                }
                else
                    Logger.WriteLog(LogType.Error, $"CreatureThink: unsuported Traget type {target}"); // todo

                var targetDistX = (targetX - creature.Actor.Position.X);
                var targetDistY = (targetY - creature.Actor.Position.Y);
                var targetDistZ = (targetZ - creature.Actor.Position.Z);
                var targetDistSqr = (targetDistX * targetDistX + targetDistY * targetDistY + targetDistZ * targetDistZ);
                // stop tracking target after target exceeds a certain distance to home pos
                // Note: For patrolling creatures the homePos is the last arrived path node 
                var homeLocDistX = (creature.HomePos.Position.X - targetX);
                var homeLocDistZ = (creature.HomePos.Position.Z - targetZ);
                var homeLocDist = homeLocDistX * homeLocDistX + homeLocDistZ * homeLocDistZ;

                if (homeLocDist >= 60.0f * 60.0f)
                {
                    creature.LastRestTime = 0; // forces AI to immediately calculate new wander position
                    SetActionWander(creature);
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
                    UpdateEntityMovement(targetDistX, targetDistY, targetDistZ, creature, mapChannel, 0.0f, false);

                    // execute action and quit
                    var dmg = action.MinDamage + (new Random().Next() % (action.MaxDamage - action.MinDamage + 1));

                    // do damage
                    MissileManager.Instance.MissileLaunch(mapChannel, creature.Actor, creature.Controller.ActionFighting.TargetEntityId, dmg, action.ActionId, action.ActionArgId);

                    // set cooldown
                    action.CooldownTimer = action.Coolodown;

                    // creature used action, break loop
                    break;
                }

                if (needToMove == false)
                    return;

                if (targetDistSqr <= 3.0f * 3.0f)
                    return;// near enough, dont move

                // after checking for melee and range attack without success, do pathing
                // invalidate path if the target moved away too far from the original path destination
                var tempPos = new float[3];
                tempPos[0] = targetX;
                tempPos[1] = targetY;
                tempPos[2] = targetZ;
                // generate path if there is no current
                if (creature.Controller.TimerPathUpdateLock <= 0)
                {
                    if (creature.Controller.PathLength > 0 && GetDistanceSqr(creature.Controller.ActionFighting.LockedTargetPosition, tempPos) > 1.0f)
                        creature.Controller.PathLength = 0;

                    if (creature.Controller.PathLength == 0)
                    {
                        // update path update lock timer
                        creature.Controller.TimerPathUpdateLock = 5000;

                        var pathTarget = new float[3];
                        if (targetDistSqr < 0.1f)
                        {
                            // if too near, move out of enemy by running to random point somewhere x units around the creature
                            var angle = (new Random().Next() / 32767.0f) * 6.28318f; // random angle
                            var distance = 2.5f; // keep 2.5 meter distance
                            pathTarget[0] = targetX + (float)Math.Cos(angle) * distance;
                            pathTarget[1] = targetY;
                            pathTarget[2] = targetZ + (float)Math.Sin(angle) * distance;
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
                            pathTarget[0] = targetX + vecV2A[0] * distance;
                            pathTarget[1] = targetY;
                            pathTarget[2] = targetZ + vecV2A[1] * distance;
                        }

                        var startPos = new float[3];
                        var endPos = new float[3];
                        startPos[0] = creature.Actor.Position.X;
                        startPos[1] = creature.Actor.Position.Y;
                        startPos[2] = creature.Actor.Position.Z;
                        endPos[0] = pathTarget[0];
                        endPos[1] = pathTarget[1];
                        endPos[2] = pathTarget[2];
                        creature.Controller.PathIndex = 0;
                        creature.Controller.PathLength = 1;
                        creature.Controller.Path = endPos;
                        if (creature.Controller.Path == null)
                        {
                            Logger.WriteLog(LogType.Error, "Cannot find path");
                            return;
                        }
                        // also update path target variable (using creature position, not path target position)
                        creature.Controller.ActionFighting.LockedTargetPosition[0] = targetX;
                        creature.Controller.ActionFighting.LockedTargetPosition[1] = targetY;
                        creature.Controller.ActionFighting.LockedTargetPosition[2] = targetZ;
                    }
                }
                // follow path
                if (creature.Controller.PathIndex < creature.Controller.PathLength)
                {
                    // get distance
                    var nextPathNodePos = creature.Controller.Path;
                    var difX = nextPathNodePos[0] - creature.Actor.Position.X;
                    var difY = nextPathNodePos[1] - creature.Actor.Position.Y;
                    var difZ = nextPathNodePos[2] - creature.Actor.Position.Z;
                    var dist = difX * difX + difZ * difZ;
                    var skipDetected = false;

                    if (dist > 0.01f) // to avoid division by zero
                    {
                        UpdateEntityMovement(difX, difY, difZ, creature, mapChannel, creature.RunSpeed, true);
                        // on high movement speeds the movement steps can be large, check if creature didn't run too far
                        difX = nextPathNodePos[0] - creature.Actor.Position.X;
                        difY = nextPathNodePos[1] - creature.Actor.Position.Y;
                        difZ = nextPathNodePos[2] - creature.Actor.Position.Z;

                        var dist2 = difX * difX + difZ * difZ;

                        if (dist2 >= dist)
                            skipDetected = true;
                    }

                    if (dist < 0.9f || skipDetected)
                    {
                        creature.Controller.PathIndex++; // goto next node
                        if (creature.Controller.PathIndex >= creature.Controller.PathLength)
                        {
                            creature.Controller.PathLength = 0;
                            creature.Controller.PathIndex = 0;
                        }
                    }
                }
            }//---fighting
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

        private float GetDistanceSqr(float[] p1, float[] p2)
        {
            float dx = p1[0] - p2[0];
            float dy = p1[1] - p2[1];
            float dz = p1[2] - p2[2];
            return dx * dx + dy * dy + dz * dz;
        }

        private void SetActionWander(Creature creature)
        {
            creature.Controller.CurrentAction = BehaviorActionWander;
            creature.Controller.ActionWander.State = WanderIdle;
            creature.Controller.PathIndex = 0;
            creature.Controller.PathLength = 0;
        }

        private void SetActionPathFollowing(Creature creature)
        {
            creature.Controller.CurrentAction = BehaviorActionFollowingPath;
            // random position bias added to every node (to make groups look like they do not run on the same path)
            creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0] =  ((new Random().Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
            creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1] = ((new Random().Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;

            Logger.WriteLog(LogType.AI, $"path 0{creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0]}");
            Logger.WriteLog(LogType.AI, $"path 1{creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1]}");
        }
    }
}
