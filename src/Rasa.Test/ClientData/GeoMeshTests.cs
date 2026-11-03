using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;

namespace Rasa.Test.ClientData
{
    /// <summary>
    /// GeoMesh carries a collision bone's volume through its parents into the object's frame, as the
    /// client does: an ArchHumOutpostFenceStraight8V03's walkway and round gun base hang off the
    /// fence's own bone, which turns them upright and moves them 8 m out.
    /// </summary>
    [TestClass]
    public class GeoMeshTests
    {
        private static readonly Quaternion QuarterTurnY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        [TestMethod]
        public void AChildCollisionBoneIsCarriedThroughItsParent()
        {
            var triangle = new[] { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0) };
            var geo = Geo(
                Bone("frame", -1, QuarterTurnY, new Vector3(10, 0, 0), Vector3.One),
                Bone("collision", 0, Quaternion.Identity, new Vector3(0, 2, 0), Vector3.One, Walkable(triangle)));

            var mesh = GeoMesh.Parse(geo);

            Assert.HasCount(3, mesh.Vertices);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, mesh.Triangles);

            for (var i = 0; i < 3; i++)
            {
                var expected = Vector3.Transform(triangle[i] + new Vector3(0, 2, 0), QuarterTurnY) + new Vector3(10, 0, 0);
                Assert.IsLessThan(1e-5f, Vector3.Distance(expected, mesh.Vertices[i]), $"vertex {i}: {mesh.Vertices[i]}, not {expected}");
            }

            // (1, 0, 0) on the child is (10, 2, -1) on the object, not (1, 2, 0) as the child alone puts it.
            Assert.IsLessThan(1e-5f, Vector3.Distance(new Vector3(10, 2, -1), mesh.Vertices[0]));
        }

        [TestMethod]
        public void ParentsAreFollowedToTheRootWithTheirScale()
        {
            var geo = Geo(
                Bone("root", -1, Quaternion.Identity, new Vector3(0, 0, 5), new Vector3(2, 2, 2)),
                Bone("arm", 0, QuarterTurnY, new Vector3(1, 0, 0), Vector3.One),
                Bone("collision", 1, Quaternion.Identity, Vector3.Zero, Vector3.One, Box(new Vector3(1, 0, 0), new Vector3(0.5f, 0.5f, 0.5f))));

            var mesh = GeoMesh.Parse(geo);

            Assert.HasCount(8, mesh.Vertices);
            Assert.HasCount(36, mesh.Triangles);

            var centre = mesh.Vertices.Aggregate(Vector3.Zero, (sum, v) => sum + v) / 8;
            // The box centre (1, 0, 0) turned by the arm to (0, 0, -1), moved to (1, 0, -1), doubled by
            // the root to (2, 0, -2) and moved to (2, 0, 3).
            Assert.IsLessThan(1e-5f, Vector3.Distance(new Vector3(2, 0, 3), centre), centre.ToString());
            Assert.AreEqual(2f, mesh.Vertices.Max(v => v.Y) - mesh.Vertices.Min(v => v.Y), 1e-5f, "the root's scale doubles the box");
        }

        [TestMethod]
        public void ARootCollisionBoneIsReadAsBefore()
        {
            var triangle = new[] { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0) };
            var geo = Geo(
                Bone("frame", -1, QuarterTurnY, new Vector3(10, 0, 0), Vector3.One),
                Bone("collision", -1, Quaternion.Identity, new Vector3(-8, 0, 0), Vector3.One, Walkable(triangle)));

            var mesh = GeoMesh.Parse(geo);

            CollectionAssert.AreEqual(triangle.Select(v => v + new Vector3(-8, 0, 0)).ToArray(), mesh.Vertices.ToArray());
        }

        [TestMethod]
        public void ABadOrLoopingParentEndsTheChain()
        {
            var triangle = new[] { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0) };
            var geo = Geo(
                Bone("loop", 1, Quaternion.Identity, new Vector3(1, 0, 0), Vector3.One),
                Bone("collision", 0, Quaternion.Identity, Vector3.Zero, Vector3.One, Walkable(triangle)),
                Bone("stray", 7, Quaternion.Identity, new Vector3(0, 5, 0), Vector3.One, Walkable(triangle)));

            var mesh = GeoMesh.Parse(geo);

            Assert.HasCount(6, mesh.Vertices, "both volumes are read and the loop ends");
            CollectionAssert.AreEqual(triangle.Select(v => v + new Vector3(0, 5, 0)).ToArray(), mesh.Vertices.Skip(3).ToArray(), "a parent out of range is the root");
        }

        private static byte[] Geo(params byte[][] bones) =>
            Concat(Encoding.ASCII.GetBytes("CHNKBLXX"), Chunk("GBOD", Concat(bones)));

        private static byte[] Bone(string name, int parent, Quaternion rotation, Vector3 translation, Vector3 scale, params byte[][] volumes)
        {
            using var bdat = new MemoryStream();
            using (var w = new BinaryWriter(bdat, Encoding.ASCII, true))
            {
                w.Write(Encoding.ASCII.GetBytes(name));
                w.Write((byte)0);
                foreach (var f in new[] { rotation.X, rotation.Y, rotation.Z, rotation.W, translation.X, translation.Y, translation.Z, scale.X, scale.Y, scale.Z })
                    w.Write(f);
                foreach (var volume in volumes)
                    w.Write(volume);
            }

            var header = new byte[16];
            BitConverter.GetBytes(parent).CopyTo(header, 0);
            return Chunk("PBON", Concat(header, Chunk("BDAT", bdat.ToArray())));
        }

        private static byte[] Walkable(Vector3[] triangle)
        {
            using var body = new MemoryStream();
            using (var w = new BinaryWriter(body, Encoding.ASCII, true))
            {
                w.Write(Chunk("BVOL", BitConverter.GetBytes(2f)));
                w.Write(1);
                w.Write(triangle.Length);
                foreach (var v in triangle) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); }
                foreach (var i in new ushort[] { 0, 1, 2 }) w.Write(i);
            }

            return Chunk("BVWS", body.ToArray());
        }

        private static byte[] Box(Vector3 centre, Vector3 half)
        {
            using var body = new MemoryStream();
            using (var w = new BinaryWriter(body, Encoding.ASCII, true))
            {
                w.Write(Chunk("BVOL", BitConverter.GetBytes(1f)));
                foreach (var f in new[] { centre.X, centre.Y, centre.Z, half.X, half.Y, half.Z }) w.Write(f);
            }

            return Chunk("BVBX", body.ToArray());
        }

        /// <summary>A chunk: its tag stored reversed, payload size, version 1, 0, payload.</summary>
        private static byte[] Chunk(string tag, byte[] payload)
        {
            var header = new byte[16];
            Encoding.ASCII.GetBytes(new string(tag.Reverse().ToArray())).CopyTo(header, 0);
            BitConverter.GetBytes(payload.Length).CopyTo(header, 4);
            BitConverter.GetBytes(1).CopyTo(header, 8);
            return Concat(header, payload);
        }

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    }
}
