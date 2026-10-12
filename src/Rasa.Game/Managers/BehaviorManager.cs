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
        /// Wander pace, metres per second. creature.walk_speed is 5 for every row in the database -
        /// a jog, and the client shows it as one. Strolling creatures are capped at a walk; chases
        /// still use run_speed.
        /// </summary>
        public const float WanderWalkSpeed = 1.6f;

        /// <summary>
        /// How long a creature stands before its next stroll, counted from when it stopped - after
        /// a stroll, a fight or a leash alike. A spawned creature's first wait is drawn from anywhere
        /// in the interval (<see cref="StartWandering"/>), so a camp that came up together does not
        /// set off together; after that, strolls of different lengths keep them apart. Not in the
        /// client.
        /// </summary>
        public const long WanderIntervalMs = 45000;

        /// <summary>The furthest a stroll goes from where the creature stands, across the ground.</summary>
        public const float WanderStepDistance = 10f;

        /// <summary>The least room a stroll's end leaves to every other creature, and to wherever another is already walking.</summary>
        public const float WanderSpacing = 4f;

        /// <summary>The least a stroll moves the creature, so the spot it picks is not the one it is standing on.</summary>
        public const float WanderMinStep = 2f;

        /// <summary>
        /// The spawn zone of a creature whose pool is a point (radius 0) or that has no pool - a GM
        /// spawn - is this far around its spawn point. A pool with a radius is its own zone.
        /// </summary>
        public const float WanderZoneFallbackRadius = 10f;

        /// <summary>How many points a stroll draws before it gives up until the next interval.</summary>
        private const int WanderAttempts = 16;

        /// <summary>A stroll that has not arrived in this long - walking into a wall off the navmesh - ends where the creature is.</summary>
        private const long WanderMoveTimeoutMs = 20000;
        public const byte PathLengthLimit = 72;

        private const byte PathModeOneShot  = 0; // creature will walk along the path once
        private const byte PathModeCycle    = 1; // creature will walk along the path, then go to the very first node again and repeat
        private const byte PathModeReturn   = 2; // creature will walk along the path, then go the the whole path back and repeat

        public const byte BehaviorActionIdle = 0;
        public const byte BehaviorActionFollowingPath = 1;  // will automatically be triggered by wander if there is an active ai path
        public const byte BehaviorActionFighting = 2;
        public const byte BehaviorActionWander = 3;

        /// <summary>
        /// Walking its beat (Patrols; <see cref="AdvancePatrol"/>): what a creature with one does
        /// in place of standing and strolling. It notices a fight as a wandering creature does,
        /// and goes back to wandering - and from there to its beat - when it is over.
        /// </summary>
        public const byte BehaviorActionPatrol = 4;

        /// <summary>
        /// Trailing a master, or holding an anchor point. Only minions are ever in this state;
        /// an ordinary creature has a spawn point to wander around instead.
        /// </summary>
        public const byte BehaviorActionFollow = 5;

        /// <summary>A mission scene's actor walking an authored path to an authored pose (SetActionScriptedMove).</summary>
        internal const byte BehaviorActionScriptedMove = 7;

        /// <summary>
        /// Chased too far from home and running back to it (<see cref="Leash"/>): it takes no
        /// damage and picks no fight on the way, and arrives whole.
        /// </summary>
        public const byte BehaviorActionReturning = 6;

        /// <summary>
        /// How far a creature with no master follows a fight from its home before it gives up and
        /// goes back (<see cref="Leash"/>): its own distance, across the ground. The target's
        /// does not matter - a sniper at home shooting someone forty metres off is not chasing.
        /// Not in the client; 60 m is what the leash here has always been.
        /// </summary>
        public const float MaxChaseDistance = 60f;

        /// <summary>How close to home a returning creature has to get to count as there.</summary>
        private const float HomeReachedDistance = 2f;

        /// <summary>The least time a returning creature is given before it is put home regardless.</summary>
        private const long ReturnTimeoutMinMs = 10000;

        /// <summary>How often a creature re-tests whether it can see what it is shooting at.</summary>
        private const long SightRecheckMs = 500;

        /// <summary>
        /// How long an attack in range may have left to cool down and still keep its creature
        /// where it stands (ClosesInWhileCooling). A gunner between shots holds its ground; a
        /// Kael whose tectonic strike is ten seconds from ready walks in and uses its fists.
        /// </summary>
        public const long HoldForCooldownMs = 1500;

        /// <summary>
        /// The least and most a creature waits past an action's cooldown before it may use it
        /// again, as a share of that cooldown (NextCooldown): a random wait of 1% to 50% on top, so
        /// a pack does not fire in step and a rotation does not tick like a clock.
        /// </summary>
        public const double CooldownJitterMin = 0.01;
        public const double CooldownJitterMax = 0.50;

        private static readonly Random CooldownRandom = new Random();

        /// <summary>
        /// A creature's first attack of a fight is not instant (OpensNow): each think it has an
        /// attack ready, in range and in sight, it rolls to use it - OpeningChanceStep (25%) on the
        /// first such think, 50% on the second, 75% on the third, and certain on the
        /// OpeningThinks-th (the fourth). Until then it stands and faces its target. After the
        /// first attack its actions follow their cooldowns (NextCooldown, ByReadiness).
        /// </summary>
        public const int OpeningThinks = 4;
        public const double OpeningChanceStep = 1.0 / OpeningThinks;

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

        /// <summary>
        /// The nearest thing in one cell this creature would pick a fight with, within range,
        /// if it is nearer than what has been found so far.
        /// </summary>
        private static void ScanCell(MapChannel mapChannel, Creature creature, MapCell cell, float range, ref float foundDistance, ref ulong foundId)
        {
            // A mission escort never picks a fight with a player, and only with a creature that is
            // on the other side of its scene (CreatureManager.IsHostileTarget).
            var escort = IsMissionEscort(creature);

            foreach (var client in cell.ClientList)
            {
                if (escort)
                    break;

                // Cell lists can hold a client whose character is already gone. A player is
                // FRIENDLY - sought by HOSTILE creatures only - whatever Polymorph has made
                // them look like.
                // An Aggressive summon also goes for its master's enemies across a wargame, unless
                // PvP Safety is on them (Pvp.SummonMayFight).
                if (client.Player == null || (!TargetCategories.Seeks(creature.TargetCategory, client.Player.CombatCategory)
                    && !(Pvp.SummonMayFight(creature, client.Player.EntityId) && !Pvp.IsSafe(client.Player))))
                    continue;

                // Gone, and waiting to be taken out of the world: nothing to pick a fight with -
                // unless the connection dropped mid-fight and the body is still in it (CombatLogout).
                if (client.Player.IsGone)
                    continue;

                if (client.Player.GmFlagAlwaysFriendly)
                    continue;

                if (client.Player.Attributes[Attributes.Health].Current <= 0)
                    continue;

                // Cloaked (Cloak Wave): it cannot be noticed however close it stands. Nor can
                // someone watching a camera script, who has no controls to answer with.
                if (Detection.IsHidden(client.Player) || CameraScripts.IsWatching(client.Player))
                    continue;

                // check distance so creature attack closes target
                var dist = Vector3.Distance(creature.Position, client.Player.Position);

                // Stealth Armor: noticed that much closer (Manifestation.DetectionRangePercent).
                if (dist <= range * client.Player.DetectionRangePercent / 100f)
                {
                    // set target and change state
                    if (dist < foundDistance)
                    {
                        foundId = client.Player.EntityId;
                        foundDistance = dist;
                    }
                }
            }

            foreach (var tCreature in cell.CreatureList)
            {
                if (escort && !CreatureManager.IsHostileTarget(mapChannel, creature, tCreature))
                    continue;

                if (tCreature.Attributes[Attributes.Health].Current <= 0 || tCreature.State == CharacterState.Dying)
                    continue;

                if (!Game.Missions.World.CreatureGameplayRules.CanParticipateInCombat(tCreature))
                    continue;

                if (tCreature == creature)
                    continue;

                if (!TargetCategories.Seeks(creature.TargetCategory, tCreature.TargetCategory) && !Pvp.SummonMayFight(creature, tCreature.EntityId))
                    continue;

                // check distance
                var dist = Vector3.Distance(creature.Position, tCreature.Position);

                if (dist <= range)
                {
                    // set target and change state
                    if (dist < foundDistance)
                    {
                        foundId = tCreature.EntityId;
                        foundDistance = dist;
                    }
                }
            }

            // A Personal Waypoint a player has put down (PersonalWaypoints): to a creature that
            // fights players it is what its owner is, and no more hidden than it looks.
            foreach (var obj in cell.DynamicObjectList)
            {
                if (escort || obj.DynamicObjectType != DynamicObjectType.PersonalWaypoint || !PersonalWaypoints.MayBeFoughtBy(creature, obj.EntityId))
                    continue;

                var dist = Vector3.Distance(creature.Position, obj.Position);

                if (dist <= range && dist < foundDistance)
                {
                    foundId = obj.EntityId;
                    foundDistance = dist;
                }
            }
        }

        /// <CheckForAttackableEntityInRange>
        /// Checks for enemy creatures and players within the given range
        /// </CheckForAttackableEntityInRange>
        private bool CheckForAttackableEntityInRange(MapChannel mapChannel, Creature creature, float range)
        {
            var foundEntity_distance = range + 100.0f; // value that is guaranteed to be higher than the found creature
            var foundEntity_entityId = 0ul;

            // Blinded (Tactical Evasion's mag flash): it notices nobody at all.
            if (Detection.IsBlind(creature))
                return false;

            // Only HOSTILE and FRIENDLY go looking for a fight, and only with each other
            // (TargetCategories.Seeks): a NEUTRAL creature waits to be attacked, and an object or
            // decoration takes no part.
            if (creature.TargetCategory != TargetCategory.Hostile && creature.TargetCategory != TargetCategory.Friendly)
                return false;

            // Only the cells the range can reach. This used to walk the whole 5x5 matrix - 128 m
            // across - for an aggro range that is 18 m unless something raised it, on every think
            // of every idle creature on the map, player nearby or not. A range under one cell
            // fits in the 3x3 around the cell the creature is standing in now; anything longer
            // keeps the matrix, which is as far as anything can see.
            var reach = (int)Math.Ceiling(range / CellManager.CellSize);

            if (reach >= 2)
            {
                foreach (var cell in CellManager.CellsIn(mapChannel, creature.Cells))
                    ScanCell(mapChannel, creature, cell, range, ref foundEntity_distance, ref foundEntity_entityId);
            }
            else
            {
                var cells = mapChannel.MapCellInfo.Cells;

                // As CellManager.GetCell numbers them; a cell the map has not got is empty ground.
                var centreX = (uint)(creature.Position.X / CellManager.CellSize + CellManager.CellBias);
                var centreZ = (uint)(creature.Position.Z / CellManager.CellSize + CellManager.CellBias);

                for (var dx = -reach; dx <= reach; dx++)
                    for (var dz = -reach; dz <= reach; dz++)
                    {
                        var cellX = (uint)(centreX + dx);
                        var cellZ = (uint)(centreZ + dz);

                        if (cells.TryGetValue((cellX & 0xFFFF) | (cellZ << 16), out var cell))
                            ScanCell(mapChannel, creature, cell, range, ref foundEntity_distance, ref foundEntity_entityId);
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

            // A crab mine: AbilityManager steers it, clears it away when it is spent, and all it
            // does here is go where it is sent - it is never a corpse to despawn.
            if (creature.IsScripted)
            {
                needCellUpdate = CellChanged(creature, delta);
                return;
            }

            // A corpse by its state as much as by its health: a creature marked dead with health
            // left on it is not one to go on to the fighting below.
            if (creature.State == CharacterState.Dead || creature.Attributes[Attributes.Health].Current <= 0)
            {
                // The dead hold no pose (NpcPoses).
                NpcPoses.Drop(mapChannel, creature);

                // A walker that has fallen becomes its wreck (AlternateMesh).
                AlternateMesh.DeadTick(mapChannel, creature, Environment.TickCount64);

                // A corpse with loot still on it, or with someone's window open on it, stays
                // longer than one that has been cleared: twenty seconds from the kill is about
                // one more fight, and bodies were going before anyone could loot them. The clock
                // runs under the map's loot lock (LootDispenserManager.AdvanceCorpseLifetime).
                // A turret's wreck stays on its mount whatever the clock says, until its pool
                // puts it back in service (AlternateMesh.KeepsWreck).
                if (LootDispenserManager.Instance.AdvanceCorpseLifetime(mapChannel, creature, delta)
                    && !AlternateMesh.KeepsWreck(creature))
                    needDeletion = true;

                return; // creature dead
            }

            // Held in its Critical Death window: it neither moves nor fights, nor holds a pose.
            if (creature.State == CharacterState.Dying)
            {
                NpcPoses.Drop(mapChannel, creature);
                return;
            }

            // What it shouts about how it stands now against the think before: a fight begun or
            // over, a wound, a halt on its beat (Battlecries).
            Battlecries.Observe(mapChannel, creature);

            // Knocked back, or stunned: it goes where the knockback carries it and nothing else,
            // but it can still cross into another cell doing so.
            var knockedBack = StepKnockback(mapChannel, creature, delta);

            // A pose is held standing idle at a post (NpcPoses): thrown, stunned, walking or
            // run by something else, it is off until the creature is idle again.
            if (creature.PoseShown && (knockedBack || Stuns.IsStunned(creature) || !NpcPoses.AtRest(creature)))
                NpcPoses.Drop(mapChannel, creature);

            // Mid-charge (Kael rushing blow), winding up to blow itself up (a Fithik), to drop an
            // egg (a Stalker) or any ability (CreatureWindups), in a cocoon (an Atta grub), a
            // cloud (a Miasma, CreatureMiasma), or climbing out of its egg cluster (a Fithik,
            // FithikEggClusters), it does nothing else until that is done.
            if (knockedBack || Stuns.IsStunned(creature) || KaelRushingBlow.IsCharging(creature) || CreatureBombs.IsSelfDestructing(creature)
                || CreatureSupport.IsCasting(creature) || CreatureHabits.IsBusy(creature) || CreatureSummons.IsBusy(creature)
                || CreatureWindups.IsWindingUp(creature) || CreatureMiasma.IsDissipated(creature) || FithikEggClusters.IsHatching(creature))
            {
                // Busy in a fight is still the fight. LastAgression is how long the fighting
                // think has gone without attending to its target, and AggressionTime (5 s) of
                // it gives the fight up; it grew through every return here, so a long windup, a
                // channel or a chain of stuns ran it out, and the creature came out of them to
                // drop its hate table and turn on whoever stood nearest.
                if (creature.Controller.CurrentAction == BehaviorActionFighting)
                    creature.LastAgression = 0;

                needCellUpdate = CellChanged(creature, delta);
                return;
            }

            // A mission scene's actor: an authored walk, an escort at its owner's side, a base's
            // defender. Each keeps its own cell as it goes.
            if (creature.Controller.CurrentAction == BehaviorActionScriptedMove)
            {
                AdvanceScriptedMove(mapChannel, creature, delta);
                return;
            }

            if (creature.Controller.CurrentAction == BehaviorActionFighting &&
                !Game.Missions.World.CreatureGameplayRules.CanParticipateInCombat(creature))
            {
                creature.Hate.Clear();
                CreatureWeaponDraw.Stow(mapChannel, creature);
                creature.Controller.ActionFighting.TargetEntityId = 0;
                creature.Target = 0;
                SetActionAnchor(creature, creature.Position);
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
                var randomCreatureIndex = Random.Shared.Next(tCell.CreatureList.Count);
                // get the creature
                var tCreature = tCell.CreatureList[randomCreatureIndex];
                // is it a different alive creature?
                // An emplacement is bolted down: it neither pushes nor is pushed.
                // Scripted actors keep their authored path and final pose.
                if (creature != tCreature && tCreature.Attributes[Attributes.Health].Current > 0
                    && !Emplacements.Is(creature) && !Emplacements.Is(tCreature)
                    && !IsMissionEscort(creature) && !Game.Missions.World.CreatureGameplayRules.IsDefender(creature)
                    && tCreature.Controller.CurrentAction != BehaviorActionScriptedMove
                    && !IsMissionEscort(tCreature) && !Game.Missions.World.CreatureGameplayRules.IsDefender(tCreature))
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
            if (CellChanged(creature, delta))
                needCellUpdate = true;

            // Running home after a chase that went too far: nothing else until it is there.
            if (creature.Controller.CurrentAction == BehaviorActionReturning)
            {
                ReturnHome(mapChannel, creature, delta);
                return;
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

                var wander = creature.Controller.ActionWander;

                if (wander.State == WanderIdle)
                {
                    wander.IdleMs += delta;

                    // A creature with a beat walks it whenever it has nothing else to do (Patrols).
                    if (Patrols.Has(creature))
                    {
                        SetActionPatrol(creature);
                        return;
                    }

                    // An NPC with a post goes back to it and takes its pose up again (NpcPoses).
                    if (NpcPoses.HoldsPost(creature) && !creature.PoseShown && TakePost(mapChannel, creature))
                        return;

                    // A Filcher sees loot to steal, a hurt Xanx a dead one to eat; a Predator
                    // scans where it stands (CreatureHabits). An errand does not wait for the
                    // stroll's interval.
                    var errand = CreatureHabits.WhereTo(mapChannel, creature);

                    if (errand.HasValue)
                    {
                        wander.WanderDestination = errand.Value;
                        wander.State = WanderMoving;
                        wander.MovingMs = 0;
                        wander.IdleMs = 0;
                        wander.Errand = true;
                        creature.Controller.Path.Clear();
                        creature.Controller.PathIndex = 0;
                        creature.LastRestTime = 0;
                    }

                    //--- stands for WanderIntervalMs after it stops, then strolls
                    if (wander.IdleMs >= WanderIntervalMs)
                    {
                        // Whether or not it finds somewhere to go, the next try is a full interval off.
                        wander.IdleMs = 0;

                        // does creature have a path?
                        if (creature.Controller.AiPathFollowing.GeneralPath != null)
                        {
                            // has path -> don't wander aimlessly, go path walking
                            SetActionPathFollowing(creature);
                            return;
                        }

                        if (creature.WalkSpeed < 0.01f || creature.RunSpeed < 0.01f)
                            return; // creature doesn't wander

                        // A Shield Drone covers a piece of ground, so it stays on it.
                        if (ShieldDrone.HoldsGround(creature))
                            return;

                        // An NPC at a post stays at it (NpcPoses).
                        if (NpcPoses.HoldsPost(creature))
                            return;

                        var destination = PickStroll(mapChannel, creature);

                        // Nowhere in its zone with room enough: it stays where it is.
                        if (!destination.HasValue)
                            return;

                        wander.WanderDestination = destination.Value;
                        wander.State = WanderMoving;
                        wander.MovingMs = 0;
                        creature.Controller.Path.Clear();
                        creature.Controller.PathIndex = 0;
                        creature.LastRestTime = 0;
                    }
                }

                if (wander.State == WanderMoving)
                {
                    wander.MovingMs += delta;

                    // following path (short path)
                    if (creature.Controller.Path.Count == 0)
                        BuildPath(mapChannel, creature, wander.WanderDestination);

                    // Frightened, or on an errand: it runs, rather than strolls. Off a dropship or
                    // through a teleporter: it marches in.
                    var wanderSpeed = wander.Fleeing || wander.Errand ? creature.RunSpeed : wander.Arriving ? creature.WalkSpeed : Math.Min(creature.WalkSpeed, WanderWalkSpeed);
                    var timeout = wander.Arriving ? wander.ArrivalTimeoutMs : WanderMoveTimeoutMs;

                    if (FollowPath(mapChannel, creature, wanderSpeed, delta) || wander.MovingMs >= timeout)
                    {
                        wander.Arriving = false;

                        // There, or as near as it is going to get: it stops, and the interval
                        // starts again from now.
                        StopWalking(creature);
                        wander.Fleeing = false;

                        if (wander.Errand)
                        {
                            wander.Errand = false;
                            CreatureHabits.Arrived(mapChannel, creature);
                        }

                        wander.State = WanderIdle;
                        wander.IdleMs = 0;
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
                var assistedTarget = AssistedTarget(creature);

                if (assistedTarget != 0)
                {
                    creature.Target = assistedTarget;
                    SetActionFighting(creature, assistedTarget);

                    if (creature.Controller.CurrentAction == BehaviorActionFighting)
                        return;
                }

                var destination = creature.Controller.ActionFollow.Anchor;

                if (!creature.Controller.ActionFollow.HasAnchor)
                {
                    // TryGetValue: GetActor throws on a miss.
                    Actor followed = null;
                    if (creature.Controller.ActionFollow.FollowTargetId != 0)
                        EntityManager.Instance.Actors.TryGetValue(creature.Controller.ActionFollow.FollowTargetId, out followed);

                    // A mission escort follows its owner by character, whatever entity they are now.
                    if (followed == null && creature.SpawnPool?.FollowOwnerCharacterId > 0)
                    {
                        followed = mapChannel.ClientList
                            .Select(client => client?.Player)
                            .FirstOrDefault(player => player?.Id == creature.SpawnPool.FollowOwnerCharacterId);
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
            else if (creature.Controller.CurrentAction == BehaviorActionPatrol)
            {
                // It notices a fight on its beat as a wandering creature does.
                if (creature.LastAgression >= AggroScanDelayMs && ScansForEnemies(creature)
                    && CheckForAttackableEntityInRange(mapChannel, creature, creature.AggroRange))
                    return;

                AdvancePatrol(mapChannel, creature, delta);
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
                        creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0] = ((Random.Shared.Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
                        creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1] = ((Random.Shared.Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
                        // update home position to be at the (old) current node
                        creature.HomePos.Position = new Vector3(currentTargetNodePos[0], currentTargetNodePos[1], currentTargetNodePos[2]);
                    }
                }
            }
            else if (creature.Controller.CurrentAction == BehaviorActionFighting)
            {
                // Who it hates most and can still fight is who it fights (Threat). Nobody left:
                // the fight is over and the table goes with it. A minion assisting someone fights
                // what they have targeted first, switching when they do, and falls back on its
                // hate when they have nothing it can fight selected.
                var assisting = AssistedTarget(creature);
                // Mission escorts and defenders already chose a target within their owner's or
                // base's bounds above; ambient hate must not override that assignment.
                var chosen = IsMissionEscort(creature) || Game.Missions.World.CreatureGameplayRules.IsDefender(creature)
                    ? creature.Controller.ActionFighting.TargetEntityId
                    : assisting != 0 ? assisting : Threat.ChooseTarget(creature);

                if (assisting != 0)
                    creature.Target = assisting;

                if (chosen == 0)
                {
                    GiveUp(creature);
                    return;
                }

                if (chosen != creature.Controller.ActionFighting.TargetEntityId)
                    SetActionFighting(creature, chosen);

                // get target
                var target = EntityManager.Instance.GetEntityType(creature.Controller.ActionFighting.TargetEntityId);

                if (target == 0)
                {
                    // target disappeared (player logout or deleted for some reason) - leave combat mode
                    if (!Threat.Retarget(creature))
                        GiveUp(creature);

                    return;
                }

                // leave combat after 
                if (creature.LastAgression > creature.AggressionTime)
                {
                    GiveUp(creature);
                    return;
                }

                // get position of target
                var targetPosition = new Vector3();
                Actor targetActor = null;

                if (target == EntityType.Character)
                {
                    var player = EntityManager.Instance.GetPlayer(creature.Controller.ActionFighting.TargetEntityId);
                    targetActor = player;

                    // if target dead, on to the next it hates, or back to wandering
                    if (player.Attributes[Attributes.Health].Current <= 0 || player.State == CharacterState.Dead)
                    {
                        if (!Threat.Retarget(creature))
                            GiveUp(creature);

                        return;
                    }

                    // Cloaked, or watching a camera script: off the table, and on to whoever it hates next.
                    if (Detection.IsHidden(player) || CameraScripts.IsWatching(player))
                    {
                        if (!Threat.Retarget(creature))
                            StopFighting(creature);

                        return;
                    }

                    // Blinded: it cannot fight what it cannot see, but it remembers who it hated.
                    if (Detection.IsBlind(creature))
                    {
                        StopFighting(creature);
                        return;
                    }

                    targetPosition = player.Position;
                }

                else if (target == EntityType.Creature)
                {
                    var targetCreature = EntityManager.Instance.GetCreature(creature.Controller.ActionFighting.TargetEntityId);
                    targetActor = targetCreature;

                    if (targetCreature.Attributes[Attributes.Health].Current <= 0 || targetCreature.State == CharacterState.Dead || targetCreature.State == CharacterState.Dying)
                    {
                        if (Threat.Retarget(creature))
                            return;

                        // GiveUp stows the weapon, which takes the stance down with it
                        // (CreatureWeaponDraw); a creature with no weapon never held one.
                        GiveUp(creature);
                        return;
                    }

                    targetPosition = targetCreature.Position;
                }
                else if (target == EntityType.Object && PersonalWaypoints.TryGetPosition(creature.Controller.ActionFighting.TargetEntityId, out var waypointPosition))
                {
                    // A Personal Waypoint (PersonalWaypoints): a place to walk up to and hit,
                    // with no actor to it.
                    targetPosition = waypointPosition;
                }
                else if (target == EntityType.Object && AbilityManager.TryGetPlantPosition(creature.Controller.ActionFighting.TargetEntityId, out var plantPosition))
                {
                    // A Hortimonculus (AbilityManager): the same, for as long as it stands.
                    targetPosition = plantPosition;
                }
                else
                    Logger.WriteLog(LogType.Error, $"CreatureThink: unsuported Traget type {target}"); // todo

                var targetDistX = (targetPosition.X - creature.Position.X);
                var targetDistY = (targetPosition.Y - creature.Position.Y);
                var targetDistZ = (targetPosition.Z - creature.Position.Z);
                var targetDistSqr = (targetDistX * targetDistX + targetDistY * targetDistY + targetDistZ * targetDistZ);
                if (creature.MasterEntityId != 0)
                {
                    // A minion is leashed to its master, by where its target is: a fight that
                    // has gone that far from them is dropped, and it goes back to them.
                    var master = LeashCentre(creature);
                    var masterDistX = master.X - targetPosition.X;
                    var masterDistZ = master.Z - targetPosition.Z;

                    if (masterDistX * masterDistX + masterDistZ * masterDistZ >= MaxChaseDistance * MaxChaseDistance)
                    {
                        creature.LastRestTime = 0;
                        GiveUp(creature);
                        return;
                    }
                }
                else if (ChasedTooFar(creature))
                {
                    // Dragged too far from home: it runs back, forgetting the fight, and is whole
                    // again when it gets there. For a patrolling creature home is the last path
                    // node it reached.
                    Leash(mapChannel, creature);
                    return;
                }
                creature.LastAgression = 0; // update aggression time if we found our target

                // Free to act, yet for StallEvadeMs it has neither struck nor gained ground - its
                // target where the navmesh gives no route, or inside the reach of every attack it
                // has - it gives the fight up and goes home whole (Stalled).
                if (Stalled(creature, delta))
                {
                    Leash(mapChannel, creature);
                    return;
                }

                // Brought low, a Fithik blows itself up on whoever is around it (CreatureBombs).
                var selfDestruct = creature.Actions.FirstOrDefault(CreatureBombs.IsSelfDestruct);

                if (selfDestruct != null && CreatureBombs.ShouldSelfDestruct(creature.Attributes[Attributes.Health].Current, creature.Attributes[Attributes.Health].CurrentMax))
                {
                    CreatureBombs.StartSelfDestruct(mapChannel, creature, selfDestruct);
                    return;
                }

                // Brought to half its health, an Atta grub cocoons, and comes out an adult
                // (CreatureSummons).
                if (CreatureSummons.TryCocoon(mapChannel, creature))
                    return;

                // A Caretaker or a Technician looks after its side first: a heal, a repair or a
                // revive, when one would do something (CreatureSupport). Its part in the fight
                // (Stalled).
                if (CreatureSupport.TryStart(mapChannel, creature))
                {
                    MadeProgress(creature);
                    return;
                }

                // A Xanx brought low eats a dead Xanx it is standing by (CreatureHabits).
                if (CreatureHabits.TryInFight(mapChannel, creature))
                {
                    MadeProgress(creature);
                    return;
                }

                var needToMove = true;

                // An attack in range that it could not use for want of a clear line: it has to go
                // round whatever is in the way, not stand at the edge of its range.
                var sightBlocked = false;

                // Longest ready first: every attack is used as it comes off cooldown (ByReadiness).
                foreach (var action in ByReadiness(creature.Actions))
                {
                    // Heals and revives are not aimed at the enemy (CreatureSupport.TryStart), nor
                    // are the habits (CreatureHabits), nor a Shield Drone's heal, which its shield
                    // casts on its own schedule (ShieldDrone.Worker): taken as an attack, it was a
                    // shot of no damage at the player that reset the heal's cooldown.
                    if (CreatureSupport.Is(action) || CreatureHabits.Is(action) || action.ActionId == ShieldDrone.HealAction)
                        continue;

                    // check if we can execute action
                    if (targetDistSqr < action.RangeMin * action.RangeMin || targetDistSqr >= action.RangeMax * action.RangeMax)
                        continue;

                    // A shot needs to see its target; a blow at arm's length does not, and nor
                    // does an Amoeboid bringing up another one, which is aimed at nobody.
                    if (!CreatureAttacks.IsMelee(action) && !AmoeboidVomit.IsVomit(action) && !HasLineOfSight(mapChannel, creature, targetActor))
                    {
                        sightBlocked = true;
                        continue;
                    }

                    if (action.CooldownTimer > 0)
                    {
                        // An attack cooling down holds its creature at this range only if there
                        // is nothing closer in it could be using meanwhile. In range, with the
                        // attack coming: that is a fight going on (Stalled).
                        if (!ClosesInWhileCooling(creature, action))
                        {
                            needToMove = false;
                            MadeProgress(creature);
                        }

                        continue;   // action on cooldown
                    }

                    // The first attack of a fight waits on a roll that grows each think (OpensNow):
                    // until it comes up, the creature stands and faces its target.
                    if (!OpensNow(creature))
                    {
                        needToMove = false;
                        MadeProgress(creature);
                        UpdateEntityMovement(targetDistX, targetDistY, targetDistZ, creature, mapChannel, 0.0f, false, delta);
                        break;
                    }

                    // Rage, Scourge, a warcry, chaff: on itself or its own side (CreatureBuffs); a
                    // Howler's shriek, on every player around it (CreatureDebuffs); a
                    // Technician's turret, a Hunter's pet or a Thrax's Necromite
                    // (CreatureSummons); a Stalker's egg charge or a Necromite's bomb on a body
                    // (CreatureBombs); a Miasma turning to a cloud to coalesce on whoever is
                    // around it (CreatureMiasma). Only when it would do something; otherwise on to
                    // the next action.
                    if (CreatureBuffs.Is(action) || CreatureDebuffs.Is(action) || CreatureSummons.IsSummon(action) || CreatureBombs.IsOvulate(action)
                        || CreatureMiasma.Is(action) || CreatureBombs.IsCorpseExplosion(action))
                    {
                        var used = CreatureBuffs.Is(action) ? CreatureBuffs.Perform(mapChannel, creature, action, targetActor)
                            : CreatureDebuffs.Is(action) ? CreatureDebuffs.Perform(mapChannel, creature, action)
                            : CreatureSummons.IsSummon(action) ? CreatureSummons.Perform(mapChannel, creature, action, targetActor)
                            : CreatureMiasma.Is(action) ? CreatureMiasma.Dissipate(mapChannel, creature, action)
                            : CreatureBombs.IsCorpseExplosion(action) ? CreatureBombs.StartCorpseExplosion(mapChannel, creature, action)
                            : CreatureBombs.Ovulate(mapChannel, creature, action);

                        if (!used)
                            continue;

                        // Standing, facing its target, as for an attack below. The loop used to
                        // break here with needToMove still set, so the think went on to take a
                        // chase step - a movement at run speed - as the special began, and the
                        // clients ran the creature on through its windup.
                        needToMove = false;
                        UpdateEntityMovement(targetDistX, targetDistY, targetDistZ, creature, mapChannel, 0.0f, false, delta);

                        action.CooldownTimer = NextCooldown(creature, action);
                        MadeProgress(creature);
                        break;
                    }

                    needToMove = false;

                    // rotate
                    UpdateEntityMovement(targetDistX, targetDistY, targetDistZ, creature, mapChannel, 0.0f, false, delta);

                    // execute action and quit
                    var dmg = (int)(action.MinDamage + (Random.Shared.Next() % (action.MaxDamage - action.MinDamage + 1)));

                    // An attack that is a damage over time, or an effect alone, lands no hit: its
                    // row's damage is the effect's (CreatureEffectAttacks).
                    if (!CreatureEffectAttacks.Hits(CreatureEffectAttacks.KindOf(action.ActionId, action.ActionArgId)))
                        dmg = 0;

                    // Raised or lowered by what is on it: a Thrax boss's Rage.
                    dmg = GameEffectManager.ApplyDamageDealt(creature, dmg);

                    // A Laser crit on it weakens its ranged attacks - a charge's blow is not one.
                    if (!CreatureAttacks.IsMelee(action) && !KaelRushingBlow.Is(action))
                        dmg = GameEffectManager.ApplyRangedDamage(creature, dmg);

                    // Not every creature action is an attack. The Amoeboid's vomit is TARGET_NONE
                    // with a range of 0 and regurgitates another Amoeboid - a missile aimed at
                    // whoever it is fighting is the wrong thing entirely.
                    if (AmoeboidVomit.IsVomit(action))
                    {
                        AmoeboidVomit.Perform(mapChannel, creature, action);

                        action.CooldownTimer = NextCooldown(creature, action);
                        MadeProgress(creature);
                        break;
                    }

                    // A rushing blow - a Kael's, an AFS soldier's - is a charge: its windup carries
                    // the creature to its target, and the blow lands when it gets there
                    // (KaelRushingBlow).
                    if (KaelRushingBlow.Is(action))
                    {
                        creature.Controller.Path.Clear();
                        KaelRushingBlow.Start(mapChannel, creature, action, targetActor, dmg);
                        AbilityManager.OnCreatureActed(mapChannel, creature, true);

                        action.CooldownTimer = NextCooldown(creature, action);
                        MadeProgress(creature);
                        break;
                    }

                    var actionData = new ActionData(creature, action.ActionId, action.ActionArgId, creature.Controller.ActionFighting.TargetEntityId, 0);

                    // An ability lands when its windup is done, and its flight after that; the
                    // creature stands meanwhile (CreatureWindups). A weapon's shot lands as it did.
                    var landsIn = CreatureWindups.Begin(mapChannel, creature, action, (float)Math.Sqrt(targetDistSqr));

                    if (landsIn.HasValue)
                    {
                        creature.Controller.Path.Clear();
                        creature.Controller.PathIndex = 0;
                        StopMoving(creature);
                    }

                    // do damage, of the type the attack's weapon deals
                    MissileManager.Instance.MissileLaunch(mapChannel, actionData, dmg, CreatureAttacks.ArmorPierceOf(action), damageType: CreatureAttacks.DamageTypeOf(action),
                        melee: CreatureAttacks.IsMelee(action), splashRadius: Splash.RadiusOf(action), creatureAction: action, landsInMs: landsIn);

                    // Feedback on it burns it for acting: this is the hostile action the server has.
                    AbilityManager.OnCreatureActed(mapChannel, creature, true);

                    // set cooldown, lengthened by whatever slows its attacks (Called Shot: Arm)
                    action.CooldownTimer = NextCooldown(creature, action);
                    MadeProgress(creature);

                    // creature used action, break loop
                    break;
                }

                if (needToMove == false)
                {
                    // Holding here - an attack cooling down with nothing closer to use, the
                    // opening roll - after a think that stepped: the clients carry the last
                    // movement on at its speed until they are told otherwise.
                    StopIfMoving(creature);
                    return;
                }

                // A Shield Drone does not go to its target; the target comes to it. The guide
                // gives it "Offense: None" and tells the player to "sprint directly toward them",
                // which only works if the drone stays where its shield is - so it keeps whoever
                // shot it in its hate table, strikes anything that closes inside three metres,
                // and never leaves the ground it is covering.
                if (ShieldDrone.HoldsGround(creature))
                    return;

                // An emplacement shoots from its mount or not at all: a target out of its reach or
                // its sight is off its table, and it turns to the next it hates, or stands down.
                if (Emplacements.Is(creature))
                {
                    if (!Threat.Retarget(creature))
                        GiveUp(creature);

                    return;
                }

                if (targetDistSqr <= 3.0f * 3.0f && !sightBlocked)
                {
                    // near enough, dont move - and say so, after the step that brought it here
                    StopIfMoving(creature);
                    return;
                }

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

                    if (sightBlocked)
                    {
                        // Something between them: the navmesh route to the target itself goes
                        // round it, where a point short of the target on this side would not.
                        pathTarget = targetPosition;
                    }
                    else if (targetDistSqr < 0.1f)
                    {
                        // if too near, move out of enemy by running to random point somewhere x units around the creature
                        var angle = (Random.Shared.Next() / 32767.0f) * 6.28318f; // random angle
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
                var before = creature.Position;
                FollowPath(mapChannel, creature, creature.RunSpeed, delta);

                // Ground gained is a fight going on; a step that went nowhere - no route, a force
                // field, the end of a path that stops short - is not (Stalled).
                if (creature.Position != before)
                    MadeProgress(creature);
            }//---fighting
        }

        #region Wander

        /// <summary>
        /// Whether the creature can go nowhere: it has no walking speed and no running speed. A
        /// vendor, a hospital's medic, a mission's contact. It neither strolls, nor chases, nor
        /// runs home, so it is on the spot its pool put it for as long as it lives
        /// (SpawnPoolManager.SpawnPoint). One with a running speed alone does not stroll either,
        /// but it runs when it fights.
        /// </summary>
        public static bool NeverMoves(Creature creature)
        {
            return creature != null && creature.WalkSpeed < 0.01f && creature.RunSpeed < 0.01f;
        }

        /// <summary>
        /// Where a stroll goes: a point within <see cref="WanderStepDistance"/> of where the
        /// creature stands and at least <see cref="WanderMinStep"/> from it, inside its spawn zone
        /// (<see cref="WanderZoneOf"/>), and at least <see cref="WanderSpacing"/> from every other
        /// living creature around and from wherever another is already walking to - so a camp
        /// spreads over its ground instead of bunching. Drawn from the walkable surface where the
        /// map has a navmesh, so never inside a rock or off a cliff; from the disc, on the ground,
        /// where it has none. <see cref="WanderAttempts"/> draws, then nothing - it stays put until
        /// the next interval. A creature that has ended up outside its zone - a chase that did not
        /// leash, a knockback - and finds nothing goes home instead.
        /// </summary>
        public static Vector3? PickStroll(MapChannel mapChannel, Creature creature)
        {
            var (centre, radius) = WanderZoneOf(creature);
            var taken = SpotsTakenAround(mapChannel, creature);

            for (var attempt = 0; attempt < WanderAttempts; attempt++)
            {
                var candidate = NavMeshManager.RandomPointAround(mapChannel, creature.Position, WanderStepDistance)
                                ?? NavMeshManager.SnapToGround(mapChannel, creature.Position + SpawnPoolManager.InDisc(WanderStepDistance));

                if (IsGoodStroll(candidate, creature.Position, centre, radius, taken))
                    return candidate;
            }

            return AcrossGround(creature.Position, centre) > radius ? creature.HomePos.Position : (Vector3?)null;
        }

        /// <summary>Whether a stroll from <paramref name="from"/> may end at <paramref name="candidate"/>; distances across the ground.</summary>
        public static bool IsGoodStroll(Vector3 candidate, Vector3 from, Vector3 zoneCentre, float zoneRadius, IEnumerable<Vector3> taken)
        {
            var step = AcrossGround(candidate, from);

            if (step < WanderMinStep || step > WanderStepDistance)
                return false;

            if (AcrossGround(candidate, zoneCentre) > zoneRadius)
                return false;

            foreach (var spot in taken)
                if (AcrossGround(candidate, spot) < WanderSpacing)
                    return false;

            return true;
        }

        /// <summary>
        /// A creature's spawn zone: its pool's area when the pool has a radius (a camp, a nest),
        /// otherwise <see cref="WanderZoneFallbackRadius"/> around the point it spawned on.
        /// </summary>
        public static (Vector3 Centre, float Radius) WanderZoneOf(Creature creature)
        {
            var pool = creature.SpawnPool;

            if (pool != null && pool.Radius > 0 && pool.MapContextId == creature.MapContextId)
                return (pool.Position, pool.Radius);

            return (creature.HomePos.Position, WanderZoneFallbackRadius);
        }

        /// <summary>Where every other living creature around stands, and where each one strolling is headed.</summary>
        private static List<Vector3> SpotsTakenAround(MapChannel mapChannel, Creature creature)
        {
            var spots = new List<Vector3>();

            if (mapChannel == null)
                return spots;

            foreach (var cell in CellManager.CellsIn(mapChannel, creature.Cells))
                foreach (var other in cell.CreatureList)
                {
                    if (other == creature || other.State == CharacterState.Dead || other.State == CharacterState.Dying)
                        continue;

                    if (!other.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                        continue;

                    spots.Add(other.Position);

                    if (other.Controller?.CurrentAction == BehaviorActionWander && other.Controller.ActionWander.State == WanderMoving)
                        spots.Add(other.Controller.ActionWander.WanderDestination);
                }

            return spots;
        }

        private static float AcrossGround(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;

            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// An idle NPC whose pose is off (NpcPoses) gets back to its post: it walks there if it is
        /// more than NpcPoses.PostReachedDistance away and can walk - once, noticing a fight on
        /// the way as any walk in does - and true is returned while it is setting off. There, or
        /// wherever that walk left it, or where it stands if it cannot walk or has no post on
        /// this map (a creature a GM posed that was never placed), it faces the way it was placed
        /// and takes the pose up again.
        /// </summary>
        private bool TakePost(MapChannel mapChannel, Creature creature)
        {
            var post = creature.HomePos.Position;

            if (!creature.PoseWalkedBack && creature.WalkSpeed >= 0.01f
                && creature.HomePos.MapContextid == creature.MapContextId
                && AcrossGround(creature.Position, post) > NpcPoses.PostReachedDistance)
            {
                creature.PoseWalkedBack = true;
                WalkIn(creature, post);
                return true;
            }

            NpcPoses.StandAtPost(mapChannel, creature);
            return false;
        }

        /// <summary>
        /// Tells the clients the creature has stopped where it is. The last step of a walk goes out
        /// with the walking speed, and the clients carry an entity on at the speed they were last
        /// given - left there, it would drift past the spot for its whole wait.
        /// </summary>
        private static void StopWalking(Creature creature)
        {
            PublishMovement(creature, new Movement(creature.Position, 0f, 0x08, new Vector2(creature.LastYaw, 0f)));
        }

        /// <summary>
        /// Puts a creature to wandering: standing, with the interval to its first stroll starting
        /// now - or, for one just spawned (<paramref name="staggered"/>), already partly run, by a
        /// random amount, so that a camp that came up together does not set off together.
        /// </summary>
        public static void StartWandering(Creature creature, bool staggered)
        {
            var controller = creature.Controller;

            controller.CurrentAction = BehaviorActionWander;
            controller.ActionWander.State = WanderIdle;
            controller.ActionWander.Fleeing = false;
            controller.ActionWander.Errand = false;
            controller.ActionWander.Arriving = false;
            controller.ActionWander.MovingMs = 0;
            controller.ActionWander.IdleMs = staggered ? Random.Shared.Next((int)WanderIntervalMs) : 0;
            controller.Path.Clear();
            controller.PathIndex = 0;
        }

        /// <summary>
        /// A creature that has just arrived - off a dropship on a pad, through a teleporter - walks
        /// to <paramref name="destination"/> on its pool's ground, which is its home from now on:
        /// at its walk speed, with time enough for the distance. It notices a fight on the way as
        /// a wandering creature does.
        /// </summary>
        public void WalkIn(Creature creature, Vector3 destination)
        {
            if (creature.Controller == null)
                return;

            StartWandering(creature, false);

            creature.HomePos.Position = destination;

            if (creature.WalkSpeed < 0.01f)
                return;

            var wander = creature.Controller.ActionWander;

            wander.WanderDestination = destination;
            wander.State = WanderMoving;
            wander.MovingMs = 0;
            wander.Arriving = true;
            wander.ArrivalTimeoutMs = ReturnTimeoutFor(Vector3.Distance(creature.Position, destination), creature.WalkSpeed);
        }

        #endregion

        #region Patrol

        /// <summary>
        /// Puts a creature with a beat (Patrols) onto it: it makes for the step it was last going
        /// to - the first, for one just made - from wherever it is.
        /// </summary>
        private static void SetActionPatrol(Creature creature)
        {
            var controller = creature.Controller;
            var patrol = controller.ActionPatrol;

            controller.CurrentAction = BehaviorActionPatrol;
            controller.Path.Clear();
            controller.PathIndex = 0;

            if (patrol.Step < 0 || patrol.Step >= creature.Patrol.Count)
                patrol.Step = 0;

            patrol.Arrived = false;
            patrol.Faced = false;
            patrol.WaitMs = 0;
            patrol.Rejoining = true;
            patrol.LeftAt = creature.Position;
        }

        /// <summary>
        /// One think of a beat (Patrols). Standing out a turn or a pause, nothing. On a step, the
        /// turn to its facing and its pause, then on to the next. Off the beat, a walk to the
        /// step across the navmesh. Otherwise a turn to face the step if it is not ahead, and
        /// then the walk: straight at it, at walk speed, and exactly onto it.
        /// </summary>
        private void AdvancePatrol(MapChannel mapChannel, Creature creature, long delta)
        {
            // Its beat taken away, or its legs: it stands and strolls as any creature does.
            if (!Patrols.Has(creature))
            {
                SetActionWander(creature);
                return;
            }

            var steps = creature.Patrol;
            var controller = creature.Controller;
            var patrol = controller.ActionPatrol;

            if (patrol.Step < 0 || patrol.Step >= steps.Count)
                patrol.Step = 0;

            // Not where the patrol left it: pushed aside, thrown, put somewhere. It goes back
            // to the step, and does again whatever it does there.
            if (Vector3.Distance(creature.Position, patrol.LeftAt) > Patrols.MovedTolerance)
            {
                patrol.Rejoining = true;
                patrol.Arrived = false;
                patrol.Faced = false;
                patrol.WaitMs = 0;
                patrol.LeftAt = creature.Position;
            }

            if (patrol.WaitMs > 0)
            {
                patrol.WaitMs -= delta;

                if (patrol.WaitMs > 0)
                    return;

                patrol.WaitMs = 0;
            }

            if (patrol.Arrived)
            {
                if (!patrol.Faced)
                {
                    var here = steps[patrol.Step];

                    patrol.Faced = true;
                    patrol.WaitMs = (here.Facing is float facing ? TurnTo(creature, facing) : 0) + here.PauseMs;

                    if (patrol.WaitMs > 0)
                        return;
                }

                patrol.Step = (patrol.Step + 1) % steps.Count;
                patrol.Arrived = false;
                patrol.Faced = false;
            }

            var step = steps[patrol.Step];
            var across = AcrossGround(creature.Position, step.Position);

            if (patrol.Rejoining)
            {
                if (across > Patrols.RejoinDistance)
                {
                    if (controller.Path.Count == 0)
                        BuildPath(mapChannel, creature, step.Position);

                    // No route - no navmesh under one end of it - is a straight walk from here.
                    if (controller.Path.Count > 0)
                    {
                        // The end of the route is as near the step as the mesh goes.
                        if (FollowPath(mapChannel, creature, creature.WalkSpeed, delta))
                            patrol.Rejoining = false;

                        creature.Rotation = creature.LastYaw;
                        patrol.LeftAt = creature.Position;
                        return;
                    }
                }

                patrol.Rejoining = false;
                controller.Path.Clear();
                controller.PathIndex = 0;
            }

            var to = step.Position - creature.Position;
            var remaining = to.Length();
            var moved = remaining;

            // Over the step already, a hair above or below it, there is no way to face: it is put on it.
            if (across > 0.01f)
            {
                var heading = AbilityManager.YawTowards(creature.Position, step.Position);

                // The step is not ahead of it: it turns where it stands, and walks when it has.
                if (MathF.Abs(Patrols.Turn(creature.LastYaw, heading)) > Patrols.TurnInPlace)
                {
                    patrol.WaitMs = TurnTo(creature, heading);
                    return;
                }

                moved = UpdateEntityMovement(to.X, to.Y, to.Z, creature, mapChannel, creature.WalkSpeed, true, delta, grounded: false);
                creature.Rotation = creature.LastYaw;
            }

            // The step is clamped to the distance left, so covering it is being there.
            if (moved >= remaining - 0.001f)
            {
                creature.Position = step.Position;

                if (Patrols.WalksOn(steps, patrol.Step, creature.LastYaw))
                {
                    // Nothing to do here and the next step ahead: its next think walks on.
                    patrol.Step = (patrol.Step + 1) % steps.Count;
                }
                else
                {
                    patrol.Arrived = true;
                    patrol.Faced = false;

                    // The last step of a walk goes out with its speed, and the clients carry
                    // it on at that until they are told it has stopped (StopWalking) - whether
                    // that step was this think's, or the one that brought it off the navmesh
                    // and onto the step a think ago.
                    if (moved > 0.001f || controller.LastMovement?.Velocity > 0f)
                        PublishMovement(creature, new Movement(creature.Position, 0f, 0x08, new Vector2(creature.LastYaw, 0f)));
                }
            }

            patrol.LeftAt = creature.Position;
        }

        /// <summary>
        /// Turns a standing creature to face <paramref name="yaw"/>: one stop, where it is, with
        /// the new facing, which the client turns it to at its class's own rate. Returns how long
        /// the turn is given (Patrols.TurnMs) - nothing, and nothing sent, if it faces that way
        /// already.
        /// </summary>
        private static long TurnTo(Creature creature, float yaw)
        {
            var turn = Patrols.Turn(creature.LastYaw, yaw);

            if (MathF.Abs(turn) < Patrols.FacingTolerance)
                return 0;

            creature.LastYaw = yaw;
            creature.Rotation = yaw;

            PublishMovement(creature, new Movement(creature.Position, 0f, 0x08, new Vector2(yaw, 0f)));

            return Patrols.TurnMs(turn);
        }

        #endregion

        /// <summary>A creature following the player whose mission it is in (SpawnPool.FollowOwnerCharacterId).</summary>
        internal static bool IsMissionEscort(Creature creature) =>
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
                if (creature.Controller.CurrentAction == BehaviorActionFighting)
                    GiveUp(creature);
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

        /// <summary>
        /// How far from its owner an escort takes a fight on: as far as it may stray (20 m) and
        /// then its longest attack's reach, and never beyond 35 m.
        /// </summary>
        private static float EscortEngageDistance(Creature creature)
        {
            var reach = creature.Actions.Count == 0 ? 0 : creature.Actions.Max(action => action.RangeMax);
            return (float)Math.Min(35, 20 + reach);
        }

        private bool AdvanceEscort(MapChannel map, Creature creature, long delta)
        {
            var owner = CreatureManager.FindEscortOwner(map, creature);
            if (owner == null)
            {
                if (creature.Controller.CurrentAction == BehaviorActionFighting)
                    GiveUp(creature);
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
                Vector3.Distance(currentTarget.Position, destination) <= EscortEngageDistance(creature))
                target = currentTarget;

            if (target != null)
            {
                if (creature.Controller.CurrentAction != BehaviorActionFighting ||
                    creature.Controller.ActionFighting.TargetEntityId != target.EntityId)
                    SetActionFighting(creature, target.EntityId);
                return false;
            }

            if (!hadOwnerTarget && !wasFighting && gap <= 20 && !follow.CatchUpRunning &&
                creature.LastAgression >= AggroScanDelayMs && ScansForEnemies(creature) &&
                CheckForAttackableEntityInRange(map, creature, creature.AggroRange))
            {
                // Only a fight it can have without leaving its owner: one it picked up beyond
                // that it would walk off after, give up at the leash and come back for again.
                if (EntityManager.Instance.Creatures.TryGetValue(
                        creature.Controller.ActionFighting.TargetEntityId, out var found) &&
                    Vector3.Distance(found.Position, destination) <= EscortEngageDistance(creature))
                    return false;

                creature.Controller.ActionFighting.TargetEntityId = 0;
                wasFighting = true;
            }

            if (wasFighting)
            {
                GiveUp(creature);
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
        /// Sets the creature's path to <paramref name="destination"/>: the navmesh corners when the
        /// map has one and both ends are on it. Mission escorts and defenders never use the
        /// straight-line fallback that ordinary creatures retain on maps without a navmesh.
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
            else if (IsMissionEscort(creature) || Game.Missions.World.CreatureGameplayRules.IsDefender(creature))
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

            if (distSqr > Math.Min(0.01f, arrivalDistance * arrivalDistance)) // to avoid division by zero
            {
                var moved = UpdateEntityMovement(difX, difY, difZ, creature, mapChannel, speed, true, delta,
                    synchronizeVisibility: synchronizeVisibility);

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

        /// <summary>Counts down to the creature's next cell check and, when it is due, says whether it has moved into another cell.</summary>
        private static bool CellChanged(Creature creature, long delta)
        {
            creature.UpdatePositionCounter -= delta;

            if (creature.UpdatePositionCounter > 0)
                return false;

            creature.UpdatePositionCounter = CreatureManager.CreatureLocationUpdateTime;

            return CellManager.Instance.GetCellSeed(creature.Position) != creature.Cells[2, 2];
        }

        /// <summary>
        /// A step of a scripted creature's carry, on its own clock (a crab mine, every 100 ms):
        /// the same as a knockback's, for whoever runs it.
        /// </summary>
        public bool StepCarry(MapChannel mapChannel, Creature creature, long delta)
        {
            if (creature.State == CharacterState.Dead)
                return false;

            return StepKnockback(mapChannel, creature, delta);
        }

        /// <summary>
        /// One tick of a carry in progress (a Vortex's pull, a crab mine's run, a Kael's rush):
        /// the creature goes toward where it is being carried at the carry's speed, and is told
        /// to stop when it gets there. A rush is stepped without a word to the clients, who are
        /// making the run themselves. Returns whether it was moving.
        /// </summary>
        private bool StepKnockback(MapChannel mapChannel, Creature creature, long delta)
        {
            if (creature.KnockbackTo == null)
                return false;

            var to = creature.KnockbackTo.Value;
            var difX = to.X - creature.Position.X;
            var difY = to.Y - creature.Position.Y;
            var difZ = to.Z - creature.Position.Z;
            var remaining = Math.Sqrt(difX * difX + difZ * difZ);

            if (remaining > 0.05)
            {
                var speed = creature.KnockbackSpeed > 0 ? creature.KnockbackSpeed : CrowdControl.KnockbackSpeed;
                var moved = UpdateEntityMovement(difX, difY, difZ, creature, mapChannel, speed, true, delta, knockback: true, faceAlong: creature.KnockbackIsPull,
                    publish: !creature.IsRushing);

                if (moved < remaining - 0.05)
                    return true;
            }

            creature.KnockbackTo = null;
            creature.KnockbackSpeed = 0;
            creature.KnockbackIsPull = false;
            creature.RushTo = null;
            StopMoving(creature);

            return true;
        }

        /// <summary>
        /// A knockback (CrowdControl.Knockback): the creature is thrown to where it ends, facing
        /// back along the way it went. Its clients fly the arc themselves (MovementType.Knockback)
        /// from wherever they have it, and look at nothing else sent for it until it is back on
        /// its feet, so they are told once and the server has it at the end from the start.
        /// Whatever was carrying it stops.
        /// </summary>
        public void Throw(MapChannel mapChannel, Creature creature, Vector3 destination, Vector3 direction)
        {
            creature.KnockbackTo = null;
            creature.KnockbackSpeed = 0;
            creature.KnockbackIsPull = false;
            creature.RushTo = null;

            creature.Position = destination;
            creature.LastYaw = (float)Math.Atan2(direction.X, direction.Z);

            // To the clients that have it where it was thrown from, before its cell moves with it.
            PublishMovement(creature, Movement.Knockback(destination, new Vector2(creature.LastYaw, 0f)));

            if (mapChannel != null)
                SynchronizeMovementCell(mapChannel, creature);
        }

        /// <summary>
        /// A rush (KaelRushingBlow): the creature runs straight to where it ends at this speed,
        /// facing the way it goes. Its clients make the run themselves (MovementType.Rush), so
        /// they are told once, here. The server keeps its own place along the line a tick at a
        /// time (StepKnockback) and sends nothing more until it is over. The speed is held to what
        /// a movement can carry, so the two run at the same rate.
        /// </summary>
        public void Rush(Creature creature, Vector3 destination, float speed)
        {
            speed = Math.Clamp(speed, 1f, Movement.MaxVelocity);

            creature.KnockbackTo = destination;
            creature.KnockbackDirection = CrowdControl.AwayFrom(creature.Position, destination);
            creature.KnockbackSpeed = speed;
            creature.KnockbackIsPull = true;    // faces the way it runs
            creature.RushTo = destination;
            creature.LastYaw = AbilityManager.YawTowards(creature.Position, destination);

            PublishMovement(creature, Movement.Rush(destination, speed, new Vector2(creature.LastYaw, 0f)));
        }

        /// <summary>
        /// Ends whatever is carrying the creature, where it has got to. A rush is the exception:
        /// its clients are making the run themselves and cannot be stopped part-way, so the
        /// creature is put where the rush ends, which is where they will have it.
        /// </summary>
        public void EndCarry(MapChannel mapChannel, Creature creature)
        {
            if (creature.IsRushing)
            {
                creature.Position = creature.RushTo.Value;

                if ((mapChannel ?? creature.RuntimeMapChannel) is MapChannel map)
                    SynchronizeMovementCell(map, creature);
            }

            creature.KnockbackTo = null;
            creature.KnockbackSpeed = 0;
            creature.KnockbackIsPull = false;
            creature.RushTo = null;
        }

        /// <summary>
        /// Tells everyone who can see the creature that it is standing still where it is, facing
        /// the way it last faced. Used when something stops it in its tracks - a stun, a freeze, the
        /// end of a knockback - since the clients otherwise carry on extrapolating its last
        /// movement.
        /// </summary>
        public void StopMoving(Creature creature)
        {
            var movement = new Movement(new Vector3(creature.Position.X, creature.Position.Y, creature.Position.Z), 0.0f, 0x08, new Vector2(creature.LastYaw, 0f));

            if (creature.Controller != null)
                creature.Controller.LastMovement = movement;

            CellManager.Instance.CellMoveObject(creature, movement);
        }

        /// <summary>
        /// <see cref="StopMoving"/>, if the last movement the clients were sent was a moving one
        /// (Controller.LastMovement, which every published step records). They extrapolate a
        /// movement at its speed until the next, so a creature whose last step was a chase's and
        /// which then stands - a special begun, an attack waited on, a fight given up - ran on
        /// across their screens until something else was sent for it. A creature that is already
        /// standing as far as they know is not sent another stop.
        /// </summary>
        private static void StopIfMoving(Creature creature)
        {
            if (creature?.Controller?.LastMovement is not { Velocity: > 0 })
                return;

            PublishMovement(creature, new Movement(creature.Position, 0f, 0x08, new Vector2(creature.LastYaw, 0f)));
        }

        private double GetDistanceSqr(Vector3 p1, Vector3 p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            float dz = p2.Z - p1.Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
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
            // todo: When on heavy load, the server should increase the time between calls to
            //       this function. (check player updating as a reference)

            // A thinking creature can add a cell to the table (GetCell makes the ones it is asked
            // for), so the walk is over a copy - one kept on the map and refilled, not a new list
            // on every think (MapChannel.ThinkCells).
            var tempCells = mapChannel.ThinkCells;
            var processedCreatures = new HashSet<Creature>(ReferenceEqualityComparer.Instance);

            tempCells.Clear();
            tempCells.AddRange(mapChannel.MapCellInfo.Cells.Values);

            foreach (var mapCell in tempCells)
            {
                if (mapCell == null) // should never happen, but still do a check for safety
                    continue;
                // creatures
                if (mapCell.CreatureList.Count > 0)
                {

                    // Over a copy, once each: a scene actor's own cell move can take it into a
                    // cell not walked yet.
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
                    // remove creature from world; its pool's dead count goes down with it (CellManager)
                    CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creatureList[f]);
                }
            }
        }
        
        /// <summary>
        /// Whether a creature should close in while this attack cools down: it has longer than
        /// HoldForCooldownMs left, and the creature has an attack of shorter reach to go and use.
        ///
        /// Every attack in range used to pin its creature in place, cooling or not. That was
        /// harmless while each creature had one attack, or a gun and a melee of about the same
        /// cadence; it is not once a brute has a ranged special - a Kael with a twenty metre
        /// tectonic strike would stand at twenty metres waiting for it forever and never smash.
        /// A creature whose shortest-reach attack is this one keeps its ground as before, so a
        /// ranged creature still stands off and shoots.
        /// </summary>
        public static bool ClosesInWhileCooling(Creature creature, CreatureAction action)
        {
            if (action.CooldownTimer <= HoldForCooldownMs)
                return false;

            foreach (var other in creature.Actions)
            {
                if (other == action || other.RangeMax <= 0 || AmoeboidVomit.IsVomit(other) || other.ActionId == ShieldDrone.HealAction || CreatureSupport.Is(other) || CreatureHabits.Is(other))
                    continue;

                if (other.RangeMax < action.RangeMax)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// When a creature may use an action again after using it now: its cooldown, lengthened by
        /// whatever slows its attacks (Called Shot: Arm), plus a random CooldownJitterMin to
        /// CooldownJitterMax of that again.
        /// </summary>
        public static long NextCooldown(Creature creature, CreatureAction action)
        {
            double unit;

            lock (CooldownRandom)
                unit = CooldownRandom.NextDouble();

            return WithJitter(action.Cooldown * (creature != null ? GameEffectManager.AttackRateModifierOf(creature) : 1.0), unit);
        }

        /// <summary>A cooldown with its wait on top, for a roll in [0, 1): 1% of it at 0, 50% at 1.</summary>
        public static long WithJitter(double cooldown, double unit)
        {
            if (cooldown <= 0)
                return 0;

            var share = CooldownJitterMin + Math.Max(0, Math.Min(1, unit)) * (CooldownJitterMax - CooldownJitterMin);

            return (long)Math.Round(cooldown * (1 + share));
        }

        /// <summary>
        /// A creature's actions in the order it should try them: the one that came off cooldown
        /// longest ago first (a ready action's timer keeps counting down below zero), then the rest.
        /// Every attack gets its turn as it comes ready, rather than the first slots taking every
        /// opening and the later ones waiting behind them. Ties keep their slot order.
        /// </summary>
        /// <summary>The chance of opening on the next think after this many held ones: 25%, 50%, 75%, then certain.</summary>
        public static double OpeningChance(int heldThinks)
        {
            return Math.Min(1.0, (Math.Max(0, heldThinks) + 1) * OpeningChanceStep);
        }

        /// <summary>
        /// Whether a creature with an attack ready uses it now: always once it has opened the
        /// fight, and always for a minion (it fights on its master's word, not as an enemy);
        /// otherwise the opening roll, unit being a roll in [0, 1). A held think is counted.
        /// </summary>
        public static bool OpensNow(Creature creature, double unit)
        {
            var fighting = creature.Controller.ActionFighting;

            if (fighting.Opened || creature.MasterEntityId != 0)
                return true;

            if (unit < OpeningChance(fighting.OpeningRolls))
            {
                fighting.Opened = true;
                return true;
            }

            fighting.OpeningRolls++;
            return false;
        }

        private static bool OpensNow(Creature creature)
        {
            double unit;

            lock (CooldownRandom)
                unit = CooldownRandom.NextDouble();

            return OpensNow(creature, unit);
        }

        /// <summary>
        /// The actions soonest ready first, equals in the order the creature has them - what
        /// OrderBy gave, as one list and an insertion sort over a handful of entries rather than
        /// the LINQ machinery, on every think of every fighting creature.
        /// </summary>
        public static List<CreatureAction> ByReadiness(IEnumerable<CreatureAction> actions)
        {
            var list = new List<CreatureAction>(actions);

            for (var i = 1; i < list.Count; i++)
            {
                var action = list[i];
                var j = i - 1;

                while (j >= 0 && list[j].CooldownTimer > action.CooldownTimer)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = action;
            }

            return list;
        }

        public void SetActionFighting(Creature creature, ulong targetEntityId)
        {
            TrySetActionFighting(creature, targetEntityId);
        }

        /// <summary>
        /// A scene can preflight the AI's refusals before obtaining its source combat authorization.
        /// The actual transition always rechecks that authorization.
        /// </summary>
        internal static bool CanSetActionFighting(Creature creature, ulong targetEntityId,
            bool requireCombatAuthorization = true)
        {
            // Running home after a leash: nothing pulls it back into a fight on the way - not a
            // hit, not an assist, not the scan.
            if (IsReturning(creature))
                return false;

            // A passive minion does not fight, and this is the one place worth saying so: it
            // covers both the aggro scan and being shot at (MissileManager calls straight in
            // here), so there is no second path where passive quietly stops meaning passive.
            if (creature.MasterEntityId != 0 && creature.Stance == MinionStance.Passive)
                return false;

            // Nor does a minion with no attack (the Repair Bot): it would only chase its target
            // and stand there.
            if (creature.MasterEntityId != 0 && creature.Actions.Count == 0)
                return false;

            // Only a fight its category allows: nothing on its own side, nothing that takes no
            // part in fights (TargetCategories.MayFight, MayFightPlayer). A NEUTRAL creature
            // fights whoever attacks it; an object or decoration fights nobody.
            return requireCombatAuthorization
                ? MayFight(creature, targetEntityId)
                : MayFightTarget(creature, targetEntityId);
        }

        internal bool TrySetActionFighting(Creature creature, ulong targetEntityId)
        {
            if (!CanSetActionFighting(creature, targetEntityId))
                return false;

            // A fight starting, not a change of target within one: its first attack is rolled for again (OpensNow).
            if (creature.Controller.CurrentAction != BehaviorActionFighting)
            {
                creature.Controller.ActionFighting.Opened = false;
                creature.Controller.ActionFighting.OpeningRolls = 0;
            }

            creature.Controller.CurrentAction = BehaviorActionFighting;

            // Its health stops coming back for the fight, and the clients hear so now: the hit
            // that started it sent its own update first, carrying the resting rate, and left to
            // the next regeneration second they would predict a bar creeping up (CreatureHealth).
            if (creature.Attributes.TryGetValue(Attributes.Health, out var health))
                CreatureHealth.SyncRate(creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId), creature, health);

            // Whatever the creature was walking towards is not where the fight is: without this
            // it chased its last wander node before ever heading for its target.
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
            creature.Controller.TimerPathUpdateLock = 0;
            creature.Controller.ActionFighting.TargetEntityId = targetEntityId;
            creature.Controller.ActionFighting.StalledMs = 0;
            creature.LastAgression = 0;

            // Whatever brought it here - the scan, a hit, an assist - the target is on its table.
            Threat.Noticed(creature, targetEntityId);

            // A pose it held at its post comes off (NpcPoses): it stands up, with what it really holds.
            NpcPoses.Drop(creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId), creature);

            // Its weapon out for the fight, if it carries one.
            CreatureWeaponDraw.Draw(creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId), creature);

            // Its target, to everyone who can see it: the guns that aim, aim (Targets).
            Targets.Sync(creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId), creature);
            return true;
        }

        /// <summary>
        /// Whether the creature's target category lets it fight this entity: another creature by
        /// TargetCategories.MayFight, a player by MayFightPlayer (their CombatCategory); nothing else.
        ///
        /// Mind Control bends it (AbilityManager.MindControl): a Frightened creature fights nobody;
        /// a Confused one any combatant creature and the players it could fight anyway; a Subverted
        /// one only the creatures of its own category. And whoever a Confused or Subverted creature
        /// turns on may answer it, whatever their categories.
        /// </summary>
        public static bool MayFight(Creature creature, ulong entityId)
        {
            if (!Game.Missions.World.CreatureGameplayRules.CanParticipateInCombat(creature))
                return false;

            return MayFightTarget(creature, entityId);
        }

        private static bool MayFightTarget(Creature creature, ulong entityId)
        {
            var pump = AbilityManager.MindControlPumpOf(creature);

            if (pump == AbilityManager.MindControlFrighten)
                return false;

            // A player's creature fights its master's enemies across a wargame, and theirs (Pvp).
            if (Pvp.SummonMayFight(creature, entityId))
                return true;

            if (EntityManager.Instance.Creatures.TryGetValue(entityId, out var other))
            {
                if (other == creature || !Game.Missions.World.CreatureGameplayRules.CanParticipateInCombat(other))
                    return false;

                if (pump == AbilityManager.MindControlConfusion)
                    return TargetCategories.IsCombatant(other.TargetCategory);

                if (pump == AbilityManager.MindControlSubversion)
                    return other.TargetCategory == creature.TargetCategory;

                if (TargetCategories.IsCombatant(creature.TargetCategory) && AbilityManager.IsMindConfused(other))
                    return true;

                return TargetCategories.MayFight(creature.TargetCategory, other.TargetCategory);
            }

            if (EntityManager.Instance.Players.TryGetValue(entityId, out var player))
                return pump != AbilityManager.MindControlSubversion && TargetCategories.MayFightPlayer(creature.TargetCategory, player.CombatCategory);

            // A Personal Waypoint (PersonalWaypoints) is fought by what fights its owner, and so is
            // a Hortimonculus (AbilityManager). A Confused or Subverted creature has turned on
            // creatures, and leaves them.
            return pump == 0 && (PersonalWaypoints.MayBeFoughtBy(creature, entityId) || AbilityManager.PlantMayBeFoughtBy(creature, entityId));
        }

        /// <summary>
        /// Mind Control's Frighten: the creature drops its fight and runs FleeDistance away from
        /// whoever it fears, along the navmesh where the map has one.
        /// </summary>
        public void Flee(MapChannel mapChannel, Creature creature, Vector3 from)
        {
            if (creature.Controller == null || creature.RunSpeed < 0.01f)
                return;

            var away = new Vector3(creature.Position.X - from.X, 0, creature.Position.Z - from.Z);

            if (away.LengthSquared() < 0.01f)
            {
                var angle = Random.Shared.NextDouble() * Math.PI * 2;
                away = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
            }

            var destination = creature.Position + Vector3.Normalize(away) * FleeDistance;

            destination = NavMeshManager.NearestWalkable(mapChannel, destination) ?? destination;

            SetActionWander(creature);
            creature.Controller.ActionWander.WanderDestination = destination;
            creature.Controller.ActionWander.State = WanderMoving;
            creature.Controller.ActionWander.Fleeing = true;
            creature.LastRestTime = 0;
        }

        /// <summary>How far a frightened creature runs from whoever frightened it before it looks again.</summary>
        public const float FleeDistance = 20f;

        /// <summary>The fight is over for this creature: it forgets everyone it hated and wanders again.</summary>
        public void GiveUp(Creature creature)
        {
            creature.Hate.Clear();
            StopFighting(creature);
        }

        /// <summary>
        /// How long a creature stands in a fight, free to act, without striking or gaining ground
        /// before it gives the fight up and goes home whole (<see cref="Stalled"/>). A player
        /// standing where the navmesh gives no route - a ledge, a rock - or inside the reach of
        /// every attack the creature has, used to be a free kill: the creature stood there,
        /// "in the fight" for as long as it was shot at, and never reset nor healed. Long
        /// enough that a windup (a Bane Hunter's, 4.9 s) or a slow attack's cooldown is not
        /// taken for it; a creature held still - stunned, rooted, frozen - is not free to act,
        /// and that time does not count.
        /// </summary>
        public const long StallEvadeMs = 15_000;

        /// <summary>
        /// Whether this creature gives up a fight it cannot press (<see cref="Stalled"/>): one of
        /// its own, with something to press it with. A minion is its master's to call off; a
        /// mission's escort or defender holds the place it was given; an emplacement stands
        /// down on its own terms (Emplacements) and a Shield Drone never leaves its ground
        /// (ShieldDrone); and a creature with no attack has no fight to be stalled in.
        /// </summary>
        internal static bool Evades(Creature creature)
        {
            return creature.MasterEntityId == 0
                && creature.Actions.Count > 0
                && !IsMissionEscort(creature)
                && !Game.Missions.World.CreatureGameplayRules.IsDefender(creature)
                && !Emplacements.Is(creature)
                && !ShieldDrone.HoldsGround(creature);
        }

        /// <summary>
        /// One fighting think's worth on the stall clock, for a creature free to act, and whether
        /// it has run out (<see cref="StallEvadeMs"/>). The clock is put back by
        /// <see cref="MadeProgress"/>: an attack used, one in range and coming, a step that gained ground.
        /// </summary>
        internal static bool Stalled(Creature creature, long delta)
        {
            if (!Evades(creature))
                return false;

            if (creature.MovementSpeed <= 0 || CrowdControl.IsRooted(creature))
                return false;

            creature.Controller.ActionFighting.StalledMs += delta;

            return creature.Controller.ActionFighting.StalledMs >= StallEvadeMs;
        }

        /// <summary>The creature is pressing its fight: the stall clock starts again (<see cref="Stalled"/>).</summary>
        internal static void MadeProgress(Creature creature)
        {
            creature.Controller.ActionFighting.StalledMs = 0;
        }

        #region Leash

        /// <summary>Whether the creature is running home after a leash: it takes no damage and starts no fight.</summary>
        public static bool IsReturning(Creature creature)
        {
            return creature?.Controller?.CurrentAction == BehaviorActionReturning;
        }

        /// <summary>Whether a creature with no master has followed a fight further from home than <see cref="MaxChaseDistance"/>, across the ground.</summary>
        public static bool ChasedTooFar(Creature creature)
        {
            var home = creature.HomePos.Position;
            var dx = creature.Position.X - home.X;
            var dz = creature.Position.Z - home.Z;

            return dx * dx + dz * dz > MaxChaseDistance * MaxChaseDistance;
        }

        /// <summary>
        /// The creature has chased too far: it forgets everyone, sheds what its attackers put on
        /// it - a DoT would otherwise go on killing it all the way home - drops out of its combat
        /// stance and runs back. It is put home regardless when it has taken twice as long as a
        /// straight run would, and never less than <see cref="ReturnTimeoutMinMs"/>.
        /// </summary>
        public void Leash(MapChannel mapChannel, Creature creature)
        {
            var controller = creature.Controller;

            creature.Hate.Clear();

            if (mapChannel != null)
            {
                foreach (var effect in creature.ActiveEffects.Values.Where(e => !e.IsBuff && !e.IsSkillPassive).ToList())
                    GameEffectManager.Instance.DettachEffect(mapChannel, creature, effect);

                CreatureWeaponDraw.Stow(mapChannel, creature);
            }

            var distance = Vector3.Distance(creature.Position, creature.HomePos.Position);

            controller.CurrentAction = BehaviorActionReturning;
            controller.ActionFighting.TargetEntityId = 0;
            Targets.Sync(mapChannel, creature);
            controller.ActionReturning.Elapsed = 0;
            controller.ActionReturning.TimeoutMs = ReturnTimeoutFor(distance, creature.RunSpeed);
            controller.Path.Clear();
            controller.PathIndex = 0;

            if (mapChannel != null)
                BuildPath(mapChannel, creature, creature.HomePos.Position);
        }

        /// <summary>How long a creature gets to run <paramref name="distance"/> metres home before it is put there: twice the straight run, at least <see cref="ReturnTimeoutMinMs"/>.</summary>
        public static long ReturnTimeoutFor(float distance, float runSpeed)
        {
            return Math.Max(ReturnTimeoutMinMs, (long)(distance / Math.Max(1f, runSpeed) * 2000f));
        }

        /// <summary>
        /// One think of the run home: along the path at run speed, and whole again on arrival -
        /// the end of the path, within <see cref="HomeReachedDistance"/> of home, or put there
        /// when the time is up.
        /// </summary>
        private void ReturnHome(MapChannel mapChannel, Creature creature, long delta)
        {
            var controller = creature.Controller;
            var home = creature.HomePos.Position;

            controller.ActionReturning.Elapsed += delta;

            var dx = creature.Position.X - home.X;
            var dz = creature.Position.Z - home.Z;
            var arrived = dx * dx + dz * dz <= HomeReachedDistance * HomeReachedDistance;

            if (!arrived && controller.ActionReturning.Elapsed >= controller.ActionReturning.TimeoutMs)
            {
                // Stuck, or off the navmesh with no way round: put it there.
                creature.Position = NavMeshManager.SnapToGround(mapChannel, home);
                CellManager.Instance.CellMoveObject(creature, new Movement(creature.Position, 0f, 0x08, new Vector2(creature.LastYaw, 0f)));
                arrived = true;
            }

            if (!arrived)
            {
                if (controller.Path.Count == 0)
                    BuildPath(mapChannel, creature, home);

                // The whole of a path built to home is as near home as the navmesh goes.
                if (!FollowPath(mapChannel, creature, creature.RunSpeed, delta))
                    return;
            }

            Restore(mapChannel, creature);

            creature.Hate.Clear();
            creature.LastRestTime = 0;
            creature.LastAgression = 0;
            SetActionWander(creature);
        }

        /// <summary>Health and armour back to their maximum, told to everyone around.</summary>
        public static void Restore(MapChannel mapChannel, Creature creature)
        {
            if (creature.Attributes.TryGetValue(Attributes.Health, out var health))
            {
                health.Current = health.CurrentMax;

                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, creature, new Packets.MapChannel.Server.UpdateHealthPacket(health, creature.EntityId));
            }

            if (creature.Attributes.TryGetValue(Attributes.Armor, out var armor))
            {
                armor.Current = armor.CurrentMax;

                if (mapChannel != null)
                    CellManager.Instance.CellCallMethod(mapChannel, creature, new Packets.MapChannel.Server.UpdateArmorPacket(GameEffectManager.WithRegen(creature, armor), creature.EntityId));
            }
        }

        #endregion

        #region Line of sight

        /// <summary>
        /// Whether the creature can see the target to shoot at it: any of the points on the
        /// target that cover is judged by (Cover.SamplePoints) in the clear from the creature's
        /// eyes, against the map's collision meshes and terrain. The same test cover uses, so a
        /// creature shoots at a target half behind a wall - the wall takes its share of the hit -
        /// and not at one wholly behind it. With no cover file for the map, everything is in
        /// sight, as it always was. Tested at most every <see cref="SightRecheckMs"/> per target.
        /// </summary>
        private static bool HasLineOfSight(MapChannel mapChannel, Creature creature, Actor target)
        {
            if (target == null || mapChannel?.Cover == null)
                return true;

            var fighting = creature.Controller.ActionFighting;

            if (fighting.SightTargetId == target.EntityId && fighting.SightRecheckIn > 0)
                return fighting.SightClear;

            fighting.SightTargetId = target.EntityId;
            fighting.SightRecheckIn = SightRecheckMs;
            fighting.SightClear = Sees(mapChannel.Cover, creature, target);

            return fighting.SightClear;
        }

        /// <summary>Whether any of the target's sample points is in the clear from the creature's eyes.</summary>
        public static bool Sees(Navigation.CoverMesh cover, Creature creature, Actor target)
        {
            if (cover == null)
                return true;

            return Cover.Visible(cover, Cover.EyeOf(creature), Cover.SamplePoints(target, creature.Position)) > 0;
        }

        #endregion
        
        private void SetActionPathFollowing(Creature creature)
        {
            creature.Controller.CurrentAction = BehaviorActionFollowingPath;
            creature.Controller.Path.Clear();
            creature.Controller.PathIndex = 0;
            // random position bias added to every node (to make groups look like they do not run on the same path)
            creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0] =  ((Random.Shared.Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;
            creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1] = ((Random.Shared.Next() % 1001) - 500) / 500.0f * creature.Controller.AiPathFollowing.GeneralPath.NodeOffsetRandomization;

            Logger.WriteLog(LogType.AI, $"path 0{creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[0]}");
            Logger.WriteLog(LogType.AI, $"path 1{creature.Controller.AiPathFollowing.RandomPathNodeBiasXZ[1]}");
        }

        /// <summary>
        /// The creature gives up whatever it was fighting - it died, left, or went out of sight
        /// (Detection) - and goes back to wandering.
        /// </summary>
        public void StopFighting(Creature creature)
        {
            if (creature.Controller == null)
                return;

            var mapChannel = creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId);

            // The fight is over: the weapon goes away (CreatureWeaponDraw).
            CreatureWeaponDraw.Stow(mapChannel, creature);

            // And a chase that was under way stops where it is. Nothing said so: the creature
            // went to wandering idle, which sends nothing until its first stroll, and the
            // clients ran it on at its last chase step's speed meanwhile.
            StopIfMoving(creature);

            // A creature that was following someone, or holding a spot - a minion, a mission
            // escort - goes back to that rather than wandering off where the fight left it.
            if (creature.Controller.ActionFollow.FollowTargetId != 0 || creature.Controller.ActionFollow.HasAnchor)
            {
                creature.Controller.CurrentAction = BehaviorActionFollow;
                creature.Controller.ActionFollow.PathUpdateTime = 0;
                creature.Controller.Path.Clear();
                creature.Controller.PathIndex = 0;
            }
            else
                SetActionWander(creature);

            // No target any more: the guns that aimed at it let go (Targets).
            Targets.Sync(mapChannel, creature);
        }

        /// <summary>
        /// Where a fight is leashed from: a creature's home, or for one with a master on the map
        /// (a reanimated creature, a minion, a traitor), the master - it goes where they go, and
        /// its spawn point is no longer its home.
        /// </summary>
        private static Vector3 LeashCentre(Creature creature)
        {
            if (creature.MasterEntityId != 0
                && EntityManager.Instance.Players.TryGetValue(creature.MasterEntityId, out var master)
                && master.MapContextId == creature.MapContextId)
                return master.Position;

            return creature.HomePos.Position;
        }

        /// <summary>Back to wandering after something else - a fight, a leash, a path - with the full interval to go before it strolls.</summary>
        private void SetActionWander(Creature creature)
        {
            StartWandering(creature, false);
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

            // An order can take it out of a fight: its gun lets go of the target (Targets).
            Targets.Sync(creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId), creature);
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

            Targets.Sync(creature.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(creature.MapContextId), creature);
        }

        /// <summary>
        /// What the entity a minion assists has targeted, when the minion may fight it (Threat.CanFight
        /// - alive, on the map, one its category allows); 0 when it assists nobody, is Passive, or
        /// the assisted has nothing it can fight selected - a player with a friend or a corpse
        /// selected is not asking for help.
        /// </summary>
        public static ulong AssistedTarget(Creature creature)
        {
            var assistId = creature.Controller.ActionFollow.AssistTargetId;

            if (assistId == 0 || creature.MasterEntityId == 0 || creature.Stance == MinionStance.Passive)
                return 0;

            // TryGetValue: the assisted player may have gone, and GetActor throws on a miss.
            if (!EntityManager.Instance.Actors.TryGetValue(assistId, out var assisted) || assisted.Target == 0 || assisted.Target == creature.EntityId)
                return 0;

            return Threat.CanFight(creature, assisted.Target) ? assisted.Target : 0;
        }

        /// <summary>
        /// Whether this creature goes looking for a fight. An ordinary creature always does; a
        /// minion does only when its master has set it Aggressive. Defensive still fights back,
        /// because retaliation comes through SetActionFighting rather than through a scan. One under
        /// Mind Control's Frighten, Confusion or Subversion does not: its fear, or the worker that
        /// picks who it turns on, decides.
        /// </summary>
        private static bool ScansForEnemies(Creature creature)
        {
            if (creature.ScriptedCombatGate != null)
                return false;

            var pump = AbilityManager.MindControlPumpOf(creature);

            if (pump > 0 && pump <= AbilityManager.MindControlSubversion)
                return false;

            return creature.MasterEntityId == 0 || creature.Stance == MinionStance.Aggressive;
        }

        private void UpdateCreatureTimers(Creature creature, long delta)
        {
            creature.LastAgression += delta;
            creature.LastRestTime += delta + Random.Shared.Next(1, 100);
            creature.Controller.TimerPathUpdateLock -= delta;
            creature.Controller.ActionFighting.SightRecheckIn -= delta;

            // update cooldown timer of all actions
            foreach (var action in creature.Actions)
                action.CooldownTimer -= delta;
        }
        
        /// <summary>
        /// Steps a creature toward (difX, difY, difZ) and broadcasts the movement.
        /// </summary>
        /// <param name="speed">Units per second; also what the client is told to extrapolate at.</param>
        /// <param name="elapsedMs">Time since the creature last moved.</param>
        /// <param name="knockback">
        /// A knockback carrying it: at the given speed whatever slows it, frozen or not, and facing
        /// back the way it came. Otherwise the speed is the creature's own times MovementSpeed (its
        /// slows), and a frozen creature only turns.
        /// </param>
        /// <returns>The distance actually moved.</returns>
        /// <param name="faceAlong">A carry that faces the way it goes (a Vortex pull) rather than back the way it came.</param>
        /// <param name="publish">Whether the step is sent to the clients: not for a rush, which they are making themselves.</param>
        /// <param name="grounded">Whether the step ends on the navmesh, as every walk's does but a beat's between two authored points (Patrols).</param>
        float UpdateEntityMovement(double difX, double difY, double difZ, Creature creature, MapChannel mapChannel, float speed, bool isMoved, long elapsedMs, bool knockback = false, bool faceAlong = false,
            bool synchronizeVisibility = false, bool publish = true, bool grounded = true)
        {
            if (!knockback)
            {
                speed = (float)(speed * creature.MovementSpeed);

                if (isMoved && CrowdControl.IsRooted(creature))
                    isMoved = false;
            }

            var remaining = Math.Sqrt(difX * difX + difY * difY + difZ * difZ);
            if (remaining < 0.0001d)
                return 0;
            var length = 1.0d / remaining;
            difX *= length;
            difY *= length;
            difZ *= length;
            var vX = knockback && !faceAlong ? (float)Math.Atan2(difX, difZ) : (float)Math.Atan2(-difX, -difZ);
            creature.LastYaw = vX;

            var velocity = isMoved ? speed : 0.0f;

            // Distance from elapsed time rather than a fixed speed/4 per call. The fixed step was
            // calibrated for the original 250 ms think rate; tying it to real time keeps creatures
            // at their stated speed however the MainLoop ticks land. Clamped to the distance left
            // so a large step cannot carry the creature past its node.
            var step = (float)Math.Min(velocity * elapsedMs / 1000.0d, remaining);

            // A force field that stops it (ForceFields): no further, walking or carried.
            if (isMoved && step > 0f && ForceFields.Stops(mapChannel, creature, creature.Position,
                    creature.Position + new Vector3((float)(difX * step), (float)(difY * step), (float)(difZ * step))))
            {
                isMoved = false;
                velocity = 0f;
                step = 0f;
            }

            if (isMoved)
            {
                creature.Position += new Vector3((float)(difX * step), (float)(difY * step), (float)(difZ * step));

                // Path corners carry the navmesh height; between them the ground is not a
                // straight line, so keep the feet on it.
                if (grounded)
                    creature.Position = NavMeshManager.SnapToGround(mapChannel, creature.Position);
            }

            if (synchronizeVisibility)
            {
                creature.Rotation = vX;
                SynchronizeMovementCell(mapChannel, creature);
            }

            if (!publish)
                return step;

            // send movement update
            var movement = new Movement(new Vector3(creature.Position.X, creature.Position.Y, creature.Position.Z), velocity, 0x08, new Vector2(vX, 0f));

            PublishMovement(creature, movement);

            return step;
        }
    }
}
