using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Navigation;

namespace Rasa.Test.World
{
    [TestClass]
    public class VelonHollowNavigationTests
    {
        private const string Map = "adv_foreas_howlingmaw_deathburrow";
        private static readonly HashSet<(int X, int Z)> Rebuilt = new()
        {
            (19, 10), (20, 10), (20, 12), (21, 11), (22, 11), (23, 9), (23, 10)
        };
        private static string NavFile
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln"))) directory = directory.Parent;
                return Path.Combine(directory?.FullName ?? throw new AssertFailedException("Repository not found."), "navmesh", Map + ".nav");
            }
        }
        private static NavMeshQuery Query() => TestNavMeshes.Query(NavFile);
        private static readonly (Vector3 Near, Vector3 Far)[] Seams = {
            (new(171.9f, 72.2f, -470.6f), new(173.1f, 72.4f, -468.9f)),
            (new(102.8f, 67.5f, -440.1f), new(100.8f, 67.3f, -440f)),
            (new(.2f, 65.2f, -482.3f), new(-1.8f, 65.1f, -482.8f)),
            (new(43.6f, 87.6f, -383.6f), new(43.6f, 87.8f, -381.6f))
        };
        private static readonly Vector3 Hospital = new(165, 77, -520);
        private static readonly Vector3[] Temples = {
            new(-115.2f, 60.3f, -488.8f), // Map region 5: Mausoleum of the Beholden
            new(-119.3f, 81.8f, -279.5f), // Map region 6: Shrine of the Penitent
            new(631.6f, 49.8f, -343.2f) // Map region 7: Sanctuary of Renewal
        };

        [TestMethod]
        [DataRow(0, false)] [DataRow(0, true)]
        [DataRow(1, false)] [DataRow(1, true)]
        [DataRow(2, false)] [DataRow(2, true)]
        [DataRow(3, false)] [DataRow(3, true)]
        public void EachReportedSeamCanBeCrossedInBothDirections(int seam, bool reverse)
        {
            var pair = Seams[seam];
            AssertRoute(reverse ? pair.Far : pair.Near, reverse ? pair.Near : pair.Far);
        }

        [TestMethod]
        [DataRow(0, false)] [DataRow(0, true)]
        [DataRow(1, false)] [DataRow(1, true)]
        [DataRow(2, false)] [DataRow(2, true)]
        public void HospitalAndEachTempleLandmarkAreConnected(int temple, bool reverse)
        {
            AssertRoute(reverse ? Temples[temple] : Hospital, reverse ? Hospital : Temples[temple]);
        }

        [TestMethod]
        [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
        public void ExistingPiecesOfTheEscortCorridorRemainConnected(int section)
        {
            var pairs = new[] { (Hospital, Seams[0].Near), (Seams[0].Far, Seams[1].Near),
                (Seams[1].Far, Seams[2].Near), (Seams[2].Far, Seams[3].Near) };
            AssertRoute(pairs[section].Item1, pairs[section].Item2);
        }

        private static void AssertRoute(Vector3 start, Vector3 end)
        {
            var query = Query();
            var route = query.FindPath(start, end, out var complete);
            Assert.IsNotNull(route);
            Assert.IsTrue(complete, $"Reported Velon Hollow route must reach {end} from {start}.");
            Assert.AreEqual(true, query.Reachable(start, end));
            Assert.IsLessThan(.1f, Vector3.Distance(query.Nearest(end).Value, route.Last()));
        }

        [TestMethod]
        public void TheWestSeamUsesADetourRatherThanOpeningAStraightShortcut()
        {
            var query = Query();
            var route = query.FindPath(Seams[1].Near, Seams[1].Far, out var complete);
            Assert.IsTrue(complete);
            Assert.IsFalse(query.IsWalkClear(Seams[1].Near, Seams[1].Far));
            Assert.IsGreaterThan(2, route.Count, "The path goes around the intervening geometry.");
        }

        [TestMethod]
        public void OnlySevenTilePayloadsChangeAndTheOriginalPhysicalAgentIsRetained()
        {
            using var file = new BinaryReader(File.OpenRead(NavFile));
            Assert.AreEqual(0x4d534554, file.ReadInt32());
            Assert.AreEqual(0x8802, file.ReadInt32());
            Assert.AreEqual(1600, file.ReadInt32());
            file.BaseStream.Position = 44;
            var tiles = new SortedDictionary<(int X, int Z, int Layer), byte[]>();
            for (var i = 0; i < 1600; i++)
            {
                file.ReadInt64();
                var data = file.ReadBytes(file.ReadInt32());
                tiles.Add((BitConverter.ToInt32(data, 8), BitConverter.ToInt32(data, 12), BitConverter.ToInt32(data, 16)), data);
            }
            Assert.AreEqual(file.BaseStream.Length, file.BaseStream.Position);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var tile in tiles.Where(t => !Rebuilt.Contains((t.Key.X, t.Key.Z)))) hash.AppendData(tile.Value);
            // Fingerprint of the 1,593 untouched tile payloads, as the child-bone collision rebuild
            // (79f36cd6) left them.
            Assert.AreEqual("9987EEB4666E81D9345548174DF6FEB4AC2224A7F7DA871B73B3919A03645B08", Convert.ToHexString(hash.GetHashAndReset()));
            var mesh = Query().NavMesh;
            var selected = Enumerable.Range(0, mesh.GetMaxTiles()).Select(mesh.GetTile)
                .Where(t => t?.data?.header != null && Rebuilt.Contains((t.data.header.x, t.data.header.y))).ToArray();
            Assert.HasCount(7, selected);
            foreach (var tile in selected)
            {
                Assert.AreEqual(2f, tile.data.header.walkableHeight, .001f);
                Assert.AreEqual(.6f, tile.data.header.walkableRadius, .001f);
                Assert.AreEqual(.9f, tile.data.header.walkableClimb, .001f);
            }
        }

        [TestMethod]
        [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
        public void RepairedCrossingsHavePhysicalFloorAndTwoMetersOfHeadroom(int seam)
        {
            var query = Query();
            var cover = CoverMesh.Read(Path.ChangeExtension(NavFile, ".cover"));
            var pair = Seams[seam];
            var route = query.FindPath(pair.Near, pair.Far, out var complete);
            Assert.IsTrue(complete);
            var previous = PhysicalFloor(cover, query.Nearest(pair.Near).Value);
            foreach (var corner in route)
            {
                var from = previous;
                var steps = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(from, corner) / .25f));
                for (var step = 1; step <= steps; step++)
                {
                    var at = Vector3.Lerp(from, corner, (float)step / steps);
                    at.Y = query.GroundHeight(at) ?? at.Y;
                    at = PhysicalFloor(cover, at);
                    Assert.IsTrue(cover.GeometryBlocks(at + Vector3.UnitY * .2f, at - Vector3.UnitY * .2f), $"No physical floor at {at}.");
                    Assert.IsFalse(cover.GeometryBlocks(at + Vector3.UnitY * .1f, at + Vector3.UnitY * 2f), $"Headroom at {at}.");
                    foreach (var height in new[] { .8f, 1.6f })
                        Assert.IsFalse(cover.GeometryBlocks(previous + Vector3.UnitY * height, at + Vector3.UnitY * height), $"Body crosses geometry at {at}.");
                    Assert.IsLessThan(.9f, Math.Abs(at.Y - previous.Y), $"Physical step at {at} exceeds the original climb limit.");
                    previous = at;
                }
            }
        }

        private static Vector3 PhysicalFloor(CoverMesh cover, Vector3 at)
        {
            var low = at.Y - 1.2f;
            var high = at.Y + .8f;
            var top = new Vector3(at.X, high, at.Z);
            Assert.IsTrue(cover.GeometryBlocks(top, new(at.X, low, at.Z)), $"No collision floor near {at}.");
            for (var i = 0; i < 12; i++)
            {
                var middle = (low + high) / 2;
                if (cover.GeometryBlocks(top, new(at.X, middle, at.Z))) low = middle; else high = middle;
            }
            at.Y = (low + high) / 2 + .005f;
            return at;
        }
    }
}
