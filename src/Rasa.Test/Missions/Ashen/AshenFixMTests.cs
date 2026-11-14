using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using M = AshenFixMV1;
    using N = AshenAvernusOutpostNpcsV1;
    using Hive = AshenAvernusHiveNpcsV1;
    using Outpost = AshenAvernusOutpostTests;
    using Last = AshenPetrovichsLastStandV1;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen fix job M (AshenFixMV1, migration 20271110001930, to-fix row 48): the places and walks of sets 15-19 on the squad
    /// maps (Indra Caverns 2055, Avernus Outpost 2110, the Bane Conscription Facility 1988) measured against where the hostile
    /// pools' creatures really stand (AshenSpawnReach: 1500 seeded spawn draws a pool, 3D, against Creature.AggroRange 18 m);
    /// Petrovich, pad 916, Vandeweghe and pad 918 moved off upstream pools' spawn ground; Petrovich, Drazen and Petrovich at the
    /// top back on their own respawn time in a squad's copy.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixMTests
    {
        private const string FixM = "20271110001930_Ashen_fix_m";

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>
        /// The pools a place or a leg may come nearer to than AggroRange, and how near at the least (0: through their ground), each
        /// a rule break in docs/ashen-playtest.md (beyond the place's own pool and the pools its walks name as passed):
        /// <list type="bullet">
        /// <item>1360's own Watch Post pool 790565 on the way into the Watch Post (1395 requires 1360).</item>
        /// <item>1452's own Kretas 790603 in Central Administration (the area round him).</item>
        /// <item>Valve 1 in the processing room of upstream 531088 (set 16's valve rule break; its walk passes 531088).</item>
        /// <item>Kretas' teleporter 18.0 m from 531095's draws (Central Administration's west end; its walk passes at 12.4 m).</item>
        /// <item>Tananda, her area and the men's stop 16.6-17.1 m from upstream 531051's draws: the Cave In's floor is all within
        /// 20 m of them (the best spot 19.8 m, a 2-sample corner), and the walk there passes through them.</item>
        /// <item>The Bane's pool, 1537's own target.</item>
        /// <item>Peacock -> the field's charge 9.7 m from upstream 531068's draws (the yard, beside 531067 and 531081 it already
        /// passes: the field is the only way, 1497's own objective).</item>
        /// </list>
        /// </summary>
        internal static readonly Dictionary<string, Dictionary<uint, float>> Breaks = new()
        {
            ["leg: Diren to the Watch Post"] = new() { [790565] = 0 },
            ["area: 1452/2"] = new() { [790603] = 0 },
            ["place: valve 1"] = new() { [531088] = 0 },
            ["place: Kretas' teleporter"] = new() { [531095] = 17 },
            ["place: Tananda"] = new() { [531051] = 16 },
            ["area: 1536/1"] = new() { [531051] = 16 },
            ["place: the men's stop"] = new() { [531051] = 15.5f },
            ["place: the Bane's pool"] = new() { [AshenAvernusHiveNpcsV1.BanePool] = 0 },
            ["leg: the bottom to the Bane"] = new() { [AshenAvernusHiveNpcsV1.BanePool] = 0 },
            ["leg: Peacock to the field's charge"] = new() { [531068] = 8.5f }
        };

        /// <summary>The missions of sets 15-19 on the three maps (their areas are checked as places).</summary>
        private static readonly uint[] Missions = { 1559, 1360, 1395, 1532, 1447, 1451, 1452, 1473, 1533, 1535, 1536, 1537, 1581, 1497, 1498, 1500 };

        private sealed record Item(string Label, List<Vector3> Points, HashSet<uint> Allowed, bool FixM);

        private static string Key(string label) => label;

        /// <summary>Sets 15-19's places, legs (every 0.5 m) and areas on one map, each with the pools it may pass.</summary>
        private static List<Item> Items(WildernessRuntimeTestHarness harness, uint map)
        {
            var items = new List<Item>();
            HashSet<uint> Set(IEnumerable<uint> pools) => pools.Where(pool => pool != 0).ToHashSet();
            void Legs(Dictionary<string, Vector3[]> legs, Func<string, IEnumerable<uint>> passes)
            {
                foreach (var (leg, corners) in legs)
                    items.Add(new($"leg: {leg}", AshenSpawnReach.Samples(corners), Set(passes(leg)), false));
            }
            if (map == AshenIndraFacilityNpcsV1.Caverns)
            {
                foreach (var place in AshenIndraFacilityTests.Places())
                    items.Add(new($"place: {place.Label}", new() { place.At }, Set(new[] { place.Own }), false));
                var w15 = AshenIndraFacilityTests.Walks();
                Legs(AshenIndraFacilityTests.Legs, leg => w15.Where(walk => walk.Legs.Contains(leg)).Select(walk => walk.Own));
                foreach (var place in AshenGothaPlantTests.Places())
                    items.Add(new($"place: {place.Label}", new() { place.At }, Set(new[] { place.Own }), false));
                var w16 = AshenGothaPlantTests.Walks();
                Legs(AshenGothaPlantTests.Legs, leg => w16.Where(walk => walk.Legs.Contains(leg)).SelectMany(walk => walk.Passes));
            }
            else if (map == N.Outpost)
            {
                var fixM = new[] { "place: Petrovich in the graveyard", "place: the graveyard's pad", "place: the officer", "place: soldier 1", "place: soldier 2",
                    "leg: the graveyard pad to Petrovich", "place: Drazen's pad", "leg: to his pad", "place: Vandeweghe", "place: the queen pad", "leg: the queen pad to Vandeweghe", "area: 1533/1", "area: 1537/2" };
                foreach (var place in Outpost.Places())
                    items.Add(new($"place: {place.Label}", new() { place.At }, new(), fixM.Contains($"place: {place.Label}")));
                var w17 = Outpost.Walks();
                foreach (var (leg, corners) in Outpost.Legs)
                    items.Add(new($"leg: {leg}", AshenSpawnReach.Samples(corners), Set(w17.Where(walk => walk.Legs.Contains(leg)).SelectMany(walk => walk.Passes)), fixM.Contains($"leg: {leg}")));
                foreach (var place in AshenAvernusHiveTests.Places())
                    items.Add(new($"place: {place.Label}", new() { place.At }, new(), fixM.Contains($"place: {place.Label}")));
                var w18 = AshenAvernusHiveTests.Walks();
                foreach (var (leg, corners) in AshenAvernusHiveTests.Legs)
                    items.Add(new($"leg: {leg}", AshenSpawnReach.Samples(corners), Set(w18.Where(walk => walk.Legs.Contains(leg)).SelectMany(walk => walk.Passes)), fixM.Contains($"leg: {leg}")));
                // A pad's arrival: its origin and 1 m (the server's local teleport).
                items.Add(new("place: pad 915's arrival", new() { V(M.CavernPadAt) + Vector3.UnitY }, new(), true));
                items.Add(new("place: pad 916's arrival", new() { V(M.GraveyardPadAt) + Vector3.UnitY }, new(), true));
                items.Add(new("place: pad 918's arrival", new() { V(M.QueenPadAt) + Vector3.UnitY }, new(), true));
                foreach (var area in Areas(harness, map))
                    items.Add(area with { FixM = fixM.Contains(area.Label) });
                return items;
            }
            else
            {
                foreach (var place in AshenConscriptionEntryTests.Places())
                    items.Add(new($"place: {place.Label}", new() { place.At }, new(), place.Label == "the lab's charge"));
                var w19 = AshenConscriptionEntryTests.Walks();
                Legs(AshenConscriptionEntryTests.Legs, leg => w19.Where(walk => walk.Legs.Contains(leg)).SelectMany(walk => walk.Passes));
            }
            items.AddRange(Areas(harness, map));
            return items;
        }

        private static IEnumerable<Item> Areas(WildernessRuntimeTestHarness harness, uint map) =>
            harness.World.MissionAreaEntries.AsNoTracking().Where(row => row.MapContextId == map && row.ContentRevision == AshenMissionDataV1.Revision).AsEnumerable()
                .Where(row => Missions.Contains(row.MissionId))
                .Select(row => new Item($"area: {row.MissionId}/{row.AreaId}", new() { new((float)row.PosX, (float)row.PosY, (float)row.PosZ) }, new(), false));

        /// <summary>
        /// Every place, leg and area of sets 15-19 on the three squad maps against every hostile pool's spawn ground on its map
        /// (upstream and ours; AshenSpawnReach.Ground, 1500 seeded draws a pool): fix M's places and legs keep 20 m (AshenSpawnReach
        /// .Clearance) from every pool; every other one keeps AggroRange (18 m) from every pool but its own, the ones its walks name
        /// as passed and the documented breaks (Breaks, each at its least distance; each break's label must be an item and each
        /// of its pools within AggroRange of it, so a stale break fails). Set 17's Petrovich, pad 916's walk to him,
        /// set 18's Vandeweghe and queen pad failed it (0.3-4.0 m from 531050's and 531058's draws).
        /// </summary>
        [TestMethod]
        public void ThePlacesAndWalksKeepOutOfThePoolsReach()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(AshenSpawnReach.AggroRange, new Creature().AggroRange, "Creature.AggroRange");
            var failures = new List<string>();
            var report = new List<string>();
            var measured = new Dictionary<string, Dictionary<uint, float>>();
            foreach (var (map, into, hostile) in new (uint, Func<MapChannel>, Func<List<SpawnPoolEntry>>)[]
            {
                (AshenIndraFacilityNpcsV1.Caverns, () => AshenGothaPlantTests.IntoGotha(harness), () => AshenIndraFacilityTests.HostilePools(harness)),
                (N.Outpost, () => AshenAvernusHiveTests.IntoHive(harness), () => Outpost.HostilePools(harness)),
                (AshenConscriptionEntryNpcsV1.Facility, () => AshenConscriptionEntryTests.IntoFacility(harness), () => AshenConscriptionEntryTests.HostilePools(harness))
            })
            {
                var copy = into();
                var pools = hostile().Select(entry => entry.Id).ToList();
                var ground = AshenSpawnReach.Ground(copy, pools);
                Assert.HasCount(pools.Count, ground, $"every hostile pool of map {map} drawn");
                var items = Items(harness, map);
                foreach (var item in items)
                {
                    var reach = AshenSpawnReach.Reach(item.Points, ground);
                    var breaks = Breaks.GetValueOrDefault(item.Label) ?? new Dictionary<uint, float>();
                    if (Breaks.ContainsKey(item.Label))
                        measured[item.Label] = reach.ToDictionary(r => r.Pool, r => r.Near);
                    foreach (var (pool, near, at) in reach)
                    {
                        var where = FormattableString.Invariant($"{map} {item.Label}: {near:0.0} m from pool {pool}'s spawn draws at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})");
                        if (near < AshenSpawnReach.Clearance)
                            report.Add($"{AshenSpawnReach.Verdict(near)} {where}");
                        if (item.FixM)
                        {
                            if (near < AshenSpawnReach.Clearance)
                                failures.Add(where + " (fix M's: 20 m)");
                        }
                        else if (item.Allowed.Contains(pool))
                            continue;
                        else if (breaks.TryGetValue(pool, out var kept))
                        {
                            if (near < kept)
                                failures.Add(where + FormattableString.Invariant($" (its break keeps {kept})"));
                        }
                        else if (near < AshenSpawnReach.AggroRange)
                            failures.Add(where);
                    }
                }
                if (map == N.Outpost)
                {
                    // The regression: set 17's and 18's places on 531050's and 531058's spawn ground.
                    foreach (var (label, points, pool) in new (string, List<Vector3>, uint)[]
                    {
                        ("set 17's Petrovich", new() { V(M.PetrovichBefore) }, 531050),
                        ("set 17's pad 916 -> Petrovich", AshenSpawnReach.Samples(new[] { V(M.GraveyardPadBefore), V(M.PetrovichBefore) }), 531050),
                        ("set 18's Vandeweghe", new() { V(M.VandewegheBefore) }, 531058),
                        ("set 18's queen pad", new() { V(M.QueenPadBefore) }, 531058)
                    })
                    {
                        var near = AshenSpawnReach.Reach(points, new Dictionary<uint, List<Vector3>> { [pool] = ground[pool] }).Single().Near;
                        Assert.IsLessThan(AshenSpawnReach.AggroRange, near, $"{label} stood within {pool}'s reach (row 48)");
                    }
                }
                else if (map == AshenConscriptionEntryNpcsV1.Facility)
                {
                    // The regression: set 19's lab charge at the Atta Laboratory's door, within the Atta Imperial Guard's reach.
                    var near = AshenSpawnReach.Reach(new List<Vector3> { V(M.LabChargeBefore) }, new Dictionary<uint, List<Vector3>> { [520002] = ground[520002] }).Single().Near;
                    Assert.IsLessThan(AshenSpawnReach.AggroRange, near, "set 19's lab charge stood within 520002's reach");
                }
            }
            // No stale break: each names an item that is measured, and each of its pools is really within AggroRange of it.
            foreach (var (label, pools) in Breaks)
            {
                if (!measured.TryGetValue(label, out var near))
                {
                    failures.Add($"break {label}: no such place, leg or area");
                    continue;
                }
                foreach (var pool in pools.Keys)
                    if (!near.TryGetValue(pool, out var at) || at >= AshenSpawnReach.AggroRange)
                        failures.Add(FormattableString.Invariant($"break {label}: pool {pool} {(near.ContainsKey(pool) ? $"{at:0.0} m" : "not drawn")}, not within {AshenSpawnReach.AggroRange} m (a stale break)"));
            }
            Console.WriteLine(string.Join("\n", report));
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// Fix M's legs on the client floor with fix J's rules (AshenFixBTests.ClientFloorLine, rampFoot, 0.5 m half width), there
        /// and back, each with its start moved 0.13, 0.27 and 0.4 m: pad 916 down its ramp to Petrovich, Petrovich to the tunnel
        /// (1535), Drazen to pad 915's ramp and up it, the queen pad down its ramp to Vandeweghe, Vandeweghe down to the bottom; each
        /// of fix M's corners to the next by a complete navmesh path. Set 17's leg pad 916 -> Petrovich was a straight line off the pad's side.
        /// </summary>
        [TestMethod]
        public void FixMsLegsPassFixJsRules()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, N.Outpost)), meshes, 1);
            var onPads = AddPads(harness, geometry, meshes);
            var nav = Outpost.IntoOutpost(harness).NavMesh;
            var legs = new[]
            {
                Outpost.Legs["the graveyard pad to Petrovich"], Outpost.Legs["the graveyard to the tunnel"], Outpost.Legs["to his pad"],
                AshenAvernusHiveTests.Legs["the queen pad to Vandeweghe"], AshenAvernusHiveTests.Legs["down to the bottom 1"]
            };
            Assert.AreEqual(V(M.GraveyardPadAt), legs[0][0], "from pad 916");
            Assert.AreEqual(V(M.PetrovichAt), legs[0][^1], "to Petrovich");
            Assert.AreEqual(V(M.PetrovichAt), legs[1][0], "from Petrovich");
            Assert.AreEqual(V(M.CavernPadAt), legs[2][^1], "to pad 915");
            Assert.AreEqual(V(M.QueenPadAt), legs[3][0], "from pad 918");
            Assert.AreEqual(V(M.VandewegheAt), legs[3][^1], "to Vandeweghe");
            Assert.AreEqual(V(M.VandewegheAt), legs[4][0], "from Vandeweghe");
            var failures = new List<string>();
            foreach (var corners0 in legs)
            {
                var corners = onPads(corners0);
                var name = FormattableString.Invariant($"({corners[0].X:0}, {corners[0].Z:0})->({corners[^1].X:0}, {corners[^1].Z:0})");
                var first = Vector2.Distance(new(corners[0].X, corners[0].Z), new(corners[1].X, corners[1].Z));
                foreach (var shift in new[] { 0f, 0.13f, 0.27f, 0.4f }.Where(shift => shift == 0 || first > shift + 0.05f))
                {
                    var there = corners.ToList();
                    there[0] = Vector3.Lerp(corners[0], corners[1], shift / first);
                    foreach (var (line, direction) in new[] { (there, "there"), (Enumerable.Reverse(there).ToList(), "back") })
                        if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, 0.5f, new List<Vector3>(), rampFoot: true) is { } why)
                            failures.Add($"{name} {direction} +{shift}: {why}");
                }
                // The navmesh: fix M's new corners (the first four; the rest of the bottom leg is set 18's player walk).
                for (var k = 1; k < Math.Min(corners.Length, 4); k++)
                {
                    var path = nav.FindPath(nav.Nearest(corners[k - 1]) ?? corners[k - 1], corners[k], out var complete);
                    if (!complete || path == null || path.Count == 0 || Vector2.Distance(new(path[^1].X, path[^1].Z), new(corners[k].X, corners[k].Z)) > 0.5f)
                        failures.Add($"{name}: no navmesh path to corner {k}");
                }
            }
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The pads 915-920 (class 25302, prop_atta_teleporter_v01.geo) put into the map's geometry at their live rows (place,
        /// rotation), as AshenConscriptionEntryTests does for its 8044 pads: the client map places none, so without them a walk
        /// onto a pad was checked against the bare ground under it. The pad's mesh has collision: the platform
        /// (1.13 m up, x -1.4..1.4, z -2.2..2.2), its ramp to the south (-z), and a wall 7.2 m high round its west, north and east
        /// (a horseshoe from (-3.3, -2.7) by (-4.6, 1.0), (-3.4, 6.5) and (0, 9.8) to (3.3, -2.7), local; the probe, 2026-11-13),
        /// open only on the ramp's side. Returns the lift for a leg: a
        /// corner on a pad's point (its base, as the rows and the legs give it) goes up to the platform's top (the mesh's highest
        /// point within 1 m of its middle), where a player stands after the pad's arrival and before he walks down the ramp.
        /// </summary>
        internal static Func<IReadOnlyList<Vector3>, Vector3[]> AddPads(WildernessRuntimeTestHarness harness, MapGeometry geometry, MeshLibrary meshes)
        {
            var mesh = meshes.GetForClass((int)N.PadClass);
            Assert.IsTrue(mesh is { HasCollision: true }, "the 25302 pad's mesh has collision");
            var top = mesh.Vertices.Where(point => MathF.Abs(point.X) < 1 && MathF.Abs(point.Z) < 1).Max(point => point.Y);
            Assert.IsTrue(top is > 1f and < 1.3f, $"the platform's top {top} m up (1.1-1.22 m, the probe)");
            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.Id >= M.CavernPad && row.Id <= 920 && row.MapContextId == N.Outpost).ToList();
            Assert.HasCount(6, rows, "pads 915-920");
            foreach (var row in rows)
            {
                Assert.AreEqual(N.PadClass, row.ClassId, $"pad {row.Id}'s class");
                AshenConscriptionEntryTests.AddObject(geometry, meshes, (int)row.ClassId, new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ), (float)row.Rotation);
            }
            var bases = rows.Select(row => new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ)).ToList();
            return corners => corners.Select(corner => bases.Any(pad => Vector3.Distance(pad, corner) < 0.01f) ? corner + new Vector3(0, top, 0) : corner).ToArray();
        }

        /// <summary>
        /// Whether a leg end at a pad (class 25302, rotation 0) comes on or goes off by its ramp: the corner next to the pad on the
        /// ramp's line, within 0.75 m east or west of the pad's middle and 5.5-7.5 m south (-z) of it (the platform drops on its
        /// other sides; the real client let no one off pad 915's west side, row 48, ESTIMATE of the ramp from the pad's geometry).
        /// </summary>
        internal static bool ByTheRamp(Vector3 pad, Vector3 next) => MathF.Abs(next.X - pad.X) <= 0.75f && pad.Z - next.Z is >= 5.5f and <= 7.5f;

        /// <summary>
        /// Every leg of sets 17 and 18 that starts or ends at one of the pads 915-920 (where they stand now) comes on and goes off
        /// by the pad's ramp only (ByTheRamp), both ways (a leg is walked there and back), and every pad is walked to or from. A west
        /// or east approach is refused: set 17's leg onto pad 915 from Drazen (from the west) and its leg off pad 916 (to the
        /// south-east, straight off the platform's side), and made-up east and west ones.
        /// </summary>
        [TestMethod]
        public void ThePadsAreWalkedOnAndOffByTheirRampsOnly()
        {
            var pads = new List<(uint Id, Vector3 At)> { (M.CavernPad, V(M.CavernPadAt)), (M.GraveyardPad, V(M.GraveyardPadAt)) };
            pads.AddRange(AshenAvernusHiveTests.LivePads.Select(pad => (pad.Id, V(pad.At))));
            Assert.HasCount(6, pads);
            var failures = new List<string>();
            var reached = new HashSet<uint>();
            foreach (var (leg, corners) in Outpost.Legs.Concat(AshenAvernusHiveTests.Legs))
                foreach (var (end, next) in new[] { (corners[0], corners[1]), (corners[^1], corners[^2]) })
                    foreach (var (id, at) in pads.Where(pad => Vector3.Distance(pad.At, end) < 0.01f))
                    {
                        reached.Add(id);
                        if (!ByTheRamp(at, next))
                            failures.Add(FormattableString.Invariant($"{leg}: pad {id}'s neighbour corner ({next.X}, {next.Z}) is off its ramp's line"));
                    }
            CollectionAssert.AreEquivalent(pads.Select(pad => pad.Id).ToArray(), reached.ToArray(), "every pad walked to or from");
            Assert.IsEmpty(failures, string.Join("; ", failures));

            Assert.IsFalse(ByTheRamp(V(M.CavernPadBefore), new Vector3(57.5f, 478.62f, -315f)), "set 17: onto pad 915 from the west (Drazen)");
            Assert.IsFalse(ByTheRamp(V(M.GraveyardPadBefore), V(M.PetrovichBefore)), "set 17: off pad 916 to the south-east");
            var pad = V(M.CavernPadAt);
            Assert.IsFalse(ByTheRamp(pad, pad + new Vector3(-6.5f, 0, 0)), "from the west");
            Assert.IsFalse(ByTheRamp(pad, pad + new Vector3(6.5f, 0, 0)), "from the east");
            Assert.IsFalse(ByTheRamp(pad, pad + new Vector3(0, 0, 6.5f)), "from the north");
            Assert.IsTrue(ByTheRamp(pad, pad + new Vector3(0, 0, -6.5f)), "from the south: the ramp");
        }

        /// <summary>
        /// The rows: Petrovich's pool, pads 916 and 918, 1533's area and 1537's area 2 and markers 2-3 at fix M's places; the three
        /// scenes fix M's (and otherwise sets 17's and 18's); 1533's names Petrovich and Drazen, 1535's Petrovich at the top as
        /// public-spawn actors; 1535's first ambush man and 1497's front field on the navmesh's reach (MissionGroundTests); 1498's
        /// lab charge and its marker moved (fix L's scene otherwise); fix M's five evidence rows.
        /// </summary>
        [TestMethod]
        public void TheRowsAreFixMs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            static Vector3 At(double x, double y, double z) => new((float)x, (float)y, (float)z);
            var pool = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == M.Petrovich);
            Assert.IsLessThan(0.001f, Vector3.Distance(V(M.PetrovichAt), At(pool.PosX, pool.PosY, pool.PosZ)), "Petrovich's pool");
            Assert.AreEqual((N.Outpost, M.Petrovich, 0.0, 20U), (pool.MapContextId, pool.Creature1Id, pool.Radius, pool.RespawnTime), "set 17's pool otherwise (2 s)");
            foreach (var (id, at) in new[] { (M.CavernPad, M.CavernPadAt), (M.GraveyardPad, M.GraveyardPadAt), (M.QueenPad, M.QueenPadAt) })
            {
                var pad = world.TeleporterEntries.AsNoTracking().Single(entry => entry.Id == id);
                Assert.IsLessThan(0.001f, Vector3.Distance(V(at), At(pad.PosX, pad.PosY, pad.PosZ)), $"pad {id}");
                Assert.AreEqual((N.Outpost, 0.0), (pad.MapContextId, pad.Rotation), $"pad {id}: rotation 0, its ramp to the south");
            }
            MissionAreaEntry Area(uint mission, uint area) => world.MissionAreaEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.AreaId == area);
            var graveyard = Area(M.LastStand, M.GraveyardArea);
            Assert.IsLessThan(0.001f, Vector3.Distance(V(M.PetrovichAt), At(graveyard.PosX, graveyard.PosY, graveyard.PosZ)), "1533's area round Petrovich");
            var colonel = Area(M.Queen, M.ColonelArea);
            Assert.IsLessThan(0.001f, Vector3.Distance(V(M.VandewegheAt), At(colonel.PosX, colonel.PosY, colonel.PosZ)), "1537's area round Vandeweghe");
            foreach (var (objective, indicator) in new[] { (M.ColonelPlaceObjective, M.ColonelPlaceMarker), (M.ColonelObjective, M.ColonelMarker) })
            {
                var marker = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == M.Queen && entry.ContentRevision == revision && entry.ObjectiveId == objective);
                Assert.AreEqual(indicator, marker.IndicatorId, $"1537.{objective}'s marker");
                Assert.IsLessThan(0.001f, Vector3.Distance(V(M.VandewegheAt), At(marker.PosX, marker.PosY, marker.PosZ)), $"1537.{objective}'s marker on him");
            }
            string Scene(uint mission) => world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision).Bindings;
            Assert.AreEqual(M.LastStandNow(), Scene(M.LastStand), "1533's scene");
            Assert.AreEqual(M.RunNow(), Scene(M.Run), "1535's scene");
            Assert.AreEqual(M.QueenNow(), Scene(M.Queen), "1537's scene");
            Assert.AreEqual(AshenFixSV1.RaidNow(), Scene(M.Raid), "1497's scene (fix M's, the terminal moved by fix S since)");
            Assert.AreEqual(AshenFixS2V1.BlastNow(), Scene(M.Blast), "1498's scene (fix M's, the teleporter and Slocombe moved by fix S2 since)");
            Assert.AreNotEqual(M.BlastBefore(), M.BlastNow());
            var lab = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == M.Blast && entry.ContentRevision == revision && entry.ObjectiveId == M.LabChargeObjective);
            Assert.AreEqual(M.LabChargeMarker, lab.IndicatorId, "1498.5's marker");
            Assert.IsLessThan(0.001f, Vector3.Distance(V(M.LabChargeAt), At(lab.PosX, lab.PosY, lab.PosZ)), "1498.5's marker on the charge");
            Assert.AreNotEqual(M.RaidBefore(), M.RaidNow());
            Assert.AreNotEqual(M.LastStandBefore(), M.LastStandNow());
            Assert.AreNotEqual(M.RunBefore(), M.RunNow());
            Assert.AreNotEqual(M.QueenBefore(), M.QueenNow());
            foreach (var id in new[] { M.Petrovich, M.Drazen, M.PetrovichTop })
                Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(id), $"{id}: a scene names his pool");
            Assert.IsFalse(SpawnPoolManager.Instance.IsNamedPublicSpawn(N.Karter), "Karter (106 m from every pool's draws) not");
            Assert.AreEqual(4, world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == revision && entry.SourceUri == M.Source &&
                entry.EvidenceId == M.Evidence && (entry.MissionId == M.LastStand || entry.MissionId == M.Run || entry.MissionId == M.Queen || entry.MissionId == M.Raid)), "the evidence rows");
            Assert.AreEqual(1, world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == revision && entry.SourceUri == M.Source &&
                entry.EvidenceId == M.BlastEvidence && entry.MissionId == M.Blast), "1498's evidence row (fix L has its 4)");
        }

        /// <summary>
        /// In the squad's copy (its pools come back only 45 min after their last death) Petrovich in the graveyard, Drazen and
        /// Petrovich at the top, each killed, are back on their own pools' respawn time (2 s; the scenes name them,
        /// SpawnPoolManager.ReturnsOnOwnTime): the row 48 stall, Petrovich killed by the graveyard's Atta, does not hold 1533 and 1535.
        /// Then pad 915 sets the player down outside 1533's area, the walk down pad 916's ramp to Petrovich in 0.5 m steps enters it
        /// (MissionAreaService), and the turn-in at the Petrovich who came back pays.
        /// </summary>
        [TestMethod]
        public void PetrovichAndDrazenComeBackOnTheirOwnTime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = Outpost.IntoOutpost(harness);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");
            foreach (var id in new[] { M.Petrovich, M.Drazen, M.PetrovichTop })
            {
                Spawn(copy, id);
                Kill(harness, copy, Npc(copy, id));
                Creature Standing() => Here(copy).FirstOrDefault(creature => creature.SpawnPool?.DbId == id && creature.State != CharacterState.Dead && creature.Npc != null);
                Assert.IsNull(Standing(), $"{id} down");
                var all = copy.SpawnPools.ToArray();
                try
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.Add(all.Single(candidate => candidate.DbId == id));
                    for (var second = 1; second <= 10; second++)
                    {
                        harness.UtcNow = harness.UtcNow.AddSeconds(1);
                        SpawnPoolManager.Instance.SpawnPoolWorker(copy, 1000);
                    }
                }
                finally
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.AddRange(all);
                }
                harness.Drain();
                Assert.IsNotNull(Standing(), $"{id} back in the squad's copy on his own respawn time");
            }

            var drazen = Giver(copy, M.Drazen);
            Accept(harness, drazen, Last.MissionId);
            Tick(harness, copy, 250);
            var service = new MissionAreaService(() => harness.Manager);
            var arrival = V(M.GraveyardPadAt) + Vector3.UnitY;
            harness.MoveTo(arrival);
            service.RecordAcceptedMovement(harness.Client, arrival + new Vector3(0, 0, 0.1f), arrival);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Last.MissionId, Last.InTheGraveyard), "the pad's arrival outside the area");
            var walk = AshenSpawnReach.Samples(Outpost.Legs["the graveyard pad to Petrovich"]);
            for (var k = 1; k < walk.Count; k++)
            {
                harness.MoveTo(walk[k]);
                service.RecordAcceptedMovement(harness.Client, walk[k - 1], walk[k]);
                harness.Drain();
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Last.MissionId, Last.InTheGraveyard), "walked into the area round him");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, Giver(copy, M.Petrovich), Last.MissionId, 1, Last.Experience, Last.Credits);
        }

        /// <summary>
        /// Fix M's migration rolled back and forth, baselined at its own migration: Down gives back sets 17's and 18's rows (Petrovich's
        /// pool, the pads, the areas, 1537's markers, 1498's charge marker, the five scenes; 1498's as fix L left it) and no fix M evidence, as a database migrated only to before
        /// fix M holds them; nothing else changes; a second Down and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixMRollsBackToSets17And18s()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixM);
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var scenes = new[] { M.Raid, M.Blast, M.LastStand, M.Run, M.Queen };
            bool MarkerOfMine(MissionIndicatorEntry entry) => entry.ContentRevision == revision &&
                ((entry.MissionId == M.Queen && (entry.ObjectiveId == M.ColonelPlaceObjective || entry.ObjectiveId == M.ColonelObjective)) ||
                 (entry.MissionId == M.Blast && entry.ObjectiveId == M.LabChargeObjective));
            string Now(uint mission) => mission == M.Raid ? M.RaidNow() : mission == M.Blast ? M.BlastNow() : mission == M.LastStand ? M.LastStandNow() : mission == M.Run ? M.RunNow() : M.QueenNow();
            string Before(uint mission) => mission == M.Raid ? M.RaidBefore() : mission == M.Blast ? M.BlastBefore() : mission == M.LastStand ? M.LastStandBefore() : mission == M.Run ? M.RunBefore() : M.QueenBefore();
            string Mine(DbContext db) => string.Join(" | ",
                string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id == M.Petrovich).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"pool {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000}"))),
                string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => entry.Id == M.CavernPad || entry.Id == M.GraveyardPad || entry.Id == M.QueenPad).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"pad {entry.Id} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000}"))),
                string.Join("; ", db.Set<MissionAreaEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision &&
                        ((entry.MissionId == M.LastStand && entry.AreaId == M.GraveyardArea) || (entry.MissionId == M.Queen && entry.AreaId == M.ColonelArea)))
                    .OrderBy(entry => entry.MissionId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"area {entry.MissionId}/{entry.AreaId} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000}"))),
                string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().AsEnumerable().Where(MarkerOfMine)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId)
                    .Select(entry => FormattableString.Invariant($"marker {(entry.MissionId == M.Blast ? "1498." : "")}{entry.ObjectiveId}/{entry.IndicatorId} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000}"))),
                string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision && scenes.Contains(entry.MissionId))
                    .OrderBy(entry => entry.MissionId).AsEnumerable()
                    .Select(entry => entry.Bindings == Now(entry.MissionId) ? $"scene {entry.MissionId} fix M" : entry.Bindings == Before(entry.MissionId) ? $"scene {entry.MissionId} sets" : $"scene {entry.MissionId} other")),
                $"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == M.Source)}");
            string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id != M.Petrovich).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => entry.Id != M.CavernPad && entry.Id != M.GraveyardPad && entry.Id != M.QueenPad).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => !(entry.ContentRevision == revision && scenes.Contains(entry.MissionId)))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}:{entry.Bindings.Length}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.Set<MissionAreaEntry>().AsNoTracking().Where(entry => !(entry.ContentRevision == revision &&
                        ((entry.MissionId == M.LastStand && entry.AreaId == M.GraveyardArea) || (entry.MissionId == M.Queen && entry.AreaId == M.ColonelArea))))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.AreaId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}/{entry.AreaId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().AsEnumerable().Where(entry => !MarkerOfMine(entry))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId)
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != M.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var all = Mine(world);
            var others = Others(world);
            StringAssert.Contains(all, "pool 873.000,284.110,-219.000");
            StringAssert.Contains(all, "pad 915 48.000,477.120,-318.000; pad 916 888.000,281.860,-212.000; pad 918 306.000,218.110,-326.000");
            StringAssert.Contains(all, "marker 1498.5/1000000004 -213.000,96.010,-169.000; marker 2/1000000002 294.000,216.610,-330.000");
            StringAssert.Contains(all, "scene 1497 fix M; scene 1498 fix M; scene 1533 fix M; scene 1535 fix M; scene 1537 fix M | evidence 5");

            var before = AshenMissionTests.Previous(world, FixM);
            Assert.AreEqual("20271110001920_Ashen_fix_l", before, "after fix L");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            var down = Mine(world);
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Mine(fresh.World), down, "Down: as a database migrated only to before fix M");
            StringAssert.Contains(down, "pool 909.000,273.860,-246.000");
            StringAssert.Contains(down, "pad 915 64.000,480.730,-312.000; pad 916 890.000,278.110,-232.000; pad 918 340.000,238.110,-252.000");
            StringAssert.Contains(down, "area 1533/1 909.000,273.860,-246.000; area 1537/2 349.000,233.110,-260.000");
            StringAssert.Contains(down, "marker 1498.5/1000000004 -216.000,96.010,-160.000; marker 2/1000000002 349.000,233.110,-260.000; marker 3/1000000003 349.000,233.110,-260.000");
            StringAssert.Contains(down, "scene 1497 sets; scene 1498 sets; scene 1533 sets; scene 1535 sets; scene 1537 sets | evidence 0");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in M.DownSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(down, Mine(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixM);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(world), "Up again");
            foreach (var sql in M.UpSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
