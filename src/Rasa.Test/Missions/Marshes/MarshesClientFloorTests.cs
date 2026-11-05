using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    /// <summary>
    /// To-fix rows 24 and 25 (real client): Kaav at his row 21 place stood where the client has no floor, and a player next
    /// to him fell through the world; the row 21 check had used the server navmesh only, whose polygons reach past the
    /// client's collision. Why this test: every friendly Marshes NPC (pools 760300-760399), every friendly or talking scene
    /// creature, every scene object, and Overseer Nelg (row 25) of the Marshes missions stands on a client collision floor:
    /// the client files' terrain and entity collision meshes (MapGeometry, the navmesh builder's input) have a surface at
    /// most MaxOver under the place (an NPC: a walkable one; an object: any not vertical one) and none more than MaxUnder
    /// over it. Without the client's files the test is inconclusive: set RASA_CLIENT_DATA to the client's data folder
    /// (default ~/Games/Tabula Rasa 1.16.5.0/data).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesClientFloorTests
    {
        private const float MaxOver = 0.8f;        // the crates' origin stands 0.57 m over their floor, Dikah 0.74 m over his slope
        private const float MaxUnder = 0.6f;       // 961's feeders stand 0.53 m into their troughs (to-fix 19)
        private const float Walkable = 0.5f;       // the floor's normal: |y| at least this (60 degrees), for an NPC
        private const float NotVertical = 0.1f;    // for an object (a flower on a cliff)

        /// <summary>
        /// The places this test does not hold, each one reported (ESTIMATE, not moved). None now. Block C's two prisoners
        /// (548) were on this list (to-fix 24: no client floor under them, 2026-11-03; block B's had found theirs at 291.57,
        /// the hospital Sakai and 550's Brock were moved onto theirs for to-fix 32, 33); Marshes_on_the_navmesh moved them
        /// 3 m along the cell row onto the server navmesh, where the client has a floor (MarshesOnTheNavmeshV1).
        /// </summary>
        private static readonly HashSet<string> Reported = new();

        // The line of sight (to-fix 27, 32, 33: the real client's "You cannot see your target from here", pm 274, for a
        // target that stood on a client floor by this test's first check). ESTIMATE, the client's own test is not known: a
        // player's eye 1.6 m over a client floor, the target's middle (an NPC 1.5 m, an object 0.8 m over its place: 961's feeders sit 0.5 m into their troughs and were used in the real client).
        private const float Eye = 1.6f, NpcMiddle = 1.5f, ObjectMiddle = 0.8f;
        private const float Ring = 2f;              // the places round the target a player stands at
        private const int RingPlaces = 16;

        /// <summary>
        /// The places the line-of-sight test does not hold, reported (not moved, not in to-fix 27, 32, 33). None now: 864's
        /// west brazier (-236, 230.88, 28) stood inside the collision of the Forean statue next to it (25535 at -235, 230.8,
        /// 30) until Marshes_braziers_on_dry_floor moved both braziers out of the fountains (MarshesBraziersOnDryFloorV1).
        /// </summary>
        private static readonly HashSet<string> ReportedSight = new();

        [TestMethod]
        public void EveryMarshesNpcAndObjectStandsOnAClientFloor()
        {
            var dataDir = ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = Places(harness);
            var world = harness.World;
            var maps = world.MapInfoEntries.AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            using var meshes = new MeshLibrary(dataDir, MeshCsv());
            var failures = new List<string>();
            var reported = 0;
            foreach (var group in places.GroupBy(place => place.Map))
            {
                var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", maps[group.Key]), meshes, 1);
                var list = group.ToList();
                var (floors, ceilings) = Surfaces(geometry, list.Select(place => (place.At, place.Npc ? Walkable : NotVertical)).ToList());
                for (var i = 0; i < list.Count; i++)
                {
                    var (label, _, at, npc) = list[i];
                    var over = at.Y - floors[i];
                    var ok = over <= MaxOver && float.IsPositiveInfinity(ceilings[i]);
                    if (Reported.Contains(label))
                    {
                        reported++;
                        continue;
                    }
                    if (!ok)
                        failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}) on {maps[group.Key]}: ") +
                            (float.IsNegativeInfinity(floors[i]) ? "no floor" : FormattableString.Invariant($"{over:0.00} m over its floor")) +
                            (float.IsPositiveInfinity(ceilings[i]) ? "" : FormattableString.Invariant($", a surface {ceilings[i] - at.Y:0.00} m over it")));
                }
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
            Assert.AreEqual(Reported.Count, reported, "the reported places are still there");
        }

        /// <summary>
        /// To-fix 27, 32 and 33 (real client): 1873's third supply crate stood inside the east deck's railing, the hospital
        /// Sakai (550's giver) and 550's Brock at the edge of their floors; each was on a client floor by the test above, but
        /// from every place round it where the player stood the client said "You cannot see your target from here", and the
        /// mission could not go on. Why this test: every place of the test above is seen from a client floor next to it: of
        /// the RingPlaces places Ring m round it, at least one has a client floor (as the test above: a walkable surface at
        /// most MaxOver under the target's height and MaxUnder over it) from whose eye height the line to the target's middle
        /// meets no client collision. The three old places fail it (checked 2026-11-04 with the old values).
        /// </summary>
        [TestMethod]
        public void EveryMarshesNpcAndObjectIsSeenFromAClientFloorNextToIt()
        {
            var dataDir = ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var places = Places(harness);
            var maps = harness.World.MapInfoEntries.AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            using var meshes = new MeshLibrary(dataDir, MeshCsv());
            var failures = new List<string>();
            foreach (var group in places.GroupBy(place => place.Map))
            {
                var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", maps[group.Key]), meshes, 1);
                var list = group.Where(place => !Reported.Contains(place.Label) && !ReportedSight.Contains(place.Label)).ToList();
                var near = Near(geometry, list.Select(place => place.At).ToList(), Ring + 1);
                for (var i = 0; i < list.Count; i++)
                {
                    var (label, _, at, npc) = list[i];
                    var aim = at + new Vector3(0, npc ? NpcMiddle : ObjectMiddle, 0);
                    var seen = false;
                    for (var k = 0; k < RingPlaces && !seen; k++)
                    {
                        var angle = k * 2 * MathF.PI / RingPlaces;
                        var stand = at + new Vector3(MathF.Sin(angle) * Ring, 0, MathF.Cos(angle) * Ring);
                        var floor = FloorAt(near[i], stand, at.Y - MaxOver, at.Y + MaxUnder);
                        if (floor is { } y && Clear(near[i], stand with { Y = y + Eye }, aim))
                            seen = true;
                    }
                    if (!seen)
                        failures.Add(FormattableString.Invariant($"{label} at ({at.X}, {at.Y}, {at.Z}) on {maps[group.Key]}"));
                }
            }
            Assert.IsEmpty(failures, "not seen from a client floor " + Ring + " m round: " + string.Join("; ", failures));
            Assert.IsTrue(ReportedSight.All(label => places.Any(place => place.Label == label)), "the reported places are still there");
        }

        /// <summary>
        /// To-fix 31: A Taste of Their Own Medicine's Thrax (the Security Checkpoint's six and the hack's waves), Hax and the
        /// medic all stand on a client floor of the Logos Research Facility. Four wave spots stood at the tunnels' sides,
        /// over the client's void (its floor at that level is 6-8 m wide; the server navmesh is wider).
        /// </summary>
        [TestMethod]
        public void TasteOfTheirOwnMedicinesThraxStandOnAClientFloor()
        {
            var dataDir = ClientData();
            var csv = MeshCsv();

            using var harness = WildernessRuntimeTestHarness.Create();
            var scene = MarshesTasteOfTheirOwnMedicineV1.Scene();
            var places = scene.Actors.Values.Where(actor => actor.Kind == SceneActorKind.Creature && actor.Position != null)
                .Select(actor => (actor.Role, At: new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z))).ToList();
            Assert.AreEqual(MarshesTasteOfTheirOwnMedicineV1.Room.Length + MarshesTasteOfTheirOwnMedicineV1.Waves.Sum(wave => wave.Members.Length) + 2,
                places.Count, "every creature of the scene");
            var maps = harness.World.MapInfoEntries.AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            using var meshes = new MeshLibrary(dataDir, csv);
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", maps[MarshesTasteOfTheirOwnMedicineV1.Facility]), meshes, 1);
            var (floors, ceilings) = Surfaces(geometry, places.Select(place => (place.At, Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
                if (places[i].At.Y - floors[i] > MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add(FormattableString.Invariant($"{places[i].Role} at ({places[i].At.X}, {places[i].At.Y}, {places[i].At.Z}): ") +
                        (float.IsNegativeInfinity(floors[i]) ? "no floor" : FormattableString.Invariant($"{places[i].At.Y - floors[i]:0.00} m over its floor")));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// A body's collision capsule as the client's collision.pyo gives it for the body's mesh: (3, radius, y, radius), the
        /// capsule's segment half length y - radius (tabula_rasa.exe 0x9212b4).
        /// </summary>
        private readonly record struct Capsule(float Radius, float Y)
        {
            public float Half => MathF.Max(0, Y - Radius);
            public float Middle => Half;                        // feet + h
            public float Top => Radius + 2 * Half;              // feet + r + 2h
            public float Quarter => Half / 2;                   // feet + h / 2
            public float Height => 2 * Y;
        }

        // collision.pyo: HumanBaseMale (692, avatar_hum_m_nude_caucasian_v01.geo), the female avatar mesh, and Kirii's class
        // NPC_Forean_Shaman (7035, creature_forean_shaman_v01.geo).
        private static readonly Capsule HumanMale = new(0.33f, 1.12f), HumanFemale = new(0.32f, 1.09f), ForeanShaman = new(0.34f, 1.41f);

        /// <summary>
        /// The client's own sight test for a talk or a use: converse.pyo and useobject.pyo CheckAction say "You cannot see
        /// your target from here" (pm 274) when world.InLineOfSight(actor.body, target.body) fails; the server does not test
        /// it. tabula_rasa.exe: InLineOfSight 0x544590 -> 0x921c60 -> ClientCollisionManager 0x942e10 tests five lines between
        /// the bodies' points (0x9219d0, the heights from the capsule 0x9217f0): the actor's middle and top, the target's
        /// middle, top and quarter; actor middle to target middle, top and quarter, actor top to target middle and top. It
        /// sees when one line meets neither a collision body (the two bodies left out) nor a walkable surface (the
        /// ClientWalkableSurfaceManager 0x927fd0: the .map meshes' collision and the terrain). <paramref name="middle"/>:
        /// whether the middle-to-middle line, the lowest pair, is clear too.
        /// </summary>
        private static bool ClientSees(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 actorFeet, Capsule actor,
            Vector3 targetFeet, Capsule target, out bool middle)
        {
            Vector3 Up(Vector3 feet, float height) => feet + new Vector3(0, height, 0);
            var lines = new[]
            {
                (Up(actorFeet, actor.Middle), Up(targetFeet, target.Middle)), (Up(actorFeet, actor.Middle), Up(targetFeet, target.Top)),
                (Up(actorFeet, actor.Middle), Up(targetFeet, target.Quarter)), (Up(actorFeet, actor.Top), Up(targetFeet, target.Middle)),
                (Up(actorFeet, actor.Top), Up(targetFeet, target.Top))
            };
            var clear = lines.Select(line => Clear(triangles, line.Item1, line.Item2)).ToArray();
            middle = clear[0];
            return clear.Any(line => line);
        }

        /// <summary>
        /// Real client 2026-11-04 and 2026-11-05: Kirii talked only from his east side at MarshesQuestNpcsHoldV1's place and
        /// at MarshesKiriiInSightV1's; from the south-west, where the client's walk to him stopped at talk range (163.8,
        /// -227.8), and from (162-163, -226 to -227) the client said "You cannot see your target from here"; from (170, -225)
        /// he talked. MarshesKiriiInSightV1 had checked one line (eye 1.6 m to middle 1.5 m) from (162, -226). Why this test:
        /// by the client's own rule (ClientSees) Kirii is seen now from every place 2, 3, 4 and 5 m round him with a client
        /// floor and room for a player, by a male and a female avatar, each with the middle-to-middle line clear too; at
        /// MarshesKiriiInSightV1's place the ring fails, the client's rule fails from (163.8, -227.8) and (163, -227), and
        /// from (162, -226) the rock pile takes the middle-to-middle line (ESTIMATE: there only the actor's top lines are
        /// clear, within the tester's spot's error); from (170, -225) he is seen there too, as in the client.
        /// </summary>
        [TestMethod]
        public void KiriiIsSeenFromTheVillageSide()
        {
            var dataDir = ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var maps = harness.World.MapInfoEntries.AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            using var meshes = new MeshLibrary(dataDir, MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", maps[MarshesPreoDasVillageNpcsV1.MapContextId]), meshes, 1);
            static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
            var now = V(MarshesKiriiInOpenV1.Now);
            var inSight = V(MarshesKiriiInSightV1.Now);
            var first = V(MarshesQuestNpcsHoldV1.KiriiAt);
            Assert.AreEqual(inSight, V(MarshesKiriiInOpenV1.Before), "Marshes_kirii_in_open moves him from MarshesKiriiInSightV1's place");
            var near = Near(geometry, new List<Vector3> { now, inSight, first }, 8);

            // The ring: places 2-5 m round him with a client floor (as EveryMarshesNpcAndObjectStandsOnAClientFloor's) and room
            // for a player (no collision from the feet to the top of the capsule).
            (int Places, List<string> Unseen) Ring(int i, Vector3 at)
            {
                var places = 0;
                var unseen = new List<string>();
                foreach (var radius in new[] { 2f, 3f, 4f, 5f })
                    for (var k = 0; k < RingPlaces; k++)
                    {
                        var angle = k * 2 * MathF.PI / RingPlaces;
                        var stand = at + new Vector3(MathF.Sin(angle) * radius, 0, MathF.Cos(angle) * radius);
                        if (FloorAt(near[i], stand, at.Y - MaxOver, at.Y + MaxUnder) is not { } y)
                            continue;
                        var feet = stand with { Y = y };
                        if (!Clear(near[i], feet + new Vector3(0, 0.05f, 0), feet + new Vector3(0, HumanMale.Height, 0)))
                            continue;
                        places++;
                        var seen = true;
                        foreach (var avatar in new[] { HumanMale, HumanFemale })
                            seen &= ClientSees(near[i], feet, avatar, at, ForeanShaman, out var middle) && middle;
                        if (!seen)
                            unseen.Add(FormattableString.Invariant($"({feet.X:0.0}, {feet.Z:0.0})"));
                    }
                return (places, unseen);
            }
            var (places, unseen) = Ring(0, now);
            Assert.IsGreaterThanOrEqualTo(48, places, "a client floor round him");
            Assert.IsEmpty(unseen, "Kirii unseen by the client's rule from " + string.Join(", ", unseen));
            Assert.IsNotEmpty(Ring(1, inSight).Unseen, "MarshesKiriiInSightV1's place is not seen from all round");
            Assert.IsNotEmpty(Ring(2, first).Unseen, "nor MarshesQuestNpcsHoldV1's");

            // The tester's spots, a male avatar on the client floor there.
            bool Sees(int i, Vector3 at, float x, float z, out bool middle)
            {
                var spot = new Vector3(x, 0, z);
                var floor = FloorAt(near[i], spot, at.Y - 2, at.Y + 1);
                Assert.IsNotNull(floor, FormattableString.Invariant($"a client floor at ({x}, {z})"));
                return ClientSees(near[i], spot with { Y = floor.Value }, HumanMale, at, ForeanShaman, out middle);
            }
            Assert.IsFalse(Sees(1, inSight, 163.8f, -227.8f, out _), "from where the walk to him stopped");
            Assert.IsFalse(Sees(1, inSight, 163, -227, out _), "from (163, -227)");
            Sees(1, inSight, 162, -226, out var fromWest);
            Assert.IsFalse(fromWest, "from (162, -226) the rock pile takes the middle-to-middle line");
            Assert.IsTrue(Sees(1, inSight, 170, -225, out _), "from the east, as in the client");
        }

        private static string ClientData()
        {
            var dataDir = Environment.GetEnvironmentVariable("RASA_CLIENT_DATA");
            if (string.IsNullOrEmpty(dataDir))
                dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Games", "Tabula Rasa 1.16.5.0", "data");
            if (!Directory.Exists(Path.Combine(dataDir, "maps")))
                Assert.Inconclusive($"no client data at {dataDir} (set RASA_CLIENT_DATA)");
            return dataDir;
        }

        private static string MeshCsv() => Path.Combine(RepositoryRoot(), "src", "Rasa.NavMesh", "data", "entity_meshes.csv");

        /// <summary>Every friendly Marshes pool NPC, friendly or talking scene creature, scene object and Nelg: label, map, place, NPC.</summary>
        private static List<(string Label, uint Map, Vector3 At, bool Npc)> Places(WildernessRuntimeTestHarness harness)
        {
            MarshesPaludosTests.EnterMarshes(harness);
            var world = harness.World;
            bool Friendly(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory != TargetCategory.Hostile;
            var places = new List<(string Label, uint Map, Vector3 At, bool Npc)>();
            foreach (var pool in world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 760300 && pool.Id <= 760399).ToList())
                if (Friendly(pool.Creature1Id))
                    places.Add(($"pool {pool.Id}", pool.MapContextId, new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ), true));
            var scenes = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).ToList()
                .Select(entry => (entry.MissionId, Scene: System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(entry.Bindings, MissionContentCodec.Options)));
            foreach (var (mission, scene) in scenes)
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Position != null))
                {
                    var npc = actor.Kind == SceneActorKind.Creature;
                    if (npc && !Friendly(actor.TemplateId) && actor.Conversation == null && actor.TemplateId != MarshesSentryTowerNpcsV1.Nelg)
                        continue;
                    places.Add(($"{mission} {actor.Role}", scene.BoundMapContextId ?? scene.MapContextId,
                        new(actor.Position.X, actor.Position.Y, actor.Position.Z), npc));
                }
            Assert.IsGreaterThan(150, places.Count, "the places are read");
            Assert.IsTrue(places.Any(place => place.Label == $"{MarshesUnderTheRadarNelgV2.MissionId} {MarshesUnderTheRadarNelgV2.NelgRole}"), "Nelg is checked");
            return places;
        }

        /// <summary>Per place, the triangles whose box comes within <paramref name="reach"/> of it (across; any height).</summary>
        private static List<(Vector3 A, Vector3 B, Vector3 C)>[] Near(MapGeometry geometry, List<Vector3> places, float reach)
        {
            const float Cell = 8;
            var near = places.Select(_ => new List<(Vector3, Vector3, Vector3)>()).ToArray();
            var cells = new Dictionary<(int, int), List<int>>();
            (int, int) Key(float x, float z) => ((int)MathF.Floor(x / Cell), (int)MathF.Floor(z / Cell));
            for (var i = 0; i < places.Count; i++)
            {
                var (x0, z0) = Key(places[i].X - reach, places[i].Z - reach);
                var (x1, z1) = Key(places[i].X + reach, places[i].Z + reach);
                for (var cx = x0; cx <= x1; cx++)
                    for (var cz = z0; cz <= z1; cz++)
                    {
                        if (!cells.TryGetValue((cx, cz), out var inCell))
                            cells[(cx, cz)] = inCell = new List<int>();
                        inCell.Add(i);
                    }
            }
            var v = geometry.Vertices;
            Vector3 V(int i) => new(v[3 * i], v[3 * i + 1], v[3 * i + 2]);
            var t = geometry.Triangles;
            var added = new HashSet<int>();
            for (var k = 0; k < t.Count; k += 3)
            {
                Vector3 a = V(t[k]), b = V(t[k + 1]), c = V(t[k + 2]);
                float minX = MathF.Min(a.X, MathF.Min(b.X, c.X)), maxX = MathF.Max(a.X, MathF.Max(b.X, c.X));
                float minZ = MathF.Min(a.Z, MathF.Min(b.Z, c.Z)), maxZ = MathF.Max(a.Z, MathF.Max(b.Z, c.Z));
                var (x0, z0) = Key(minX, minZ);
                var (x1, z1) = Key(maxX, maxZ);
                added.Clear();
                for (var cx = x0; cx <= x1; cx++)
                    for (var cz = z0; cz <= z1; cz++)
                    {
                        if (!cells.TryGetValue((cx, cz), out var inCell))
                            continue;
                        foreach (var i in inCell)
                        {
                            var p = places[i];
                            if (p.X >= minX - reach && p.X <= maxX + reach && p.Z >= minZ - reach && p.Z <= maxZ + reach && added.Add(i))
                                near[i].Add((a, b, c));
                        }
                    }
            }
            return near;
        }

        /// <summary>The highest walkable surface at (x, z) from <paramref name="low"/> to <paramref name="high"/>, or null.</summary>
        private static float? FloorAt(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 at, float low, float high)
        {
            float? best = null;
            var from = at with { Y = high + 0.01f };
            foreach (var (a, b, c) in triangles)
            {
                var n = Vector3.Cross(b - a, c - a);
                if (n.LengthSquared() < 1e-12f || MathF.Abs(Vector3.Normalize(n).Y) < Walkable)
                    continue;
                if (Hit(from, -Vector3.UnitY, a, b, c) is { } down && from.Y - down >= low && (best == null || from.Y - down > best))
                    best = from.Y - down;
            }
            return best;
        }

        /// <summary>Whether the line from <paramref name="from"/> to <paramref name="to"/> meets none of the triangles.</summary>
        private static bool Clear(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 from, Vector3 to)
        {
            var length = Vector3.Distance(from, to);
            var direction = (to - from) / length;
            foreach (var (a, b, c) in triangles)
                if (Hit(from, direction, a, b, c) is { } at && at > 0.01f && at < length - 0.01f)
                    return false;
            return true;
        }

        /// <summary>Möller-Trumbore: how far along the ray it meets the triangle, or null.</summary>
        private static float? Hit(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 e1 = b - a, e2 = c - a;
            var p = Vector3.Cross(direction, e2);
            var det = Vector3.Dot(e1, p);
            if (MathF.Abs(det) < 1e-9f)
                return null;
            var s = origin - a;
            var u = Vector3.Dot(s, p) / det;
            if (u < 0 || u > 1)
                return null;
            var q = Vector3.Cross(s, e1);
            var w = Vector3.Dot(direction, q) / det;
            if (w < 0 || u + w > 1)
                return null;
            var distance = Vector3.Dot(e2, q) / det;
            return distance >= 0 ? distance : null;
        }

        /// <summary>
        /// Per place, the highest surface (normal |y| at least the place's limit) at most MaxUnder over it, and the lowest
        /// one from there to MaxUnder + 0.3 m over it (a place sunk into a floor).
        /// </summary>
        private static (float[] Floors, float[] Ceilings) Surfaces(MapGeometry geometry, List<(Vector3 At, float MinNormal)> places)
        {
            const float Cell = 8;
            var floors = places.Select(_ => float.NegativeInfinity).ToArray();
            var ceilings = places.Select(_ => float.PositiveInfinity).ToArray();
            var cells = new Dictionary<(int, int), List<int>>();
            (int, int) Key(float x, float z) => ((int)MathF.Floor(x / Cell), (int)MathF.Floor(z / Cell));
            for (var i = 0; i < places.Count; i++)
            {
                var key = Key(places[i].At.X, places[i].At.Z);
                if (!cells.TryGetValue(key, out var inCell))
                    cells[key] = inCell = new List<int>();
                inCell.Add(i);
            }
            var v = geometry.Vertices;
            Vector3 V(int i) => new(v[3 * i], v[3 * i + 1], v[3 * i + 2]);
            var t = geometry.Triangles;
            for (var k = 0; k < t.Count; k += 3)
            {
                Vector3 a = V(t[k]), b = V(t[k + 1]), c = V(t[k + 2]);
                float minX = MathF.Min(a.X, MathF.Min(b.X, c.X)), maxX = MathF.Max(a.X, MathF.Max(b.X, c.X));
                float minZ = MathF.Min(a.Z, MathF.Min(b.Z, c.Z)), maxZ = MathF.Max(a.Z, MathF.Max(b.Z, c.Z));
                var (x0, z0) = Key(minX, minZ);
                var (x1, z1) = Key(maxX, maxZ);
                for (var cx = x0; cx <= x1; cx++)
                    for (var cz = z0; cz <= z1; cz++)
                    {
                        if (!cells.TryGetValue((cx, cz), out var inCell))
                            continue;
                        var n = Vector3.Cross(b - a, c - a);
                        if (n.LengthSquared() < 1e-12f)
                            break;
                        n = Vector3.Normalize(n);
                        foreach (var i in inCell)
                        {
                            var p = places[i].At;
                            if (p.X < minX || p.X > maxX || p.Z < minZ || p.Z > maxZ || MathF.Abs(n.Y) < places[i].MinNormal)
                                continue;
                            var det = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
                            if (MathF.Abs(det) < 1e-12f)
                                continue;
                            var l1 = ((b.Z - c.Z) * (p.X - c.X) + (c.X - b.X) * (p.Z - c.Z)) / det;
                            var l2 = ((c.Z - a.Z) * (p.X - c.X) + (a.X - c.X) * (p.Z - c.Z)) / det;
                            var l3 = 1 - l1 - l2;
                            if (l1 < -1e-4f || l2 < -1e-4f || l3 < -1e-4f)
                                continue;
                            var y = l1 * a.Y + l2 * b.Y + l3 * c.Y;
                            if (y <= p.Y + MaxUnder)
                                floors[i] = MathF.Max(floors[i], y);
                            else if (y < p.Y + MaxUnder + 0.3f)
                                ceilings[i] = MathF.Min(ceilings[i], y);
                        }
                    }
            }
            return (floors, ceilings);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            Assert.IsNotNull(directory, "the repository root from the test output folder");
            return directory.FullName;
        }
    }
}
