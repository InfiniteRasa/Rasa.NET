using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using N = AshenLeadInNpcsV1;
    using B = AshenMaunaBarrensNpcsV1;
    using H = AshenSheitanHighwayNpcsV1;
    using Y = AshenBoneyardNpcsV1;
    using C = AshenConscriptionBaseNpcsV1;
    using K = AshenFormalComplaintsNpcsV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// The Marshes and Crucible playtest lessons (as CrucibleGuardTests holds them for the Crucible), held for Ashen Desert from
    /// its first set:
    ///  - every friendly Ashen NPC (pools 790000-790999 and the upstream ones set 1 moved) stands still, outside every hostile
    ///    automatic pool's circle by a creature's reach, with no floor over its head, and the new ones have their own hit
    ///    points;
    ///  - every one of them stands on a client collision floor, is seen from a client floor next to it and from the end of
    ///    the walk to it from each waypoint that walks to it, and stands clear of the server-placed objects (the client's
    ///    files, as MarshesClientFloorTests; inconclusive without them);
    ///  - every talk target stands inside its objective's marker and clear of the map links; every marker has a map, stands
    ///    on the navmesh and a client floor, off the lava, and clear of the map links;
    ///  - each Ashen migration's Up and Down are plain SQL, safe to run twice (#205).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenGuardTests
    {
        private const uint Map = AshenMissionDataV1.MapContextId;

        /// <summary>The friendly NPCs new in the block: set 1's, set 2's, set 4's (Infanger), set 6's (Latan, Ito, Megumi), set 11's (Meeks, Hale), set 13's (Odesha, Endala) and set 14's (Chase).</summary>
        private static readonly uint[] NewNpcs = N.New.Select(npc => npc.Id).Concat(B.New.Select(npc => npc.Id)).Append(Y.Infanger)
            .Concat(K.New.Select(npc => npc.Id)).Concat(AshenWhiteOasisNpcsV1.New.Select(npc => npc.Id)).Concat(AshenIndraPassNpcsV1.New.Select(npc => npc.Id)).Append(AshenAshokaNpcsV1.Chase).ToArray();

        /// <summary>The upstream NPCs the sets use where upstream has them (set 4's Lieutenant Jan Blood, 1262), and those a later set moved (set 10's Ida Hayner, off the weapon vendor; set 11's Dania Fontecilla, off the White Oasis control point's console).</summary>
        private static readonly uint[] Kept = { Y.Blood, AshenDesignerHidesNpcsV1.IdaHayner, AshenWhiteOasisNpcsV1.Fontecilla };

        /// <summary>The friendly NPC pools of the Ashen missions on Ashen Desert: the block's, the upstream ones set 1 moved and those kept.</summary>
        private static List<SpawnPool> Npcs()
        {
            var moved = N.Moves.Select(move => move.Pool).Concat(Kept).ToHashSet();
            bool Friendly(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory != TargetCategory.Hostile;
            var npcs = SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == Map &&
                (pool.DbId is >= 790000 and <= 790999 || moved.Contains(pool.DbId)) &&
                pool.SpawnSlot.Count > 0 && pool.SpawnSlot.TrueForAll(slot => Friendly(slot.CreatureId))).ToList();
            Assert.HasCount(NewNpcs.Length + N.Moves.Length + Kept.Length, npcs, "the Ashen NPCs are read");
            return npcs;
        }

        /// <summary>
        /// To-fix rows 1, 4, 15 (Marshes): a quest giver inside a hostile pool's reach died and blocked its chain; one that can
        /// walk wandered into one. Every Ashen NPC stands ReachClearance (an 18 m AggroRange and 2 m) outside every hostile
        /// automatic pool's circle on its map (across: a Fithik pool on a ridge over an NPC counts too), never moves, and the
        /// start-up check names none of them.
        /// </summary>
        [TestMethod]
        public void TheAshenNpcsStandStillAndClearOfEveryHostilePool()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenLeadInsTests.EnterAshen(harness);
            var pools = SpawnPoolManager.Instance.LoadedSpawnPools.Values.ToList();
            bool Hostile(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory == TargetCategory.Hostile;
            var npcs = Npcs();
            var hostiles = pools.Where(pool => pool.MapContextId == Map && pool.Mode == 0 &&
                pool.SpawnSlot.Exists(slot => slot.CountMax > 0) && pool.SpawnSlot.TrueForAll(slot => Hostile(slot.CreatureId))).ToList();
            Assert.IsGreaterThan(20, hostiles.Count, "Ashen Desert's hostile pools");
            foreach (var npc in npcs)
            {
                foreach (var hostile in hostiles)
                {
                    var gap = Vector2.Distance(new(npc.Position.X, npc.Position.Z), new(hostile.Position.X, hostile.Position.Z)) - hostile.Radius;
                    Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, gap, $"NPC pool {npc.DbId} and hostile pool {hostile.DbId}");
                }
                foreach (var slot in npc.SpawnSlot)
                    Assert.IsTrue(BehaviorManager.NeverMoves(CreatureManager.Instance.LoadedCreatures[slot.CreatureId]), $"{slot.CreatureId} stands still");
            }
            var found = SpawnPoolManager.Instance.PoolsTooClose(new List<(uint, Vector3, string)>()).Select(entry => entry.Message).ToList();
            // This batch's own pools (the block's hostile ones too: "spawnpool 79...") and the upstream pools it moves (Exing's 520007,
            // fix job A's Fithik pool 540067, set 5's Naxveik 520005) or relies on (set 3's highway mortars 592191-592194, set 5's
            // trench mortars 592199, 592202-592204).
            Assert.IsFalse(found.Any(message => message.Contains("of pool 790") || message.StartsWith("spawnpool 79") ||
                    message.StartsWith($"spawnpool {AshenMaunaBarrensNpcsV1.Exing}:") || H.Mortars.Any(mortar => message.StartsWith($"spawnpool {mortar.Pool}:")) ||
                    message.StartsWith($"spawnpool {C.Naxveik}:") || C.Mortars.Any(mortar => message.StartsWith($"spawnpool {mortar.Pool}:")) ||
                    message.StartsWith($"spawnpool {AshenFixAV1.Pool}:") ||
                    npcs.Any(npc => message.Contains($"of pool {npc.DbId}"))),
                string.Join("\n", found));
        }

        /// <summary>
        /// Auditor F2 (set 3): a creature on a beat stands wherever its beat takes it, not only at its pool's point. Every step
        /// of every Ashen beat (spawnpool_patrol of the block's pools) stands ReachClearance outside every friendly NPC's pool
        /// on Ashen Desert (by its circle) and ReachClearance from every hospital and waypoint, where players arrive.
        /// </summary>
        [TestMethod]
        public void EveryAshenBeatStepStandsClearOfNpcsAndArrivals()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenLeadInsTests.EnterAshen(harness);
            bool Friendly(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory != TargetCategory.Hostile;
            var friendly = SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == Map &&
                pool.SpawnSlot.Count > 0 && pool.SpawnSlot.TrueForAll(slot => Friendly(slot.CreatureId))).ToList();
            Assert.IsGreaterThan(10, friendly.Count, "Ashen Desert's friendly NPC pools");
            var arrivals = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map).ToList()
                .Where(row => row.Description != null && (row.Description.StartsWith("Hospital:") || row.Description.StartsWith("Waypoint:"))).ToList();
            Assert.IsGreaterThan(5, arrivals.Count, "Ashen Desert's hospitals and waypoints");
            var steps = harness.World.SpawnPoolPatrolEntries.AsNoTracking().Where(row => row.PoolId >= 790000 && row.PoolId <= 790999).ToList();
            Assert.HasCount(H.PredatorPools.Sum(pool => 2 * pool.Way.Length - 2), steps, "set 3's beats");
            var near = new List<string>();
            foreach (var step in steps)
            {
                var at = new Vector2((float)step.PosX, (float)step.PosZ);
                foreach (var npc in friendly)
                {
                    var gap = Vector2.Distance(at, new(npc.Position.X, npc.Position.Z)) - npc.Radius;
                    if (gap < MarshesNpcsStandStillV1.ReachClearance)
                        near.Add(FormattableString.Invariant($"pool {step.PoolId} step {step.Step} {gap:0.0} m from NPC pool {npc.DbId}"));
                }
                foreach (var arrival in arrivals)
                {
                    var gap = Vector2.Distance(at, new((float)arrival.PosX, (float)arrival.PosZ));
                    if (gap < MarshesNpcsStandStillV1.ReachClearance)
                        near.Add(FormattableString.Invariant($"pool {step.PoolId} step {step.Step} {gap:0.0} m from {arrival.Description}"));
                }
            }
            Assert.IsEmpty(near, string.Join("; ", near));
        }

        /// <summary>
        /// To-fix row 17 (Marshes): a quest NPC with no creature_stat row plays with 100 hit points. Each new Ashen NPC has 4500
        /// and armour 300; each new Ashen creature (set 2's Atta, set 3's Desert Predator and mortars, set 4's Desert Stalker) has
        /// the hit points and armour of the World DB row it copies.
        /// </summary>
        [TestMethod]
        public void EveryNewAshenNpcHasItsOwnHitPoints()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var stats = harness.World.CreatureStatEntries.AsNoTracking().Where(row => row.Id >= 790000 && row.Id <= 790999).ToDictionary(row => row.Id);
            var npcs = Npcs().Where(pool => pool.DbId >= 790000).SelectMany(pool => pool.SpawnSlot).Select(slot => slot.CreatureId).Distinct().ToList();
            Assert.HasCount(NewNpcs.Length, npcs);
            foreach (var npc in npcs)
            {
                Assert.IsTrue(stats.TryGetValue(npc, out var stat), $"{npc} has a creature_stat row");
                Assert.AreEqual(((int)N.HitPoints, (int)N.Armor), (stat.Health, stat.Armor), $"{npc}");
            }
            var copied = harness.World.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            foreach (var (atta, _, copy, _) in B.Atta)
            {
                Assert.IsTrue(stats.TryGetValue(atta, out var stat), $"{atta} has a creature_stat row");
                Assert.AreEqual((copied[copy].Health, copied[copy].Armor), (stat.Health, stat.Armor), $"{atta} as {copy}");
            }
            foreach (var (creature, copy) in H.Mortars.Select(mortar => (mortar.Creature, H.Mortar)).Append((H.Predator, H.PredatorCopy)).Append((Y.Stalker, Y.StalkerCopy)))
            {
                Assert.IsTrue(stats.TryGetValue(creature, out var stat), $"{creature} has a creature_stat row");
                Assert.AreEqual((copied[copy].Health, copied[copy].Armor), (stat.Health, stat.Armor), $"{creature} as {copy}");
            }
        }

        /// <summary>To-fix row 21 (Marshes): an NPC on a lower navmesh layer under a floor could not be talked to. No Ashen NPC has a walkable floor within 2.5 m over its head, and each stands 2 m or more from a wall.</summary>
        [TestMethod]
        public void NoAshenNpcStandsUnderAFloor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            foreach (var npc in Npcs())
            {
                var over = desert.NavMesh.GroundHeight(npc.Position + new Vector3(0, 2.5f, 0));
                Assert.IsNotNull(over, $"navmesh at {npc.DbId}");
                Assert.IsLessThan(0.5f, over.Value - npc.Position.Y, $"{npc.DbId} at {npc.Position}: no floor over its head ({over})");
                Assert.IsGreaterThanOrEqualTo(2f, desert.NavMesh.DistanceToWall(npc.Position, 20) ?? 0, $"{npc.DbId} 2 m or more from a wall");
            }
        }

        /// <summary>
        /// To-fix rows 24 and 25 (Marshes): a player next to an NPC that stood where the client has no floor fell through the
        /// world. Every Ashen NPC has a walkable client collision surface at most MaxOver under it and none from MaxUnder to
        /// MaxUnder + 0.3 m over it.
        /// </summary>
        [TestMethod]
        public void EveryAshenNpcStandsOnAClientFloor()
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
        /// To-fix rows 27, 32, 33 (Marshes) and 34, 43 (Crucible): a target that no player place round it could see ("You cannot
        /// see your target from here"). Every Ashen NPC is seen, from the eye of a player on a client floor Ring m round it, at
        /// its middle, from all RingPlaces places, the server-placed objects with a client model counted in; and none stands
        /// inside a server-placed object or within the talk reach of one. Set 9: 1278's own turrets (ward-M: no player's target,
        /// on their platforms, which wall them in at 2 m) and wave members (wave-K-M: sent at the turrets at once, they do not
        /// wait on their spots, which are on the batteries' steep slopes) are not held to the ring (only the ring: the server-placed objects check stays); they stand on a client
        /// floor (EveryAshenNpcStandsOnAClientFloor) and their walks are checked (AshenStonewallRidgeTests). Fix E: 1277's
        /// consoles at their top floor's rim are held to the ring places on the floor (TopFloorRingPlaces or more of them).
        /// </summary>
        [TestMethod]
        public void EveryAshenNpcIsSeenFromAClientFloorRoundIt()
        {
            const int TopFloorRingPlaces = 8;
            const float TalkReach = 5;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = Places(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), F.Ring + 1);
            var objects = ServerObjects(harness, meshes);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, npc) = places[i];
                foreach (var placed in objects.Where(placed => placed.Box.Distance2D(at) < TalkReach + 1 && MathF.Abs(at.Y - placed.Box.Min.Y) < 3))
                    failures.Add(FormattableString.Invariant($"{label}: {placed.Label} {placed.Box.Distance2D(at):0.0} m off"));
                if (label.StartsWith($"{AshenKingOfTheHillV1.MissionId} ward-", StringComparison.Ordinal) ||
                    label.StartsWith($"{AshenKingOfTheHillV1.MissionId} wave-", StringComparison.Ordinal))
                    continue;
                var aim = at + new Vector3(0, npc ? F.NpcMiddle : F.ObjectMiddle, 0);
                // Fix E: 1277's consoles stand at their top floor's rim (5-6 m off the upper pad, the floor about 11 m across): the
                // ring places past the rim are in the air, not where a player stands. They are seen from every ring place on the
                // floor, and at least TopFloorRingPlaces of them are on it.
                var rim = label.StartsWith($"{AshenOnTheSameFrequencyV1.MissionId} stonewall-radar-console-", StringComparison.Ordinal);
                var onFloor = 0;
                for (var k = 0; k < F.RingPlaces; k++)
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var stand = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    var floor = F.FloorAt(near[i], stand, at.Y - F.MaxOver, at.Y + F.MaxUnder);
                    if (floor == null && rim)
                        continue;
                    onFloor++;
                    if (floor is not { } y || !F.Clear(near[i], stand with { Y = y + F.Eye }, aim))
                        failures.Add(FormattableString.Invariant($"{label} not seen from ({stand.X:0.0}, {stand.Z:0.0})"));
                }
                if (rim && onFloor < TopFloorRingPlaces)
                    failures.Add($"{label}: {onFloor} of the ring places on its floor, under {TopFloorRingPlaces}");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// To-fix row 43 (Crucible): an NPC seen from the ring round it was not seen from where the real client's walk to it
        /// stopped. Every Ashen NPC is seen (eye to its middle, the client files) from each point of the navmesh walk to it
        /// from its area's waypoint where the walk first comes 5 m to 2 m near it (every 0.5 m); each of those walks is complete.
        /// </summary>
        [TestMethod]
        public void EveryAshenNpcIsSeenFromTheEndOfTheWalkToIt()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var walks = new (uint Pool, string Label, Vector3 From)[]
            {
                (N.Ames, "the Ashen Lookout waypoint", AshenLeadInsTests.LookoutWaypoint),
                (N.MasonLinier, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                (N.Ivan, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                (N.Caldwell, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                (N.Harris, "the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                (N.Yamamoto, "the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                (N.Emmons, "the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                (N.DarisLinier, "the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                (B.Kuang, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                (B.Kwon, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                (B.Karter, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                // Set 4: Boneyard Outpost has no waypoint; Ashoka Settlement's is the nearest (Yamamoto's too).
                (Y.Infanger, "the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                (Y.Blood, "the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 6: Latan and Ito on Ashoka's lower level, walked to from the Ashoka Settlement hospital on that floor (the
                // server navmesh does not join the level to the waypoint, the client's floor does: AshenFormalComplaintsTests,
                // upstream N18); Megumi in Shadow's Edge Post.
                (K.Latan, "the Ashoka Settlement hospital", AshenFormalComplaintsTests.AshokaHospital),
                (K.Ito, "the Ashoka Settlement hospital", AshenFormalComplaintsTests.AshokaHospital),
                (K.Megumi, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                // Set 10: Ida Hayner in the Shadow's Edge weapon shop.
                (AshenDesignerHidesNpcsV1.IdaHayner, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                // Set 11: Meeks, Hale and Fontecilla in White Oasis Post.
                (AshenWhiteOasisNpcsV1.Meeks, "the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                (AshenWhiteOasisNpcsV1.Hale, "the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                (AshenWhiteOasisNpcsV1.Fontecilla, "the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 13: Odesha and Endala in the Indra Pass valley (fix G; set 13 had them by the Indra Pass waypoint, whose
                // floor has no way in: N23), walked to from White Oasis Post.
                (AshenIndraPassNpcsV1.Odesha, "the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                (AshenIndraPassNpcsV1.Endala, "the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 14: Chase in Shadow's Edge Post.
                (AshenAshokaNpcsV1.Chase, "the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint)
            };
            var npcs = Npcs().ToDictionary(npc => npc.DbId);
            Assert.HasCount(npcs.Count, walks, "a walk to each NPC");
            var failures = new List<string>();
            foreach (var (pool, label, from) in walks)
            {
                var at = npcs[pool].Position;
                var near = F.Near(geometry, new List<Vector3> { at }, 7)[0];
                var aim = at + new Vector3(0, F.NpcMiddle, 0);
                var start = nav.Nearest(from) ?? from;
                var path = nav.FindPath(start, nav.Nearest(at) ?? at, out var complete);
                if (path == null || !complete)
                {
                    failures.Add($"pool {pool}: no walk from {label}");
                    continue;
                }
                var points = path.Prepend(start).ToList();
                var ends = 0;
                foreach (var reach in new[] { 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f })
                {
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
                    ends++;
                    var floor = F.FloorAt(near, stop, stop.Y - 1.5f, stop.Y + 1) ?? stop.Y;
                    if (!F.Clear(near, stop with { Y = floor + F.Eye }, aim))
                        failures.Add(FormattableString.Invariant($"pool {pool} from {label}: not seen at ({stop.X:0.0}, {stop.Y:0.0}, {stop.Z:0.0}), {reach} m off"));
                }
                if (ends == 0)
                    failures.Add($"pool {pool} from {label}: the walk ends more than 5 m off");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>Every talk target (an Ashen Desert NPC pool with the talk's package) stands inside a marker of its objective, so the map points the player at it.</summary>
        [TestMethod]
        public void EveryTalkTargetStandsInsideItsObjectivesMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = AshenMissionDataV1.Revision;
            var echoes = Rasa.Services.Preloader.Missions.Crucible.CrucibleMissionDataV1.Revision;   // 1736 on the Crucible's revision
            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision ||
                entry.MissionId == AshenEchoesInTheSandV1.MissionId && entry.ContentRevision == echoes).ToList();
            bool Inside(uint mission, uint objective, Vector3 at) => markers.Any(marker => marker.MissionId == mission && marker.ObjectiveId == objective &&
                Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(at.X, at.Z)) <= marker.Radius);
            var packages = world.NpcPackageEntries.AsNoTracking().ToList().ToLookup(entry => entry.PackageId, entry => entry.Id);
            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Map).ToList();
            var checkedTargets = 0;
            var outside = new List<string>();
            foreach (var talk in world.MissionTriggerEntries.AsNoTracking().Where(entry => (entry.ContentRevision == revision ||
                         entry.MissionId == AshenEchoesInTheSandV1.MissionId && entry.ContentRevision == echoes) &&
                         entry.Kind == MissionTriggerKind.Conversation).ToList())
                foreach (var pool in pools.Where(pool => packages[talk.NpcPackageId.Value].Contains(pool.Creature1Id)))
                {
                    checkedTargets++;
                    if (!Inside(talk.MissionId, talk.ObjectiveId, new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ)))
                        outside.Add($"{talk.MissionId}.{talk.ObjectiveId} NPC pool {pool.Id}");
                }
            Assert.AreEqual(4 + 4 + 3 + 4 + 1 + 5 + 1 + 1, checkedTargets, "Mason Linier, Emmons, Yamamoto and Daris Linier; set 6's Latan (1254's four talks), Yamamoto (1258, 1259), Mason Linier (1261); set 11's Hale (1298's four talks); set 13's Endala (1282.2); set 14's five captains (1358), Botha (1736.2; the obelisk is a scene object) and set 20's Yamamoto (1501.6; Humphries, Lukaz and the queen are scene actors, Jawan is on Thunderhead)");
            Assert.IsEmpty(outside, string.Join(", ", outside));
        }

        /// <summary>
        /// To-fix row 14 (Marshes): a player who walks up to a target in a map link's circle is taken to the other map. No Ashen
        /// NPC stands within an enabled map link's radius and 5 m on its map, nor 7 m from a waypoint or local teleporter (where
        /// players arrive; a local teleporter takes a player 2 m from it). Set 9: 1277's consoles stand on the radar towers' second
        /// floors, about 11 m across round the upper teleporter (ControlTowerTeleporters, no stairs), so they keep TopFloorClearance
        /// (fix E: 5 m, so the walk from the lift's exit to the console stays off the pad's 2 m trigger; set 9 had 3.4 m) from the
        /// towers' teleporters instead, and fix E's ambushers on the top floors AmbushClearance (3.2 m); any other pad still 7 m.
        /// </summary>
        [TestMethod]
        public void NoNpcStandsByAMapLinkOrATeleporter()
        {
            // Fix E (to-fix rows 12, 14): the consoles 5 m and more off their towers' pads (the walk from the lift's exit stays off
            // the pad: AshenFixETests), the ambushers on the top floors 3.2 m and more (out of the pad's 2 m trigger; the 2D
            // distance counts the ground pad under it too).
            const float LinkClearance = 5, PadClearance = 7, TopFloorClearance = 5, AmbushClearance = 3.2f;
            var towerPads = AshenOnTheSameFrequencyV1.Towers.SelectMany(tower => new[] { tower.Ground, tower.Upper }).ToHashSet();
            using var harness = WildernessRuntimeTestHarness.Create();
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Map).ToList();
            Assert.IsNotEmpty(links, "Ashen Desert's map links");
            var pads = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map).ToList();
            Assert.IsGreaterThan(10, pads.Count, "Ashen Desert's waypoints and teleporters");
            var near = new List<string>();
            foreach (var (label, at, _) in Places(harness))
            {
                foreach (var link in links)
                {
                    var gap = Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius;
                    if (gap < LinkClearance)
                        near.Add($"{label} {gap:F1} m outside link {link.Id}");
                }
                var console = label.Contains(" stonewall-radar-console-", StringComparison.Ordinal);
                var ambusher = label.Contains(" stonewall-ambush-", StringComparison.Ordinal);
                foreach (var pad in pads)
                {
                    var gap = Vector2.Distance(new(at.X, at.Z), new((float)pad.PosX, (float)pad.PosZ));
                    if (gap < (console && towerPads.Contains(pad.Id) ? TopFloorClearance : ambusher && towerPads.Contains(pad.Id) ? AmbushClearance : PadClearance))
                        near.Add($"{label} {gap:F1} m from teleporter {pad.Id}");
                }
            }
            Assert.IsEmpty(near, string.Join(", ", near));
        }

        /// <summary>
        /// To-fix rows 23 and 25 (Crucible): a player or bot that hunts inside a marker that holds a map link walks into the link.
        /// Every Ashen marker has a map (MissionMarkerProjection.ResolveMap), and its circle stays MarkerClearance or more outside
        /// every enabled map link's circle on that map.
        /// </summary>
        [TestMethod]
        public void NoAshenMarkerCircleTouchesAMapLinkCircle()
        {
            const float MarkerClearance = 5;
            using var harness = WildernessRuntimeTestHarness.Create();
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0).ToList();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision ||
                entry.MissionId == AshenEchoesInTheSandV1.MissionId && entry.ContentRevision == Rasa.Services.Preloader.Missions.Crucible.CrucibleMissionDataV1.Revision).ToList();
            Assert.HasCount(AshenMissionTests.Indicators + AshenMissionTests.EchoesIndicators + AshenMissionTests.CavernsIndicators + AshenMissionTests.OutpostIndicators +
                AshenMissionTests.HiveIndicators + AshenMissionTests.EntryIndicators + AshenMissionTests.QueenIndicators, markers, "the Ashen markers");
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
        /// To-fix row 24 (Crucible): markers stood off the navmesh. Every Ashen marker on Ashen Desert has its centre on the
        /// navmesh (ground at most MarkerHeight from its y), on a client floor (a walkable surface at most MarkerHeight under
        /// it), and off the lava (LavaDamage.InLava).
        /// </summary>
        [TestMethod]
        public void EveryAshenMarkerStandsOnTheNavmeshAndAClientFloor()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision ||
                entry.MissionId == AshenEchoesInTheSandV1.MissionId && entry.ContentRevision == Rasa.Services.Preloader.Missions.Crucible.CrucibleMissionDataV1.Revision).ToList()
                .Where(marker => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId }) == Map)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(AshenMissionTests.Indicators + AshenMissionTests.EchoesIndicators + AshenMissionTests.EntryDesertIndicators +
                AshenMissionTests.QueenDesertIndicators, markers, "the Ashen markers on Ashen Desert");
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness)), meshes, 1);
            var (floors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < markers.Count; i++)
            {
                var (label, at) = markers[i];
                var ground = desert.NavMesh.GroundHeight(at + new Vector3(0, MarkerHeight, 0));
                if (ground is not { } y || MathF.Abs(y - at.Y) > MarkerHeight)
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): navmesh {ground}"));
                if (at.Y - floors[i] > MarkerHeight)
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): client floor {floors[i]}"));
                if (LavaDamage.InLava(desert.MapInfo.MapName, desert.NavMesh.Nearest(at) ?? at) || LavaDamage.InLava(desert.MapInfo.MapName, at))
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}) on lava"));
            }
            foreach (var npc in Npcs().Where(npc => LavaDamage.InLava(desert.MapInfo.MapName, npc.Position)))
                failures.Add($"pool {npc.DbId} on lava");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Set 1 review (2026-11-09): the 1274 marker on Fithik pool 540066 stood on the navmesh and a client floor, but in an
        /// isolated pocket no player can walk to. Every Ashen marker on Ashen Desert is walked to (a complete path) from its
        /// mission's area waypoint; every Ashen mission with a marker there has a waypoint here, so a later set must add its own.
        /// </summary>
        [TestMethod]
        public void EveryAshenMarkerIsWalkedToFromItsAreasWaypoint()
        {
            var waypoints = new Dictionary<uint, (string Label, Vector3 At)>
            {
                [AshenDustiestPlaceV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenReportToStonewallV1.MissionId] = ("the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                [AshenFithikInfestationV1.MissionId] = ("the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                [AshenGoodDayToDieV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenCantDrinkAshV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 2: the Mauna Barrens have no waypoint; the givers' is Shadow's Edge Post's. Exing: "Northwest of Stonewall
                // Ridge waypoint" (TaRapedia).
                [AshenAngryAngryAttaV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenHarmfulMutationsV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenChemicalOversightV1.MissionId] = ("the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                [AshenEyesOnTheGroundV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                // Set 3: the Sheitan Highway has no waypoint; it runs north from Shadow's Edge Post (the givers' waypoint).
                [AshenHighwayToHellV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenAirSuperiorityV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenArtilleryAnnihilationV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                // Set 4: the Boneyard and Boneyard Outpost have no waypoint; Ashoka Settlement's is the nearest.
                [AshenXanxlessJobV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenFallenSoldiersV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenStalkingTheStalkersV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 5: the givers are at Boneyard Outpost (Ashoka Settlement's waypoint); the Conscription Garrison's waypoint is
                // the control point's, the Bane's at the start.
                [AshenDeathFromAboveV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenTurningOutTheLightsV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenStrongestLinkV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 6: 1254's markers at Latan on Ashoka's lower level, walked to from the Ashoka Settlement hospital on that
                // floor (its ramp to the waypoint is the client's, not the server navmesh's: AshenFormalComplaintsTests, N18);
                // Yamamoto's (1258, 1259) and the Boneyard Thrax pools' (1285) from the Ashoka waypoint; Mason Linier's (1261)
                // from Shadow's Edge Post's.
                [AshenFormalComplaintsV1.MissionId] = ("the Ashoka Settlement hospital", AshenFormalComplaintsTests.AshokaHospital),
                [AshenHumanPerspectiveV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenBribeForYourThoughtsV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenCourtMartialArtsV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenDearlyDepartedV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 7: Latan's three; the Boneyard's robots, Thrax and Stalkers from the Ashoka waypoint, and so the highway's
                // Ravagers and Predators (1266's launchers, 1265's Predators: the navmesh joins them).
                [AshenRoboticSalvageV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenWeaponSalvageV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenHeavySalvageV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 8: Ito's three, on the places of Latan's three.
                [AshenRoboticAcquisitionsV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenWeaponAcquisitionsV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenHeavyAcquisitionsV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 9: 1275's crate in the Boneyard from the Ashoka waypoint; 1277's towers and 1278's batteries from Stonewall
                // Ridge's.
                [AshenCycledFrequenciesV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenOnTheSameFrequencyV1.MissionId] = ("the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                [AshenKingOfTheHillV1.MissionId] = ("the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                // Set 10: 1287's Atta and Sally in the Mauna Barrens from Shadow's Edge Post's (as set 2's); 1289's Lashers and
                // Molly west of Ashoka from Ashoka Settlement's; 1293's Coheri Pools and 1291's north-east Maji Field from White
                // Oasis Post's (1291's eastern Boneyard Beam Manta from Ashoka's: Also; its Indra Pass one from White Oasis Post's, the Indra Pass
                // waypoint's navmesh region being joined to nothing).
                [AshenDesignerJewelryV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenWouldntWearItV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenLatestRageV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenNewFadV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 11: Maji Field's panels and Ulovik, Hale in the post and the Sheitan Embankment's pumps, all from White Oasis Post's.
                [AshenAnalyzingHarvestersV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenMurkyWatersV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenPryingEyesV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenGasShortagesV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 12: 1299's Coheri Pools Granitour, 1300's ridge Lashers west of the post (its Ashoka Lashers from Ashoka
                // Settlement's: Also) and 1302's panels, from White Oasis Post's.
                [AshenWaterPurificationV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenNeurotoxinsV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenPoisoningTheWellwaterV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 13: Veyon and the resistors in the Indra Pass valley from White Oasis Post's (fix G moved resistors 1, 2,
                // 3, 7 off the Indra Pass waypoint's floor, which has no way in: upstream N23); Maiek in the Conscription Base
                // from Ashoka Settlement's; Endala's marker (in the valley since fix G) from White Oasis Post's: Also.
                [AshenMissingScientistsV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenGodOfStormsV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                [AshenBondsOfBloodV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 14: 1358's captains from Shadow's Edge Post's (Chase's) and the others' (Also); 1286's Jirex from Ashoka
                // Settlement's; 1736's Botha and obelisk (Daedalus' Landing, the escort's way) from Ashoka Settlement's.
                [AshenSupplyLogisticsV1.MissionId] = ("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint),
                [AshenPreciousToolsV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                [AshenEchoesInTheSandV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 15: 1559's marker before the Indra Caverns door, from White Oasis Post's (as set 13's valley); it stands in
                // the door's navmesh pocket (InTheDoorPocket). Fix H: its second marker on the valley's lift pad, from White Oasis Post's.
                [AshenIndraCavernsV1.MissionId] = ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint),
                // Set 19: 1500's marker before the Bane Conscription Facility's door, from Ashoka Settlement's (Yamamoto's).
                [AshenLockdownTheConscriptionFacilityV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint),
                // Set 20: 1501's marker at Yamamoto (Boneyard Outpost), from Ashoka Settlement's (as 1258's).
                [AshenSaveTheQueenV1.MissionId] = ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint)
            };
            // A mission whose markers stand in more than one area: the other areas' waypoints (any one walks to the marker).
            var also = new Dictionary<uint, (string Label, Vector3 At)[]>
            {
                [AshenNewFadV1.MissionId] = new[] { ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint) },
                [AshenNeurotoxinsV1.MissionId] = new[] { ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint) },
                [AshenBondsOfBloodV1.MissionId] = new[] { ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint) },
                [AshenSupplyLogisticsV1.MissionId] = new[]
                {
                    ("the Ashoka Settlement waypoint", AshenLeadInsTests.AshokaWaypoint), ("the Stonewall Ridge waypoint", AshenLeadInsTests.StonewallWaypoint),
                    ("the White Oasis Post waypoint", AshenLeadInsTests.WhiteOasisWaypoint)
                }
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision ||
                entry.MissionId == AshenEchoesInTheSandV1.MissionId && entry.ContentRevision == Rasa.Services.Preloader.Missions.Crucible.CrucibleMissionDataV1.Revision).ToList()
                .Where(marker => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId }) == Map)
                .ToList();
            Assert.HasCount(AshenMissionTests.Indicators + AshenMissionTests.EchoesIndicators + AshenMissionTests.EntryDesertIndicators +
                AshenMissionTests.QueenDesertIndicators, markers, "the Ashen markers on Ashen Desert");
            var failures = new List<string>();
            foreach (var marker in markers)
            {
                var label = $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}";
                if (InTheDoorPocket.Contains((marker.MissionId, marker.ObjectiveId, marker.IndicatorId)))
                {
                    var pocket = new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ);
                    if (nav.FindPath(nav.Nearest(AshenLeadInsTests.WhiteOasisWaypoint) ?? AshenLeadInsTests.WhiteOasisWaypoint, nav.Nearest(pocket) ?? pocket, out var joined) != null && joined)
                        failures.Add($"{label}: the door's pocket is joined now (upstream N26 fixed?): take it off InTheDoorPocket");
                    continue;
                }
                if (!waypoints.TryGetValue(marker.MissionId, out var from))
                {
                    failures.Add($"{label}: no area waypoint for mission {marker.MissionId}");
                    continue;
                }
                var at = new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ);
                bool Walked(Vector3 start) => nav.FindPath(nav.Nearest(start) ?? start, nav.Nearest(at) ?? at, out var complete) != null && complete;
                if (!Walked(from.At) && !(also.TryGetValue(marker.MissionId, out var others) && others.Any(other => Walked(other.At))))
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}): no walk from {from.Label}"));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Markers in the Indra Caverns door's navmesh pocket (upstream N26: the cave mouth's navmesh is joined to nothing): 1559's
        /// before the door. The navmesh walk the guard asks for can not be had; the client floor's walk from Odesha to the door
        /// is checked instead (AshenIndraFacilityTests.TheCavernsDoorIsWalkedToFromTheUpperPad, from fix H's island pad). 1559.1's
        /// second marker (fix H, on the valley's lift pad) is walked to from White Oasis Post as any other.
        /// </summary>
        internal static readonly (uint Mission, uint Objective, uint Indicator)[] InTheDoorPocket =
            { (AshenIndraCavernsV1.MissionId, AshenIndraCavernsV1.Locate, AshenIndraCavernsV1.DoorIndicator) };

        /// <summary>
        /// #205: each Ashen World migration, in order: Up, Up again, and the tables it writes are the same; Down (to the migration
        /// before it, IMigrationsAssembly) and they are as before the Up; Down again and they still are; Up, and they are as after
        /// the first Up. Its operations are all plain SQL.
        /// </summary>
        [TestMethod]
        public void EachAshenMigrationRunsUpTwiceAndDownTwice()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var assembly = world.GetService<IMigrationsAssembly>();
            var migrator = world.GetService<IMigrator>();
            var ids = assembly.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            // The batch's migrations from 20271110000000 on (upstream has older ones with "Ashen" in their names).
            var ashen = ids.Where(id => id.Contains("_Ashen_") && string.CompareOrdinal(id, "20271110000000") >= 0).ToList();
            Assert.IsGreaterThanOrEqualTo(1, ashen.Count, "the Ashen World migrations");

            migrator.Migrate(ids[ids.IndexOf(ashen[0]) - 1]);
            // The Downs, from the last to the first, leave the rows they write as a database migrated only that far has them: a
            // Down that writes back a rounded value (233.2 for the single float 233.1999969482422 a pool row holds) is caught
            // here; the Up/Down pairs below start from the state the Downs left.
            var written = ashen.SelectMany(id => Tables(assembly.CreateMigration(assembly.Migrations[id], world.Database.ProviderName)))
                .Distinct().OrderBy(table => table, StringComparer.Ordinal).ToList();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: ids[ids.IndexOf(ashen[0]) - 1]))
                AssertSame(Dump(fresh.World, written), Dump(world, written), "the Downs against a database migrated to before the Ashen migrations");
            foreach (var id in ashen)
            {
                var migration = assembly.CreateMigration(assembly.Migrations[id], world.Database.ProviderName);
                var operations = migration.UpOperations.Concat(migration.DownOperations).ToList();
                Assert.IsTrue(operations.All(operation => operation is SqlOperation), $"{id}: plain SQL only");
                var tables = Tables(migration);
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
        /// The sets' NPCs and scene actors with a place (label, place, NPC) on Ashen Desert: set 2's five beacon spots (1245), set 4's
        /// six fallen (1263), set 5's six missile launchers and their Detonation Devices (1352) and three generators and theirs (1354),
        /// set 9's salvage crate (1275), consoles and ambushers (1277), turrets and wave members (1278), set 11's panels (1295) and
        /// fuel pumps and their Detonation Devices (1356), set 12's panels (1302, on 1295's places), set 13's bodies (1280, 1282) and
        /// resistors (1281), set 14's researchers, obelisk and ambush spots (1736).
        /// </summary>
        private static List<(string Label, Vector3 At, bool Npc)> Places(WildernessRuntimeTestHarness harness)
        {
            var places = Npcs().Select(pool => ($"pool {pool.DbId}", pool.Position, true)).ToList();
            // Set 15's scenes stand on Indra Caverns (AshenIndraFacilityTests checks their places), set 17's on Avernus Outpost
            // (AshenAvernusOutpostTests), set 19's in the Bane Conscription Facility (AshenConscriptionEntryTests).
            foreach (var (mission, scene) in AshenMissionTests.Scenes(harness).Where(entry => entry.Scene.MapContextId != AshenIndraFacilityNpcsV1.Caverns &&
                entry.Scene.MapContextId != AshenAvernusOutpostNpcsV1.Outpost && entry.Scene.MapContextId != AshenConscriptionEntryNpcsV1.Facility))
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Position != null))
                    places.Add(($"{mission} {actor.Role}", new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z),
                        actor.Kind == Rasa.Missions.Scenes.SceneActorKind.Creature));
            Assert.HasCount(NewNpcs.Length + N.Moves.Length + Kept.Length + 5 + 6 + 12 + 6 + 1 + 4 + 9 + 4 + 14 + 4 + 6 + 4 + 1 + 10 + 1 + 7, places,
                "the NPCs, 1245's five beacon spots, 1263's six fallen, 1352's launchers and devices, 1354's generators and devices, " +
                "1275's crate, 1277's four consoles and nine ambushers, 1278's four turrets and fourteen wave members, 1295's four " +
                "panels, 1356's pumps and devices, 1302's four panels, 1280's Veyon, 1281's ten resistors, 1282's Maiek, 1736's two researchers, obelisk and four ambushers");
            return places;
        }

        /// <summary>A world box across x and z (and its height).</summary>
        private readonly record struct Box(Vector3 Min, Vector3 Max)
        {
            public float Distance2D(Vector3 at) => new Vector2(MathF.Max(0, MathF.Max(Min.X - at.X, at.X - Max.X)), MathF.Max(0, MathF.Max(Min.Z - at.Z, at.Z - Max.Z))).Length();
        }

        /// <summary>The server-placed objects on Ashen Desert with a client model (the kraftwerks rows): their box (as CrucibleGuardTests.ServerObjects).</summary>
        private static List<(string Label, Box Box)> ServerObjects(WildernessRuntimeTestHarness harness, MeshLibrary meshes)
        {
            var objects = new List<(string, Box)>();
            foreach (var row in harness.World.KraftwerksEntries.AsNoTracking().Where(row => row.MapContextId == Map).ToList())
            {
                var mesh = meshes.GetForClass((int)row.ClassId);
                if (mesh == null || !mesh.HasCollision)
                    continue;
                var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)row.Rotation);
                var origin = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                var used = mesh.Triangles.Select(index => Vector3.Transform(mesh.Vertices[index], turn) + origin).ToList();
                objects.Add(($"kraftwerk {row.Id}", new Box(used.Aggregate(Vector3.Min), used.Aggregate(Vector3.Max))));
            }
            return objects;
        }

        private static string MapName(WildernessRuntimeTestHarness harness) =>
            harness.World.MapInfoEntries.AsNoTracking().Single(map => map.Id == Map).MapName;

        /// <summary>The tables a migration's plain SQL writes (Up and Down).</summary>
        private static List<string> Tables(Migration migration) =>
            migration.UpOperations.Concat(migration.DownOperations).OfType<SqlOperation>()
                .SelectMany(operation => Regex.Matches(operation.Sql, @"\b(?:INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(\w+)", RegexOptions.IgnoreCase))
                .Select(match => match.Groups[1].Value).Distinct().OrderBy(table => table, StringComparer.Ordinal).ToList();

        private static void Run(DbContext world, IReadOnlyList<MigrationOperation> operations)
        {
            var connection = world.GetService<IRelationalConnection>();
            foreach (var command in world.GetService<IMigrationsSqlGenerator>().Generate(operations))
                command.ExecuteNonQuery(connection);
        }

        /// <summary>Every row of each table, each value with its type, sorted (as CrucibleGuardTests).</summary>
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
