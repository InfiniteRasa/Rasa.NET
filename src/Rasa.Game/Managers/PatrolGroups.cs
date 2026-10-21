using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// A pack on a beat: a leader walking its pool's patrol (Patrols) with the rest of the pool
    /// keeping their places round it - a boss and its guards.
    ///
    /// The group is the pool. In a pool with a beat, the creature of its first slot is the
    /// leader and walks the beat as a lone patroller does; that slot is one creature, 1/1. Every
    /// other slot's creatures follow it (BehaviorActionFollow, with no master: they are no one's
    /// minions) and are no patrollers themselves. One pool keeps the group's respawn one clock:
    /// a dead leader or guard is topped back up as any pool's are (SpawnPoolManager.TopUp).
    ///
    ///  - Each follower has a place in the formation (<see cref="PlaceOf"/>): two abreast behind
    ///    the leader, <see cref="Abreast"/> to each side and a rank every <see cref="RankSpacing"/>
    ///    back. Its place is turned with the leader while the leader walks, and held where it is
    ///    while the leader stands - on a step, turning, sitting out a pause - so the pack does
    ///    not swing round at each about-turn of a beat walked there and back.
    ///  - The place is put on the navmesh and walked to across it (BehaviorManager.FollowGrounded):
    ///    the leader's steps are floor heights it walks between in straight lines, the
    ///    followers walk the ground beside it. A place off the mesh is given up for a point
    ///    behind the leader, <see cref="TrailDistance"/> from it.
    ///  - Each keeps its side of the beat: when the leader turns about, the two of a rank do
    ///    not cross behind it to the other side (<see cref="SideOf"/>).
    ///  - Standing at its place while the leader stands on its beat, a follower faces the way
    ///    the leader does.
    ///  - A follower whose leader is dead or gone strolls where it stands, and falls in behind
    ///    the next leader its pool sets down (BehaviorManager, the wander think).
    ///  - Unless the pool has an acting leader (spawnpool_patrol_group): then the follower with
    ///    the first place leads on the beat in the dead leader's stead (<see cref="Succeed"/>),
    ///    and gives it back once the pool's own leader is set down again (<see cref="StepDown"/>).
    ///    With its first slot empty as well, the pool has no leader of its own, and one of its
    ///    creatures always leads: a patrol of equals.
    ///  - It fights as one: one of the group that starts a fight - the scan, a hit - takes the
    ///    rest of the group into it (<see cref="Assist"/>), and a follower out of a fight its
    ///    leader is still in goes back into it. When the leader is drawn too far off its beat
    ///    and runs back (BehaviorManager.Leash), its followers give the fight up with it.
    ///  - A follower that cannot get to its place - no route there - is put on it after
    ///    as long as a run there would take twice over (<see cref="StrayDistance"/>).
    /// </summary>
    public static class PatrolGroups
    {
        /// <summary>Further than this from its place for too long, a follower is put on it.</summary>
        public const float StrayDistance = 40f;

        /// <summary>Set while one of a group takes the rest into its fight: their own fights starting do not do it over again.</summary>
        private static bool _assisting;

        /// <summary>How far to each side of the leader the two of a rank walk. Spacing.Distance.</summary>
        public const float Abreast = 3f;

        /// <summary>How far behind the leader the first rank walks.</summary>
        public const float FirstRank = 1.5f;

        /// <summary>How far behind each rank the next one walks.</summary>
        public const float RankSpacing = 3f;

        /// <summary>A follower that cannot stand at its place keeps this far behind the leader instead. MinionManager.FollowDistance.</summary>
        public const float TrailDistance = 4f;

        /// <summary>Further than this from its place - and as far again as its leader walks in a think - a follower runs to it: at the leader's pace it would never close the gap.</summary>
        public const float RunDistance = 2f;

        /// <summary>Back within this of its place - and as far again as its leader walks in a think - a running follower walks again.</summary>
        public const float WalkDistance = 0.5f;

        /// <summary>A leader heading this far off the leg of the beat it walks is not walking it: coming onto it from a fight, say. An eighth of a turn.</summary>
        public const float LegTolerance = MathF.PI / 4f;

        /// <summary>Within this of its place, while the leader stands, a follower stands too.</summary>
        public const float PlaceReached = 0.5f;

        /// <summary>
        /// A follower goes back into its leader's fight only with a target this near the leader:
        /// well inside BehaviorManager.MaxChaseDistance, where it drops one, so a target at the
        /// edge does not draw it in and out of the fight each think.
        /// </summary>
        public const float RejoinFightDistance = 45f;

        /// <summary>
        /// Whether the pool has a leader of its own: the creature of its first slot. Not a pool
        /// with an acting leader and an empty first slot (spawnpool_patrol_group), which one of
        /// its creatures always leads.
        /// </summary>
        public static bool HasLeaderSlot(SpawnPool pool) => !(pool.ActingLeader && pool.FirstSlotEmpty);

        /// <summary>Whether the pool is a group on a beat: a patrol, and more than its leader to walk it - or no leader of its own, so one of them leads.</summary>
        public static bool IsGroup(SpawnPool pool) =>
            pool?.Patrol != null && pool.SpawnSlot != null
            && (!HasLeaderSlot(pool) || pool.SpawnSlot.Sum(slot => slot?.CountMax ?? 0) > 1);

        /// <summary>Whether the creature is its pool's own leader: the creature of the pool's first slot.</summary>
        public static bool Leads(SpawnPool pool, Creature creature) =>
            pool?.Patrol != null && HasLeaderSlot(pool) && pool.SpawnSlot?.Count > 0 && creature != null && pool.SpawnSlot[0]?.CreatureId == creature.DbId;

        /// <summary>
        /// Whether the creature keeps a place round its pool's leader: of a group's pool, not
        /// leading it, no one's minion, and not under a player's Mind Control, which has it
        /// follow them instead (AbilityManager.MindControl).
        /// </summary>
        public static bool Follows(Creature creature) =>
            creature != null && creature.MasterEntityId == 0 && creature.Patrol == null && IsGroup(creature.SpawnPool)
            && AbilityManager.MindControlPumpOf(creature) == 0;

        /// <summary>
        /// Why a pool with a beat cannot be walked by a group, or null when it can. The leader
        /// is the first slot's one creature: a slot of several, or a pool that draws its
        /// creatures at random, could set down no leader or two; a later slot of the same
        /// creature could not be told from it. A pool with no leader of its own has none of
        /// these to fear: whichever of it is set down first leads. A pool with no creatures has
        /// nobody to walk it, and nobody to walk it wrongly.
        /// </summary>
        public static string Refuses(SpawnPool pool)
        {
            if (pool.SpawnSlot == null || pool.SpawnSlot.Count == 0 || !HasLeaderSlot(pool))
                return null;

            var leader = pool.SpawnSlot[0];

            if (leader == null || leader.CreatureId == 0)
                return "its first slot, the leader, holds no creature";

            if (leader.CountMin != 1 || leader.CountMax != 1)
                return $"its first slot, the leader, is {leader.CountMin}-{leader.CountMax} creatures, not 1";

            if (pool.DrawCount > 0)
                return "it draws its creatures at random (spawnpool_draw)";

            if (pool.SpawnSlot.Skip(1).Any(slot => slot?.CreatureId == leader.CreatureId))
                return $"another slot holds its leader's creature {leader.CreatureId}";

            return null;
        }

        /// <summary>
        /// The pool's leader, its own or acting, to follow: null when it is dead, gone, was never
        /// set down, or is under a player's Mind Control (AbilityManager.MindControl) - the group
        /// does not trail it to the player, and strolls where it is until it is free.
        /// </summary>
        public static Creature LeaderOf(MapChannel map, SpawnPool pool)
        {
            var leader = StandingLeader(map, pool);

            return leader != null && AbilityManager.MindControlPumpOf(leader) == 0 ? leader : null;
        }

        /// <summary>
        /// The pool's leader while it stands on the map, under Mind Control or not: a group whose
        /// leader is only held for a time has no other leader set down, nor one acting for it.
        /// </summary>
        public static Creature StandingLeader(MapChannel map, SpawnPool pool)
        {
            var leader = pool?.Leader;

            if (leader == null || !ReferenceEquals(leader.SpawnPool, pool) || !CreatureManager.IsLivingOnMap(map, leader))
                return null;

            return leader;
        }

        /// <summary>The living creatures keeping places round the leader.</summary>
        public static IEnumerable<Creature> FollowersOf(MapChannel map, Creature leader) =>
            EntityManager.Instance.Creatures.Values.Where(creature =>
                creature != leader && ReferenceEquals(creature.SpawnPool, leader.SpawnPool)
                && creature.Controller.ActionFollow.FollowTargetId == leader.EntityId
                && CreatureManager.IsLivingOnMap(map, creature));

        /// <summary>
        /// Puts the follower in behind the leader, on the first place of the formation no other
        /// follower of it holds.
        /// </summary>
        public static void Attach(MapChannel map, Creature follower, Creature leader)
        {
            var slot = FreeSlot(map, follower, leader);

            BehaviorManager.Instance.SetActionFollow(follower, leader.EntityId);
            follower.Controller.ActionFollow.FormationSlot = slot;
            follower.Controller.ActionFollow.PlaceOffset = null;
            follower.Controller.ActionFollow.DroppedTargetId = 0;
        }

        /// <summary>The first place of the leader's formation no other follower of it holds.</summary>
        private static int FreeSlot(MapChannel map, Creature follower, Creature leader)
        {
            var taken = FollowersOf(map, leader)
                .Where(other => other != follower)
                .Select(other => other.Controller.ActionFollow.FormationSlot)
                .ToHashSet();

            var slot = 0;

            while (taken.Contains(slot))
                slot++;

            return slot;
        }

        /// <summary>
        /// A group with an acting leader and no leader standing: the follower with the first
        /// place of the formation leads it on the beat, from the step the last leader was
        /// walking to, and the rest fall in behind it - those in a fight too, so they give it up
        /// with it when it is drawn too far (LeashFollowers). Null for a pool with no acting
        /// leader, a leader still standing, or nobody left to lead.
        /// </summary>
        public static Creature Succeed(MapChannel map, SpawnPool pool)
        {
            if (pool == null || !pool.ActingLeader || !IsGroup(pool) || map == null || StandingLeader(map, pool) != null)
                return null;

            var next = MembersOf(map, pool)
                .Where(creature => creature.Patrol == null)
                .OrderBy(creature => creature.Controller.ActionFollow.FormationSlot < 0 ? int.MaxValue : creature.Controller.ActionFollow.FormationSlot)
                .ThenBy(creature => creature.EntityId)
                .FirstOrDefault();

            if (next == null)
                return null;

            var step = pool.Leader?.Controller.ActionPatrol.Step ?? 0;
            var follow = next.Controller.ActionFollow;

            follow.FollowTargetId = 0;
            follow.FormationSlot = -1;
            follow.PlaceOffset = null;
            follow.CatchUpRunning = false;
            follow.StrayMs = 0;

            next.Patrol = pool.Patrol;
            next.LastYaw = (float)next.Rotation;
            next.Controller.ActionPatrol.Step = step;
            SetLeader(pool, next);

            // In their old order of places, so each keeps near where it walked.
            foreach (var other in MembersOf(map, pool)
                         .Where(creature => creature != next && creature.Patrol == null)
                         .OrderBy(creature => creature.Controller.ActionFollow.FormationSlot < 0 ? int.MaxValue : creature.Controller.ActionFollow.FormationSlot)
                         .ThenBy(creature => creature.EntityId)
                         .ToList())
                Reattach(map, other, next);

            // Idle, it sets off on the beat at its next think; in a fight, once that is over.
            if (next.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting && !BehaviorManager.IsReturning(next))
                BehaviorManager.StartWandering(next, false);

            return next;
        }

        /// <summary>
        /// The creature now leads the pool's group: its own leader set down, or an acting one.
        /// Its formation starts afresh, not mirrored for a beat its last leader walked.
        /// </summary>
        public static void SetLeader(SpawnPool pool, Creature leader)
        {
            pool.Leader = leader;
            pool.FormationYaw = null;
            pool.FormationMirrored = false;
        }

        /// <summary>
        /// The follower drops its fight as it is - the target too far from its group, or out of
        /// its reach - and goes back to its place. Unlike a leash it is not made whole. A target
        /// too far off it goes for again once it is back within <see cref="RejoinFightDistance"/>
        /// of the leader; one out of its reach it is not drawn back into its leader's fight with
        /// (<paramref name="outOfReach"/>): it would only stand stalled again.
        /// </summary>
        public static void DropFight(Creature follower, bool outOfReach)
        {
            follower.Controller.ActionFollow.DroppedTargetId = outOfReach ? follower.Controller.ActionFighting.TargetEntityId : 0;
            follower.LastRestTime = 0;
            BehaviorManager.Instance.GiveUp(follower);
        }

        /// <summary>
        /// The pool's own leader is back: the one that led in its place gives the beat up and
        /// falls in behind it, and so do those that followed it, each staying in any fight it is in.
        /// </summary>
        public static void StepDown(MapChannel map, Creature acting, Creature leader)
        {
            if (Leads(acting.SpawnPool, acting))
                return;

            var followers = FollowersOf(map, acting).ToList();

            acting.Patrol = null;
            Reattach(map, acting, leader);

            foreach (var follower in followers)
                Reattach(map, follower, leader);
        }

        /// <summary>
        /// Puts the follower in behind the leader; one in a fight, running home, or moved by a
        /// mission scene only once that is over, though it counts as the leader's from now.
        /// </summary>
        private static void Reattach(MapChannel map, Creature follower, Creature leader)
        {
            var action = follower.Controller.CurrentAction;

            if (action != BehaviorManager.BehaviorActionFighting && action != BehaviorManager.BehaviorActionScriptedMove
                && !BehaviorManager.IsReturning(follower))
            {
                Attach(map, follower, leader);
                return;
            }

            follower.Controller.ActionFollow.FormationSlot = FreeSlot(map, follower, leader);
            follower.Controller.ActionFollow.FollowTargetId = leader.EntityId;
            follower.Controller.ActionFollow.PlaceOffset = null;
            follower.Controller.ActionFollow.DroppedTargetId = 0;
        }

        /// <summary>
        /// A formation place, from the leader, as it faces <paramref name="yaw"/>: slot 0 behind
        /// on one side, 1 on the other, then the next rank back. Mirrored, each on the other side.
        /// </summary>
        public static Vector3 Offset(int slot, float yaw, bool mirrored = false)
        {
            var right = (slot % 2 == 0) != mirrored ? -Abreast : Abreast;
            var back = FirstRank + RankSpacing * (slot / 2);

            // A walk along (dx, dz) has the yaw atan2(-dx, -dz): ahead is (-sin, -cos).
            var ahead = new Vector3(-MathF.Sin(yaw), 0, -MathF.Cos(yaw));
            var side = new Vector3(MathF.Cos(yaw), 0, -MathF.Sin(yaw));

            return side * right - ahead * back;
        }

        /// <summary>
        /// Where the follower stands round its leader: its formation place, turned with the
        /// leader while the leader walks and held while it stands, on the navmesh. A place off
        /// the mesh - a cliff, a wall the beat runs along - is taken on the other side of the
        /// leader; that off the mesh too, a point <see cref="TrailDistance"/> straight behind
        /// it; and that too, the leader's own ground.
        /// </summary>
        public static Vector3 PlaceOf(MapChannel map, Creature follower, Creature leader)
        {
            var follow = follower.Controller.ActionFollow;
            var walking = leader.Controller.LastMovement?.Velocity > 0f;
            var yaw = (float)leader.Rotation;
            var slot = Math.Max(0, follow.FormationSlot);

            if (walking || follow.PlaceOffset == null)
                follow.PlaceOffset = Offset(slot, yaw, SideOf(leader));

            if (Spacing.Placed(map, leader.Position + follow.PlaceOffset.Value) is Vector3 place)
                return place;

            if (Spacing.Placed(map, leader.Position + Offset(slot, yaw, !leader.SpawnPool.FormationMirrored)) is Vector3 across)
                return across;

            var behind = new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw)) * TrailDistance;

            return Spacing.Placed(map, leader.Position + behind) ?? Spacing.Placed(map, leader.Position) ?? leader.Position;
        }

        /// <summary>
        /// Whether the leader's formation is mirrored. Each time the leader is seen walking a leg
        /// of its beat more than a quarter turn from the way it last walked one - the about-turn
        /// at the end of a beat walked there and back - the formation is mirrored over, so each
        /// follower stays on the side of the beat it was on. A corner of a circuit, taken a step
        /// at a time, turns it with the leader. Walking anything else - a chase, the way back
        /// onto its beat, from wherever that brings it to the step - it is left as it is.
        /// </summary>
        public static bool SideOf(Creature leader)
        {
            var pool = leader.SpawnPool;
            var steps = leader.Patrol;
            var patrol = leader.Controller.ActionPatrol;

            if (leader.Controller.CurrentAction != BehaviorManager.BehaviorActionPatrol || patrol.Rejoining
                || steps == null || steps.Count < 2 || patrol.Step < 0 || patrol.Step >= steps.Count)
                return pool.FormationMirrored;

            var yaw = (float)leader.Rotation;
            var from = steps[(patrol.Step + steps.Count - 1) % steps.Count].Position;
            var leg = AbilityManager.YawTowards(from, steps[patrol.Step].Position);

            if (MathF.Abs(Patrols.Turn(yaw, leg)) > LegTolerance)
                return pool.FormationMirrored;

            if (pool.FormationYaw is float last && MathF.Abs(Patrols.Turn(last, yaw)) > MathF.PI / 2f)
                pool.FormationMirrored = !pool.FormationMirrored;

            pool.FormationYaw = yaw;

            return pool.FormationMirrored;
        }

        /// <summary>The living creatures of the pool on the map: its leader and its followers.</summary>
        public static IEnumerable<Creature> MembersOf(MapChannel map, SpawnPool pool) =>
            EntityManager.Instance.Creatures.Values.Where(creature =>
                ReferenceEquals(creature.SpawnPool, pool) && creature.MasterEntityId == 0
                && AbilityManager.MindControlPumpOf(creature) == 0 && CreatureManager.IsLivingOnMap(map, creature));

        /// <summary>
        /// One of a group has started a fight with <paramref name="targetEntityId"/>: the rest of
        /// the group not in one go into it too, knowing whom it hates. One running home after a
        /// leash does not (BehaviorManager.CanSetActionFighting), nor one a mission scene moves.
        /// </summary>
        public static void Assist(MapChannel map, Creature creature, ulong targetEntityId)
        {
            if (_assisting || creature.MasterEntityId != 0 || !IsGroup(creature.SpawnPool) || map == null)
                return;

            _assisting = true;

            try
            {
                var grudges = creature.Hate.Ranked();

                foreach (var other in MembersOf(map, creature.SpawnPool).Where(other => other != creature).ToList())
                {
                    if (other.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting
                        || other.Controller.CurrentAction == BehaviorManager.BehaviorActionScriptedMove)
                        continue;

                    foreach (var grudge in grudges)
                        other.Hate.Ensure(grudge.Key, Threat.NoticedThreat);

                    BehaviorManager.Instance.TrySetActionFighting(other, targetEntityId);
                }
            }
            finally
            {
                _assisting = false;
            }
        }

        /// <summary>
        /// One of a group has been hit by someone it did not know of: the rest of the group know
        /// of them too, and may turn on them - a healer behind the one they fight, a second
        /// attacker. Only a newcomer to its table is passed on, so a hit is not a search of the
        /// group each time.
        /// </summary>
        public static void ShareHate(Creature victim, ulong attackerId, bool newcomer)
        {
            if (!newcomer || victim.MasterEntityId != 0 || !IsGroup(victim.SpawnPool))
                return;

            var map = victim.RuntimeMapChannel ?? MapChannelManager.Instance.FindByContextId(victim.MapContextId);

            if (map == null)
                return;

            foreach (var other in MembersOf(map, victim.SpawnPool))
                if (other != victim && other.Controller.CurrentAction != BehaviorManager.BehaviorActionScriptedMove)
                    other.Hate.Ensure(attackerId, Threat.NoticedThreat);
        }

        /// <summary>
        /// The leader is home again after a leash, put there if it took too long, and whole: so
        /// is the group. Its followers not in a fight are made whole and forget whom they hated,
        /// as those that ran home with it are - one that had dropped its fight is not left hurt.
        /// Those further than <see cref="StrayDistance"/> from it are put on their places, rather
        /// than left to find their own way back.
        /// </summary>
        public static void Regroup(MapChannel map, Creature leader)
        {
            if (map == null || leader.Patrol == null || !IsGroup(leader.SpawnPool) || LeaderOf(map, leader.SpawnPool) != leader)
                return;

            foreach (var follower in FollowersOf(map, leader).ToList())
            {
                if (follower.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting || BehaviorManager.IsReturning(follower)
                    || follower.Controller.CurrentAction == BehaviorManager.BehaviorActionScriptedMove)
                    continue;

                BehaviorManager.Restore(map, follower);
                follower.Hate.Clear();
                follower.LastRestTime = 0;
                follower.Controller.ActionFollow.DroppedTargetId = 0;

                if (Spacing.AcrossGround(follower.Position, leader.Position) > StrayDistance)
                    BehaviorManager.Instance.PutAtPlace(map, follower, PlaceOf(map, follower, leader));
            }
        }

        /// <summary>The leader runs back to its beat after a leash: its followers still fighting give the fight up and run back with it.</summary>
        public static void LeashFollowers(MapChannel map, Creature leader)
        {
            if (map == null || leader.Patrol == null || !IsGroup(leader.SpawnPool))
                return;

            foreach (var follower in FollowersOf(map, leader).ToList())
                if (follower.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                    BehaviorManager.Instance.Leash(map, follower);
        }

        /// <summary>
        /// Whether the creature leads a group along its beat: other creatures it bumps into are
        /// not pushed apart from it (BehaviorManager), as its followers would push it off the beat
        /// at every step. A lone patroller is pushed as any creature, and rejoins its beat.
        /// </summary>
        public static bool LeadsOnBeat(Creature creature) =>
            creature.Controller.CurrentAction == BehaviorManager.BehaviorActionPatrol && IsGroup(creature.SpawnPool);

        /// <summary>A follower's part in its group, for a GM: .npcinfo.</summary>
        public static string Describe(MapChannel map, Creature creature)
        {
            if (creature?.SpawnPool == null || !IsGroup(creature.SpawnPool))
                return null;

            var leader = LeaderOf(map, creature.SpawnPool);

            if (creature.Patrol != null)
                return leader == creature
                    ? $"leading {FollowersOf(map, creature).Count()}{(Leads(creature.SpawnPool, creature) ? "" : ", acting")}"
                    : "leading none: not its pool's leader";

            if (leader == null)
                return "leaderless, until its pool sets a leader down";

            var follow = creature.Controller.ActionFollow;

            return follow.FollowTargetId == leader.EntityId
                ? $"following {leader.EntityId}, place {follow.FormationSlot}"
                : $"not following its leader {leader.EntityId}";
        }
    }
}
