using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Navigation;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = MarshesClientFloorTests;
    using X = AshenFixEV1;

    /// <summary>
    /// Ashen fix job E: On The Same Frequency's (1277) consoles off the walk from the lift's exit and used only from their floor,
    /// its ambushes on the top floors (to-fix rows 12-14), and the Sheitan Highway's Ravager pools on the camp's edge, two each,
    /// with the markers of 1247, 1266 and 1269 on them (the encounter tuning).
    ///
    /// Why these tests: in the real client the walk from the lift to a console crossed the upper pad, whose trigger sent the
    /// player down (row 12); a console was used from the ground floor 13.6 m under it (row 13); the ambushers stood at the
    /// tower's foot and never reached the top (row 14); a solo player died twice at the Ravager pools, which pulled with the
    /// camp's pool 531034. A console back by the pad, a walk line over its trigger, an ambush off the top floor or not walked to
    /// the player, a pool back in the camp's reach, a marker off its pool, or a Down that does not put set 9's scene and set 3's
    /// pools back would bring them back. The runtime side (the use refused from the ground floor, the ambushers on the top
    /// floor) is in AshenStonewallRidgeTests.
    ///
    /// The map: Ligo Ashen Desert (1734), loaded as AshenLeadInsTests loads it. The client floor checks read the client's files
    /// (as MarshesClientFloorTests).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixETests
    {
        private const string FixE = "20271110001010_Ashen_fix_e";

        /// <summary>The camp pool's members notice a player within Creature.AggroRange (18 m), and stroll within its radius (30 m).</summary>
        private const float AggroRange = 18;

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
        private static Vector3 V(ScenePosition at) => new(at.X, at.Y, at.Z);
        private static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new(a.X, a.Z), new(b.X, b.Z));

        /// <summary>The nearest the straight line from <paramref name="a"/> to <paramref name="b"/> comes to <paramref name="at"/>, in 3D.</summary>
        internal static float Nearest(Vector3 a, Vector3 b, Vector3 at)
        {
            var ab = b - a;
            var k = Math.Clamp(Vector3.Dot(at - a, ab) / ab.LengthSquared(), 0, 1);
            return Vector3.Distance(a + ab * k, at);
        }

        /// <summary>
        /// Why the walk from the lift's exit <paramref name="exit"/> to a console at <paramref name="console"/> on the top floor
        /// over <paramref name="pad"/> is not safe, or null: the console 5-6 m off the pad, on the floor; the exit off the pad's
        /// trigger; the straight walk keeps WalkClearance (2.5 m) from the pad's centre and is a client-floor walk both ways.
        /// </summary>
        internal static string ConsoleWalk(NavMeshQuery nav, MapGeometry geometry, Vector3 pad, Vector3 exit, Vector3 console)
        {
            var off = Flat(console, pad);
            if (off < 5 || off > 6)
                return FormattableString.Invariant($"the console {off:0.00} m off the pad");
            if (MathF.Abs(console.Y - pad.Y) > 1)
                return FormattableString.Invariant($"the console {console.Y - pad.Y:0.00} m over the pad's floor");
            if (Flat(exit, pad) < X.PadTrigger + 0.5f)
                return FormattableString.Invariant($"the exit {Flat(exit, pad):0.00} m off the pad");
            var near = F.Near(geometry, new List<Vector3> { console }, 3)[0];
            if (F.FloorAt(near, console, console.Y - 0.3f, console.Y + 0.3f) is null)
                return "no client floor under the console";
            if (nav.Nearest(console) is not { } end || Flat(end, console) > 1.5f)
                return "no navmesh within 1.5 m of the console";
            var clear = MathF.Min(Nearest(exit, end, pad), Nearest(exit, console, pad));
            if (clear < X.WalkClearance)
                return FormattableString.Invariant($"the walk comes {clear:0.00} m from the pad's centre");
            if (AshenFixBTests.ClientFloorLine(geometry, new[] { pad, exit }, exit) is { } off1)
                return $"off the pad to the exit: {off1}";
            if (AshenFixBTests.ClientFloorLine(geometry, new[] { exit, end }, end, halfWidth: 0.35f) is { } there)
                return $"exit to console: {there}";
            if (AshenFixBTests.ClientFloorLine(geometry, new[] { end, exit }, exit, halfWidth: 0.35f) is { } back)
                return $"console to exit: {back}";
            return null;
        }

        /// <summary>
        /// Row 12: on each tower's top floor the console stands 5-6 m off the upper teleporter (its World DB row), on the client
        /// floor, and the straight walk from the lift's exit side (ExitAt: tower 1 east, 2 east, 3 north-west, 4 west, the real
        /// client) to it keeps 2.5 m from the pad's centre (its trigger takes a player within 2 m) and is a client-floor walk
        /// there and back; the exit is walked to from the pad. Set 9's consoles (2.7-4 m off) fail it.
        /// </summary>
        [TestMethod]
        public void TheWalkFromTheLiftsExitToEachConsoleKeepsOffThePad()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var pads = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList()
                .ToDictionary(row => row.Id, row => new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ));
            var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(harness.World.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == X.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings, MissionContentCodec.Options);
            var failures = new List<string>();
            var set9Failures = 0;
            Assert.HasCount(4, X.Towers, "four towers");
            for (var tower = 0; tower < X.Towers.Length; tower++)
            {
                var pad = pads[X.Towers[tower].Upper];
                var console = V(scene.Actors[AshenOnTheSameFrequencyV1.ConsoleRole(tower)].Position);
                Assert.AreEqual(V(X.Towers[tower].ConsoleAt), console, $"console {tower + 1}: the World DB's scene holds fix E's place");
                if (ConsoleWalk(desert.NavMesh, geometry, pad, V(X.ExitAt[tower]), console) is { } why)
                    failures.Add($"tower {tower + 1}: {why}");
                if (ConsoleWalk(desert.NavMesh, geometry, pad, V(X.ExitAt[tower]), V(AshenOnTheSameFrequencyV1.Towers[tower].ConsoleAt)) != null)
                    set9Failures++;
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
            Assert.AreEqual(4, set9Failures, "set 9's consoles (2.7-4 m off the pads) fail the check");
        }

        /// <summary>
        /// Row 14: at towers 2-4 the three ambush spots are on the top floor (within 1 m of the upper pad's height), 3.2-4.5 m off
        /// the pad (out of its 2 m trigger), on the navmesh and the client floor, and each walks on the client floor to the
        /// lift's exit and to the console (and they to it): the ambushers reach a player there. Tower 1 has none.
        /// </summary>
        [TestMethod]
        public void TheAmbushesComeOnTheTopFloorWhereThePlayerIs()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var pads = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList()
                .ToDictionary(row => row.Id, row => new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ));
            Assert.IsEmpty(X.Towers[0].AmbushAt, "none at Emmons' tower");
            var failures = new List<string>();
            for (var tower = 1; tower < 4; tower++)
            {
                var pad = pads[X.Towers[tower].Upper];
                var exit = nav.Nearest(V(X.ExitAt[tower])).Value;
                var console = nav.Nearest(V(X.Towers[tower].ConsoleAt)).Value;
                Assert.HasCount(3, X.Towers[tower].AmbushAt, $"tower {tower + 1}: three");
                foreach (var spot in X.Towers[tower].AmbushAt.Select(V))
                {
                    var label = FormattableString.Invariant($"tower {tower + 1}'s ambush spot ({spot.X}, {spot.Z})");
                    if (MathF.Abs(spot.Y - pad.Y) > 1)
                        failures.Add($"{label}: not on the top floor ({spot.Y - pad.Y:0.0} m)");
                    if (Flat(spot, pad) is < 3.2f or > 4.5f)
                        failures.Add(FormattableString.Invariant($"{label}: {Flat(spot, pad):0.00} m off the pad"));
                    if (nav.Nearest(spot) is not { } on || Flat(on, spot) > 0.3f || MathF.Abs(on.Y - spot.Y) > 0.6f)
                    {
                        failures.Add($"{label}: not on the navmesh");
                        continue;
                    }
                    var near = F.Near(geometry, new List<Vector3> { spot }, 2)[0];
                    if (F.FloorAt(near, spot, spot.Y - F.MaxUnder, spot.Y + F.MaxOver) is null)
                        failures.Add($"{label}: no client floor");
                    foreach (var (to, name) in new[] { (exit, "the lift's exit"), (console, "the console") })
                    {
                        if (AshenFixBTests.ClientFloorWalk(nav, geometry, on, to) is { } there)
                            failures.Add($"{label} to {name}: {there}");
                        if (AshenFixBTests.ClientFloorWalk(nav, geometry, to, on) is { } back)
                            failures.Add($"{name} to {label}: {back}");
                    }
                }
                foreach (var spot in AshenOnTheSameFrequencyV1.Towers[tower].AmbushAt.Select(V))
                    Assert.IsGreaterThan(4f, pad.Y - spot.Y, $"set 9's spot at tower {tower + 1} was at the foot (the fix's reason)");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Row 13: the consoles keep their user within MaxUseHeight of their floor (3 m; the floors are 13.4 m apart), as the World
        /// DB's scene holds it; the field is opt-in: no other Ashen scene object has it, and a use action without it writes no
        /// "maxUseHeight" (the JSON of every other scene stays as it was).
        /// </summary>
        [TestMethod]
        public void OnlyTheConsolesKeepTheirUserOnTheirFloor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var scenes = harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().ToList();
            var consoles = 0;
            foreach (var entry in scenes)
            {
                var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(entry.Bindings, MissionContentCodec.Options);
                foreach (var actor in scene.Actors.Values.Where(actor => actor.UseAction != null))
                    if (entry.MissionId == X.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision)
                    {
                        Assert.AreEqual(X.MaxUseHeight, actor.UseAction.MaxUseHeight, $"{actor.Role}: used only from its floor");
                        consoles++;
                    }
                    else
                        Assert.AreEqual(0f, actor.UseAction.MaxUseHeight, $"{entry.MissionId} {actor.Role}: no floor limit (opt-in)");
            }
            Assert.AreEqual(4, consoles, "1277's four consoles");
            Assert.IsTrue(X.MaxUseHeight is > 0 and < 13.4f / 2, $"a floor limit ({X.MaxUseHeight} m) under half the 13.4 m between the floors");
            Assert.IsFalse(JsonSerializer.Serialize(new SceneObjectAction(1, 1, 1), MissionContentCodec.Options).Contains("maxUseHeight", StringComparison.OrdinalIgnoreCase),
                "no limit is not written");
            var limited = new SceneObjectAction(1, 1, 1, 1, 3);
            Assert.IsTrue(limited.OffTheFloor(353.3f, 366.9f), "13.6 m under");
            Assert.IsTrue(limited.OffTheFloor(370.0f, 366.9f), "3.1 m over");
            Assert.IsFalse(limited.OffTheFloor(367.5f, 366.9f), "on the floor");
            Assert.IsFalse(new SceneObjectAction(1, 1, 1).OffTheFloor(0, 100), "no limit: any height");
        }

        /// <summary>
        /// The encounter tuning: the two Sheitan Highway Ravager pools (World DB rows) stand at fix E's places with two Ravagers
        /// (531009), radius 0, as set 3's respawn; each is the camp pool 531034's radius and its members' aggro reach (30 + 18 m)
        /// with 5 m to spare off its point; set 3's places were inside it. The markers of 1247 (r20), 1266 and 1269 (r25) stand on
        /// the pools' points. Each pool's point is on the client floor (within 1 m under the navmesh), 5 m or more from a navmesh edge, and walked to
        /// on the client floor from the Shadow's Edge Post waypoint and back.
        /// </summary>
        [TestMethod]
        public void TheRavagerPoolsStandOnTheCampsEdgeOutOfItsReach()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var world = harness.World;
            var camp = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == 531034);
            Assert.AreEqual((X.CampPool.X, X.CampPool.Y, X.CampPool.Z, X.CampRadius), (camp.PosX, camp.PosY, camp.PosZ, camp.Radius), "the camp's upstream pool");
            var reach = (float)X.CampRadius + AggroRange + 5;
            var shadows = nav.Nearest(AshenLeadInsTests.ShadowsEdgeWaypoint).Value;
            var failures = new List<string>();
            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision).ToList();
            Assert.HasCount(2, X.RavagerPools, "two pools");
            foreach (var (pool, before, now) in X.RavagerPools)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((now.X, now.Y, now.Z), (row.PosX, row.PosY, row.PosZ), $"pool {pool} at fix E's place");
                Assert.AreEqual((X.Ravager, (byte)2, (byte)2, 0.0, 1200U), (row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount, row.Radius, row.RespawnTime),
                    $"pool {pool}: two Ravagers, a point, set 3's respawn");
                var at = V(now);
                var off = Flat(at, V(X.CampPool));
                if (off < reach)
                    failures.Add(FormattableString.Invariant($"pool {pool} {off:0.0} m off the camp pool, under {reach}"));
                Assert.IsLessThan(reach, Flat(V(before), V(X.CampPool)), $"set 3's place of {pool} was in the camp's reach (the fix's reason)");
                // The pool's point is the client's floor; the navmesh there lies up to 0.8 m over it (790086: 314.27 / 315.06).
                if (nav.Nearest(at) is not { } on || Flat(on, at) > 0.5f || MathF.Abs(on.Y - at.Y) > 1f)
                {
                    failures.Add($"pool {pool}: not on the navmesh");
                    continue;
                }
                if ((nav.DistanceToWall(on, 6) ?? 0) < 5)
                    failures.Add($"pool {pool}: under 5 m from a navmesh edge");
                var near = F.Near(geometry, new List<Vector3> { at }, 2)[0];
                if (F.FloorAt(near, at, at.Y - F.MaxUnder, at.Y + F.MaxOver) is null)
                    failures.Add(FormattableString.Invariant($"pool {pool}: no client floor (the nearest within 3 m: {F.FloorAt(near, at, at.Y - 3, at.Y + 3)})"));
                if (AshenFixBTests.ClientFloorWalk(nav, geometry, shadows, on) is { } there)
                    failures.Add($"pool {pool} from the Shadow's Edge Post waypoint: {there}");
                if (AshenFixBTests.ClientFloorWalk(nav, geometry, on, shadows) is { } back)
                    failures.Add($"pool {pool} back to the Shadow's Edge Post waypoint: {back}");
                foreach (var marker in X.Markers.Where(marker => marker.Pool == pool))
                {
                    var entry = markers.Single(entry => entry.MissionId == marker.Mission && entry.ObjectiveId == marker.Objective && entry.IndicatorId == marker.Indicator);
                    Assert.AreEqual((now.X, now.Y, now.Z), (entry.PosX, entry.PosY, entry.PosZ), $"{marker.Mission}.{marker.Objective}/{marker.Indicator} on pool {pool}");
                }
            }
            Assert.HasCount(6, X.Markers, "1247's, 1266's and 1269's markers on the two pools");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>The pools of each set 10 order's own targets (its hides' kind and its boss; 1287's also set 2's Atta it takes the carapace off).</summary>
        internal static readonly (uint Mission, uint[] Pools)[] OrderPools =
        {
            (AshenDesignerJewelryV1.MissionId, new uint[] { AshenDesignerHidesNpcsV1.SallyPool, 790046, 790047, 790048, 790049, AshenFixFV1.AttaPool }), // fix F's pool
            (AshenWouldntWearItV1.MissionId, AshenDesignerHidesNpcsV1.LasherPools.Select(pool => pool.Pool).Append(AshenDesignerHidesNpcsV1.MollyPool).ToArray()),
            (AshenNewFadV1.MissionId, AshenDesignerHidesNpcsV1.MantaPools.Select(pool => pool.Pool).Append(AshenDesignerHidesNpcsV1.TrudyPool).ToArray()),
            (AshenLatestRageV1.MissionId, AshenDesignerHidesNpcsV1.GranitourPools.Select(pool => pool.Pool).Append(AshenDesignerHidesNpcsV1.WendyPool).ToArray())
        };

        /// <summary>
        /// The least the last ApproachReach (100 m) of the walk to <paramref name="at"/> from <paramref name="from"/> by the corners of
        /// <paramref name="way"/> (the navmesh's path between them, there and back) keeps from <paramref name="hostile"/> (2D, less
        /// each pool's radius), and the pool it comes nearest.
        /// </summary>
        internal static (float Clear, uint Pool) Approach(NavMeshQuery nav, Vector3 from, (Vector3[] There, Vector3[] Back) way, Vector3 at,
            IReadOnlyList<SpawnPoolEntry> hostile)
        {
            var best = (Clear: float.MaxValue, Pool: 0U);
            foreach (var legs in new[] { way.There.Prepend(from).Append(at).ToList(), way.Back.Prepend(at).Append(from).ToList() })
                for (var leg = 0; leg + 1 < legs.Count; leg++)
                {
                    var start = nav.Nearest(legs[leg]) ?? legs[leg];
                    var path = nav.FindPath(start, nav.Nearest(legs[leg + 1]) ?? legs[leg + 1], out _)?.Prepend(start).ToList() ?? new List<Vector3> { start };
                    for (var k = 0; k + 1 < path.Count; k++)
                        for (var step = 0; step <= 20; step++)
                        {
                            var point = Vector3.Lerp(path[k], path[k + 1], step / 20f);
                            if (Flat(point, at) > X.ApproachReach)
                                continue;
                            foreach (var pool in hostile)
                            {
                                var clear = Flat(point, V((pool.PosX, pool.PosY, pool.PosZ))) - (float)pool.Radius;
                                if (clear < best.Clear)
                                    best = (clear, pool.Id);
                            }
                        }
                }
            return best;
        }

        /// <summary>
        /// Rows 17 and 18 (cannot-win cases): every set 10 boss (Sally, Molly, Trudy, Wendy) stands BossClearance (35 m) and more
        /// from every other hostile pool of Ashen Desert (2D, less its radius), and the last 100 m of the walk to each set 10 place
        /// from its waypoint by set 10's routes (AshenDesignerHidesTests.Places) keep WalkClearancePools (30 m) and more from every
        /// hostile pool not of its order (a fight there does not pull in another camp). Set 10's places of the pools fix E moves
        /// fail it (Sally 20.6 m from Atta pool 790047, Molly's walk 22.3 m from the Fithik pool 540067, Trudy 15.8 m from Manta
        /// pool 790370, the Boneyard Manta's walk 2.2 m from Nitroglazer pool 790244).
        /// </summary>
        [TestMethod]
        public void TheOrdersPlacesKeepClearOfOtherHostilePools()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList();
            var hostile = pools.Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.TargetCategory == TargetCategory.Hostile)).ToList();
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var bosses = new[] { AshenDesignerHidesNpcsV1.SallyPool, AshenDesignerHidesNpcsV1.MollyPool, AshenDesignerHidesNpcsV1.TrudyPool, AshenDesignerHidesNpcsV1.WendyPool };
            float Spot(uint pool, Vector3 at) => hostile.Where(row => row.Id != pool).Min(row => Flat(at, V((row.PosX, row.PosY, row.PosZ))) - (float)row.Radius);
            List<SpawnPoolEntry> NotOfItsOrder(uint pool) => hostile.Where(row => !OrderPools.Single(order => order.Pools.Contains(pool)).Pools.Contains(row.Id)).ToList();
            Vector3 At(uint pool) => pools.Where(row => row.Id == pool).AsEnumerable().Select(row => V((row.PosX, row.PosY, row.PosZ))).Single();
            var failures = new List<string>();
            foreach (var boss in bosses)
                if (Spot(boss, At(boss)) is var spot && spot < X.BossClearance)
                    failures.Add(FormattableString.Invariant($"boss pool {boss}: {spot:0.0} m from another hostile pool"));
            var walked = 0;
            foreach (var (pool, label, from, way) in AshenDesignerHidesTests.Places.Where(place => place.Pool != AshenDesignerHidesNpcsV1.IdaHayner))
            {
                walked++;
                var (clear, near) = Approach(nav, from, way, At(pool), NotOfItsOrder(pool));
                if (clear < X.WalkClearancePools)
                    failures.Add(FormattableString.Invariant($"{label}: the walk comes {clear:0.0} m from hostile pool {near}"));
            }
            Assert.AreEqual(11 + 1, walked, "the eleven set 10 hostile pools and fix F's are walked to");
            Assert.IsEmpty(failures, string.Join("; ", failures));

            // Set 10's places of the pools fix E moves fail it.
            foreach (var move in X.Set10Moves)
            {
                var before = V(move.Before);
                var place = AshenDesignerHidesTests.Places.Single(entry => entry.Pool == move.Pool);
                var spot = Spot(move.Pool, before);
                var (clear, _) = Approach(nav, place.From, place.Way, before, NotOfItsOrder(move.Pool));
                Assert.IsTrue((bosses.Contains(move.Pool) && spot < X.BossClearance) || clear < X.WalkClearancePools,
                    FormattableString.Invariant($"set 10's place of pool {move.Pool} fails ({spot:0.0} m, walk {clear:0.0} m): the fix's reason"));
            }
        }

        /// <summary>
        /// Ashen_fix_e rolls back: Down puts set 9's scene (its exact JSON), set 3's pools (places, three each) and markers and set
        /// 10's moved pools and markers back and takes out its evidence rows; Up again, fix E's are back. A second Up (its SQL run again) changes nothing, and
        /// a row that holds neither value (a scene, a pool) is not touched by Up or Down. The other Ashen scenes, pools and
        /// markers do not change.
        /// </summary>
        [TestMethod]
        public void FixERollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            string Scene() => world.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == X.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
            (double, double, double, byte, byte) Pool(uint id) => world.SpawnPoolEntries.AsNoTracking().Where(entry => entry.Id == id).AsEnumerable()
                .Select(entry => (entry.PosX, entry.PosY, entry.PosZ, entry.Creature1MinCount, entry.Creature1MaxCount)).Single();
            (double, double, double) Marker((uint Mission, uint Objective, uint Indicator, uint Pool) marker) => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == marker.Mission && entry.ContentRevision == AshenMissionDataV1.Revision &&
                    entry.ObjectiveId == marker.Objective && entry.IndicatorId == marker.Indicator).AsEnumerable()
                .Select(entry => (entry.PosX, entry.PosY, entry.PosZ)).Single();
            int Evidence() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == AshenMissionDataV1.Revision &&
                entry.EvidenceId == X.Evidence && entry.SourceUri == X.Source);
            var poolIds = X.RavagerPools.Select(pool => pool.Pool).Concat(X.Set10Moves.Select(move => move.Pool)).ToArray();
            var movedMarkers = X.Markers.Select(marker => (marker.Mission, marker.Objective, marker.Indicator))
                .Concat(X.Set10Moves.Select(move => (move.Mission, move.Objective, move.Indicator))).ToList();
            (double, double, double) PoolAt(uint id) => world.SpawnPoolEntries.AsNoTracking().Where(entry => entry.Id == id).AsEnumerable()
                .Select(entry => (entry.PosX, entry.PosY, entry.PosZ)).Single();
            string Others(Rasa.Context.World.SqliteWorldContext db) => string.Join("; ", db.SpawnPoolEntries.AsNoTracking()
                    .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && !poolIds.Contains(entry.Id)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.MissionId != X.MissionId)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable().Select(entry => $"{entry.MissionId}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Where(entry => !movedMarkers.Contains((entry.MissionId, entry.ObjectiveId, entry.IndicatorId)))
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")));
            void Fixed(string when)
            {
                Assert.AreEqual(X.SceneNow(), Scene(), $"{when}: fix E's scene");
                foreach (var (pool, _, now) in X.RavagerPools)
                    Assert.AreEqual((now.X, now.Y, now.Z, (byte)2, (byte)2), Pool(pool), $"{when}: pool {pool} at fix E's place, two");
                foreach (var marker in X.Markers)
                {
                    var now = X.RavagerPools.Single(pool => pool.Pool == marker.Pool).Now;
                    Assert.AreEqual((now.X, now.Y, now.Z), Marker(marker), $"{when}: {marker.Mission}.{marker.Objective}/{marker.Indicator}");
                }
                foreach (var move in X.Set10Moves)
                {
                    Assert.AreEqual((move.Now.X, move.Now.Y, move.Now.Z), PoolAt(move.Pool), $"{when}: set 10's pool {move.Pool} at fix E's place");
                    Assert.AreEqual((move.Now.X, move.Now.Y, move.Now.Z), Marker((move.Mission, move.Objective, move.Indicator, move.Pool)), $"{when}: its marker");
                }
                Assert.AreEqual(X.Evidences.Length, Evidence(), $"{when}: fix E's evidence rows");
            }
            // The other Ashen rows as a database migrated only up to fix E holds them: a Down to the migration before fix E also
            // takes out the later sets (set 11 sorts after it), and the Up goes only to fix E.
            string others;
            using (var atFixE = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixE))
                others = Others(atFixE.World);
            Assert.AreNotEqual(X.SceneBefore(), X.SceneNow(), "fix E changes the scene");
            Fixed("all migrations");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixE));
            world.ChangeTracker.Clear();
            Assert.AreEqual(X.SceneBefore(), Scene(), "set 9's scene, exact");
            foreach (var (pool, before, _) in X.RavagerPools)
                Assert.AreEqual((before.X, before.Y, before.Z, (byte)3, (byte)3), Pool(pool), $"pool {pool}: set 3's place and three, exact");
            foreach (var marker in X.Markers)
            {
                var before = X.RavagerPools.Single(pool => pool.Pool == marker.Pool).Before;
                Assert.AreEqual((before.X, before.Y, before.Z), Marker(marker), $"set 3's {marker.Mission}.{marker.Objective}/{marker.Indicator}, exact");
            }
            foreach (var move in X.Set10Moves)
            {
                Assert.AreEqual((move.Before.X, move.Before.Y, move.Before.Z), PoolAt(move.Pool), $"set 10's pool {move.Pool}, exact");
                Assert.AreEqual((move.Before.X, move.Before.Y, move.Before.Z), Marker((move.Mission, move.Objective, move.Indicator, move.Pool)), $"set 10's marker of {move.Pool}, exact");
            }
            Assert.AreEqual(0, Evidence(), "no fix E evidence");
            Assert.AreEqual(others, Others(world), "nothing else changes");
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.Previous(world, FixE)))
                Assert.AreEqual(fresh.World.Set<MissionSceneBindingEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == X.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings, Scene(),
                    "the Down's scene is a database's migrated only to set 9");

            world.GetService<IMigrator>().Migrate(FixE);
            world.ChangeTracker.Clear();
            Fixed("Up again");
            // The scene's JSON has braces: ExecuteSqlRaw reads {0} as a parameter, so they go doubled.
            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in X.UpSql())
                Run(sql);
            Fixed("a second Up");
            // The guards: a scene or a pool that holds neither set 9's / set 3's value nor fix E's is left alone both ways.
            Run($"UPDATE mission_scene_binding SET bindings = '{{}}' WHERE mission_id = {X.MissionId} AND content_revision = '{AshenMissionDataV1.Revision}';");
            Run("UPDATE spawnpool SET pos_x = -200 WHERE id = 790085;");
            foreach (var sql in X.DownSql().Concat(X.UpSql()))
                Run(sql);
            Assert.AreEqual("{}", Scene(), "a scene holding another value is not touched");
            Assert.AreEqual(-200.0, Pool(790085).Item1, "a pool at another place is not touched");
            Assert.AreEqual((X.RavagerPools[1].Now.X, X.RavagerPools[1].Now.Y, X.RavagerPools[1].Now.Z, (byte)2, (byte)2), Pool(790086), "the other pool: Down then Up");
            Run($"UPDATE mission_scene_binding SET bindings = '{X.SceneNow()}' WHERE mission_id = {X.MissionId} AND content_revision = '{AshenMissionDataV1.Revision}';");
            Run(FormattableString.Invariant($"UPDATE spawnpool SET pos_x = {X.RavagerPools[0].Now.X} WHERE id = 790085;"));
            world.ChangeTracker.Clear();
            Fixed("restored");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
