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
    ///    not swing round at a corner.
    ///  - At an about-turn - the end of a beat walked there and back - the group countermarches
    ///    (<see cref="Reform"/>): the leader turns about as it comes onto the step, its
    ///    followers walk past it, facing the way they came, to their places behind it the new
    ///    way, and turn about there; and it walks on once they are in place
    ///    (<see cref="Formed"/>, at most <see cref="ReformHoldMs"/> after its pause). Turning
    ///    with it and then running back to the far side of it, as they did, they slid there
    ///    facing the wrong way (user playtest 2026-10-22).
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
    ///  - A follower fights on a tether to its leader (<see cref="TetherDistance"/>), not a
    ///    leash: it chases no further from it, and at the tether's end, with nothing it can
    ///    strike its target with, it holds and evades (<see cref="HoldsAtTether"/>), then goes
    ///    back to its place (<see cref="TetherHoldMs"/>). So the guards stay by a boss that is
    ///    stunned or rooted, rather than dropping the fight, and are not shot down from beyond
    ///    their reach. Ours, decided 2026-10-21 (group-patrol-plan.md, "Tether"): the retail
    ///    server's rule is not known; evading where a mob cannot reach its attacker is what
    ///    other MMOs do (World of Warcraft's Evade).
    ///  - A follower that cannot get to its place - no route there - is put on it after
    ///    as long as a run there would take twice over (<see cref="StrayDistance"/>).
    /// </summary>
    public static class PatrolGroups
    {
        /// <summary>Further than this from its place for too long, a follower is put on it.</summary>
        public const float StrayDistance = 40f;

        /// <summary>Set while one of a group takes the rest into its fight: their own fights starting do not do it over again.</summary>
        private static bool _assisting;

        /// <summary>How far to each side of the leader the two of a rank walk, by default: Spacing.Distance. A pool may set its own (<see cref="AbreastOf"/>).</summary>
        public const float Abreast = 3f;

        /// <summary>How far behind the leader the first rank walks: half of <see cref="Abreast"/>, or of a pool's own spacing.</summary>
        public const float FirstRank = Abreast / 2;

        /// <summary>How far behind each rank the next one walks: <see cref="Abreast"/>, or a pool's own spacing.</summary>
        public const float RankSpacing = Abreast;

        /// <summary>
        /// How far to each side of the leader its group walks: the pool's own spacing
        /// (spawnpool_patrol_group.spacing) - for creatures too big for the default - else
        /// <see cref="Abreast"/>. The ranks behind scale with it.
        /// </summary>
        public static float AbreastOf(SpawnPool pool) => pool?.FormationSpacing ?? Abreast;

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
        /// The longest a leader that turned about waits on its step, past its pause, for its
        /// followers to reach their new places (<see cref="Reform"/>): one that cannot does not
        /// hold the beat up. A guess (ESTIMATE): the countermarch of two ranks 5 m apart is 10 m,
        /// 5 s at 2 m/s.
        /// </summary>
        public const long ReformHoldMs = 10000;

        /// <summary>A turn of the leader's further than this, where it stands, is an about-turn: its group countermarches (<see cref="Reform"/>). A quarter turn.</summary>
        public const float AboutTurn = MathF.PI / 2f;

        /// <summary>
        /// How far from its leader a follower goes in a fight. A guess, to be tuned in play:
        /// enough to reach whoever stands by the boss, short of leaving it - half the 60 m a
        /// lone creature chases (BehaviorManager.MaxChaseDistance).
        /// </summary>
        public const float TetherDistance = 30f;

        /// <summary>Within this of the tether's end a follower counts as at it.</summary>
        public const float TetherSlack = 1.5f;

        /// <summary>How long a follower holds at its tether's end, evading, before it gives the fight up and goes back to its place.</summary>
        public const long TetherHoldMs = 10_000;

        /// <summary>
        /// A follower goes back into its leader's fight only with a target this near the leader:
        /// inside the tether, so it can get to it, and short of its end, so a target there does
        /// not draw it in and out of the fight.
        /// </summary>
        public const float RejoinFightDistance = TetherDistance - 5f;

        /// <summary>Within this of its place, on the navmesh, a follower steps straight onto it rather than along a route. Two of Abreast: the width of the formation.</summary>
        public const float DirectDistance = 2 * Abreast;

        /// <summary><see cref="DirectDistance"/> for the pool's own spacing: the width of its formation.</summary>
        public static float DirectDistanceOf(SpawnPool pool) => 2 * AbreastOf(pool);

        /// <summary>
        /// Within this of its place while its leader stands, a follower walks there rather than
        /// running: the leader waits for it. Four of the pool's spacing: a countermarch of up to
        /// four ranks.
        /// </summary>
        public static float ReformWalkDistanceOf(SpawnPool pool) => 4 * AbreastOf(pool);

        /// <summary>Whether the creature leads a group on its beat with someone following it.</summary>
        public static bool LeadsFollowers(MapChannel map, Creature leader) =>
            IsGroup(leader.SpawnPool) && FollowersOf(map, leader).Any();

        /// <summary>
        /// The leader has turned about where it stands, to <paramref name="yaw"/>: the formation is
        /// set for the new way at once, as it would be once the leader walks it - mirrored, each
        /// follower on the side of the beat it was on, when the new way is more than a quarter turn
        /// from the leg it last walked, as <see cref="SideOf"/> has it - and the ranks are reversed - the
        /// first rank goes to the back and the last comes to the front - so the followers walking
        /// round, each the same way, do not walk through one another. Their places are put anew.
        /// </summary>
        public static void Reform(MapChannel map, Creature leader, float yaw)
        {
            var pool = leader.SpawnPool;
            var followers = FollowersOf(map, leader).ToList();

            if (pool.FormationYaw is float last && MathF.Abs(Patrols.Turn(last, yaw)) > MathF.PI / 2f)
                pool.FormationMirrored = !pool.FormationMirrored;

            pool.FormationYaw = yaw;

            if (followers.Count == 0)
                return;

            var ranks = followers.Max(follower => Math.Max(0, follower.Controller.ActionFollow.FormationSlot)) / 2 + 1;

            foreach (var follower in followers)
            {
                var follow = follower.Controller.ActionFollow;
                var slot = Math.Max(0, follow.FormationSlot);

                follow.FormationSlot = 2 * (ranks - 1 - slot / 2) + slot % 2;
                follow.PlaceOffset = null;
            }
        }

        /// <summary>
        /// Whether each follower keeping a place round the leader stands at it, turned the way the
        /// leader faces: the leader that turned about walks on once the last of them has made its
        /// turn (<see cref="Reform"/>). One in a fight, or not following, is not waited for.
        /// </summary>
        public static bool Formed(MapChannel map, Creature leader) =>
            FollowersOf(map, leader).All(follower =>
                follower.Controller.CurrentAction != BehaviorManager.BehaviorActionFollow
                || (Spacing.AcrossGround(follower.Position, PlaceOf(map, follower, leader)) <= PlaceReached + 0.05f
                    && MathF.Abs(Patrols.Turn((float)follower.Rotation, (float)leader.Rotation)) < Patrols.FacingTolerance));

        /// <summary>A leader this far from where the follower's place was last put on the navmesh has the place put on it again; nearer, the place is moved with it.</summary>
        public const float ReplaceDistance = 1f;

        /// <summary>A leader turned this far from where the follower's place was last put has it put again. A twentieth of a turn.</summary>
        public const float ReplaceTurn = MathF.PI / 10f;

        /// <summary>While the leader walks, a follower's place is put on the navmesh again at least this often.</summary>
        public const long ReplaceMs = 500;

        /// <summary>A follower further above or below its place than this does not step straight onto it: it would float up or down a bank. Up a step, not a ledge.</summary>
        public const float DirectClimb = 2f;

        /// <summary>
        /// Whether the follower may step straight onto its place: level enough with it
        /// (<see cref="DirectClimb"/>), and a straight walk there over the navmesh meets no wall,
        /// fence or drop. The walk is looked at once each time the place is put on the navmesh
        /// (<see cref="PlaceFor"/>), not each think. With no navmesh, any ground is walkable.
        /// </summary>
        public static bool StraightTo(MapChannel map, Creature follower, Vector3 place)
        {
            if (MathF.Abs(follower.Position.Y - place.Y) > DirectClimb)
                return false;

            var follow = follower.Controller.ActionFollow;

            if (map?.NavMesh == null)
                return true;

            follow.StraightClear ??= map.NavMesh.IsWalkClear(follower.Position, place);

            return follow.StraightClear.Value;
        }

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
                .Where(creature => creature.Patrol == null && creature.Controller.CurrentAction != BehaviorManager.BehaviorActionScriptedMove)
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
        /// The follower drops its fight as it is - held at its tether's end too long, or the
        /// target out of its reach - and goes back to its place. Unlike a leash it is not made
        /// whole. A target beyond its tether it goes for again once it is within <see cref="RejoinFightDistance"/>
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
        /// <paramref name="abreast"/>: how far to each side (<see cref="AbreastOf"/>); the first rank
        /// is half that behind, each next one that further back.
        /// </summary>
        public static Vector3 Offset(int slot, float yaw, bool mirrored = false, float abreast = Abreast)
        {
            var right = (slot % 2 == 0) != mirrored ? -abreast : abreast;
            var back = abreast / 2 + abreast * (slot / 2);

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
        public static Vector3 PlaceOf(MapChannel map, Creature follower, Creature leader) =>
            PlaceOf(map, follower, leader, out _);

        /// <summary>
        /// <see cref="PlaceOf"/> for the follower's think, without a navmesh query each think:
        /// the place last put on the navmesh moves with the leader, and is put on it again only
        /// once the leader is <see cref="ReplaceDistance"/> from where it was then, or turned
        /// <see cref="ReplaceTurn"/>, or walking <see cref="ReplaceMs"/> later - or the follower
        /// has a new place (PlaceOffset cleared). <paramref name="placed"/>: the place is its own
        /// formation place, or the one across, on the navmesh - not a fallback behind the leader.
        /// </summary>
        public static Vector3 PlaceFor(MapChannel map, Creature follower, Creature leader, long delta, out bool placed)
        {
            var follow = follower.Controller.ActionFollow;
            var walking = leader.Controller.LastMovement?.Velocity > 0f;
            var yaw = (float)leader.Rotation;

            follow.PlacedMs += delta;

            if (follow.PlaceOffset != null && follow.PlacedAt is Vector3 last
                && Spacing.AcrossGround(leader.Position, follow.PlacedLeaderAt) < ReplaceDistance
                && MathF.Abs(Patrols.Turn(follow.PlacedLeaderYaw, yaw)) < ReplaceTurn
                && (!walking || follow.PlacedMs < ReplaceMs))
            {
                placed = follow.PlacedOnMesh;
                return last + (leader.Position - follow.PlacedLeaderAt);
            }

            var place = PlaceOf(map, follower, leader, out placed);

            follow.PlacedAt = place;
            follow.PlacedLeaderAt = leader.Position;
            follow.PlacedLeaderYaw = yaw;
            follow.PlacedOnMesh = placed;
            follow.PlacedMs = 0;
            follow.StraightClear = null;

            return place;
        }

        private static Vector3 PlaceOf(MapChannel map, Creature follower, Creature leader, out bool placed)
        {
            var follow = follower.Controller.ActionFollow;
            var walking = leader.Controller.LastMovement?.Velocity > 0f;
            var yaw = (float)leader.Rotation;
            var slot = Math.Max(0, follow.FormationSlot);

            if (walking || follow.PlaceOffset == null)
                follow.PlaceOffset = Offset(slot, yaw, SideOf(leader), AbreastOf(leader.SpawnPool));

            placed = true;

            if (Spacing.Placed(map, leader.Position + follow.PlaceOffset.Value) is Vector3 place)
                return place;

            if (Spacing.Placed(map, leader.Position + Offset(slot, yaw, !leader.SpawnPool.FormationMirrored, AbreastOf(leader.SpawnPool))) is Vector3 across)
                return across;

            placed = false;

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
                        || other.Controller.CurrentAction == BehaviorManager.BehaviorActionScriptedMove
                        || other.Controller.ActionFollow.DroppedTargetId == targetEntityId)
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

        /// <summary>The leader a follower in a fight is tethered to: its standing leader, not under Mind Control; null for any other creature.</summary>
        public static Creature TetherOf(MapChannel map, Creature creature) =>
            Follows(creature) ? LeaderOf(map, creature.SpawnPool) : null;

        /// <summary>
        /// Whether the follower is at the end of its tether with its target beyond it: it can
        /// get no nearer to it. Asked once it has found no attack it can make from where it is.
        /// </summary>
        public static bool HoldsAtTether(Creature follower, Creature leader, Vector3 targetPosition) =>
            Spacing.AcrossGround(targetPosition, leader.Position) > TetherDistance
            && Spacing.AcrossGround(follower.Position, leader.Position) >= TetherDistance - TetherSlack;

        /// <summary>The point, or where the tether lets the follower get to on the way there from its leader: no further than <see cref="TetherDistance"/> from it.</summary>
        public static Vector3 Tether(MapChannel map, Creature leader, Vector3 point)
        {
            var across = new Vector3(point.X - leader.Position.X, 0, point.Z - leader.Position.Z);
            var length = across.Length();

            if (length <= TetherDistance)
                return point;

            return NavMeshManager.SnapToGround(map, leader.Position + across * (TetherDistance / length));
        }

        /// <summary>Whether the creature is a follower holding at its tether's end in a fight: it takes no damage, nor any debuff (DamageImmunity, GameEffectManager).</summary>
        public static bool Evades(Creature creature) =>
            creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && creature.Controller.ActionFighting.TetherHeldMs > 0;

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
