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
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using P = Rasa.Test.Missions.Marshes.MarshesPreoDasTests;
    using X = AshenFixJV1;
    using N = AshenIndraFacilityNpcsV1;
    using Digging = AshenDiggingForTheTruthV1;
    using Facility = AshenIndraFacilityTests;

    /// <summary>
    /// Ashen fix job J (AshenFixJV1, migration 20271110001610): the Watch Post's east pool 790566 away from the facility's
    /// entrance tunnel (to-fix row 36), 1447's drills 4-6 out of the lava (to-fix row 38), and 1452's 3 opening 6 with 5 (set 16
    /// re-audit 1; its playthrough: AshenGothaPlantTests.DesistancesKretasCountsBeforeCentralAdministration).
    ///
    /// Why these tests: a pool whose creature can reach the only way in (by roam, aggro or chase) hits every player who passes;
    /// one off the Watch Post's floor or off its navmesh can not be killed for 1360's objective 3; a drill in or by the lava burns
    /// the player who shoots it; a Down that leaves a row unlike the sets' leaves the database unlike a set 16 one.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixJTests
    {
        private const string FixJ = "20271110001610_Ashen_fix_j";
        private const string MapName = "adv_arieki_ligo_ashendesert_indracaverns";

        /// <summary>The facility's entrance tunnel (fix I): its line along z -328 from its mouth at x 52 past the door (64) to behind it (86).</summary>
        internal static readonly Vector2 TunnelMouth = new(52, -328), TunnelEnd = new(86, -328);

        /// <summary>The walk from the Watch Post's west pool (a corner of set 15's walk in) up to the east pool's terrace (2026-11-12, client floor).</summary>
        internal static readonly Vector3[] ToThePool =
        {
            new(-36, 6.3f, -355), new(-31.1f, 9.78f, -317.2f), new(-31.1f, 11.78f, -305.2f), new(-32.3f, 12.58f, -303.6f), new(-35.1f, 13.78f, -301.6f),
            new(-37.1f, 14.38f, -301.6f), new(-50, 14.56f, -296)
        };


        [TestInitialize]
        public void Clock() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.RestoreClock();

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static float ToTunnel(Vector2 at)
        {
            var along = Math.Clamp(Vector2.Dot(at - TunnelMouth, Vector2.Normalize(TunnelEnd - TunnelMouth)), 0, Vector2.Distance(TunnelMouth, TunnelEnd));
            return Vector2.Distance(at, TunnelMouth + Vector2.Normalize(TunnelEnd - TunnelMouth) * along);
        }

        private static float ToLine(Vector2 at, IReadOnlyList<Vector3> line)
        {
            var best = float.MaxValue;
            for (var i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = new(line[i].X, line[i].Z), b = new(line[i + 1].X, line[i + 1].Z);
                var ab = b - a;
                var t = ab.LengthSquared() < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(at - a, ab) / ab.LengthSquared(), 0, 1);
                best = MathF.Min(best, Vector2.Distance(at, a + ab * t));
            }
            return best;
        }

        /// <summary>
        /// Row 36: the east pool's home, with its creature's roam (a radius-0 pool's: BehaviorManager.WanderZoneFallbackRadius round
        /// it) counted: past the leash (MaxChaseDistance, across the ground) from every point of the entrance tunnel and the door,
        /// so neither its stroll, its notice (aggro) nor a chase brings it in (a creature put at any point of the tunnel has chased
        /// too far: BehaviorManager.ChasedTooFar); 30 m plus the roam off every walk leg of 1360 and 1395 (set 15's Legs, all of
        /// them); 34 m plus the roam off every other set 15 place (AshenIndraFacilityTests.Places) and set 16's walk legs; 60 m outside
        /// every other hostile pool's circle of Indra Caverns.
        /// </summary>
        [TestMethod]
        public void TheEastPoolKeepsOffTheWayIn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = Facility.IntoCaverns(harness);
            var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == X.Pool);
            var home = new Vector2((float)row.PosX, (float)row.PosZ);
            var roam = BehaviorManager.WanderZoneFallbackRadius;
            Assert.AreEqual(0.0, row.Radius, "a point pool: its roam is the fallback zone");
            Assert.IsGreaterThan(BehaviorManager.MaxChaseDistance, ToTunnel(home), "past the leash from the tunnel");
            Assert.IsGreaterThan(BehaviorManager.MaxChaseDistance, Vector2.Distance(home, new(P.ToVector(AshenFixIV1.DoorAt).X, P.ToVector(AshenFixIV1.DoorAt).Z)), "and from the door");
            foreach (var (leg, corners) in Facility.Legs)
                Assert.IsGreaterThanOrEqualTo(30 + roam, ToLine(home, corners), $"{leg}: 30 m and the roam");
            foreach (var (leg, corners) in AshenGothaPlantTests.Legs)
                Assert.IsGreaterThanOrEqualTo(30 + roam, ToLine(home, corners), $"set 16's {leg}");
            foreach (var place in Facility.Places().Where(place => place.Own != X.Pool))
                Assert.IsGreaterThanOrEqualTo(34 + roam, Vector2.Distance(home, new(place.At.X, place.At.Z)), $"{place.Label}: 34 m and the roam");
            foreach (var other in Facility.HostilePools(harness).Where(pool => pool.Id != X.Pool))
                Assert.IsGreaterThanOrEqualTo(60f, Vector2.Distance(home, new((float)other.PosX, (float)other.PosZ)) - (float)other.Radius, $"60 m outside pool {other.Id}");

            Facility.Stand(copy, X.Pool);
            var ravager = P.Here(copy).First(one => one.SpawnPool?.DbId == X.Pool && one.State != CharacterState.Dead);
            Assert.IsLessThan(1f, Vector2.Distance(home, new(ravager.HomePos.Position.X, ravager.HomePos.Position.Z)), "its home is the pool's point");
            var kept = ravager.Position;
            try
            {
                for (var x = TunnelMouth.X; x <= TunnelEnd.X; x += 1)
                {
                    ravager.Position = new Vector3(x, 16, TunnelMouth.Y);
                    Assert.IsTrue(BehaviorManager.ChasedTooFar(ravager), $"in the tunnel at x {x}: chased too far");
                }
            }
            finally
            {
                ravager.Position = kept;
            }
        }

        /// <summary>
        /// Row 36: the east pool stands on the Watch Post's north-west terrace, on the client floor (within 1 m of its height),
        /// on the navmesh (within 1 m), with headroom; the navmesh joins it to the Watch Post (the creature walks out and home;
        /// a complete path from set 15's Watch Post point); a player walks to it from the west pool's corner of the walk in and
        /// back on the client floor at 0.5 m half width (AshenGothaPlantTests.WalkHalfWidth). Marker 223 (1360's 3) stands
        /// between the two pools, both pools' roam inside its 45 m, on the navmesh and a client floor.
        /// </summary>
        [TestMethod]
        public void TheEastPoolIsWalkedToAndMarked()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = Facility.IntoCaverns(harness).NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
            var at = V(X.PoolAt);
            var marker = V(X.MarkerAt);
            var near = F.Near(geometry, new List<Vector3> { at, marker }, 3);
            Assert.IsTrue(F.FloorAt(near[0], at, at.Y - 2, at.Y + 1.5f) is { } floor && MathF.Abs(floor - at.Y) < 0.1f, "on the client floor");
            Assert.IsTrue(nav.GroundHeight(at + new Vector3(0, 1, 0)) is { } ground && MathF.Abs(ground - at.Y) <= 1, "on the navmesh");
            Assert.IsFalse(near[0].Any(t => F.Hit(at + new Vector3(0, 0.3f, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 2.2f), "headroom");
            var start = nav.Nearest(Facility.Legs["Diren to the Watch Post"][^1]) ?? Facility.Legs["Diren to the Watch Post"][^1];
            Assert.IsTrue(nav.FindPath(start, at, out var joined) != null && joined, "the navmesh joins it to the Watch Post");
            Assert.AreEqual(V(N.WatchPostWestAt).X, ToThePool[0].X, 0.01f, "from the west pool's corner of the walk in");
            Assert.IsTrue(Facility.Legs["Diren to the Watch Post"].Any(corner => Vector3.Distance(corner, ToThePool[0]) < 0.01f), "a corner of set 15's walk in");
            Assert.IsLessThan(0.01f, Vector3.Distance(ToThePool[^1], at), "to the pool");
            var back = Enumerable.Reverse(ToThePool).ToList();
            Assert.IsNull(AshenFixBTests.ClientFloorLine(geometry, ToThePool, at, null, AshenGothaPlantTests.WalkHalfWidth), "there");
            Assert.IsNull(AshenFixBTests.ClientFloorLine(geometry, back, back[^1], null, AshenGothaPlantTests.WalkHalfWidth), "back");

            Assert.IsTrue(nav.GroundHeight(marker + new Vector3(0, 2, 0)) is { } markerGround && MathF.Abs(markerGround - marker.Y) <= 2, "marker 223 on the navmesh");
            Assert.IsTrue(F.FloorAt(near[1], marker, marker.Y - 2, marker.Y + 1) is { } markerFloor && MathF.Abs(markerFloor - marker.Y) <= 0.5f, "and a client floor");
            foreach (var pool in new[] { V(N.WatchPostWestAt), at })
                Assert.IsLessThanOrEqualTo((float)AshenThresholdV1.WatchPostMarkerRadius,
                    Vector2.Distance(new(marker.X, marker.Z), new(pool.X, pool.Z)) + BehaviorManager.WanderZoneFallbackRadius, "a pool and its roam in the marker");
        }

        /// <summary>The lava's reach in a column: LavaDamage.InLava anywhere from <paramref name="low"/> to <paramref name="high"/>, every 0.25 m (a surface counts from 0.5 m under it to 0.1 m over it).</summary>
        private static bool LavaInColumn(float x, float z, float low, float high)
        {
            for (var y = low; y <= high; y += 0.25f)
                if (LavaDamage.InLava(MapName, new Vector3(x, y, z)))
                    return true;
            return false;
        }

        /// <summary>The nearest column (0.5 m grid, 25 m round) with lava at any height from 12 m under <paramref name="at"/> to 4 m over it, across the ground.</summary>
        private static float NearestLava(Vector3 at)
        {
            var best = float.MaxValue;
            for (var dx = -25f; dx <= 25; dx += 0.5f)
                for (var dz = -25f; dz <= 25; dz += 0.5f)
                {
                    var d = MathF.Sqrt(dx * dx + dz * dz);
                    if (d < best && LavaInColumn(at.X + dx, at.Z + dz, at.Y - 12, at.Y + 4))
                        best = d;
                }
            return best;
        }

        /// <summary>
        /// Row 38: set 16's drills 4, 5 and 6 stood in the lava river (LavaDamage.InLava at their floors); fix J's stand on the
        /// mining area's client floor and the navmesh, and the nearest lava at any height in a column (from 12 m under the drill,
        /// the river 4.6 m under the platform, to 4 m over it; 0.5 m grid) is 10 m or more off across the ground: measured
        /// 2026-11-12 drill 4 17.1 m, drill 5 13.8 m, drill 6 11.3 m (each asserted within 0.5 m). Each inside the mining
        /// marker's circle (fix J's marker 134), 8 m or more from every other drill. No column of any drill walk leg of set 16
        /// (AshenGothaPlantTests.Legs: the way in from the north cavern and from drill 1 to each, sampled every 1/20 of a leg)
        /// holds lava from 12 m under to 4 m over the leg; the legs' 0.5 m half width walks both ways:
        /// AshenGothaPlantTests.ThePlacesAreWalkedToOnTheClientsFloor. Marker 134 stands on the platform's client floor, on no
        /// lava column.
        /// </summary>
        [TestMethod]
        public void TheDrillsStandOffTheLava()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = Facility.IntoCaverns(harness).NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, X.Drills.Select(drill => drill.Number).ToArray(), "drills 4-6 move");
            var measured = new Dictionary<int, float> { [4] = 17.1f, [5] = 13.8f, [6] = 11.3f };
            var mining = V(X.MiningMarkerAt);
            foreach (var (number, place) in X.Drills)
            {
                var before = P.ToVector(X.DrillBefore(number));
                var at = P.ToVector(place);
                var near = F.Near(geometry, new List<Vector3> { before, at }, 1);
                Assert.IsTrue(F.FloorAt(near[0], before, before.Y - 1, before.Y + 1) is { } old && LavaDamage.InLava(MapName, before with { Y = old }), $"set 16's drill {number} in the lava");
                Assert.IsTrue(F.FloorAt(near[1], at, at.Y - 1, at.Y + 1) is { } floor && MathF.Abs(floor - at.Y) < 0.1f, $"drill {number} on the client floor");
                Assert.IsTrue(nav.GroundHeight(at + new Vector3(0, 1, 0)) is { } ground && MathF.Abs(ground - at.Y) <= 1, $"drill {number} on the navmesh");
                var lava = NearestLava(at);
                Console.WriteLine(FormattableString.Invariant($"drill {number}: nearest lava {lava:0.0} m"));
                Assert.IsGreaterThanOrEqualTo(10f, lava, $"drill {number}: lava 10 m or more off");
                Assert.AreEqual(measured[number], lava, 0.5f, $"drill {number}: the measured distance");
                Assert.IsLessThan((float)Digging.MiningMarkerRadius, Vector2.Distance(new(at.X, at.Z), new(mining.X, mining.Z)), $"drill {number} in the mining marker's circle");
                for (var other = 1; other <= Digging.Drills; other++)
                    if (other != number)
                        Assert.IsGreaterThanOrEqualTo(6f, Vector3.Distance(at, P.ToVector(X.DrillAt(other))), $"drill {number} apart from drill {other} (their bodies: TheWalksEndOutsideTheSceneBodies)");
            }
            for (var number = 1; number <= Digging.Drills; number++)
                Assert.IsLessThan((float)Digging.MiningMarkerRadius - 2, Vector2.Distance(new(P.ToVector(X.DrillAt(number)).X, P.ToVector(X.DrillAt(number)).Z), new(mining.X, mining.Z)), $"drill {number} inside marker 134");
            var markerNear = F.Near(geometry, new List<Vector3> { mining }, 1)[0];
            Assert.IsTrue(F.FloorAt(markerNear, mining, mining.Y - 1, mining.Y + 1) is { } markerFloor && MathF.Abs(markerFloor - mining.Y) < 0.1f, "marker 134 on the platform's floor");
            Assert.IsTrue(nav.GroundHeight(mining + new Vector3(0, 2, 0)) is { } markerGround && MathF.Abs(markerGround - mining.Y) <= 2, "marker 134 on the navmesh");
            Assert.IsFalse(LavaInColumn(mining.X, mining.Z, mining.Y - 12, mining.Y + 4), "marker 134 over no lava");
            Assert.IsTrue(LavaInColumn((float)X.MiningMarkerBefore.X, (float)X.MiningMarkerBefore.Z, (float)X.MiningMarkerBefore.Y - 1, (float)X.MiningMarkerBefore.Y + 1), "set 16's marker 134 in the river");

            var legs = AshenGothaPlantTests.Legs.Where(leg => leg.Key.Contains("drill", StringComparison.Ordinal)).ToList();
            CollectionAssert.AreEquivalent(new[] { "the north cavern to drill 1", "drill 1 to drill 2", "drill 2 to drill 3", "drill 1 to drill 4", "drill 1 to drill 5", "drill 1 to drill 6" },
                legs.Select(leg => leg.Key).ToArray(), "the drill legs");
            foreach (var (label, corners) in legs)
                for (var k = 0; k + 1 < corners.Length; k++)
                    for (var i = 0; i <= 20; i++)
                    {
                        var p = Vector3.Lerp(corners[k], corners[k + 1], i / 20f);
                        Assert.IsFalse(LavaInColumn(p.X, p.Z, p.Y - 12, p.Y + 4), FormattableString.Invariant($"{label}: lava under or at ({p.X:0.0}, {p.Z:0.0})"));
                    }
            foreach (var number in new[] { 4, 5, 6 })
                Assert.AreEqual(P.ToVector(X.DrillAt(number)), AshenGothaPlantTests.Legs[$"drill 1 to drill {number}"][^1], $"the leg ends at drill {number}");
        }

        /// <summary>
        /// To-fix row 42 (real client 2026-11-12 10:4x, no data change): 1451's life support 1 (-172, 137.11, -416) stands on the
        /// south hall's upper deck, 5.5 m over the lower one (131.58) that runs under it. Set 16's last leg (-157, -414.5) >
        /// (-172, -416) started beside the ramp, 1.9 m north of its foot: ClientFloorLine took the ramp's side (131.48) for the
        /// start's floor and passed, the client stayed on the lower deck at (-171.2, 131.6, -415.9) and refused the use (pm 274,
        /// no line of sight). With the rampFoot rule (DeckMatch: start and end on the corners' own deck) that leg, read on the
        /// floor under its start (129.61), and the tester's stop fail; the way from the ramp's foot (the tester's, LS1 used
        /// 10:51; AshenGothaPlantTests' legs 10 and 11) passes there and back at 0.5 m.
        /// </summary>
        [TestMethod]
        public void LifeSupport1IsClimbedToFromTheRampsFoot()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
            var support = new Vector3(AshenSubversionV1.SupportsAt[0].X, AshenSubversionV1.SupportsAt[0].Y, AshenSubversionV1.SupportsAt[0].Z);
            string Walk(params Vector3[] corners) =>
                AshenFixBTests.ClientFloorLine(geometry, corners, corners[^1], null, AshenGothaPlantTests.WalkHalfWidth, rampFoot: true);
            StringAssert.Contains(Walk(new(-157, 130.6f, -414.5f), support), "another deck", "set 16's leg starts on the ramp's side");
            Assert.IsNotNull(Walk(new(-157, 129.61f, -414.5f), support), "set 16's leg from the floor under its start");
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, new[] { new Vector3(-157, 129.61f, -414.5f), support }, support), "also without the width");
            StringAssert.Contains(Walk(new(-171.2f, 131.6f, -415.9f), support), "ends on the floor", "the tester's stop under it");
            var way = AshenGothaPlantTests.Legs["the plant 10: (-157, -382) to the ramp's foot (-152, -416.4)"]
                .Concat(AshenGothaPlantTests.Legs["the plant 11: up the ramp from its foot (-152, -416.4) to (-172, -416)"].Skip(1)).ToArray();
            Assert.AreEqual(support, way[^1], "the way ends at life support 1");
            Assert.IsNull(Walk(way), "the way up from the ramp's foot");
            Assert.IsNull(Walk(way.Reverse().ToArray()), "and back down");
        }

        /// <summary>
        /// To-fix row 43 (real client 2026-11-12): the scene's objects with a solid body (their class's mesh collision, placed at the
        /// object's place and yaw) block the client where their top stands more than MaxClientStep over the floor; the map walk
        /// check sees the map's own geometry only. The old charge (-406, 176.31, -303) stood inside the Outpost Generator's body;
        /// fix J's charge and the walks' end places stand outside every body, the charge 1.5 m from its walk's end (the client's
        /// use from 14 m did nothing), the teleporters 3.5 m at most, the pad's end inside its circle. Drills 4-6's bodies keep
        /// 0.5 m and more from every other drill's (fix J's first places overlapped drill 2 and each other).
        /// </summary>
        [TestMethod]
        public void TheWalksEndOutsideTheSceneBodies()
        {
            var dataDir = F.ClientData();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var bodies = new List<(string Role, List<(Vector3 A, Vector3 B, Vector3 C)> Tris, Vector3 Lo, Vector3 Hi)>();
            foreach (var scene in new[] { AshenSubversionV1.Scene(), X.DesistanceScene(), X.DiggingScene() })
                foreach (var actor in scene.Actors.Values.Where(a => a.Kind == Rasa.Missions.Scenes.SceneActorKind.Object && a.Position != null))
                {
                    if (!meshes.TryGetMeshName((int)actor.TemplateId, out var name) || meshes.Get(name) is not { HasCollision: true } mesh)
                        continue;
                    var turn = Quaternion.CreateFromYawPitchRoll((float)actor.Orientation, 0, 0);
                    var at = new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z);
                    var tris = new List<(Vector3, Vector3, Vector3)>();
                    for (var k = 0; k < mesh.Triangles.Count; k += 3)
                        tris.Add((Vector3.Transform(mesh.Vertices[mesh.Triangles[k]], turn) + at, Vector3.Transform(mesh.Vertices[mesh.Triangles[k + 1]], turn) + at,
                            Vector3.Transform(mesh.Vertices[mesh.Triangles[k + 2]], turn) + at));
                    var points = tris.SelectMany(t => new[] { t.Item1, t.Item2, t.Item3 }).ToList();
                    bodies.Add((actor.Role, tris, points.Aggregate(Vector3.Min), points.Aggregate(Vector3.Max)));
                }
            string In(Vector3 at)
            {
                foreach (var (role, tris, lo, hi) in bodies)
                {
                    if (at.X < lo.X || at.X > hi.X || at.Z < lo.Z || at.Z > hi.Z || lo.Y > at.Y + AshenFixBTests.Head)
                        continue;
                    var from = new Vector3(at.X, 400, at.Z);
                    if (tris.Select(t => F.Hit(from, -Vector3.UnitY, t.A, t.B, t.C)).Where(d => d != null).Select(d => 400 - d.Value).DefaultIfEmpty(float.MinValue).Max() > at.Y + AshenFixBTests.MaxClientStep)
                        return role;
                }
                return null;
            }
            Assert.AreEqual(AshenDesistanceV1.GeneratorRole, In(V3(X.ChargeBefore)), "set 16's charge stood inside the generator's body");
            Assert.IsNull(In(V3(X.ChargeAt)), "fix J's charge stands outside it");
            var ends = new (string Name, Vector3 At, Vector3 Object, float Reach)[]
            {
                ("teleporter 1", AshenGothaPlantTests.BesideTeleporter1, V3(Digging.TeleportersAt[0]), 3.6f),
                ("teleporter 2", AshenGothaPlantTests.BesideTeleporter2, V3(Digging.TeleportersAt[1]), 3.6f),
                ("the pad", AshenGothaPlantTests.OnThePad, V(AshenDesistanceV1.PadAt), (float)AshenDesistanceV1.PadRadius),
                ("the charge", AshenGothaPlantTests.BesideTheCharge, V3(X.ChargeAt), 1.6f),
                ("Kretas' teleporter", AshenGothaPlantTests.BesideKretasTeleporter, V3(AshenDesistanceV1.TeleporterAt), 3f),
            };
            foreach (var (name, at, obj, reach) in ends)
            {
                Assert.IsNull(In(at), $"{name}: the walk's end outside every body");
                Assert.IsLessThanOrEqualTo(reach, Vector2.Distance(new(at.X, at.Z), new(obj.X, obj.Z)), $"{name}: within {reach} m");
            }
            // The drills' bodies on a 0.5 m grid: 1.5 m and more between fix J's and every other drill's.
            HashSet<(int, int)> Body(string role)
            {
                var (_, tris, lo, hi) = bodies.Single(b => b.Role == role);
                var cells = new HashSet<(int, int)>();
                for (var x = lo.X; x <= hi.X; x += 0.5f)
                    for (var z = lo.Z; z <= hi.Z; z += 0.5f)
                        if (In(new Vector3(x, lo.Y, z)) == role)
                            cells.Add(((int)MathF.Round(x * 2), (int)MathF.Round(z * 2)));
                return cells;
            }
            var drills = Enumerable.Range(1, 6).ToDictionary(n => n, n => Body(Digging.DrillRole(n)));
            foreach (var (number, _) in X.Drills)
                foreach (var other in Enumerable.Range(1, 6).Where(n => n != number))
                {
                    var gap = drills[number].Min(a => drills[other].Min(b => MathF.Sqrt((a.Item1 - b.Item1) * (a.Item1 - b.Item1) + (a.Item2 - b.Item2) * (a.Item2 - b.Item2)) / 2));
                    Assert.IsGreaterThanOrEqualTo(0.5f, gap, $"drill {number}'s body {gap:0.0} m from drill {other}'s");
                }
        }

        private static Vector3 V3(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);

        /// <summary>
        /// The rows: the pool at fix J's place and otherwise set 15's (the Ravager, one, radius 0, 120 s); marker 223 on the
        /// pools' middle, radius 45; 1447's scene with fix J's drills 4-6 and otherwise set 16's; markers 221 of objectives 9-11 on them;
        /// 1452's 3 with two more actions (5 reveals 6, 6 activates it), 5's own opening of 6 kept; fix J's three evidence rows.
        /// </summary>
        [TestMethod]
        public void TheRowsAreFixJs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var pool = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == X.Pool);
            Assert.IsLessThan(0.001f, Vector3.Distance(V(X.PoolAt), new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ)), "the pool's place");
            Assert.AreEqual((N.ThraxRavager, (byte)1, (byte)1, N.WatchPostRespawnTenths, 0.0, N.Caverns),
                (pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.RespawnTime, pool.Radius, pool.MapContextId), "set 15's pool otherwise");

            MissionIndicatorEntry Marker(uint mission, uint objective) => world.MissionIndicatorEntries.AsNoTracking()
                .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.ObjectiveId == objective);
            var watch = Marker(X.Threshold, X.WatchPostObjective);
            Assert.AreEqual((X.WatchPostMarker, AshenThresholdV1.WatchPostMarkerRadius), (watch.IndicatorId, watch.Radius), "marker 223");
            Assert.IsLessThan(0.01f, Vector3.Distance(V(X.MarkerAt), new((float)watch.PosX, (float)watch.PosY, (float)watch.PosZ)), "between the pools");
            var miningMarker = Marker(X.Digging, X.MiningObjective);
            Assert.AreEqual((X.MiningMarker, Digging.MiningMarkerRadius), (miningMarker.IndicatorId, miningMarker.Radius), "marker 134");
            Assert.IsLessThan(0.01f, Vector3.Distance(V(X.MiningMarkerAt), new((float)miningMarker.PosX, (float)miningMarker.PosY, (float)miningMarker.PosZ)), "the drills' middle");
            foreach (var (number, at) in X.Drills)
            {
                var drill = Marker(X.Digging, X.DrillObjective(number));
                Assert.AreEqual((X.DrillMarker, Digging.ObjectMarkerRadius), (drill.IndicatorId, drill.Radius), $"marker 221 of drill {number}");
                Assert.IsLessThan(0.01f, Vector3.Distance(P.ToVector(at), new((float)drill.PosX, (float)drill.PosY, (float)drill.PosZ)), $"on drill {number}");
            }

            var stored = world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(entry => entry.MissionId == X.Digging && entry.ContentRevision == revision).Bindings;
            Assert.AreEqual(X.DiggingSceneNow(), stored, "fix J's scene");
            var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(stored, MissionContentCodec.Options);
            foreach (var (number, at) in X.Drills)
            {
                var role = Digging.DrillRole(number);
                Assert.AreEqual(at, scene.Actors[role].Position, $"drill {number}");
                scene.Actors[role] = scene.Actors[role] with { Position = X.DrillBefore(number) };
            }
            Assert.AreEqual(X.DiggingSceneBefore(), JsonSerializer.Serialize(scene, MissionContentCodec.Options), "set 16's but drills 4-6");

            var actions = world.Set<MissionActionEntry>().AsNoTracking().Where(entry => entry.MissionId == X.Desistance && entry.ContentRevision == revision &&
                    (entry.ObjectiveId == X.OpenerObjective || entry.ObjectiveId == AshenDesistanceV1.Administration)).OrderBy(entry => entry.ObjectiveId).ThenBy(entry => entry.ActionId).AsEnumerable()
                .Select(entry => $"{entry.ObjectiveId}.{entry.ActionId}:{(MissionActionKind)entry.Kind}>{entry.TargetObjectiveId}/{entry.ObjectiveState}").ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "3.1:ConsumeMissionItem>/", "3.2:StartScenario>/", "3.3:RevealObjective>5/", "3.4:ActivateObjective>5/1",
                "3.5:RevealObjective>6/", "3.6:ActivateObjective>6/1", "5.1:RevealObjective>6/", "5.2:ActivateObjective>6/1"
            }, actions, "3 opens 5 and 6; 5 still opens 6");
            Assert.AreEqual(3, world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == revision && entry.SourceUri == X.Source &&
                ((entry.MissionId == X.Threshold && entry.EvidenceId == X.ThresholdEvidence) || (entry.MissionId == X.Digging && entry.EvidenceId == X.DiggingEvidence) ||
                 (entry.MissionId == X.Desistance && entry.EvidenceId == X.DesistanceEvidence))), "the evidence rows");
        }

        /// <summary>
        /// Fix J's migration rolled back and forth, baselined at its own migration: Down gives back set 15's pool and marker 223,
        /// set 16's 1447 scene and markers 221, 1452's actions without 3.5 and 3.6, and no fix J evidence, as a database migrated
        /// only to before fix J holds them; nothing else changes (the other pools, scenes, markers, actions and evidence); a
        /// second Down and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixJRollsBackToSet16s()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixJ);   // baselined at its own migration
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            string Mine(DbContext db) => string.Join(" | ",
                db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id == X.Pool).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"pool {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000}")).Single(),
                string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision &&
                        ((entry.MissionId == X.Threshold && entry.ObjectiveId == X.WatchPostObjective) || (entry.MissionId == X.Desistance && entry.ObjectiveId == X.GeneratorObjective) || (entry.MissionId == X.Digging && (entry.ObjectiveId == X.MiningObjective || (entry.ObjectiveId >= X.DrillObjective(4) && entry.ObjectiveId <= X.DrillObjective(6))))))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"marker {entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000},{entry.Radius:R}"))),
                db.Set<MissionSceneBindingEntry>().AsNoTracking().Single(entry => entry.MissionId == X.Digging && entry.ContentRevision == revision).Bindings is var scene &&
                    scene == X.DiggingSceneNow() ? "scene fix J" : scene == X.DiggingSceneBefore() ? "scene set 16" : "scene other",
                db.Set<MissionSceneBindingEntry>().AsNoTracking().Single(entry => entry.MissionId == X.Desistance && entry.ContentRevision == revision).Bindings is var charge &&
                    charge == X.DesistanceSceneNow() ? "1452 scene fix J" : charge == X.DesistanceSceneBefore() ? "1452 scene set 16" : "1452 scene other",
                $"actions {db.Set<MissionActionEntry>().AsNoTracking().Count(entry => entry.MissionId == X.Desistance && entry.ContentRevision == revision && entry.ObjectiveId == X.OpenerObjective)}",
                $"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == X.Source)}");
            string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id != X.Pool).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => !((entry.MissionId == X.Digging || entry.MissionId == X.Desistance) && entry.ContentRevision == revision))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}:{entry.Bindings.Length}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => !(entry.ContentRevision == revision &&
                        ((entry.MissionId == X.Threshold && entry.ObjectiveId == X.WatchPostObjective) || (entry.MissionId == X.Desistance && entry.ObjectiveId == X.GeneratorObjective) || (entry.MissionId == X.Digging && (entry.ObjectiveId == X.MiningObjective || (entry.ObjectiveId >= X.DrillObjective(4) && entry.ObjectiveId <= X.DrillObjective(6)))))))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<MissionActionEntry>().AsNoTracking().Where(entry => !(entry.MissionId == X.Desistance && entry.ObjectiveId == X.OpenerObjective))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.TransitionId).ThenBy(entry => entry.ActionId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.ObjectiveId}.{entry.TransitionId}.{entry.ActionId}:{entry.Kind}>{entry.TargetObjectiveId}"))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != X.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var all = Mine(world);
            var others = Others(world);
            StringAssert.Contains(all, "pool -50.000,14.560,-296.000");
            StringAssert.Contains(all, "scene fix J | 1452 scene fix J | actions 6 | evidence 3");

            var before = AshenMissionTests.Previous(world, FixJ);
            Assert.AreEqual(AshenMissionTests.GothaPlant, before, "after set 16");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            var down = Mine(world);
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Mine(fresh.World), down, "Down: as a database migrated only to before fix J");
            StringAssert.Contains(down, "pool 44.000,16.350,-335.000");
            StringAssert.Contains(down, "marker 1360.3/223 4.000,16.200,-345.500,45");
            StringAssert.Contains(down, "marker 1447.1/134 -311.000,85.860,245.000,40; marker 1447.9/221 -311.000,85.860,245.000,6; marker 1447.10/221 -312.000,85.860,227.000,6; marker 1447.11/221 -322.000,85.860,211.000,6");
            StringAssert.Contains(down, "scene set 16 | 1452 scene set 16 | actions 4 | evidence 0");
            StringAssert.Contains(down, "marker 1452.3/214 -401.000,176.310,-301.000,6");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in X.DownSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(down, Mine(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixJ);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(world), "Up again");
            foreach (var sql in X.UpSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
