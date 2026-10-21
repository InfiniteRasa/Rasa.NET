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
    ///  - A follower whose leader is dead or gone strolls where it stands, and falls in behind
    ///    the next leader its pool sets down (BehaviorManager, the wander think).
    /// </summary>
    public static class PatrolGroups
    {
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

        /// <summary>Whether the pool is a group on a beat: a patrol, and more than its leader to walk it.</summary>
        public static bool IsGroup(SpawnPool pool) =>
            pool?.Patrol != null && pool.SpawnSlot != null && pool.SpawnSlot.Sum(slot => (int)slot.CountMax) > 1;

        /// <summary>Whether the creature is the one of its pool that walks the beat: the creature of the pool's first slot.</summary>
        public static bool Leads(SpawnPool pool, Creature creature) =>
            pool?.Patrol != null && pool.SpawnSlot?.Count > 0 && creature != null && pool.SpawnSlot[0].CreatureId == creature.DbId;

        /// <summary>Whether the creature keeps a place round its pool's leader: of a group's pool, not its leader, and no one's minion.</summary>
        public static bool Follows(Creature creature) =>
            creature != null && creature.MasterEntityId == 0 && creature.Patrol == null && IsGroup(creature.SpawnPool);

        /// <summary>
        /// Why a pool with a beat cannot be walked by a group, or null when it can. The leader
        /// is the first slot's one creature: a slot of several, or a pool that draws its
        /// creatures at random, could set down no leader or two; a later slot of the same
        /// creature could not be told from it. A pool with no creatures has nobody to walk it,
        /// and nobody to walk it wrongly.
        /// </summary>
        public static string Refuses(SpawnPool pool)
        {
            if (pool.SpawnSlot == null || pool.SpawnSlot.Count == 0)
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

        /// <summary>The pool's leader while it stands on the map; null when it is dead, gone, or was never set down.</summary>
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
            var taken = FollowersOf(map, leader)
                .Where(other => other != follower)
                .Select(other => other.Controller.ActionFollow.FormationSlot)
                .ToHashSet();

            var slot = 0;

            while (taken.Contains(slot))
                slot++;

            BehaviorManager.Instance.SetActionFollow(follower, leader.EntityId);
            follower.Controller.ActionFollow.FormationSlot = slot;
            follower.Controller.ActionFollow.PlaceOffset = null;
        }

        /// <summary>A formation place, from the leader, as it faces <paramref name="yaw"/>: slot 0 behind on one side, 1 on the other, then the next rank back.</summary>
        public static Vector3 Offset(int slot, float yaw)
        {
            var right = slot % 2 == 0 ? -Abreast : Abreast;
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
                follow.PlaceOffset = Offset(Math.Max(0, follow.FormationSlot), (float)leader.Rotation);

            if (Spacing.Placed(map, leader.Position + follow.PlaceOffset.Value) is Vector3 place)
                return place;

            var away = new Vector3(follower.Position.X - leader.Position.X, 0, follower.Position.Z - leader.Position.Z);

            if (away.LengthSquared() < 0.01f)
                return follower.Position;

            return Spacing.Placed(map, leader.Position + Vector3.Normalize(away) * TrailDistance) ?? follower.Position;
        }

        /// <summary>A follower's part in its group, for a GM: .npcinfo.</summary>
        public static string Describe(MapChannel map, Creature creature)
        {
            if (creature?.SpawnPool == null || !IsGroup(creature.SpawnPool))
                return null;

            var leader = LeaderOf(map, creature.SpawnPool);

            if (creature.Patrol != null)
                return leader == creature ? $"leading {FollowersOf(map, creature).Count()}" : "leading none: not its pool's leader";

            if (leader == null)
                return "leaderless, until its pool sets a leader down";

            var follow = creature.Controller.ActionFollow;

            return follow.FollowTargetId == leader.EntityId
                ? $"following {leader.EntityId}, place {follow.FormationSlot}"
                : $"not following its leader {leader.EntityId}";
        }
    }
}
