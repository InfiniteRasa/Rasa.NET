using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Navigation;

    /// <summary>
    /// The sizes the startup memory report works out (<see cref="MemoryFootprint"/>,
    /// <see cref="CoverMesh.MemoryBytes"/>): arrays by .NET's 64-bit layout, a cover by what it
    /// holds, and a real map's navmesh counted the way its header says it should be.
    /// </summary>
    [TestClass]
    public class MemoryFootprintTests
    {
        [TestMethod]
        public void ArraysAreHeaderPlusElementsPaddedToEight()
        {
            Assert.AreEqual(24, MemoryFootprint.ArrayBytes(0, sizeof(float)));
            Assert.AreEqual(96, MemoryFootprint.ArrayBytes(18, sizeof(float)));
            Assert.AreEqual(120, MemoryFootprint.ArrayBytes(3, 32));
            Assert.AreEqual(32, MemoryFootprint.ArrayBytes(1, 1));
        }

        [TestMethod]
        public void CoverCountsItsTrianglesTreeAndTerrain()
        {
            var vertices = new float[] { 0, 0, 0, 10, 0, 0, 10, 0, 10, 0, 0, 10 };
            var cover = CoverMesh.Build(vertices, new[] { 0, 1, 2, 0, 2, 3 });

            Assert.AreEqual(2, cover.TriangleCount);
            Assert.AreEqual(
                MemoryFootprint.ArrayBytes(2 * 9, sizeof(float))
                + MemoryFootprint.ArrayBytes(cover.NodeCount, 32)
                + MemoryFootprint.ArrayBytes(cover.TerrainCells, sizeof(float)),
                cover.MemoryBytes);
        }

        [TestMethod]
        public void NavMeshCountsMatchItsTileHeaders()
        {
            var navMesh = NavMeshFile.Read(NavMeshFile.PathFor(NavMeshDirectory(), "characterselection"));
            var footprint = MemoryFootprint.Of(navMesh);

            var tiles = 0;
            long polys = 0, bvNodes = 0;

            for (var i = 0; i < navMesh.GetMaxTiles(); i++)
            {
                var header = navMesh.GetTile(i)?.data?.header;

                if (header == null)
                    continue;

                tiles++;
                polys += header.polyCount;
                bvNodes += header.bvNodeCount;
            }

            Assert.IsTrue(tiles > 0, "the test mesh has tiles");
            Assert.AreEqual(tiles, footprint.Tiles);
            Assert.AreEqual(polys, footprint.Polys);
            Assert.AreEqual(bvNodes, footprint.BvNodes);
            // At least each tile's fixed objects and each poly's own object.
            Assert.IsTrue(footprint.Bytes >= tiles * (56L + 80 + 120) + polys * 56);
        }

        [TestMethod]
        public void NoNavMeshIsNothing()
        {
            Assert.AreEqual(default, MemoryFootprint.Of(null));
        }

        [TestMethod]
        public void BytesReadForTheLog()
        {
            Assert.AreEqual("12.0 KB", MemoryFootprint.Format(12 * 1024));
            Assert.AreEqual("583.1 MB", MemoryFootprint.Format((long)(583.1 * 1024 * 1024)));
            Assert.AreEqual("1.24 GB", MemoryFootprint.Format((long)(1.24 * 1024 * 1024 * 1024)));
        }

        private static string NavMeshDirectory()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var navmesh = Path.Combine(dir.FullName, "navmesh");

                if (Directory.Exists(navmesh))
                    return navmesh;
            }

            throw new DirectoryNotFoundException("No navmesh folder above " + AppContext.BaseDirectory);
        }
    }
}
