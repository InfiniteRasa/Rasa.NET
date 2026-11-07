using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using N = CrucibleOutpostIntrepidNpcsV1;
    using G = CrucibleGrupaChiefsNpcsV1;
    using W = CrucibleWardensAndLashersNpcsV1;
    using S = CrucibleMinefieldAndSalvageNpcsV1;
    using V = CrucibleStaalSigalsNpcsV1;
    using A = CrucibleAwolCampNpcsV1;
    using K = CrucibleKardashAndPrometheusNpcsV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// The Marshes playtest lessons, held for the Crucible from its first set (NOTES-marshes-to-fix rows 1, 4, 14, 15, 17, 21,
    /// 24, 27, 32, 33 and #205):
    ///  - every friendly Crucible NPC (pools 770000-770999, the upstream ones sets 1, 2 and 6 moved, and the upstream ones
    ///    sets 2 and 6 talk to where they stand: Vargo, Melching, Breiburg) stands still, outside every
    ///    hostile automatic pool's circle by a creature's reach, with no floor over its head, and the new ones have their
    ///    own hit points;
    ///  - every one of them and every scene object stands on a client collision floor and is seen from a client floor next
    ///    to it (the client's files, as MarshesClientFloorTests; inconclusive without them);
    ///  - every talk and use target stands inside its objective's marker and clear of the map links;
    ///  - the migration's Up and Down are plain SQL, safe to run twice, and the guard's keys are the tables' keys.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleGuardTests
    {
        private const uint Map = CrucibleMissionDataV1.MapContextId;

        /// <summary>
        /// The friendly NPC pools of the Crucible missions on the Crucible: the block's, the upstream ones the sets moved, and the
        /// ones set 2 kept. Set 7's Cirkin stands in the Staal Junkyard (CrucibleStaalJunkyardTests holds him to these rules there),
        /// set 8a's Botha on Ashen Desert (CrucibleKardashAndPrometheusTests).
        /// </summary>
        private static List<SpawnPool> Npcs()
        {
            var moved = N.Moves.Select(move => move.Pool).Concat(G.Moves.Select(move => move.Pool)).Concat(G.Kept.Select(kept => kept.Pool))
                .Concat(A.Moves.Select(move => move.Pool)).Concat(A.Kept.Select(kept => kept.Pool)).Concat(K.Moves.Select(move => move.Pool)).ToHashSet();
            bool Friendly(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory != TargetCategory.Hostile;
            var npcs = SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == Map &&
                (pool.DbId is >= 770000 and <= 770999 || moved.Contains(pool.DbId)) &&
                pool.SpawnSlot.Count > 0 && pool.SpawnSlot.TrueForAll(slot => Friendly(slot.CreatureId))).ToList();
            Assert.HasCount(N.New.Length + N.Moves.Length + G.New.Length + G.Moves.Length + G.Kept.Length + W.New.Length + V.New.Length +
                A.Moves.Length + A.Kept.Length + K.Moves.Length + K.New.Count(npc => npc.Map == Map), npcs,
                "the Crucible NPCs are read");
            return npcs;
        }

        /// <summary>
        /// To-fix rows 1, 4, 15 (Marshes): a quest giver inside a hostile pool's reach died and blocked its chain; one that can
        /// walk wandered into one. Every Crucible NPC stands ReachClearance (an 18 m AggroRange and 2 m) outside every hostile
        /// automatic pool's circle on its map, never moves, and the start-up check names none of them.
        /// </summary>
        [TestMethod]
        public void TheCrucibleNpcsStandStillAndClearOfEveryHostilePool()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools.Values.ToList();
            bool Hostile(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory == TargetCategory.Hostile;
            var npcs = Npcs();
            foreach (var npc in npcs)
            {
                foreach (var hostile in pools.Where(pool => pool.MapContextId == npc.MapContextId && pool.Mode == 0 &&
                             pool.SpawnSlot.Exists(slot => slot.CountMax > 0) && pool.SpawnSlot.TrueForAll(slot => Hostile(slot.CreatureId))))
                {
                    var gap = Vector2.Distance(new(npc.Position.X, npc.Position.Z), new(hostile.Position.X, hostile.Position.Z)) - hostile.Radius;
                    Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, gap, $"NPC pool {npc.DbId} and hostile pool {hostile.DbId}");
                }
                foreach (var slot in npc.SpawnSlot)
                    Assert.IsTrue(BehaviorManager.NeverMoves(CreatureManager.Instance.LoadedCreatures[slot.CreatureId]), $"{slot.CreatureId} stands still");
            }
            // A scene hostile (Roscoe, 1146) seeks friendly creatures in its aggro range as a pool's hostile does: it comes out
            // ReachClearance from every Crucible NPC, or it can attack Vargo (100 hit points) when the player steps back.
            foreach (var (creature, at) in new[] { (G.Roscoe, G.RoscoeAt) })
            {
                Assert.IsTrue(Hostile(creature), $"{creature} is hostile");
                foreach (var npc in npcs.Where(npc => npc.MapContextId == CrucibleMissionDataV1.MapContextId))
                {
                    var gap = Vector2.Distance(new(npc.Position.X, npc.Position.Z), new((float)at.X, (float)at.Z));
                    Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, gap, $"NPC pool {npc.DbId} and scene hostile {creature}");
                }
            }
            var found = SpawnPoolManager.Instance.PoolsTooClose(new List<(uint, Vector3, string)>()).Select(entry => entry.Message).ToList();
            Assert.IsFalse(found.Any(message => message.Contains("of pool 770") || npcs.Any(npc => message.Contains($"of pool {npc.DbId}"))),
                string.Join("\n", found));
        }

        /// <summary>To-fix row 17 (Marshes): a quest NPC with no creature_stat row plays with 100 hit points. Each new Crucible NPC has 4500 and armour 300.</summary>
        [TestMethod]
        public void EveryNewCrucibleNpcHasItsOwnHitPoints()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var stats = harness.World.CreatureStatEntries.AsNoTracking().Where(row => row.Id >= 770000 && row.Id <= 770999).ToDictionary(row => row.Id);
            var npcs = Npcs().Where(pool => pool.DbId >= 770000).SelectMany(pool => pool.SpawnSlot).Select(slot => slot.CreatureId).Distinct().ToList();
            Assert.HasCount(N.New.Length + G.New.Length + W.New.Length + V.New.Length + K.New.Count(npc => npc.Map == Map), npcs);
            foreach (var npc in npcs)
            {
                Assert.IsTrue(stats.TryGetValue(npc, out var stat), $"{npc} has a creature_stat row");
                Assert.AreEqual(((int)N.HitPoints, (int)N.Armor), (stat.Health, stat.Armor), $"{npc}");
            }
        }

        /// <summary>To-fix row 21 (Marshes): an NPC on a lower navmesh layer under a floor could not be talked to. No Crucible NPC has a walkable floor within 2.5 m over its head.</summary>
        [TestMethod]
        public void NoCrucibleNpcStandsUnderAFloor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            foreach (var npc in Npcs())
            {
                var over = crucible.NavMesh.GroundHeight(npc.Position + new Vector3(0, 2.5f, 0));
                Assert.IsNotNull(over, $"navmesh at {npc.DbId}");
                Assert.IsLessThan(0.5f, over.Value - npc.Position.Y, $"{npc.DbId} at {npc.Position}: no floor over its head ({over})");
            }
        }

        /// <summary>
        /// To-fix rows 24 and 25 (Marshes): a player next to an NPC that stood where the client has no floor fell through the
        /// world. Every Crucible NPC (an NPC: a walkable surface) and scene object (any not vertical one) has a client
        /// collision surface at most MaxOver under it and none from MaxUnder to MaxUnder + 0.3 m over it.
        /// </summary>
        [TestMethod]
        public void EveryCrucibleNpcAndObjectStandsOnAClientFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = Places(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var (floors, ceilings) = F.Surfaces(geometry, places.Select(place => (place.At, place.Npc ? F.Walkable : F.NotVertical)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, _) = places[i];
                if (at.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): ") +
                        (float.IsNegativeInfinity(floors[i]) ? "no floor" : FormattableString.Invariant($"{at.Y - floors[i]:0.00} m over its floor")) +
                        (float.IsPositiveInfinity(ceilings[i]) ? "" : FormattableString.Invariant($", a surface {ceilings[i] - at.Y:0.00} m over it")));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// To-fix rows 27, 32 and 33 (Marshes): a target on a client floor that no player place round it could see ("You cannot
        /// see your target from here"). Every Crucible NPC and scene object is seen, from the eye of a player on a client floor
        /// Ring m round it, at its middle, from at least one of RingPlaces places.
        /// </summary>
        [TestMethod]
        public void EveryCrucibleNpcAndObjectIsSeenFromAClientFloorNextToIt()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = Places(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), F.Ring + 1);
            // To-fix row 34: the server-placed objects with a client model (the crafting stations) block the sight too.
            var objects = ServerObjects(harness, meshes);
            for (var i = 0; i < places.Count; i++)
                foreach (var placed in objects.Where(placed => placed.Box.Distance2D(places[i].At) < F.Ring + 1))
                    near[i].AddRange(placed.Triangles);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, npc) = places[i];
                var aim = at + new Vector3(0, npc ? F.NpcMiddle : F.ObjectMiddle, 0);
                var seen = false;
                for (var k = 0; k < F.RingPlaces && !seen; k++)
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var stand = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    if (F.FloorAt(near[i], stand, at.Y - F.MaxOver, at.Y + F.MaxUnder) is { } y && F.Clear(near[i], stand with { Y = y + F.Eye }, aim))
                        seen = true;
                }
                if (!seen)
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z})"));
            }
            Assert.IsEmpty(failures, "not seen from a client floor " + F.Ring + " m round: " + string.Join("; ", failures));
        }

        /// <summary>
        /// To-fix row 34: Vargo stood inside the Staal Centrum crafting station, and the client answered every use with "You
        /// cannot see your target from here". No Crucible NPC or scene object stands inside a server-placed object's model
        /// (the kraftwerks rows on the Crucible, their class's client mesh) or within 0.5 m of it across.
        /// </summary>
        [TestMethod]
        public void NoCrucibleNpcStandsInsideAServerPlacedObject()
        {
            const float Clearance = 0.5f;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = Places(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var objects = ServerObjects(harness, meshes);
            Assert.IsGreaterThanOrEqualTo(2, objects.Count, "the Outpost Intrepid and Staal Centrum crafting stations");
            var inside = new List<string>();
            foreach (var (label, at, _) in places)
                foreach (var placed in objects.Where(placed => placed.Box.Distance2D(at) < Clearance && at.Y >= placed.Box.Min.Y - 1 && at.Y <= placed.Box.Max.Y))
                    inside.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}) in {placed.Label}"));
            Assert.IsEmpty(inside, string.Join("; ", inside));

            // To-fix row 43: a player who walks up to an NPC stops anywhere within the talk reach of it, also beside a crafting
            // station, and the station hid Vargo from there. So no server-placed object is within TalkReach and 1 m of an NPC:
            // then no line from the talk reach to the NPC crosses one.
            const float TalkReach = 5;
            var near = new List<string>();
            foreach (var (label, at, npc) in places.Where(place => place.Npc))
                foreach (var placed in objects.Where(placed => placed.Box.Distance2D(at) < TalkReach + 1 && MathF.Abs(at.Y - placed.Box.Min.Y) < 3))
                    near.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): {placed.Label} {placed.Box.Distance2D(at):0.0} m off"));
            Assert.IsEmpty(near, string.Join("; ", near));
        }

        /// <summary>
        /// To-fix row 43: Vargo was seen from 8 of 8 places 2 m round him, but not from the end of the real client's walk to
        /// him from Munit (on his far side, 4.2 m off, with the crafting station in the line). Every Crucible NPC on the
        /// Crucible is seen (eye to its middle, the client files and the kraftwerks models) from each point of each navmesh
        /// walk to it that ends a walk: where the walk first comes 5 m to 2 m near it (every 0.5 m). For Vargo, whose stall is
        /// among the Staal Centrum's props, also the navmesh points on that level 1 m and 2 m to each side of it across the walk
        /// (the client's walk does not keep to the server's line: the tester stopped 1.3 m beside it). The walks: from each waypoint, office teleporter and NPC in Starts that has a
        /// complete navmesh path to it; Vargo's from Munit, the lower office teleporter, Cugin and Vitto must be complete.
        /// </summary>
        [TestMethod]
        public void EveryCrucibleNpcIsSeenFromTheEndOfEachWalkToIt()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            var nav = crucible.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var objects = ServerObjects(harness, meshes);
            Vector3 Pool(uint pool) => SpawnPoolManager.Instance.LoadedSpawnPools.Values.Single(entry => entry.DbId == pool).Position;
            Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
            var starts = new (string Label, Vector3 At)[]
            {
                ("the Outpost Intrepid waypoint", CrucibleOutpostIntrepidTests.OutpostWaypoint), ("the Fort Intrepid waypoint", CrucibleOutpostIntrepidTests.FortWaypoint),
                ("the Staal waypoint", CrucibleOutpostIntrepidTests.StaalWaypoint), ("the Infirmary waypoint", CrucibleOutpostIntrepidTests.InfirmaryWaypoint),
                ("the Labbna waypoint", CrucibleOutpostIntrepidTests.LabbnaWaypoint), ("the Awol Camp waypoint", AwolCampWaypoint),
                ("the lower office teleporter", V(G.LowerPadAt)), ("the upper office teleporter", V(G.UpperPadAt)),
                ("Munit", Pool(N.Munit)), ("Cugin", Pool(G.Cugin)), ("Vitto", Pool(N.Vitto)),
                ("the Prometheus Outpost waypoint", V(K.PrometheusWaypoint))
            };
            var mustWalk = new Dictionary<uint, string[]> { [G.Vargo] = new[] { "Munit", "the lower office teleporter", "Cugin", "Vitto" } };
            var npcs = Npcs().Where(npc => npc.MapContextId == Map).ToList();
            var near = F.Near(geometry, npcs.Select(npc => npc.Position).ToList(), 7);
            for (var i = 0; i < npcs.Count; i++)
                near[i].AddRange(objects.Where(placed => placed.Box.Distance2D(npcs[i].Position) < 7).SelectMany(placed => placed.Triangles));
            bool Seen(int i, Vector3 stand)
            {
                var floor = F.FloorAt(near[i], stand, stand.Y - 1.5f, stand.Y + 1) ?? stand.Y;
                return F.Clear(near[i], stand with { Y = floor + F.Eye }, npcs[i].Position + new Vector3(0, F.NpcMiddle, 0));
            }
            var failures = new List<string>();
            var walked = 0;
            for (var i = 0; i < npcs.Count; i++)
            {
                var at = npcs[i].Position;
                foreach (var (label, from) in starts)
                {
                    if (Vector2.Distance(new(from.X, from.Z), new(at.X, at.Z)) < 6)
                        continue;
                    var path = nav.FindPath(nav.Nearest(from) ?? from, nav.Nearest(at) ?? at, out var complete);
                    if (path == null || !complete)
                    {
                        if (mustWalk.TryGetValue(npcs[i].DbId, out var must) && must.Contains(label))
                            failures.Add($"pool {npcs[i].DbId}: no walk from {label}");
                        continue;
                    }
                    walked++;
                    var points = path.Prepend(nav.Nearest(from) ?? from).ToList();
                    foreach (var reach in new[] { 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f })
                    {
                        // The first point of the walk that comes within reach.
                        Vector3? end = null;
                        for (var k = 0; k + 1 < points.Count && end == null; k++)
                            for (var s = 0; s <= 40 && end == null; s++)
                            {
                                var p = Vector3.Lerp(points[k], points[k + 1], s / 40f);
                                if (Vector3.Distance(p, at) <= reach)
                                    end = p;
                            }
                        if (end is not { } stop)
                            continue;
                        var across = Vector3.Normalize(new Vector3(at.Z - stop.Z, 0, stop.X - at.X));
                        var unseen = (npcs[i].DbId == G.Vargo ? new[] { 0f, -1f, 1f, -2f, 2f } : new[] { 0f }).Select(side => stop + across * side)
                            .Select(stand => nav.GroundHeight(stand + new Vector3(0, 1, 0)) is { } y && MathF.Abs(y - stop.Y) <= 0.6f ? stand with { Y = y } : (Vector3?)null)
                            .Where(stand => stand != null && !Seen(i, stand.Value)).FirstOrDefault();
                        if (unseen is { } stand)
                        {
                            failures.Add(FormattableString.Invariant($"pool {npcs[i].DbId} from {label}: not seen at ({stand.X:0.0}, {stand.Y:0.0}, {stand.Z:0.0}), {reach} m off"));
                            break;
                        }
                    }
                }
            }
            Assert.IsGreaterThanOrEqualTo(npcs.Count, walked, "walks to the NPCs");

            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// To-fix row 44: the navmesh walk between two Warden Bot pools passed 5.3 m from the Chaukas Robotics Facility door
        /// (map link 98, r6), and a player who hunted between them went through it; a Warden Bot also chased the player from
        /// its pool to the door. For each Crucible objective with markers on hostile pools (a pool centre within 0.5 m of a
        /// marker centre), every navmesh walk between two of its pools, and from the Awol Camp and Labbna waypoints to each pool
        /// of 1147, 1202, 1203 and 1204, keeps WalkClearance or more outside every enabled map link's circle on the Crucible;
        /// and each pool of those four missions has its circle MaxChaseDistance and WalkClearance outside every link's circle, so a
        /// creature that follows a fight as far from home as it can still stops WalkClearance short of a door.
        /// </summary>
        [TestMethod]
        public void NoWalkBetweenTheBotPoolsOfAnObjectivePassesAMapLink()
        {
            const float WalkClearance = 10;
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            var nav = crucible.NavMesh;
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Map).ToList();
            float LinkGap(Vector3 at) => links.Min(link => Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius);
            bool Hostile(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory == TargetCategory.Hostile;
            var hostiles = SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == Map && pool.SpawnSlot.Count > 0 &&
                pool.SpawnSlot.TrueForAll(slot => Hostile(slot.CreatureId))).ToList();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList();
            var groups = markers.GroupBy(marker => (marker.MissionId, marker.ObjectiveId)).Select(group => (group.Key, Pools: hostiles.Where(pool =>
                group.Any(marker => Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(pool.Position.X, pool.Position.Z)) < 0.5f)).ToList()))
                .Where(group => group.Pools.Count > 0).ToList();
            var chaukas = new uint[] { CrucibleGameWardensV1.MissionId, CrucibleArchitectsOfDestructionV1.MissionId, CrucibleSalvageDutyV1.MissionId,
                CrucibleSalvageDutyV1.MoreMissionId };
            CollectionAssert.IsSubsetOf(chaukas, groups.Select(group => group.Key.MissionId).Distinct().ToArray(), "the four missions' markers are on their pools");
            Assert.AreEqual(3 * 3 + 2 * 5 + 3 * 5 + 4, groups.Where(group => chaukas.Contains(group.Key.MissionId)).Sum(group => group.Pools.Count),
                "1147's three objectives on the three Warden Bot pools, 1203's two and 1204's three on the five bot pools, 1202's on the four Reconstructor pools");

            var failures = new List<string>();
            void Walk(string label, Vector3 from, Vector3 to)
            {
                var path = nav.FindPath(nav.Nearest(from) ?? from, nav.Nearest(to) ?? to, out var complete);
                if (path == null || !complete)
                {
                    failures.Add($"{label}: no walk");
                    return;
                }
                var points = path.Prepend(nav.Nearest(from) ?? from).ToList();
                for (var k = 0; k + 1 < points.Count; k++)
                    for (var s = 0; s <= 40; s++)
                    {
                        var at = Vector3.Lerp(points[k], points[k + 1], s / 40f);
                        if (LinkGap(at) < WalkClearance)
                        {
                            failures.Add(FormattableString.Invariant($"{label} passes ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0}), {LinkGap(at):0.0} m outside a link's circle"));
                            return;
                        }
                    }
            }
            var walks = 0;
            foreach (var ((mission, objective), pools) in groups)
                for (var a = 0; a < pools.Count; a++)
                    for (var b = a + 1; b < pools.Count; b++, walks++)
                        Walk($"{mission}.{objective} pool {pools[a].DbId} to {pools[b].DbId}", pools[a].Position, pools[b].Position);
            foreach (var pool in groups.Where(group => chaukas.Contains(group.Key.MissionId)).SelectMany(group => group.Pools).Distinct())
            {
                Walk($"the Awol Camp waypoint to pool {pool.DbId}", AwolCampWaypoint, pool.Position);
                Walk($"the Labbna waypoint to pool {pool.DbId}", CrucibleOutpostIntrepidTests.LabbnaWaypoint, pool.Position);
                walks += 2;
                if (LinkGap(pool.Position) - pool.Radius < BehaviorManager.MaxChaseDistance + WalkClearance)
                    failures.Add(FormattableString.Invariant($"pool {pool.DbId}: its circle {LinkGap(pool.Position) - pool.Radius:0.0} m outside a link's circle"));
            }
            Assert.IsGreaterThanOrEqualTo(3 * 3 + 2 * 10 + 3 * 10 + 6 + 14, walks, "the walks between the pools and from the waypoints");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// To-fix row 44 (fix job F): the whole ground a bot of the Chaukas pools can reach keeps RoamClearance outside the
        /// Chaukas Robotics Facility door (map link 98) and every other link's circle, read off the spawned creatures as the
        /// server moves them: its strolls stay in its wander zone (BehaviorManager.WanderZoneOf: the pool's circle, or
        /// WanderZoneFallbackRadius round its home for a point pool); a fight it follows ends when it is more than
        /// MaxChaseDistance from its home (ChasedTooFar, checked on each think), so it can be one think's run (its run speed for
        /// CreatureThinkInterval, 250 ms) past that before it turns back (Leash). The pools: the Warden Bot pools and every
        /// Reconstructor pool of 1147, 1202, 1203 and 1204. Their homes are read off the spawned bots and, as a spawn draws
        /// them, off many draws (a radius pool's reach past its circle: fix job F made four of them points).
        /// </summary>
        [TestMethod]
        public void TheChaukasBotsRoamAndChaseNoNearerThanTenMetresToADoor()
        {
            const float RoamClearance = 10, ThinkSeconds = 0.25f; // BehaviorManager.CreatureThinkInterval
            const uint ChaukasDoor = 98;
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Map).ToList();
            Assert.IsTrue(links.Any(link => link.Id == ChaukasDoor), "the Chaukas Robotics Facility door is a link on the Crucible");
            float LinkGap(Vector3 at) => links.Min(link => Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius);
            var pools = CrucibleSalvageDutyV1.BotPools.Select(pool => pool.Pool).Concat(CrucibleArchitectsOfDestructionV1.Pools.Select(pool => pool.Pool))
                .Concat(W.WardenPools.Select(pool => pool.Pool)).Distinct().ToArray();
            Assert.HasCount(3 + 2 + 2, pools, "three Warden Bot pools, the two Reconstructor pools of set 4 and the two of set 5");
            MarshesPaludosTests.Respawn(crucible, pools);

            var failures = new List<string>();
            var bots = 0;
            foreach (var pool in pools)
            {
                var here = MarshesPaludosTests.Alive(crucible).Where(creature => creature.SpawnPool?.DbId == pool).Distinct().ToList();
                Assert.IsNotEmpty(here, $"pool {pool} spawned");
                foreach (var bot in here)
                {
                    bots++;
                    var (centre, radius) = BehaviorManager.WanderZoneOf(bot);
                    var chase = BehaviorManager.MaxChaseDistance + bot.RunSpeed * ThinkSeconds;
                    foreach (var (what, at, reach) in new[] { ("its wander zone", centre, radius), ("its chase", bot.HomePos.Position, chase) })
                        if (LinkGap(at) - reach < RoamClearance)
                            failures.Add(FormattableString.Invariant(
                                $"pool {pool} bot {bot.EntityId}: {what} ({at.X:0.0}, {at.Z:0.0}) r{reach:0.0} comes {LinkGap(at) - reach:0.0} m outside a link's circle"));
                }
            }
            Assert.IsGreaterThanOrEqualTo(3 * 3 + 4 * 3, bots, "the bots of the seven pools");

            // A pool's creatures are set down at a navmesh random point "around" its point (SpawnPoolManager.SpawnPoint,
            // NavMeshQuery.RandomPointAround: Detour's findRandomPointAroundCircle, a point anywhere in a polygon the circle
            // touches, so it can lie well outside the circle). Every home it can draw, as far as Draws draws find, chases
            // no nearer than RoamClearance to a link's circle.
            // A point pool (radius 0, fix job F) sets them down on rings round its point (Spacing.OnRings: the second ring a little
            // over two spacings out), its wander zone WanderZoneFallbackRadius round each home.
            const int Draws = 2000;
            const float Rings = 2 * Spacing.Distance * 1.1f;
            foreach (var pool in pools)
            {
                var entry = crucible.SpawnPools.Single(candidate => candidate.DbId == pool);
                var run = MarshesPaludosTests.Alive(crucible).First(creature => creature.SpawnPool?.DbId == pool).RunSpeed;
                var reach = BehaviorManager.MaxChaseDistance + run * ThinkSeconds;
                if (entry.Radius <= 0)
                {
                    var far = Rings + Math.Max(reach, BehaviorManager.WanderZoneFallbackRadius);
                    if (LinkGap(entry.Position) - far < RoamClearance)
                        failures.Add(FormattableString.Invariant($"point pool {pool}: its rings' chase comes {LinkGap(entry.Position) - far:0.0} m outside a link's circle"));
                    continue;
                }
                var nearest = Enumerable.Range(0, Draws).Select(_ => crucible.NavMesh.RandomPointAround(entry.Position, entry.Radius))
                    .Where(home => home.HasValue).Select(home => home.Value).MinBy(LinkGap);
                var gap = LinkGap(nearest) - reach;
                if (gap < RoamClearance)
                    failures.Add(FormattableString.Invariant(
                        $"pool {pool}: a home at ({nearest.X:0.0}, {nearest.Z:0.0}) chases to {gap:0.0} m outside a link's circle"));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// To-fix row 60 (fix job F): both Lasher pools of 1157 stood on a lava lake and the real client burned there (192 Fire
        /// hits in 12 min). The server knows the client's lava (LavaSurfaces, the lakes, and LavaFlows, the rivers and falls,
        /// both from the client's .map files and meshes; LavaDamage.InLava burns a player there). No Crucible pool (770000-770999,
        /// and the upstream ones the sets moved or talk to) has lava where its creatures can stand: on the navmesh within its
        /// circle, at any of Draws spawn draws (SpawnPoolManager.SpawnPoint's RandomPointAround, which reaches past the circle),
        /// or, for a point pool, within its rings (Spacing.OnRings, two spacings and a little out); no friendly NPC stands in it,
        /// and no Crucible marker's centre is on it.
        /// </summary>
        [TestMethod]
        public void NoCruciblePoolNpcOrMarkerStandsOnLava()
        {
            const int Draws = 500;
            const float Rings = 2 * Spacing.Distance * 1.1f, Step = 1.5f;
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            var nav = crucible.NavMesh;
            var name = crucible.MapInfo.MapName;
            Assert.IsTrue(LavaDamage.InLava(name, new Vector3(-785, 284.1f, 390)), "the lake the Lashers stood on is lava to the server");

            var npcs = Npcs().Select(pool => pool.DbId).ToHashSet();
            var moved = CrucibleMarkersClearOfLinksV2.Pools.Select(pool => pool.Pool).Append(S.DergePool).ToHashSet();
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == Map &&
                (pool.DbId is >= 770000 and <= 770999 || npcs.Contains(pool.DbId) || moved.Contains(pool.DbId))).ToList();
            Assert.IsGreaterThanOrEqualTo(npcs.Count + 20, pools.Count, "the NPCs and the hostile pools");
            var failures = new List<string>();
            foreach (var pool in pools)
            {
                var at = pool.Position;
                var spots = new List<Vector3> { nav.Nearest(at) ?? at };
                if (npcs.Contains(pool.DbId))
                    spots.Add(at);
                else
                {
                    var reach = pool.Radius > 0 ? pool.Radius : Rings;
                    for (var dx = -reach; dx <= reach; dx += Step)
                        for (var dz = -reach; dz <= reach; dz += Step)
                            if (dx * dx + dz * dz <= reach * reach && nav.Nearest(at + new Vector3(dx, 0, dz)) is { } ground
                                && Vector2.Distance(new(ground.X, ground.Z), new(at.X + dx, at.Z + dz)) < 1)
                                spots.Add(ground);
                    if (pool.Radius > 0)
                        spots.AddRange(Enumerable.Range(0, Draws).Select(_ => nav.RandomPointAround(at, pool.Radius)).Where(spot => spot.HasValue).Select(spot => spot.Value));
                }
                if (spots.FirstOrDefault(spot => LavaDamage.InLava(name, spot)) is var lava && LavaDamage.InLava(name, lava))
                    failures.Add(FormattableString.Invariant($"pool {pool.DbId} (r{pool.Radius}): lava at ({lava.X:0.0}, {lava.Y:0.0}, {lava.Z:0.0})"));
            }
            foreach (var marker in harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList())
            {
                var at = new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ);
                if (LavaDamage.InLava(name, nav.Nearest(at) ?? at) || LavaDamage.InLava(name, at))
                    failures.Add(FormattableString.Invariant($"marker {marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId} at ({at.X}, {at.Y}, {at.Z}) on lava"));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>The Awol Camp waypoint (as CrucibleWardensAndLashersTests.AwolWaypoint).</summary>
        internal static readonly Vector3 AwolCampWaypoint = new(-945.54f, 309.45f, 244.47f);

        /// <summary>
        /// To-fix rows 32 and 35: the real client fell through two holes in the y 196.7 floor of Staal (the panopticon room; our
        /// client floor reader finds a floor there, NOTES-upstream-bugs N10), so no check of the client files finds them: they
        /// are named (CrucibleStaalWalksV1.Holes; to-fix row 50 adds the fall by the east stairs, CrucibleFixFV1.Holes). No
        /// Crucible NPC, scene object or marker on that level reaches into one (its circle and 1 m), and the navmesh walks to
        /// Munit (from the lower office teleporter, Vitto and Donal), to Vargo (from the teleporter and Cugin), to Yeg from Cugin,
        /// to Nema (from the teleporter and Donal), to Pico (from the teleporter and Munit), to Deirdre (from the teleporter and
        /// the Staal waypoint) and between the teleporter and Donal (row 50) keep out of rows 32 and 35's holes; the four that
        /// the navmesh takes up the east stairs past row 50's fall (upstream N10, no NPC move helps) are pinned by name.
        /// </summary>
        [TestMethod]
        public void NoCrucibleNpcMarkerOrWalkLeadsOverAStaalFloorHole()
        {
            const float Level = 2, Margin = 1;
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            Assert.HasCount(3, CrucibleFixFV1.Holes, "rows 32 and 35's two holes and row 50's fall by the east stairs");
            var holes = CrucibleFixFV1.Holes.Select(hole => (At: new Vector3((float)hole.At.X, (float)hole.At.Y, (float)hole.At.Z), R: (float)hole.Radius)).ToList();
            // Into: how far a place (with its radius) reaches into the holes' circles on its level, but the one left out.
            float? Into(Vector3 at, float radius, int except = -1) => holes.Where((hole, index) => index != except && MathF.Abs(at.Y - hole.At.Y) < Level)
                .Select(hole => (float?)(hole.R + radius + Margin - Vector2.Distance(new(at.X, at.Z), new(hole.At.X, hole.At.Z)))).Where(gap => gap > 0).Max();
            bool OnStairs(Vector3 at) => Into(at, 0) is not null && Into(at, 0, holes.Count - 1) is null; // in row 50's circle only
            var stairs = new List<string>();
            var failures = new List<string>();
            foreach (var (label, at, _) in Places(harness))
                if (Into(at, 0) is { } into)
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): {into:0.0} m into a hole's circle"));
            foreach (var marker in harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList())
                if (Into(new((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ), (float)marker.Radius) is { } into)
                    failures.Add(FormattableString.Invariant($"marker {marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}: {into:0.0} m into a hole's circle"));

            Vector3 Pool(uint pool) => SpawnPoolManager.Instance.LoadedSpawnPools.Values.Single(entry => entry.DbId == pool).Position;
            var pad = new Vector3((float)G.LowerPadAt.X, (float)G.LowerPadAt.Y, (float)G.LowerPadAt.Z);
            foreach (var (from, to, label) in new[]
            {
                (pad, Pool(N.Munit), "the lower teleporter to Munit"), (Pool(N.Vitto), Pool(N.Munit), "Vitto to Munit"),
                (Pool(N.Donal), Pool(N.Munit), "Donal to Munit"), (pad, Pool(G.Vargo), "the lower teleporter to Vargo"),
                (Pool(G.Cugin), Pool(G.Vargo), "Cugin to Vargo"), (Pool(G.Cugin), Pool(G.Yeg), "Cugin to Yeg"),
                (pad, Pool(V.Nema), "the lower teleporter to Nema"), (Pool(N.Donal), Pool(V.Nema), "Donal to Nema"),
                (pad, Pool(V.Pico), "the lower teleporter to Pico"), (Pool(N.Munit), Pool(V.Pico), "Munit to Pico"),
                (pad, Pool(A.Deirdre), "the lower teleporter to Deirdre"),
                (CrucibleOutpostIntrepidTests.StaalWaypoint, Pool(A.Deirdre), "the Staal waypoint to Deirdre"),
                (pad, Pool(N.Donal), "the lower teleporter to Donal"), (Pool(N.Donal), pad, "Donal to the lower teleporter")
            })
            {
                var path = crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(from) ?? from, crucible.NavMesh.Nearest(to) ?? to, out var complete);
                Assert.IsTrue(path != null && complete, $"{label}: a path");
                var points = Enumerable.Range(0, path.Count - 1).SelectMany(i => Enumerable.Range(0, 21).Select(k => Vector3.Lerp(path[i], path[i + 1], k / 20f))).ToList();
                foreach (var at in points)
                    if (Into(at, 0, holes.Count - 1) is not null)
                    {
                        failures.Add(FormattableString.Invariant($"{label} walks over a hole at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})"));
                        break;
                    }
                if (points.Any(OnStairs))
                    stairs.Add(label);
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));

            // Row 50 (upstream N10, not fixed): the server navmesh takes these four walks up the east stairs, past the fall at
            // (-178.8, 194.7, 355.4), as the tester's walk that fell did. No NPC or marker can move them off it (the walks start
            // and end at the lower office teleporter, Donal, Munit and Nema; the stairs are the mesh's way); a navmesh fix or a
            // safety floor can. Pinned, so that a fix of the mesh, or another walk going that way, shows here.
            CollectionAssert.AreEquivalent(new[]
                {
                    "Donal to Munit", "the lower teleporter to Nema", "the lower teleporter to Donal", "Donal to the lower teleporter"
                }, stairs, string.Join(", ", stairs));
        }

        /// <summary>
        /// Every talk target (an NPC pool of the Crucible with the talk's package, or a talking scene object) and use target
        /// stands inside a marker of its objective, so the map points the player at it.
        /// </summary>
        [TestMethod]
        public void EveryTalkAndUseTargetStandsInsideItsObjectivesMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = CrucibleMissionDataV1.Revision;
            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList();
            bool Inside(uint mission, uint objective, Vector3 at) => markers.Any(marker => marker.MissionId == mission && marker.ObjectiveId == objective &&
                Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(at.X, at.Z)) <= marker.Radius);
            var packages = world.NpcPackageEntries.AsNoTracking().ToList().ToLookup(entry => entry.PackageId, entry => entry.Id);
            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Map ||
                pool.MapContextId == CrucibleStaalJunkyardNpcsV1.Junkyard).ToList();
            var checkedTargets = 0;
            var outside = new List<string>();
            foreach (var talk in world.MissionTriggerEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision &&
                         entry.Kind == MissionTriggerKind.Conversation).ToList())
            {
                var npcs = pools.Where(pool => packages[talk.NpcPackageId.Value].Contains(pool.Creature1Id)).ToList();
                foreach (var pool in npcs)
                {
                    checkedTargets++;
                    if (!Inside(talk.MissionId, talk.ObjectiveId, new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ)))
                        outside.Add($"{talk.MissionId}.{talk.ObjectiveId} NPC pool {pool.Id}");
                }
            }
            foreach (var (mission, scene) in CrucibleMissionTests.Scenes(harness))
                foreach (var actor in scene.Actors.Values.Where(actor => actor.UseAction != null || actor.Conversation != null))
                {
                    checkedTargets++;
                    var objective = actor.UseAction?.ObjectiveId ?? actor.Conversation.ObjectiveId;
                    if (!Inside(mission, objective, new(actor.Position.X, actor.Position.Y, actor.Position.Z)))
                        outside.Add($"{mission}.{objective} {actor.Role}");
                }
            // Set 2: Lohen; Bunko, Cugin, Vargo (twice: 3 and 6) and Yeg; Fiorella, Haggus and Melching (three times: his three
            // variants are three talks). Set 3: Munit, Vitto, Donal and Rigs (1170) and DeLisi (1229). Set 5: Lohen twice (1185).
            // Set 6: 1144's ten (each choice is a talk), 1152's two, 1153's two (two choices), 1155's and 1205's three each.
            // Set 7 (in the Staal Junkyard): Cirkin (1538), 1529's six Dolomite Blocks and 1538's lockbox. Set 8a: Keplinger (1210),
            // Tiernan (1217, 1218); Botha on Ashen Desert is CrucibleKardashAndPrometheusTests'.
            Assert.AreEqual(1 + 5 + 21 + 1 + 5 + 2 + 3 + 4 + 1 + 2 + 10 + 2 + 2 + 3 + 3 + 1 + 6 + 1 + 3, checkedTargets,
                "DeLisi, the five of The Wire, the terminal, the twenty fallen, and set 2's, set 3's, set 5's, set 6's, set 7's and set 8a's talks and uses");
            Assert.IsEmpty(outside, string.Join(", ", outside));
        }

        /// <summary>
        /// To-fix row 14 (Marshes): a player who walks up to a target in a map link's circle is taken to the other map. No
        /// Crucible NPC or scene object with a use or a talk stands within an enabled map link's radius and 5 m on its map.
        /// </summary>
        [TestMethod]
        public void NoTargetStandsByAMapLink()
        {
            const float LinkClearance = 5;
            using var harness = WildernessRuntimeTestHarness.Create();
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Map).ToList();
            Assert.IsNotEmpty(links, "the Crucible's map links");
            var places = Places(harness);
            var near = new List<string>();
            foreach (var (label, at, _) in places)
                foreach (var link in links)
                {
                    var gap = Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius;
                    if (gap < LinkClearance)
                        near.Add($"{label} {gap:F1} m outside link {link.Id}");
                }
            Assert.IsEmpty(near, string.Join(", ", near));
        }

        /// <summary>
        /// To-fix rows 23 and 25: a player or bot that hunts inside a marker that holds a map link walks into the link (1149's
        /// marker on the Staal Junkyard door moved the bot into instance 2138 nine times). Every Crucible marker of every set has
        /// a map (MissionMarkerProjection.ResolveMap), and its circle stays MarkerClearance or more outside every enabled map
        /// link's circle on that map.
        /// </summary>
        [TestMethod]
        public void NoCrucibleMarkerCircleTouchesAMapLinkCircle()
        {
            const float MarkerClearance = 5;
            using var harness = WildernessRuntimeTestHarness.Create();
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0).ToList();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList();
            Assert.HasCount(CrucibleMissionTests.Indicators, markers, "the Crucible markers");
            var near = new List<string>();
            foreach (var marker in markers)
            {
                var map = MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId });
                Assert.AreNotEqual(0U, map, $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId} has a map");
                foreach (var link in links.Where(link => link.MapContextId == map))
                {
                    var gap = Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new((float)link.PosX, (float)link.PosZ)) -
                              (float)marker.Radius - (float)link.Radius;
                    if (gap < MarkerClearance)
                        near.Add(FormattableString.Invariant($"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId} {gap:F1} m outside link {link.Id}"));
                }
            }
            Assert.IsEmpty(near, string.Join(", ", near));
        }

        /// <summary>
        /// To-fix rows 23 and 25: Junkyard Dogs (1149) counts a Flaregasher killed anywhere, so each Crucible Flaregasher pool
        /// (World DB, upstream's and the ones 20271108000300 moved) is a hunting place: its circle stands ReachClearance or more
        /// outside every enabled map link's circle on the Crucible, and none stands where a player arrives.
        /// </summary>
        [TestMethod]
        public void TheFlareGasherPoolsStandClearOfTheMapLinks()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Map).ToList();
            var gasher = G.Flaregasher;
            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Map && (pool.Creature1Id == gasher ||
                pool.Creature2Id == gasher || pool.Creature3Id == gasher || pool.Creature4Id == gasher || pool.Creature5Id == gasher || pool.Creature6Id == gasher)).ToList();
            Assert.HasCount(13, pools, "the Crucible's Flaregasher pools");
            var near = new List<string>();
            foreach (var pool in pools)
                foreach (var link in links)
                {
                    var gap = Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius - (float)pool.Radius;
                    if (gap < MarshesNpcsStandStillV1.ReachClearance)
                        near.Add(FormattableString.Invariant($"pool {pool.Id} {gap:F1} m outside link {link.Id}"));
                }
            Assert.IsEmpty(near, string.Join(", ", near));
        }

        /// <summary>
        /// To-fix row 24: 1149's markers stood off the navmesh. Every Crucible marker on the Crucible has its centre on the
        /// navmesh (ground at most MarkerHeight from its y) and on a client floor (a walkable surface at most MarkerHeight
        /// under it). The Staal Junkyard's (MissionMarkerProjection.ResolveMap 2138) are CrucibleStaalJunkyardTests', the Ashen
        /// Desert one (1734) CrucibleKardashAndPrometheusTests'.
        /// </summary>
        [TestMethod]
        public void EveryCrucibleMarkerStandsOnTheNavmeshAndAClientFloor()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = CrucibleOutpostIntrepidTests.EnterCrucible(harness);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList()
                .Where(marker => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId }) == Map)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(CrucibleMissionTests.Indicators - CrucibleStaalJunkyardTests.JunkyardMarkers - CrucibleKardashAndPrometheusTests.AshenDesertMarkers, markers,
                "the Crucible markers on the Crucible (CrucibleMissionTests)");
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var (floors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < markers.Count; i++)
            {
                var (label, at) = markers[i];
                var ground = crucible.NavMesh.GroundHeight(at + new Vector3(0, MarkerHeight, 0));
                if (ground is not { } y || MathF.Abs(y - at.Y) > MarkerHeight)
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): navmesh {ground}"));
                if (at.Y - floors[i] > MarkerHeight)
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): client floor {floors[i]}"));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Set 3's, set 4's and set 5's new hostile pools (the Warden Bots by the Chaukas Robotics Facility's door, the Lashers
        /// north-east of the Awol Camp, the Smart Mines west of the Fort Intrepid ruins, the Reconstructor Bots by the facility,
        /// the Shield Drones north of Khulago Base, Preceptor Molitor's pack north-east of the Awol Camp) and
        /// Executioner Derge on the Fort Intrepid rampart, and the four Flaregasher pools the marker fix moved: each centre on a client floor (a walkable surface at most MaxOver under it, none just over it), and its circle
        /// ReachClearance or more outside every enabled map link's circle, so that a player who comes out of the facility is
        /// not set on at once.
        /// </summary>
        [TestMethod]
        public void TheNewHostilePoolsStandOnAClientFloorAndClearOfTheMapLinks()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = W.WardenPools.Concat(W.LasherPools).Concat(S.MinePools).Concat(S.ReconstructorPools).Append((Pool: S.DergePool, At: S.DergeAt)).Concat(CrucibleMarkersClearOfLinksV2.Pools.Select(pool => (pool.Pool, At: pool.Now)))
                .Concat(V.DronePools).Concat(V.ReconstructorPools).Append((Pool: V.MolitorPool, At: V.MolitorAt))
                .Append((Pool: CrucibleStaalJunkyardNpcsV1.KalamitPool, At: CrucibleStaalJunkyardNpcsV1.KalamitAt))
                .Select(pool => (Label: $"pool {pool.Pool}", Id: pool.Pool, At: new Vector3((float)pool.At.X, (float)pool.At.Y, (float)pool.At.Z))).ToList();
            Assert.HasCount(3 + 2 + 3 + 2 + 1 + 4 + 3 + 2 + 1 + 1, pools,
                "set 3's, set 4's, set 5's and set 7's pools on the Crucible, Derge and the four Flaregasher pools 20271108000300 moved");
            // Where each stands now: a later fix (20271108000800) moved some of them.
            var rows = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Map).ToDictionary(pool => pool.Id);
            pools = pools.Select(pool => (pool.Label, pool.Id, At: new Vector3((float)rows[pool.Id].PosX, (float)rows[pool.Id].PosY, (float)rows[pool.Id].PosZ))).ToList();
            var radii = rows.ToDictionary(row => row.Key, row => (float)row.Value.Radius);
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Map).ToList();
            var near = new List<string>();
            foreach (var (label, id, at) in pools)
                foreach (var link in links)
                {
                    var gap = Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius - MathF.Max(W.PoolRadius, radii[id]);
                    if (gap < MarshesNpcsStandStillV1.ReachClearance)
                        near.Add($"{label} {gap:F1} m outside link {link.Id}");
                }
            Assert.IsEmpty(near, string.Join(", ", near));

            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var (floors, ceilings) = F.Surfaces(geometry, pools.Select(pool => (pool.At, F.Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < pools.Count; i++)
                if (pools[i].At.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add($"{pools[i].Label} at {pools[i].At}");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>CrucibleSql.Keys is each table's primary key in WorldContext's model; the keyless creature_appearance has its index's columns.</summary>
        [TestMethod]
        public void TheGuardKeysAreTheTablesKeys()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var model = harness.World.Model;
            foreach (var (table, keys) in CrucibleSql.Keys)
            {
                var entity = model.GetEntityTypes().SingleOrDefault(type => type.GetTableName() == table);
                Assert.IsNotNull(entity, $"{table} is a table of the model");
                var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
                var key = entity.FindPrimaryKey();
                if (key != null)
                    CollectionAssert.AreEqual(key.Properties.Select(property => property.GetColumnName(store)).ToArray(), keys, $"{table}: the primary key");
                else
                    Assert.IsTrue(entity.GetIndexes().Any(index => index.Properties.Select(property => property.GetColumnName(store)).SequenceEqual(keys)),
                        $"{table} has no primary key: an index on {string.Join(", ", keys)}");
            }
            Assert.AreEqual(MarshesSql.Guard("INSERT INTO creature_stat (id, body, mind, spirit, health, armor) VALUES (770000, 15, 15, 15, 4500, 300);"),
                CrucibleSql.Guard("INSERT INTO creature_stat (id, body, mind, spirit, health, armor) VALUES (770000, 15, 15, 15, 4500, 300);"),
                "the same guard as the Marshes'");
        }

        /// <summary>
        /// #205: each Crucible World migration, in order: Up, Up again, and the tables it writes are the same; Down (to the
        /// migration before it, IMigrationsAssembly) and they are as before the Up; Down again and they still are; Up, and
        /// they are as after the first Up. Its operations are all plain SQL.
        /// </summary>
        [TestMethod]
        public void EachCrucibleMigrationRunsUpTwiceAndDownTwice()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var assembly = world.GetService<IMigrationsAssembly>();
            var migrator = world.GetService<IMigrator>();
            var ids = assembly.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            var crucible = ids.Where(id => id.Contains("_Crucible_")).ToList();
            Assert.IsGreaterThanOrEqualTo(1, crucible.Count, "the Crucible World migrations");

            migrator.Migrate(ids[ids.IndexOf(crucible[0]) - 1]);
            foreach (var id in crucible)
            {
                var migration = assembly.CreateMigration(assembly.Migrations[id], world.Database.ProviderName);
                var operations = migration.UpOperations.Concat(migration.DownOperations).ToList();
                Assert.IsTrue(operations.All(operation => operation is SqlOperation), $"{id}: plain SQL only");
                var tables = operations.Cast<SqlOperation>()
                    .SelectMany(operation => Regex.Matches(operation.Sql, @"\b(?:INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(\w+)", RegexOptions.IgnoreCase))
                    .Select(match => match.Groups[1].Value).Distinct().OrderBy(table => table, StringComparer.Ordinal).ToList();
                Assert.IsNotEmpty(tables, $"{id} writes rows");

                var before = Dump(world, tables);
                migrator.Migrate(id);
                var after = Dump(world, tables);
                Run(world, migration.UpOperations);
                AssertSame(after, Dump(world, tables), $"{id}: Up twice");
                migrator.Migrate(ids[ids.IndexOf(id) - 1]);
                AssertSame(before, Dump(world, tables), $"{id}: Down after Up");
                Run(world, migration.DownOperations);
                AssertSame(before, Dump(world, tables), $"{id}: Down twice");
                migrator.Migrate(id);
                AssertSame(after, Dump(world, tables), $"{id}: Up after Down");
            }
            migrator.Migrate(ids[^1]);
        }

        /// <summary>
        /// The sets' NPCs (label, place, NPC) and scene actors on the Crucible (Roscoe, a creature, as an NPC); the Staal
        /// Junkyard's scenes (1529, 1538) are CrucibleStaalJunkyardTests'.
        /// </summary>
        private static List<(string Label, Vector3 At, bool Npc)> Places(WildernessRuntimeTestHarness harness)
        {
            var places = Npcs().Select(pool => ($"pool {pool.DbId}", pool.Position, true)).ToList();
            foreach (var (mission, scene) in CrucibleMissionTests.Scenes(harness).Where(entry => entry.Scene.MapContextId != CrucibleStaalJunkyardNpcsV1.Junkyard))
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Position != null))
                    places.Add(($"{mission} {actor.Role}", new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z),
                        actor.Kind == SceneActorKind.Creature));
            Assert.HasCount(N.New.Length + N.Moves.Length + G.New.Length + G.Moves.Length + G.Kept.Length + W.New.Length + V.New.Length +
                A.Moves.Length + A.Kept.Length + K.Moves.Length + K.New.Count(npc => npc.Map == Map) + 21 + 1 + 1, places,
                "the NPCs, the terminal, the twenty fallen, Roscoe and 1217's obelisk");
            return places;
        }

        /// <summary>A world box across x and z (and its height).</summary>
        private readonly record struct Box(Vector3 Min, Vector3 Max)
        {
            public float Distance2D(Vector3 at) => new Vector2(MathF.Max(0, MathF.Max(Min.X - at.X, at.X - Max.X)), MathF.Max(0, MathF.Max(Min.Z - at.Z, at.Z - Max.Z))).Length();
        }

        /// <summary>The server-placed objects on the Crucible with a client model (the kraftwerks rows): their triangles and box.</summary>
        private static List<(string Label, List<(Vector3 A, Vector3 B, Vector3 C)> Triangles, Box Box)> ServerObjects(WildernessRuntimeTestHarness harness, MeshLibrary meshes)
        {
            var objects = new List<(string, List<(Vector3, Vector3, Vector3)>, Box)>();
            foreach (var row in harness.World.KraftwerksEntries.AsNoTracking().Where(row => row.MapContextId == Map).ToList())
            {
                var mesh = meshes.GetForClass((int)row.ClassId);
                if (mesh == null || !mesh.HasCollision)
                    continue;
                var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)row.Rotation);
                var origin = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                var points = mesh.Vertices.Select(vertex => Vector3.Transform(vertex, turn) + origin).ToList();
                var triangles = new List<(Vector3, Vector3, Vector3)>();
                for (var i = 0; i + 2 < mesh.Triangles.Count; i += 3)
                    triangles.Add((points[mesh.Triangles[i]], points[mesh.Triangles[i + 1]], points[mesh.Triangles[i + 2]]));
                var used = mesh.Triangles.Select(index => points[index]).ToList();
                objects.Add(($"kraftwerk {row.Id}", triangles,
                    new Box(used.Aggregate(Vector3.Min), used.Aggregate(Vector3.Max))));
            }
            return objects;
        }

        private static string MapName(WildernessRuntimeTestHarness harness) =>
            harness.World.MapInfoEntries.AsNoTracking().Single(map => map.Id == Map).MapName;

        private static void Run(DbContext world, IReadOnlyList<MigrationOperation> operations)
        {
            var connection = world.GetService<IRelationalConnection>();
            foreach (var command in world.GetService<IMigrationsSqlGenerator>().Generate(operations))
                command.ExecuteNonQuery(connection);
        }

        /// <summary>Every row of each table, each value with its type, sorted (as MarshesMigrationGuardTests).</summary>
        private static Dictionary<string, string[]> Dump(DbContext world, IEnumerable<string> tables)
        {
            var connection = world.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            var dump = new Dictionary<string, string[]>();
            foreach (var table in tables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i) switch
                    {
                        double number => "R:" + number.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                        byte[] bytes => "B:" + Convert.ToHexString(bytes),
                        string text => "T:" + text,
                        var value => value.GetType().Name + ":" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                    })));
                rows.Sort(StringComparer.Ordinal);
                dump[table] = rows.ToArray();
            }
            return dump;
        }

        private static void AssertSame(Dictionary<string, string[]> expected, Dictionary<string, string[]> actual, string step)
        {
            foreach (var (table, rows) in expected)
            {
                var now = actual[table];
                Assert.IsTrue(rows.SequenceEqual(now),
                    $"{step}: {table} has {now.Length} rows, {rows.Length} expected; missing {string.Join(" ; ", rows.Except(now).Take(3))}; " +
                    $"extra {string.Join(" ; ", now.Except(rows).Take(3))}");
            }
        }
    }
}
