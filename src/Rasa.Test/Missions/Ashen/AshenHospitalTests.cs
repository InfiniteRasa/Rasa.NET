using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Navigation;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Fix job D, item 2 (to-fix row 8): every hospital (teleporter type 5) and waypoint (type 2) of the Ashen Desert stands on
    /// the client's floor and on the server navmesh. Upstream's Shadow's Edge Post hospital (208) stood at y 269.3, 27 m under
    /// the hospital's floor (client floors 296.20-307.30 there, navmesh 296.68); fix D puts it at 296.5.
    /// </summary>
    [TestClass]
    public class AshenHospitalTests
    {
        /// <summary>Most a spot may stand above or below the client's floor / the navmesh ground at its x/z.</summary>
        private const float ClientSlack = 0.5f, NavSlack = 0.6f, NavReach = 0.3f;

        /// <summary>Why a player put at <paramref name="at"/> does not stand on the floor, or null when they do.</summary>
        internal static string Stand(NavMeshQuery nav, MapGeometry geometry, Vector3 at)
        {
            var tris = F.Near(geometry, new List<Vector3> { at }, 1)[0];
            var floor = F.FloorAt(tris, at with { Y = at.Y + ClientSlack }, at.Y - ClientSlack, at.Y + ClientSlack);
            if (floor is null)
                return FormattableString.Invariant($"no client floor within {ClientSlack} m of y {at.Y:0.00}");
            if (nav.Nearest(at) is not { } near || MathF.Abs(near.X - at.X) > NavReach || MathF.Abs(near.Z - at.Z) > NavReach
                || MathF.Abs(near.Y - at.Y) > NavSlack)
                return FormattableString.Invariant($"no navmesh within {NavSlack} m of y {at.Y:0.00} (nearest {nav.Nearest(at)})");
            return null;
        }

        [TestMethod]
        public void HospitalsAndWaypointsStandOnTheFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var rows = harness.World.TeleporterEntries.AsNoTracking()
                .Where(row => row.MapContextId == AshenMissionDataV1.MapContextId && (row.Type == 2 || row.Type == 5))
                .OrderBy(row => row.Id).ToList();
            CollectionAssert.AreEqual(new uint[] { 195, 196, 197, 198, 207, 208, 209, 317, 318, 319, 322 },
                rows.Select(row => row.Id).ToArray(), "the Ashen hospitals and waypoints");
            var failures = rows.Select(row => (row, why: Stand(nav, geometry, row.Position)))
                .Where(entry => entry.why is not null).Select(entry => $"{entry.row.Id} {entry.row.Description}: {entry.why}").ToList();
            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));

            var hospital = rows.Single(row => row.Id == AshenFixDV1.Hospital);
            Assert.AreEqual((AshenFixDV1.HospitalBefore.X, AshenFixDV1.HospitalBefore.Z, AshenFixDV1.HospitalRotation),
                (hospital.PosX, hospital.PosZ, hospital.Rotation), "fix D moves only the height");
            // Fail-proof: upstream's height is under the floor, on both counts.
            var upstream = hospital.Position with { Y = (float)AshenFixDV1.HospitalBefore.Y };
            StringAssert.Contains(Stand(nav, geometry, upstream), "no client floor", "269.3 is under the client's floor");
            Assert.IsFalse(nav.Nearest(upstream) is { } low && MathF.Abs(low.Y - upstream.Y) <= NavSlack, "269.3 is not on the navmesh");

            // The hospital joins the Shadow's Edge Post waypoint and the walk holds on the client's floor both ways.
            var waypoint = AshenLeadInsTests.ShadowsEdgeWaypoint;
            var at = nav.Nearest(hospital.Position).Value;
            Assert.AreEqual(nav.RegionArea(nav.Nearest(waypoint).Value), nav.RegionArea(at), "one navmesh region with the waypoint");
            Assert.IsNull(AshenFixBTests.ClientFloorWalk(nav, geometry, waypoint, at), "waypoint to hospital");
            Assert.IsNull(AshenFixBTests.ClientFloorWalk(nav, geometry, at, waypoint), "hospital to waypoint");
        }
    }
}
