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
    using R = AshenFixRV1;
    using N = AshenAvernusHiveNpcsV1;
    using B = AshenFixBTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen fix job R (AshenFixRV1, migration 20271110002140): 1537's Bane pool 790658 and its marker 6 moved off the Queen's
    /// Chamber's west ledge onto the chamber's floor (real client 2026-11-13: the way up stopped at (240.1, 169.6, -664.1) and
    /// (237.8, 169.9, -663.3)). [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixRTests
    {
        private const string FixR = "20271110002140_Ashen_fix_r";

        /// <summary>Set 18's way from the bottom up the slope to the west ledge (its legs 2's end and 3), where the real client stopped.</summary>
        internal static readonly Vector3[] SlopeUp =
        {
            new(243f, 166.36f, -644f), new(243f, 171.11f, -667f), new(242f, 171.86f, -667f), new(242f, 172.86f, -668f),
            new(240f, 174.61f, -668f), new(238f, 176.36f, -668f)
        };

        /// <summary>
        /// The steepest climb along a walk on the client's floor over 2 m (2D) of its samples, in degrees: fix R's check for the
        /// terrain slopes the real client refused that the 55-degree rule passes (as fix N's RefusedSlopes, row 50: 47-48 degrees
        /// over 2 m there). ESTIMATE: not a global rule (Ashen walks the real client made climb up to 47.6 over 2 m).
        /// </summary>
        internal static float SteepestClimb(IReadOnlyList<Vector3> track)
        {
            var steepest = 0f;
            for (var a = 0; a < track.Count; a++)
                for (var b = a + 1; b < track.Count; b++)
                {
                    var run = Vector2.Distance(new(track[a].X, track[a].Z), new(track[b].X, track[b].Z));
                    if (run < 1.9f)
                        continue;
                    if (run > 2.1f)
                        break;
                    steepest = MathF.Max(steepest, MathF.Atan2(track[b].Y - track[a].Y, run) * 180 / MathF.PI);
                }
            return steepest;
        }

        /// <summary>The climb fix R's walk keeps under (degrees over 2 m): under the slope the real client refused here (47-50) and fix N's (47-48).</summary>
        internal const float MaxClimb = 45;

        private const string Now = "790658 225.000,158.750,-502.000 r5 790654 | marker 225.000,158.750,-502.000 r9 | evidence 1";
        private const string Before = "790658 147.000,194.110,-590.000 r5 790654 | marker 147.000,194.110,-590.000 r9 | evidence 0";

        private static string Mine(DbContext db)
        {
            var pool = db.Set<SpawnPoolEntry>().AsNoTracking().Single(entry => entry.Id == R.BanePool);
            var marker = db.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == R.Queen && entry.ContentRevision == AshenMissionDataV1.Revision &&
                entry.ObjectiveId == R.SweepObjective && entry.IndicatorId == R.SweepMarker);
            return FormattableString.Invariant($"{pool.Id} {pool.PosX:0.000},{pool.PosY:0.000},{pool.PosZ:0.000} r{pool.Radius:0.###} {pool.Creature1Id} | ") +
                   FormattableString.Invariant($"marker {marker.PosX:0.000},{marker.PosY:0.000},{marker.PosZ:0.000} r{marker.Radius:0.###} | ") +
                   $"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == R.Source)}";
        }

        /// <summary>Fix R's place of the Bane's pool and 1537's marker 6 (radius kept), and its evidence row.</summary>
        [TestMethod]
        public void TheBaneStandsOnTheChambersFloor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(Now, Mine(harness.World));
        }

        /// <summary>
        /// The navmesh and the client floor: set 18's west ledge is a navmesh island (not joined to the bottom), its way up from
        /// the chamber's floor climbs over MaxClimb (the real client's stop) and fails the 55-degree rule (unshifted by the lone
        /// sample rule, SteepBand; with its start moved by two steep samples); fix R's place is on the bottom's navmesh region, and its walk there and back climbs under MaxClimb at every start shift.
        /// </summary>
        [TestMethod]
        public void TheNewPlaceIsReachedAndTheOldOneWasNot()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = AshenAvernusHiveTests.IntoHive(harness).NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, N.Outpost)), meshes, 1);
            var bottom = new Vector3(273f, 144.25f, -462f);
            Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

            Assert.IsFalse(nav.Connected(bottom, V(R.BaneBefore)) ?? false, "set 18's ledge: a navmesh island");
            Assert.IsFalse(nav.Connected(bottom, SlopeUp[^1]) ?? false, "the slope's top: on the island");
            Assert.IsTrue(nav.Connected(bottom, SlopeUp[^2]) ?? false, "the slope's foot side: the bottom's region");
            Assert.IsTrue(nav.Connected(bottom, V(R.BaneAt)) ?? false, "fix R's place: the bottom's region");

            var old = new List<Vector3>();
            // Unshifted, set 18's slope has one steep sample only (it passed the 55-degree rule before SteepBand); the steep floor
            // round it is a 0.9 m band (AshenFixBTests.SteepStretch), which the lone sample rule now refuses.
            StringAssert.Contains(B.ClientFloorLine(geometry, SlopeUp, SlopeUp[^1], null, 0.5f, old, rampFoot: true) ?? "", "a band, not a seam", "set 18's slope unshifted: a steep band round its one steep sample");
            Assert.IsGreaterThan(MaxClimb, SteepestClimb(old), "set 18's slope climbs over MaxClimb");
            var shifted = SlopeUp.ToList();
            shifted[0] = Vector3.Lerp(SlopeUp[0], SlopeUp[1], 0.13f / Vector2.Distance(new(SlopeUp[0].X, SlopeUp[0].Z), new(SlopeUp[1].X, SlopeUp[1].Z)));
            StringAssert.Contains(B.ClientFloorLine(geometry, shifted, SlopeUp[^1], null, 0.5f, rampFoot: true) ?? "", "too steep to climb", "set 18's slope with its start moved 0.13 m");

            var walk = AshenAvernusHiveTests.Legs["the bottom to the Bane"];
            Assert.AreEqual(V(R.BaneAt), walk[^1], "the walk ends at the pool");
            foreach (var (line0, direction) in new[] { (walk.ToList(), "there"), (walk.Reverse().ToList(), "back") })
                foreach (var shift in new[] { 0f, 0.13f, 0.27f, 0.4f })
                {
                    var line = line0.ToList();
                    line[0] = Vector3.Lerp(line0[0], line0[1], shift / Vector2.Distance(new(line0[0].X, line0[0].Z), new(line0[1].X, line0[1].Z)));
                    var track = new List<Vector3>();
                    Assert.IsNull(B.ClientFloorLine(geometry, line, line[^1], null, 0.5f, track, rampFoot: true), $"{direction} +{shift}");
                    Assert.IsLessThan(MaxClimb, SteepestClimb(track), $"{direction} +{shift}: the steepest climb");
                    for (var k = 1; k < line.Count; k++)
                        Assert.IsTrue(nav.Connected(bottom, line[k]) ?? false, $"{direction}: corner {k} on the bottom's region");
                }
        }

        /// <summary>
        /// Fix R's migration rolled back and forth, baselined at its own migration: Down gives back set 18's place and no fix R
        /// evidence, as a database migrated only to fix P holds them; nothing else changes; a second Down and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixRRollsBackToFixP()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixR);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id != R.BanePool).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => !(entry.MissionId == R.Queen && entry.IndicatorId == R.SweepMarker))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}/{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != R.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(Now, Mine(world), "at fix R");

            var before = AshenMissionTests.Previous(world, FixR);
            Assert.AreEqual("20271110002120_Ashen_fix_p", before, "after fix P");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Mine(fresh.World), Mine(world), "Down: as a database migrated only to fix P");
            Assert.AreEqual(Before, Mine(world), "set 18's place");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in R.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Mine(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixR);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "Up again");
            foreach (var sql in R.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
