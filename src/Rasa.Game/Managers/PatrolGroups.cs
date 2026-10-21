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

        /// <summary>Further than this from its place, a follower runs to it: at the leader's pace it would never close the gap.</summary>
        public const float RunDistance = 2f;

        /// <summary>Back within this of its place, a running follower walks again.</summary>
        public const float WalkDistance = 0.5f;

        /// <summary>Within this of its place, while the leader stands, a follower stands too.</summary>
        public const float PlaceReached = 0.5f;

        /// <summary>
        /// Whether the pool has a leader of its own: the creature of its first slot. Not a pool
        /// with an acting leader and an empty first slot (spawnpool_patrol_group), which one of
        /// its creatures always leads.
        /// </summary>
        public static bool HasLeaderSlot(SpawnPool pool) => !(pool.ActingLeader && pool.FirstSlotEmpty);

        /// <summary>Whether the pool is a group on a beat: a patrol, and more than its leader to walk it - or no leader of its own, so one of them leads.</summary>
        public static bool IsGroup(SpawnPool pool) =>
            pool?.Patrol != null && pool.SpawnSlot != null
            && (!HasLeaderSlot(pool) || pool.SpawnSlot.Sum(slot => (int)slot.CountMax) > 1);

        /// <summary>Whether the creature is its pool's own leader: the creature of the pool's first slot.</summary>
        public static bool Leads(SpawnPool pool, Creature creature) =>
            pool?.Patrol != null && HasLeaderSlot(pool) && pool.SpawnSlot?.Count > 0 && creature != null && pool.SpawnSlot[0].CreatureId == creature.DbId;

        /// <summary>Whether the creature keeps a place round its pool's leader: of a group's pool, not leading it, and no one's minion.</summary>
        public static bool Follows(Creature creature) =>
            creature != null && creature.MasterEntityId == 0 && creature.Patrol == null && IsGroup(creature.SpawnPool);

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

            if (leader.CountMin != 1 || leader.CountMax != 1)
                return $"its first slot, the leader, is {leader.CountMin}-{leader.CountMax} creatures, not 1";

            if (pool.DrawCount > 0)
                return "it draws its creatures at random (spawnpool_draw)";

            if (pool.SpawnSlot.Skip(1).Any(slot => slot.CreatureId == leader.CreatureId))
                return $"another slot holds its leader's creature {leader.CreatureId}";

            return null;
        }

        /// <summary>The pool's leader, its own or acting, while it stands on the map; null when it is dead, gone, or was never set down.</summary>
        public static Creature LeaderOf(MapChannel map, SpawnPool pool)
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
        /// walking to. Null for a pool with no acting leader, or nobody left to lead.
        /// </summary>
        public static Creature Succeed(MapChannel map, SpawnPool pool)
        {
            if (pool == null || !pool.ActingLeader || !IsGroup(pool) || map == null || LeaderOf(map, pool) != null)
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
            pool.Leader = next;

            // Idle, it sets off on the beat at its next think; in a fight, once that is over.
            if (next.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting && !BehaviorManager.IsReturning(next))
                BehaviorManager.StartWandering(next, false);

            return next;
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

        /// <summary>Puts the follower in behind the leader; one in a fight, or running home, only once it is over.</summary>
        private static void Reattach(MapChannel map, Creature follower, Creature leader)
        {
            if (follower.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting && !BehaviorManager.IsReturning(follower))
            {
                Attach(map, follower, leader);
                return;
            }

            follower.Controller.ActionFollow.FormationSlot = FreeSlot(map, follower, leader);
            follower.Controller.ActionFollow.FollowTargetId = leader.EntityId;
            follower.Controller.ActionFollow.PlaceOffset = null;
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
        /// leader while the leader walks and held while it stands, on the navmesh; a point
        /// <see cref="TrailDistance"/> behind the leader, on its side, when the place is off
        /// the mesh; where it stands when that is off it too.
        /// </summary>
        public static Vector3 PlaceOf(MapChannel map, Creature follower, Creature leader)
        {
            var follow = follower.Controller.ActionFollow;
            var walking = leader.Controller.LastMovement?.Velocity > 0f;

            if (walking || follow.PlaceOffset == null)
                follow.PlaceOffset = Offset(Math.Max(0, follow.FormationSlot), (float)leader.Rotation, SideOf(leader.SpawnPool, (float)leader.Rotation));

            if (Spacing.Placed(map, leader.Position + follow.PlaceOffset.Value) is Vector3 place)
                return place;

            var away = new Vector3(follower.Position.X - leader.Position.X, 0, follower.Position.Z - leader.Position.Z);

            if (away.LengthSquared() < 0.01f)
                return follower.Position;

            return Spacing.Placed(map, leader.Position + Vector3.Normalize(away) * TrailDistance) ?? follower.Position;
        }

        /// <summary>
        /// Whether the formation is mirrored as the leader faces <paramref name="yaw"/>. Each
        /// time the leader is seen walking more than a quarter turn from the way it last walked
        /// - the about-turn at the end of a beat walked there and back - the formation is
        /// mirrored over, so each follower stays on the side of the beat it was on. A corner of a
        /// circuit, taken a step at a time, turns it with the leader.
        /// </summary>
        public static bool SideOf(SpawnPool pool, float yaw)
        {
            if (pool.FormationYaw is float last && MathF.Abs(Patrols.Turn(last, yaw)) > MathF.PI / 2f)
                pool.FormationMirrored = !pool.FormationMirrored;

            pool.FormationYaw = yaw;

            return pool.FormationMirrored;
        }

        /// <summary>The living creatures of the pool on the map: its leader and its followers.</summary>
        public static IEnumerable<Creature> MembersOf(MapChannel map, SpawnPool pool) =>
            EntityManager.Instance.Creatures.Values.Where(creature =>
                ReferenceEquals(creature.SpawnPool, pool) && creature.MasterEntityId == 0 && CreatureManager.IsLivingOnMap(map, creature));

        /// <summary>
        /// One of a group has started a fight with <paramref name="targetEntityId"/>: the rest of
        /// the group not in one go into it too. One running home after a leash does not
        /// (BehaviorManager.CanSetActionFighting).
        /// </summary>
        public static void Assist(MapChannel map, Creature creature, ulong targetEntityId)
        {
            if (_assisting || creature.MasterEntityId != 0 || !IsGroup(creature.SpawnPool) || map == null)
                return;

            _assisting = true;

            try
            {
                foreach (var other in MembersOf(map, creature.SpawnPool).Where(other => other != creature).ToList())
                    if (other.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                        BehaviorManager.Instance.TrySetActionFighting(other, targetEntityId);
            }
            finally
            {
                _assisting = false;
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
