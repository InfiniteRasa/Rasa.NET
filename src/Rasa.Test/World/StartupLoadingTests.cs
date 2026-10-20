using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// What the game server loads at startup, loaded faster: the cover files read in blocks
    /// (CoverMesh.Read), every map's navmesh and cover read side by side (NavMeshManager
    /// .NavMeshInit), and the creatures' looks in one query (CreatureManager.CreatureInit).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class StartupLoadingTests
    {
        [TestMethod]
        public void EveryMapsCoverReadsExactlyAsItDidAValueAtATime()
        {
            var covers = Directory.GetFiles(Path.Combine(RepositoryRoot(), "navmesh"), "*" + CoverMesh.Extension);
            Assert.IsTrue(covers.Length >= 70, $"{covers.Length} cover files");

            var different = new ConcurrentBag<string>();
            Parallel.ForEach(covers, path =>
            {
                if (!CoverMesh.Read(path).SameAs(CoverMesh.ReadByElement(path)))
                    different.Add(Path.GetFileName(path));
            });

            Assert.HasCount(0, different, string.Join(", ", different));
        }

        [TestMethod]
        public void AWrittenCoverReadsBackTheSameAndAnswersTheSame()
        {
            var cover = Wall();
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + CoverMesh.Extension);

            try
            {
                cover.Write(path);
                var read = CoverMesh.Read(path);

                Assert.IsTrue(read.SameAs(cover));
                Assert.IsTrue(read.SameAs(CoverMesh.ReadByElement(path)));
                Assert.AreEqual(cover.TriangleCount, read.TriangleCount);
                Assert.AreEqual(cover.NodeCount, read.NodeCount);
                Assert.IsTrue(read.HasTerrain);
                Assert.IsTrue(read.Blocked(new Vector3(0, 1, -5), new Vector3(0, 1, 5)), "through the wall");
                Assert.IsFalse(read.Blocked(new Vector3(0, 20, -5), new Vector3(0, 20, 5)), "over it");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void ABrokenCoverIsRefused()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var good = Path.Combine(directory, "good" + CoverMesh.Extension);
                Wall().Write(good);
                var bytes = Decompress(good);

                // Not a cover file at all.
                var notCover = (byte[])bytes.Clone();
                notCover[0] = (byte)'X';
                Assert.ThrowsExactly<InvalidDataException>(() => CoverMesh.Read(Compressed(directory, "magic", notCover)));

                // Another version.
                var version = (byte[])bytes.Clone();
                BitConverter.GetBytes(2).CopyTo(version, 4);
                Assert.ThrowsExactly<InvalidDataException>(() => CoverMesh.Read(Compressed(directory, "version", version)));

                // A count below zero.
                var negative = (byte[])bytes.Clone();
                BitConverter.GetBytes(-1).CopyTo(negative, 8);
                Assert.ThrowsExactly<InvalidDataException>(() => CoverMesh.Read(Compressed(directory, "negative", negative)));

                // Cut short, in the triangles and in the terrain.
                Assert.ThrowsExactly<EndOfStreamException>(() => CoverMesh.Read(Compressed(directory, "short", bytes.Take(40).ToArray())));
                Assert.ThrowsExactly<EndOfStreamException>(() => CoverMesh.Read(Compressed(directory, "shorter", bytes.Take(bytes.Length - 4).ToArray())));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void AMapsNavmeshAndCoverLoadEachOnTheirOwn()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                File.Copy(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_afs_arena"), NavMeshFile.PathFor(directory, "arena"));
                File.WriteAllText(CoverMesh.PathFor(directory, "arena"), "not a cover");

                var arena = NavMeshManager.LoadFiles(directory, "arena");
                Assert.IsTrue(arena.HasNavMesh);
                Assert.IsNotNull(arena.NavMesh, "a broken cover leaves the navmesh");
                Assert.IsNull(arena.NavMeshError);
                Assert.IsNotNull(arena.HeightRange);
                Assert.IsNull(arena.Cover);
                Assert.IsNotNull(arena.CoverError);

                File.WriteAllText(NavMeshFile.PathFor(directory, "broken"), "not a navmesh");
                Wall().Write(CoverMesh.PathFor(directory, "broken"));

                var broken = NavMeshManager.LoadFiles(directory, "broken");
                Assert.IsTrue(broken.HasNavMesh);
                Assert.IsNull(broken.NavMesh);
                Assert.IsNotNull(broken.NavMeshError);
                Assert.IsNotNull(broken.Cover, "a broken navmesh leaves the cover");

                var none = NavMeshManager.LoadFiles(directory, "none");
                Assert.IsFalse(none.HasNavMesh);
                Assert.IsNull(none.Cover);
                Assert.IsNull(none.CoverError);
                Assert.IsNull(none.NavMeshError);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void EveryMapGetsItsOwnNavmeshAndCover()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");
            var maps = new (uint Id, string Name)[]
            {
                (1985, "adv_bootcamp"),
                (991001, "adv_afs_arena"),
                (991002, "adv_arieki_ligo_crucible_wbfacility"),
                (991003, "adv_foreas_concordia_divide_minoscaverns"),
                (991004, "adv_arieki_torden_plains_penalresearch")
            };

            foreach (var (id, name) in maps)
            {
                if (!harness.Maps.MapChannelArray.TryGetValue(id, out var map))
                    harness.Maps.MapChannelArray[id] = map = new MapChannel();

                map.MapInfo = new MapInfo(id, name, 1, 0);
                map.NavMesh = null;
                map.Cover = null;
                map.SafetyFloorY = null;
            }

            try
            {
                ((NavMeshManager)Activator.CreateInstance(typeof(NavMeshManager), nonPublic: true)).NavMeshInit(navmesh);

                foreach (var (id, name) in maps)
                {
                    var map = harness.Maps.MapChannelArray[id];
                    var own = new NavMeshQuery(NavMeshFile.Read(NavMeshFile.PathFor(navmesh, name)));

                    Assert.IsNotNull(map.NavMesh, name);
                    Assert.AreEqual(own.HeightRange(), map.NavMesh.HeightRange(), $"{name}'s own navmesh");
                    Assert.AreEqual(SafetyFloor.FloorFor(own.HeightRange().Value.Lowest), map.SafetyFloorY, name);
                    Assert.IsTrue(map.Cover.SameAs(CoverMesh.Read(CoverMesh.PathFor(navmesh, name))), $"{name}'s own cover");
                }
            }
            finally
            {
                foreach (var (id, _) in maps.Skip(1))
                    harness.Maps.MapChannelArray.Remove(id);
            }
        }

        [TestMethod]
        public void EveryCreatureIsLoadedWithItsOwnLook()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            using var world = harness.CreateWorld();
            var dressed = 0;

            foreach (var (id, creature) in harness.Creatures.LoadedCreatures)
            {
                var rows = world.Creatures.GetCreatureAppearances(id);

                CollectionAssert.AreEquivalent(
                    rows.Select(row => (row.SlotId, row.ClassId, row.Color)).ToList(),
                    creature.AppearanceData.Values.Select(slot => ((uint)slot.SlotId, slot.Class, slot.Color.Hue)).ToList(),
                    $"creature {id}");

                if (rows.Count > 0)
                    dressed++;
            }

            Assert.IsTrue(dressed > 500, $"{dressed} creatures with a look");
        }

        /// <summary>A wall 10 m high across z = 0 and a flat terrain grid at y 0 with one hole (NaN).</summary>
        private static CoverMesh Wall()
        {
            var cover = CoverMesh.Build(new float[] { -10, 0, 0, -10, 10, 0, 10, 10, 0, 10, 0, 0 }, new[] { 0, 1, 2, 0, 2, 3 });
            var heights = Enumerable.Repeat(0f, 16).ToArray();
            heights[5] = float.NaN;
            cover.SetTerrain(-20, -20, 10, 4, 4, heights);
            return cover;
        }

        private static byte[] Decompress(string path)
        {
            using var file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var bytes = new MemoryStream();
            gzip.CopyTo(bytes);
            return bytes.ToArray();
        }

        private static string Compressed(string directory, string name, byte[] content)
        {
            var path = Path.Combine(directory, name + CoverMesh.Extension);
            using var file = File.Create(path);
            using var gzip = new GZipStream(file, CompressionLevel.Fastest);
            gzip.Write(content);
            return path;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
