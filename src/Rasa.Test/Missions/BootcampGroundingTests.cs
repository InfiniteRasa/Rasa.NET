using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Navigation;

namespace Rasa.Test.Missions
{
    [TestClass]
    public class BootcampGroundingTests
    {
        [TestMethod]
        public void ExtractionAssaultApproachRemainsGroundedAndConnected()
        {
            var nav = LoadNav();
            var destination = new Vector3(-218, 101.08475f, -78);
            foreach (var actor in Rasa.Services.Preloader.Missions.BootcampExtractionDataV5.Scene(1995).Actors.Values)
            {
                if (actor.TemplateId != Rasa.Services.Preloader.Missions.BootcampExtractionDataV5.AssaultTemplate)
                    continue;
                var point = new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z);
                var ground = nav.Nearest(point);
                Assert.IsNotNull(ground);
                Assert.IsLessThan(0.1f, Vector3.Distance(point, ground.Value));
                Assert.IsLessThan(90f, point.Y);
                var route = nav.FindPath(ground.Value, destination, out var complete);
                Assert.IsTrue(complete, $"No uphill route from {ground} to the extraction pad.");
                Assert.IsLessThan(0.1f, Vector3.Distance(route[^1], destination));
            }
        }

        [TestMethod]
        [DataRow(93.2f, 137.5f, 109.11104f)]
        [DataRow(89.2f, 137.5f, 109.11104f)]
        [DataRow(97.2f, 137.5f, 109.11104f)]
        [DataRow(93.2f, 133.5f, 109.11104f)]
        [DataRow(93.2f, 141.5f, 109.36104f)]
        public void YoungbloodBaseGroundMatchesSourceSurfaceWithinFifteenCentimetres(float x, float z, float surfaceY)
        {
            // Source terrain samples and static collision were measured from the 1.16.5.0 client.
            var ground = LoadNav().GroundHeight(new Vector3(x, surfaceY, z));
            Assert.IsNotNull(ground);
            Assert.IsTrue(System.Math.Abs(ground.Value - surfaceY) < 0.15f,
                $"Base nav Y {ground.Value} at ({x},{z}) leaves Youngblood floating above source surface {surfaceY}.");
        }

        [TestMethod]
        public void AccurateGroundRetainsTheRepairedCaveBridgeAndCrashSiteRoutes()
        {
            var nav = LoadNav();
            var points = new[]
            {
                new Vector3(279.05f, 120.5f, 66.07f),
                new Vector3(93.2f, 109.11104f, 137.5f),
                new Vector3(-101.2f, 86.5f, 70.8f),
                new Vector3(-225, 101, -71)
            };
            for (var index = 1; index < points.Length; index++)
            {
                var path = nav.FindPath(points[index - 1], points[index], out var complete);
                Assert.IsNotNull(path);
                Assert.IsTrue(complete, $"Ground correction disconnected route {points[index - 1]} -> {points[index]}.");
            }
        }

        private static NavMeshQuery LoadNav()
        {
            var root = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Rasa.NET.sln")))
                root = root.Parent;
            if (root == null)
                throw new DirectoryNotFoundException("Repository root not found.");
            return new NavMeshQuery(NavMeshFile.Read(Path.Combine(root.FullName, "navmesh", "adv_bootcamp.nav")));
        }
    }
}
