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
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using S2 = AshenFixS2V1;
    using Raid = AshenRaidTheConscriptionFacilityV1;
    using Blast = AshenBlastTheBiolabV1;
    using E = AshenConscriptionEntryTests;
    using Q = AshenConscriptionQueenTests;
    using B = AshenFixBTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen fix job S2 (AshenFixS2V1, migration 20271110002160): 1498's teleporter and Slocombe (markers 287, 3) and the hall pads
    /// 921, 925, 927 moved off the HQ "hall" (terrain the real client has no floor on: fix S) to Central Operations at the spiral
    /// ramp's foot. [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixS2Tests
    {
        private const string FixS2 = "20271110002160_Ashen_fix_s2";

        private const string Now = "3 19.800,103.620,-325.600 r6 | 4 17.000,103.620,-326.200 r8 | 921 -7.000,103.700,-325.000 | 925 -7.000,103.620,-318.000 | " +
                                   "927 21.500,103.620,-319.000 | evidence 1";
        private const string Before = "3 7.000,135.940,-358.000 r6 | 4 1.500,135.940,-359.000 r8 | 921 17.000,135.940,-370.000 | 925 -2.000,135.940,-379.000 | " +
                                      "927 16.500,135.940,-359.000 | evidence 0";

        private static readonly uint[] PadIds = S2.Pads.Select(pad => pad.Id).ToArray();

        private static string Mine(DbContext db)
        {
            string Marker(uint objective, uint indicator)
            {
                var marker = db.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == S2.Blast && entry.ContentRevision == AshenMissionDataV1.Revision &&
                    entry.ObjectiveId == objective && entry.IndicatorId == indicator);
                return FormattableString.Invariant($"{objective} {marker.PosX:0.000},{marker.PosY:0.000},{marker.PosZ:0.000} r{marker.Radius:0.###}");
            }
            var pads = db.Set<TeleporterEntry>().AsNoTracking().Where(entry => PadIds.Contains(entry.Id)).OrderBy(entry => entry.Id).AsEnumerable()
                .Select(entry => FormattableString.Invariant($"{entry.Id} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000}"));
            return string.Join(" | ", new[] { Marker(S2.TeleporterObjective, S2.TeleporterMarker), Marker(S2.SlocombeObjective, S2.SlocombeMarker) }.Concat(pads)) +
                   $" | evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == S2.Source)}";
        }

        private static string Scene(DbContext db) => db.Set<MissionSceneBindingEntry>().AsNoTracking()
            .Single(entry => entry.MissionId == S2.Blast && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;

        private static Vector3 V(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);
        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>Fix S2's rows: markers 287 and 3 on the teleporter and Slocombe (radii kept), the three pads, 1498's scene, its evidence row.</summary>
        [TestMethod]
        public void TheHallsPlacesStandInCentralOperations()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(Now, Mine(harness.World));
            Assert.AreEqual(S2.BlastNow(), Scene(harness.World), "1498's scene: fix M's, the teleporter and Slocombe moved");
            Assert.AreNotEqual(S2.BlastBefore(), S2.BlastNow());
            var scene = S2.BlastScene();
            Assert.AreEqual((V(S2.TeleporterAt), V(S2.SlocombeAt)), (V(scene.Actors[Blast.TeleporterRole].Position), V(scene.Actors[Blast.SlocombeRole].Position)));
        }

        /// <summary>
        /// The client floor: set 19's and set 20's hall places stand on the terrain heightmap alone (no placed mesh within 0.7 m of
        /// it). Fix S2's places stand on Central Operations' mesh floor (the pads' and Slocombe's on it, the teleporter's too), on one
        /// navmesh region with the ramp's foot, which is not joined to the charge's (the navmesh has the terrain plate, not the ramp:
        /// no NPC route crosses, none of 1498, 1551, 1499, 1501 moves one). The walk down the spiral from the deck and the walks in
        /// Central Operations pass the client-floor rules there and back at every start shift, also with the 2 m edge margin
        /// (terrain off over the ramp). Slocombe and the teleporter are within use range of the walk's end, the pad walks end inside
        /// their pad's 2 m trigger and keep 2.5 m (2D) off the other pads.
        /// </summary>
        [TestMethod]
        public void TheNewPlacesAreOnTheClientsFloorAndTheOldOnesWereNot()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, Raid.Facility)), meshes, 1);
            bool OnTerrain((Vector3 A, Vector3 B, Vector3 C) t) =>
                new[] { t.A, t.B, t.C }.All(v => geometry.Terrain.Height(v.X, v.Z) is { } h && MathF.Abs(h - v.Y) < 0.01f);
            List<(Vector3 A, Vector3 B, Vector3 C)> Mesh(Vector3 at) => F.Near(geometry, new List<Vector3> { at }, 0.3f)[0].Where(t => !OnTerrain(t)).ToList();

            var old = new List<Vector3> { V(S2.TeleporterBefore), V(S2.SlocombeBefore) }.Concat(S2.Pads.Select(pad => V(pad.Before))).ToList();
            foreach (var at in old)
            {
                Assert.AreEqual(at.Y, geometry.Terrain.Height(at.X, at.Z) ?? 0, 0.05, $"{at}: the terrain is the floor there");
                Assert.IsNull(F.FloorAt(Mesh(at), at, at.Y - 0.7f, at.Y + 0.7f), $"{at}: no mesh floor at the terrain's height");
            }

            var copy = Q.IntoFacility(harness);
            var nav = copy.NavMesh;
            var foot = V(S2.RampFoot);
            var places = new List<Vector3> { V(S2.TeleporterAt), V(S2.SlocombeAt), V(S2.Stand), foot }.Concat(S2.Pads.Select(pad => V(pad.At))).ToList();
            foreach (var at in places)
            {
                Assert.AreEqual(at.Y, F.FloorAt(Mesh(at), at, at.Y - 0.5f, at.Y + 0.5f) ?? 0, 0.1, $"{at}: Central Operations' mesh floor");
                Assert.AreEqual(at.Y, nav.GroundHeight(at + new Vector3(0, 1, 0)) ?? 0, 0.5, $"{at}: navmesh ground");
                Assert.IsTrue(nav.Connected(foot, at) ?? false, $"{at}: one navmesh region with the ramp's foot");
            }
            Assert.IsFalse(nav.Connected(new Vector3(Raid.ChargeAt.X, Raid.ChargeAt.Y, Raid.ChargeAt.Z), foot) ?? false, "the navmesh does not join Central Operations to the charge (the terrain plate)");

            foreach (var (classId, at, yaw) in new[]
                     {
                         ((int)Raid.TerminalClass, V(AshenFixSV1.TerminalAt), (float)Raid.TerminalOrientation), ((int)Blast.TeleporterClass, V(S2.TeleporterAt), 0f)
                     }.Concat(S2.Pads.Select(pad => ((int)AshenConscriptionEntryNpcsV1.PadClass, V(pad.At), 0f))))
                E.AddObject(geometry, meshes, classId, at, yaw);
            var legs = new (string Name, Vector3[] Corners)[]
            {
                ("the terminal to Central Operations", E.Legs["the terminal to Central Operations"]), ("Central Operations to Slocombe", E.Legs["Central Operations to Slocombe"]),
                ("Central Operations to the hall pad", E.Legs["Central Operations to the hall pad"]),
                ("Central Operations to the east-wing pad", Q.Legs["Central Operations to the east-wing pad"]),
                ("Central Operations to the fortress pad", Q.Legs["Central Operations to the fortress pad"])
            };
            Assert.AreEqual(V(AshenFixSV1.TerminalStand), legs[0].Corners[0], "the walk down starts where fix S's walk to the terminal ends");
            foreach (var (name, corners) in legs.Skip(1))
                Assert.AreEqual(foot, corners[0], $"{name} starts at the ramp's foot");
            Assert.AreEqual(foot, legs[0].Corners[^1], "the walk down ends at the ramp's foot");
            foreach (var (name, corners) in legs)
                foreach (var (line0, direction) in new[] { (corners.ToList(), "there"), (corners.Reverse().ToList(), "back") })
                    foreach (var shift in new[] { 0f, 0.13f, 0.27f, 0.4f })
                    {
                        var line = line0.ToList();
                        line[0] = Vector3.Lerp(line0[0], line0[1], shift / Vector2.Distance(new(line0[0].X, line0[0].Z), new(line0[1].X, line0[1].Z)));
                        Assert.IsNull(B.ClientFloorLine(geometry, line, line[^1], OverTheRamp, 0.5f, null, rampFoot: true, edgeMargin: B.PadDeckEdgeMargin), $"{name} {direction} +{shift}");
                    }

            var stand = V(S2.Stand);
            Assert.AreEqual(stand, legs[1].Corners[^1], "the walk to Slocombe ends at the stand");
            Assert.IsLessThan(3.05f, Vector3.Distance(stand, V(S2.TeleporterAt)), "the teleporter within the player's use range (3) of the stand");
            Assert.IsLessThan(3.05f, Vector3.Distance(stand, V(S2.SlocombeAt)), "Slocombe within talk range of the stand");
            foreach (var (pad, leg) in new[] { (AshenConscriptionEntryNpcsV1.HallPad, legs[2]), (AshenConscriptionQueenNpcsV1.HallWingPad, legs[3]), (AshenConscriptionQueenNpcsV1.HallFortressPad, legs[4]) })
            {
                Assert.IsLessThan(2f, Vector3.Distance(leg.Corners[^1], V(S2.PadAt(pad))), $"{leg.Name} ends inside pad {pad}'s trigger");
                foreach (var other in PadIds.Where(id => id != pad))
                {
                    var at = V(S2.PadAt(other));
                    for (var k = 1; k < leg.Corners.Length; k++)
                        Assert.IsGreaterThan(2.5f, Flat(leg.Corners[k - 1], leg.Corners[k], at), $"{leg.Name} keeps off pad {other}");
                }
            }
            foreach (var other in PadIds)
                for (var k = 1; k < legs[1].Corners.Length; k++)
                    Assert.IsGreaterThan(2.5f, Flat(legs[1].Corners[k - 1], legs[1].Corners[k], V(S2.PadAt(other))), $"the walk to Slocombe keeps off pad {other}");
        }

        /// <summary>Over the spiral ramp (the HQ "hall" box): the terrain there is no client floor (fix S), the walks take the meshes only.</summary>
        internal static bool OverTheRamp(Vector3 at) => at.X > -12 && at.X < 24 && at.Z > -386 && at.Z < -350;

        private static float Flat(Vector3 a, Vector3 b, Vector3 point)
        {
            var (from, to, at) = (new Vector2(a.X, a.Z), new Vector2(b.X, b.Z), new Vector2(point.X, point.Z));
            var along = to - from;
            var t = along.LengthSquared() == 0 ? 0 : Math.Clamp(Vector2.Dot(at - from, along) / along.LengthSquared(), 0, 1);
            return Vector2.Distance(at, from + along * t);
        }

        /// <summary>
        /// Fix S2's migration rolled back and forth, baselined at its own migration: Down gives back the hall's places (fix M's
        /// 1498 scene) and no fix S2 evidence, as a database migrated only to fix S holds them; nothing else changes; a second Down
        /// and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixS2RollsBackToFixS()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixS2);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.MissionId != S2.Blast)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable().Select(entry => $"{entry.MissionId}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => !(entry.MissionId == S2.Blast && (entry.IndicatorId == S2.TeleporterMarker || (entry.ObjectiveId == S2.SlocombeObjective && entry.IndicatorId == S2.SlocombeMarker))))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}/{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => !PadIds.Contains(entry.Id)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != S2.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(Now, Mine(world), "at fix S2");
            Assert.AreEqual(S2.BlastNow(), Scene(world), "fix S2's scene");

            var before = AshenMissionTests.Previous(world, FixS2);
            Assert.AreEqual("20271110002150_Ashen_fix_s", before, "after fix S");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
            {
                Assert.AreEqual(Mine(fresh.World), Mine(world), "Down: as a database migrated only to fix S");
                Assert.AreEqual(Scene(fresh.World), Scene(world), "Down: the scene as at fix S");
            }
            Assert.AreEqual(Before, Mine(world), "the hall's places");
            Assert.AreEqual(S2.BlastBefore(), Scene(world), "fix M's scene");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in S2.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Mine(world), "a second Down changes nothing");
            Assert.AreEqual(S2.BlastBefore(), Scene(world), "a second Down: the scene");

            world.GetService<IMigrator>().Migrate(FixS2);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "Up again");
            foreach (var sql in S2.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(S2.BlastNow(), Scene(world), "Up: fix S2's scene");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
