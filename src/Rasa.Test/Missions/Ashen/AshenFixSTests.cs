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
    using S = AshenFixSV1;
    using Raid = AshenRaidTheConscriptionFacilityV1;
    using E = AshenConscriptionEntryTests;
    using B = AshenFixBTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen fix job S (AshenFixSV1, migration 20271110002150): 1497's terminal and its marker 251 moved off the HQ "hall" (terrain
    /// the real client has no floor on) onto the HQ tunnel's end deck (real client 2026-11-13: on set 19's corners the client
    /// walked down the tunnel ramp under the hall to (-1.97, 121.92, -371.58), 14 m under the terminal).
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixSTests
    {
        private const string FixS = "20271110002150_Ashen_fix_s";

        /// <summary>The real client's places at set 19's corners on the retry (20:02): the corner, where it stood.</summary>
        internal static readonly (Vector3 Corner, Vector3 Real)[] Retry =
        {
            (new(13f, 135.94f, -369f), new(12.76f, 134.9f, -369.66f)), (new(13.5f, 135.94f, -366f), new(13.38f, 132.92f, -366.57f)),
            (new(12f, 135.94f, -361f), new(12.19f, 130.42f, -361.77f)), (new(1.5f, 135.94f, -361.5f), new(2.23f, 126.69f, -361.52f)),
            (new(-2f, 135.94f, -362f), new(-1.24f, 125.6f, -361.91f)), (new(-2f, 135.94f, -372f), new(-1.97f, 121.92f, -371.58f))
        };

        private const string Now = "12.000,137.600,-379.500 r6 | evidence 1";
        private const string Before = "-4.000,135.940,-372.000 r6 | evidence 0";

        private static string Mine(DbContext db)
        {
            var marker = db.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == S.Raid && entry.ContentRevision == AshenMissionDataV1.Revision &&
                entry.ObjectiveId == S.TerminalObjective && entry.IndicatorId == S.TerminalMarker);
            return FormattableString.Invariant($"{marker.PosX:0.000},{marker.PosY:0.000},{marker.PosZ:0.000} r{marker.Radius:0.###} | ") +
                   $"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == S.Source)}";
        }

        private static string Scene(DbContext db) => db.Set<MissionSceneBindingEntry>().AsNoTracking()
            .Single(entry => entry.MissionId == S.Raid && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;

        private static Vector3 V(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);

        /// <summary>Fix S's terminal in 1497's scene and its marker 251 on it (radius kept), and its evidence row.</summary>
        [TestMethod]
        public void TheTerminalStandsOnTheTunnelsEndDeck()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(Now, Mine(harness.World));
            Assert.AreEqual(S.RaidNow(), Scene(harness.World), "1497's scene: fix M's, the terminal moved");
            Assert.AreNotEqual(S.RaidBefore(), S.RaidNow());
            Assert.AreEqual(V(S.TerminalAt), V(S.RaidScene().Actors[Raid.TerminalRole].Position));
        }

        /// <summary>
        /// The client floor: set 19's terminal and the hall corners stand on the terrain heightmap alone (no placed mesh within
        /// 0.7 m of it), and the real client's places on the retry lie on the tunnel ramp's mesh under them (within 0.5 m), 1-14 m
        /// down. Fix S's terminal and the walk's end stand on the deck's mesh, on the navmesh joined to the charge, no drop over
        /// EdgeDrop within 2 m; the walk from the charge passes the client-floor rules there and back at every start shift, its
        /// deck part (from mid-tunnel) also with the 2 m edge margin; the terminal is within 3 m of the walk's end.
        /// </summary>
        [TestMethod]
        public void TheNewPlaceIsOnTheClientsFloorAndTheOldOneWasNot()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, Raid.Facility)), meshes, 1);
            bool OnTerrain((Vector3 A, Vector3 B, Vector3 C) t) =>
                new[] { t.A, t.B, t.C }.All(v => geometry.Terrain.Height(v.X, v.Z) is { } h && MathF.Abs(h - v.Y) < 0.01f);
            List<(Vector3 A, Vector3 B, Vector3 C)> Mesh(Vector3 at) => F.Near(geometry, new List<Vector3> { at }, 0.3f)[0].Where(t => !OnTerrain(t)).ToList();

            foreach (var (corner, real) in Retry.Append((V(S.TerminalBefore), Retry[^1].Real)))
            {
                Assert.AreEqual(corner.Y, geometry.Terrain.Height(corner.X, corner.Z) ?? 0, 0.05, $"{corner}: the terrain is the floor there");
                Assert.IsNull(F.FloorAt(Mesh(corner), corner, corner.Y - 0.7f, corner.Y + 0.7f), $"{corner}: no mesh floor at the terrain's height");
                var ramp = F.FloorAt(Mesh(real), real, real.Y - 0.5f, real.Y + 0.5f);
                Assert.IsNotNull(ramp, $"{real}: the client stood on a mesh floor (the ramp)");
                Assert.IsGreaterThan(0.9f, corner.Y - ramp.Value, $"{real}: under the corner");
            }

            var copy = AshenConscriptionQueenTests.IntoFacility(harness);
            var nav = copy.NavMesh;
            var charge = new Vector3(Raid.ChargeAt.X, Raid.ChargeAt.Y, Raid.ChargeAt.Z);
            foreach (var at in new[] { V(S.TerminalAt), V(S.TerminalStand) })
            {
                Assert.AreEqual(at.Y, F.FloorAt(Mesh(at), at, at.Y - 0.5f, at.Y + 0.5f) ?? 0, 0.1, $"{at}: the deck's mesh floor");
                Assert.AreEqual(at.Y, nav.GroundHeight(at + new Vector3(0, 1, 0)) ?? 0, 0.3, $"{at}: navmesh ground");
                Assert.IsTrue(nav.Connected(charge, at) ?? false, $"{at}: joined to the charge");
                Assert.IsNull(B.DropEdge(F.Near(geometry, new List<Vector3> { at }, B.PadDeckEdgeMargin + 1)[0], at, B.PadDeckEdgeMargin), $"{at}: no drop within 2 m");
            }
            Assert.IsLessThan(3f, Vector3.Distance(V(S.TerminalAt), V(S.TerminalStand)), "the terminal within the player's use range (3) of the walk's end");

            foreach (var (classId, at, yaw) in new[] { ((int)Raid.ChargeClass, charge, 0f), ((int)Raid.TerminalClass, V(S.TerminalAt), (float)Raid.TerminalOrientation) })
                E.AddObject(geometry, meshes, classId, at, yaw);
            var walk = E.Legs["the charge to the terminal"];
            Assert.AreEqual(V(S.TerminalStand), walk[^1], "the walk ends at the stand");
            var deck = walk.SkipWhile(corner => corner.Z < -400.5f).ToArray();
            foreach (var (corners, margin) in new[] { (walk, 0f), (deck, B.PadDeckEdgeMargin) })
                foreach (var (line0, direction) in new[] { (corners.ToList(), "there"), (corners.Reverse().ToList(), "back") })
                    foreach (var shift in new[] { 0f, 0.13f, 0.27f, 0.4f })
                    {
                        var line = line0.ToList();
                        line[0] = Vector3.Lerp(line0[0], line0[1], shift / Vector2.Distance(new(line0[0].X, line0[0].Z), new(line0[1].X, line0[1].Z)));
                        Assert.IsNull(B.ClientFloorLine(geometry, line, line[^1], null, 0.5f, null, rampFoot: true, edgeMargin: margin), $"{corners.Length} corners {direction} +{shift}, margin {margin}");
                    }
            for (var k = 1; k < walk.Length; k++)
                Assert.IsTrue(nav.Connected(charge, walk[k]) ?? false, $"corner {k} on the charge's region");
        }

        /// <summary>
        /// Fix S's migration rolled back and forth, baselined at its own migration: Down gives back set 19's place (fix M's scene)
        /// and no fix S evidence, as a database migrated only to fix R holds them; nothing else changes; a second Down and a second
        /// Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixSRollsBackToFixR()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixS);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.MissionId != S.Raid)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable().Select(entry => $"{entry.MissionId}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => !(entry.MissionId == S.Raid && entry.IndicatorId == S.TerminalMarker))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}/{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != S.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(Now, Mine(world), "at fix S");
            Assert.AreEqual(S.RaidNow(), Scene(world), "fix S's scene");

            var before = AshenMissionTests.Previous(world, FixS);
            Assert.AreEqual("20271110002140_Ashen_fix_r", before, "after fix R");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
            {
                Assert.AreEqual(Mine(fresh.World), Mine(world), "Down: as a database migrated only to fix R");
                Assert.AreEqual(Scene(fresh.World), Scene(world), "Down: the scene as at fix R");
            }
            Assert.AreEqual(Before, Mine(world), "set 19's place");
            Assert.AreEqual(S.RaidBefore(), Scene(world), "fix M's scene");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in S.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Mine(world), "a second Down changes nothing");
            Assert.AreEqual(S.RaidBefore(), Scene(world), "a second Down: the scene");

            world.GetService<IMigrator>().Migrate(FixS);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "Up again");
            foreach (var sql in S.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(S.RaidNow(), Scene(world), "Up: fix S's scene");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
