using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;

namespace Rasa.Test.Missions.Ashen
{
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using G = AshenGothaPlantTests;
    using B = AshenFixBTests;

    /// <summary>
    /// Ashen fix job N (to-fix row 50, a test and route change only, no migration): 1452's way over the landing pad's deck to the
    /// Explosive Charge. Fix K's way north round the landing pad ran on the pit's trench rim (176.4), 0.25-0.5 m from the pit 5 m
    /// below it (171.1-171.3); the real client (2026-11-13, normal play) fell into the pit on (-352, -260) > (-359, -265) >
    /// (-364, -265) and could not get out: the pit's walls are the trench's, and its navmesh way out north-east is a slope the
    /// client does not climb ((-343.5, 171.2, -276.8) > (-340.3, 174.8, -274.0) > (-339.5, 176.4, -268.8)).
    ///
    /// Why these tests: the walk check (AshenFixBTests.ClientFloorLine) followed the highest floor under the line's middle only,
    /// so a line along a deck's edge passed (fault 1: no edge margin), and its 55 degree rule passes the pit's slope (44-51
    /// degrees on the 1 m heightmap), which the client refused (fault 2). Both must fail now, and the new way must pass.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixNTests
    {
        private const string MapName = "adv_arieki_ligo_ashendesert_indracaverns";

        /// <summary>The tester's legs where he fell (row 50, back-walk 03:05; the last deck point (-351.7, 176.4, -259.7)).</summary>
        internal static readonly Vector3[] DropLegs = { new(-352, 176.37f, -260), new(-359, 176.98f, -265), new(-364, 177.04f, -265) };

        /// <summary>Fix K's leg 24 (the regression).</summary>
        internal static readonly Vector3[] FixKWay =
        {
            new(-289.13f, 177.61f, -286.65f), new(-329, 175.43f, -287), new(-341, 175.93f, -265), new(-352, 176.37f, -260), new(-359, 176.98f, -265),
            new(-364, 177.04f, -265), new(-378, 176.37f, -274), new(-379, 176.37f, -296), new(-390, 175.81f, -296), new(-390.8f, 175.81f, -297)
        };

        /// <summary>The navmesh's way out of the pit the tester's client did not climb (row 50).</summary>
        internal static readonly Vector3[] PitSlope = { new(-343.5f, 171.2f, -276.8f), new(-340.3f, 174.8f, -274.0f), new(-339.5f, 176.4f, -268.8f) };

        /// <summary>Where the tester stood in the pit (row 50), and the rim point of fix K's way above it.</summary>
        private static readonly Vector3 InThePit = new(-377.6f, 171.1f, -293.8f), RimAbove = new(-379, 176.37f, -296);

        private static MeshLibrary meshes;
        private static MapGeometry geometry;

        [ClassInitialize]
        public static void Load(TestContext _)
        {
            var dataDir = F.ClientData();
            meshes = new MeshLibrary(dataDir, F.MeshCsv());
            geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
        }

        [ClassCleanup]
        public static void Unload() => meshes?.Dispose();

        private static string Walk(IReadOnlyList<Vector3> line, float edgeMargin, List<Vector3> track = null) =>
            B.ClientFloorLine(geometry, line, line[^1], null, G.WalkHalfWidth, track, rampFoot: true, edgeMargin: edgeMargin);

        /// <summary>
        /// Fault 1: the tester's drop legs pass fix K's rules (0.5 m half width, rampFoot) without an edge margin and fail with
        /// the pad deck's (2 m), there and back, at the pit's edge (a drop of 5 m and more); fix K's whole leg 24 fails it too.
        /// The tester's spot in the pit lies more than EdgeDrop under the rim (the client floor there is the pit's).
        /// </summary>
        [TestMethod]
        public void TheTestersDropOffThePitsRimIsRefused()
        {
            foreach (var (line, direction) in new[] { (DropLegs.ToList(), "there"), (DropLegs.Reverse().ToList(), "back") })
            {
                Assert.IsNull(Walk(line, 0), $"the drop legs {direction} pass fix K's rules (the fault)");
                var why = Walk(line, B.PadDeckEdgeMargin);
                StringAssert.Contains(why ?? "", "an edge the client walks off", $"the drop legs {direction}: {why}");
            }
            foreach (var (line, direction) in new[] { (FixKWay.ToList(), "there"), (FixKWay.Reverse().ToList(), "back") })
            {
                Assert.IsNull(Walk(line, 0), $"fix K's leg 24 {direction} passed fix K's rules");
                StringAssert.Contains(Walk(line, B.PadDeckEdgeMargin) ?? "", "an edge the client walks off", $"fix K's leg 24 {direction} with the edge margin");
            }

            var near = F.Near(geometry, new List<Vector3> { InThePit, RimAbove }, 1);
            var pit = F.FloorAt(near[0], InThePit, InThePit.Y - 2, InThePit.Y + 1);
            Assert.IsNotNull(pit, "a client floor in the pit");
            Assert.AreEqual(InThePit.Y, pit.Value, 0.3, "the tester stood on the pit's floor");
            Assert.IsGreaterThan(B.EdgeDrop, RimAbove.Y - pit.Value, "the pit lies more than EdgeDrop under the rim");
            Assert.IsNotNull(B.DropEdge(F.Near(geometry, new List<Vector3> { RimAbove }, 4)[0], RimAbove, B.PadDeckEdgeMargin), "fix K's rim point is at the edge");
        }

        /// <summary>
        /// Fault 2: the pit's north-east slope is under the 55 degree rule all along the tester's climb (the steepest floor 51
        /// degrees), so the old check let lines up it pass (the straight line from the tester's foot to his top, and lines from
        /// further south); with RefusedSlopes the tester's climb and those lines fail. Down it still passes (the client slides).
        /// </summary>
        [TestMethod]
        public void ThePitsSlopeIsRefused()
        {
            var samples = new List<Vector3>();
            for (var k = 0; k + 1 < PitSlope.Length; k++)
                for (var s = 0; s <= 20; s++)
                    samples.Add(Vector3.Lerp(PitSlope[k], PitSlope[k + 1], s / 20f));
            var near = F.Near(geometry, samples, 1);
            var steepest = 0f;
            for (var i = 0; i < samples.Count; i++)
            {
                var floor = F.FloorAt(near[i], samples[i] with { Y = 180 }, 165, 180);
                Assert.IsNotNull(floor, $"a floor at {samples[i]}");
                var normal = B.FloorNormal(near[i], samples[i] with { Y = floor.Value });
                Assert.IsNotNull(normal, $"a floor normal at {samples[i]}");
                steepest = MathF.Max(steepest, MathF.Acos(normal.Value) * 180 / MathF.PI);
            }
            Assert.IsLessThan(B.MaxClimbAngle, steepest, "the slope is under the 55 degree rule (why the old check passed it)");
            Assert.IsGreaterThan(44f, steepest, FormattableString.Invariant($"but steep: {steepest:0.0} degrees"));

            var lines = new List<(string Name, Vector3[] Line)>
            {
                ("the tester's climb", PitSlope),
                ("straight from the tester's foot to his top", new[] { PitSlope[0], PitSlope[^1] }),
                ("from (-344, -280)", new[] { new Vector3(-344, 171.3f, -280), PitSlope[^1] }),
                ("from (-345, -282)", new[] { new Vector3(-345, 171.7f, -282), PitSlope[^1] })
            };
            foreach (var (name, line) in lines)
            {
                var why = B.ClientFloorLine(geometry, line, line[^1], null, G.WalkHalfWidth);
                StringAssert.Contains(why ?? "", "the real client does not climb it", $"{name}: {why}");
            }
            Assert.IsNull(B.ClientFloorLine(geometry, new[] { PitSlope[^1], PitSlope[0] }, PitSlope[0], null, G.WalkHalfWidth), "down the slope passes");

            // The entry is Indra Caverns' only: the geometry is known as that map, another map's is not.
            Assert.AreEqual(B.IndraCaverns, B.SlopeMapOf(geometry), "the walks' geometry is Indra Caverns");
            var dataDir = F.ClientData();
            var desert = MapGeometry.Load(Path.Combine(dataDir, "maps", "adv_arieki_ligo_ashendesert"), meshes, 1);
            Assert.AreEqual("", B.SlopeMapOf(desert), "Ligo Ashen Desert has no refused slope");
            Assert.IsNull(B.RefusedSlopeAt(B.SlopeMapOf(desert), PitSlope[1]), "not at the same x, z on another map");
        }

        /// <summary>
        /// The new way (leg 24 and 44, AshenGothaPlantTests): both ways on the client floor with the edge margin (2 m from the pit's
        /// edge on the rim), never on the pit's slope, its corners on the terrain west of the rim 10 m and more from any drop,
        /// ending beside the charge.
        /// </summary>
        [TestMethod]
        public void TheNewWayKeepsOffThePit()
        {
            var way = G.Line(new[] { G.PadDeck[1], "the plant 44: the pad (-390.8, -297) to beside the charge (-390.8, -301)" });
            foreach (var (line, direction) in new[] { (way, "there"), (Enumerable.Reverse(way).ToList(), "back") })
            {
                var track = new List<Vector3>();
                Assert.IsNull(Walk(line, B.PadDeckEdgeMargin, track), $"the new way {direction}");
                Assert.IsTrue(track.All(at => B.RefusedSlopeAt(B.IndraCaverns, at) == null), $"{direction}: never on a refused slope");
            }
            var west = way.Where(at => at.X <= -390.5f).ToList();
            Assert.IsGreaterThan(1, west.Count, "the way's part on the terrain west of the rim");
            var near = F.Near(geometry, west, 11);
            for (var i = 0; i < west.Count; i++)
                Assert.IsNull(B.DropEdge(near[i], west[i], 10), FormattableString.Invariant($"a drop within 10 m of ({west[i].X:0.0}, {west[i].Y:0.00}, {west[i].Z:0.0})"));
            Assert.AreEqual(G.BesideTheCharge, way[^1], "it ends beside the charge");
        }
    }
}
