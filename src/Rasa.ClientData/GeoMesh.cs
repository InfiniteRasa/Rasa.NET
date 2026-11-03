using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Rasa.ClientData
{
    /// <summary>
    /// The collision geometry of a client <c>.geo</c> mesh.
    ///
    /// A <c>.geo</c> is a chunk tree: <c>CHNKBLXX</c>, then chunks of tag (4 chars, stored reversed),
    /// size, version, 0, payload. <c>GBOD</c> holds <c>BBOX</c>, the skeleton <c>PSKE</c> with one
    /// <c>PBON</c> per bone, and the render pieces. A <c>PBON</c> starts with its parent's index in the
    /// skeleton (-1 for a root). Each bone's <c>BDAT</c> is: name, quaternion (x y z w), translation and
    /// scale relative to its parent, and for collision bones a bounding volume: <c>BVWS</c> (walkable
    /// surface: <c>BVOL</c>{radius}, u32 triangle count, u32 vertex count, vertices, u16 indices) or
    /// <c>BVBX</c> (box: <c>BVOL</c>{radius}, centre, half extents).
    ///
    /// A collision bone's volume is in the bone's own frame: it is carried through the bone and then
    /// each parent up to the root into the object's frame, y up. That is what the client walks on and
    /// collides with - the render mesh is not needed. Most collision bones are roots. In the rest the
    /// parent turns or moves the volume, by up to 57 m: the outpost fence pieces with a walkway out to
    /// a round gun base (ArchHumOutpostFenceStraight8V03, CornerV04), the corridor bulkheads, the Bane
    /// chunnels, catwalks and consoles, the junkyard pieces and the doors.
    ///
    /// Chunk payloads mix scalars with child chunks, so children are found by scanning for their tag
    /// inside the parent's range rather than by walking a list.
    /// </summary>
    public sealed class GeoMesh
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();

        /// <summary>The object's render bounds (BBOX), when present.</summary>
        public Vector3 BoundsMin { get; private set; }
        public Vector3 BoundsMax { get; private set; }
        public bool HasBounds { get; private set; }

        public bool HasCollision => Triangles.Count > 0;

        public static GeoMesh Parse(byte[] data)
        {
            var mesh = new GeoMesh();

            if (data.Length < 8 || Encoding.ASCII.GetString(data, 0, 8) != "CHNKBLXX")
                throw new InvalidOperationException("not a .geo chunk file");

            foreach (var (_, start, end) in Scan(data, 8, data.Length, "GBOD"))
            {
                foreach (var (_, s, e) in Scan(data, start, end, "BBOX"))
                {
                    mesh.BoundsMin = ReadVector3(data, s + 1);
                    mesh.BoundsMax = ReadVector3(data, s + 13);
                    mesh.HasBounds = true;
                    break;
                }

                // The whole skeleton first: a bone's parent can come after it.
                var bones = new List<Bone>();

                foreach (var (_, boneStart, boneEnd) in Scan(data, start, end, "PBON"))
                    bones.Add(Bone.Read(data, boneStart, boneEnd));

                for (var i = 0; i < bones.Count; i++)
                    if (bones[i] != null)
                        mesh.ReadCollision(data, bones, i);

                break;
            }

            return mesh;
        }

        /// <summary>One bone: its parent's index, its transform relative to the parent, and its BDAT's range.</summary>
        private sealed class Bone
        {
            public int Parent;
            public Quaternion Rotation;
            public Vector3 Translation;
            public Vector3 Scale;
            public int Start;
            public int End;

            /// <summary>The bone a PBON holds, or null when it has no readable BDAT.</summary>
            public static Bone Read(byte[] data, int start, int end)
            {
                if (start + 4 > end)
                    return null;

                var parent = BitConverter.ToInt32(data, start);

                foreach (var (_, s, e) in Scan(data, start, end, "BDAT"))
                {
                    var nameEnd = Array.IndexOf(data, (byte)0, s);

                    if (nameEnd < 0 || nameEnd + 41 > e)
                        return null;

                    var o = nameEnd + 1;

                    return new Bone
                    {
                        Parent = parent,
                        Rotation = new Quaternion(BitConverter.ToSingle(data, o), BitConverter.ToSingle(data, o + 4), BitConverter.ToSingle(data, o + 8), BitConverter.ToSingle(data, o + 12)),
                        Translation = ReadVector3(data, o + 16),
                        Scale = ReadVector3(data, o + 28),
                        Start = s,
                        End = e
                    };
                }

                return null;
            }
        }

        /// <summary>
        /// A point in bone <paramref name="index"/>'s frame, carried through that bone and each parent
        /// in turn into the object's frame. A parent index out of range, or a loop, ends the chain.
        /// </summary>
        private static Vector3 ToObject(List<Bone> bones, int index, Vector3 p)
        {
            for (var depth = 0; index >= 0 && index < bones.Count && bones[index] != null && depth < bones.Count; depth++)
            {
                var bone = bones[index];
                p = Vector3.Transform(p * bone.Scale, bone.Rotation) + bone.Translation;
                index = bone.Parent;
            }

            return p;
        }

        private void ReadCollision(byte[] data, List<Bone> bones, int index)
        {
            var s = bones[index].Start;
            var e = bones[index].End;

            Vector3 Transform(Vector3 p) => ToObject(bones, index, p);

            foreach (var (_, vs, ve) in Scan(data, s, e, "BVWS"))
            {
                var p = vs + 20; // past the BVOL{radius} chunk
                var triangleCount = BitConverter.ToInt32(data, p);
                var vertexCount = BitConverter.ToInt32(data, p + 4);
                p += 8;

                if (triangleCount <= 0 || vertexCount <= 0 || p + vertexCount * 12 + triangleCount * 6 > ve + 1)
                    continue;

                var baseIndex = Vertices.Count;

                for (var i = 0; i < vertexCount; i++)
                    Vertices.Add(Transform(ReadVector3(data, p + i * 12)));

                p += vertexCount * 12;

                for (var i = 0; i < triangleCount * 3; i++)
                {
                    var vertex = BitConverter.ToUInt16(data, p + i * 2);
                    Triangles.Add(baseIndex + Math.Min(vertex, vertexCount - 1));
                }
            }

            foreach (var (_, bs, be) in Scan(data, s, e, "BVBX"))
            {
                var centre = ReadVector3(data, bs + 20);
                var half = ReadVector3(data, bs + 32);
                var baseIndex = Vertices.Count;

                for (var sx = -1; sx <= 1; sx += 2)
                    for (var sy = -1; sy <= 1; sy += 2)
                        for (var sz = -1; sz <= 1; sz += 2)
                            Vertices.Add(Transform(centre + new Vector3(sx * half.X, sy * half.Y, sz * half.Z)));

                // corner index = (x>0)*4 + (y>0)*2 + (z>0); each face as two triangles
                int[][] faces =
                {
                    new[] { 0, 1, 3, 2 }, new[] { 4, 6, 7, 5 }, new[] { 0, 4, 5, 1 },
                    new[] { 2, 3, 7, 6 }, new[] { 0, 2, 6, 4 }, new[] { 1, 5, 7, 3 }
                };

                foreach (var f in faces)
                {
                    Triangles.Add(baseIndex + f[0]); Triangles.Add(baseIndex + f[1]); Triangles.Add(baseIndex + f[2]);
                    Triangles.Add(baseIndex + f[0]); Triangles.Add(baseIndex + f[2]); Triangles.Add(baseIndex + f[3]);
                }
            }
        }

        private static Vector3 ReadVector3(byte[] data, int offset)
        {
            return new Vector3(BitConverter.ToSingle(data, offset), BitConverter.ToSingle(data, offset + 4), BitConverter.ToSingle(data, offset + 8));
        }

        /// <summary>
        /// Every chunk with this tag inside [start, end) whose header is plausible, in file order,
        /// as (version, payloadStart, payloadEnd). Tags are written reversed in the file.
        /// </summary>
        internal static IEnumerable<(uint version, int start, int end)> Scan(byte[] data, int start, int end, string tag)
        {
            var pattern = new byte[4];

            for (var i = 0; i < 4; i++)
                pattern[i] = (byte)tag[3 - i];

            var o = IndexOf(data, pattern, start, end);

            while (o >= 0 && o + 16 <= end)
            {
                var size = BitConverter.ToUInt32(data, o + 4);
                var version = BitConverter.ToUInt32(data, o + 8);
                var unknown = BitConverter.ToUInt32(data, o + 12);

                if (unknown == 0 && size < int.MaxValue && o + 16 + (long)size <= end)
                {
                    yield return (version, o + 16, o + 16 + (int)size);
                    o = IndexOf(data, pattern, o + 16 + (int)size, end);
                }
                else
                    o = IndexOf(data, pattern, o + 1, end);
            }
        }

        private static int IndexOf(byte[] data, byte[] pattern, int start, int end)
        {
            var last = Math.Min(end, data.Length) - pattern.Length;

            for (var i = start; i <= last; i++)
            {
                if (data[i] != pattern[0])
                    continue;

                var match = true;

                for (var j = 1; j < pattern.Length; j++)
                    if (data[i + j] != pattern[j]) { match = false; break; }

                if (match)
                    return i;
            }

            return -1;
        }
    }
}
