using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;

    /// <summary>
    /// A map's flowing lava (LavaFlows) as triangles in the world, built the first time the map is
    /// asked about and kept: each placed mesh's lava, put where the map puts it, with the box it
    /// fits in so that a point nowhere near it costs one comparison.
    ///
    /// A triangle is touched in one of two ways, by how it lies:
    ///  - one that can be stood on - no steeper than <see cref="SteepNormalY"/> - is touched from
    ///    above it or just under: the point is over the triangle, and its height is within the
    ///    given distances of the lava's there. A river's bed, a slope, the top of a fall.
    ///  - a steeper one, a fall's sheet, is touched from beside it: the point is within the given
    ///    reach of the sheet, measured straight out from it.
    /// </summary>
    public static class LavaFlowFields
    {
        /// <summary>
        /// The upward part of a triangle's unit normal below which it is a fall's sheet rather
        /// than ground: steeper than about 70 degrees from level.
        /// </summary>
        public const float SteepNormalY = 0.35f;

        private readonly struct Triangle
        {
            public readonly Vector3 A, B, C, Normal;
            public readonly bool Steep;

            public Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;

                var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));

                // Whichever way round the corners run, "above" is up.
                Normal = normal.Y < 0 ? -normal : normal;
                Steep = Normal.Y < SteepNormalY;
            }
        }

        private sealed class Piece
        {
            public Vector3 Min = new Vector3(float.MaxValue);
            public Vector3 Max = new Vector3(float.MinValue);
            public readonly List<Triangle> Triangles = new List<Triangle>();
        }

        private static readonly ConcurrentDictionary<string, Piece[]> Fields = new ConcurrentDictionary<string, Piece[]>();

        /// <summary>Whether the map has any flowing lava.</summary>
        public static bool Has(string mapName) => mapName != null && LavaFlows.ByMap.ContainsKey(mapName);

        /// <summary>How many triangles of lava the map's pieces come to. For tests and for a GM's curiosity.</summary>
        public static int TriangleCount(string mapName)
        {
            var count = 0;

            foreach (var piece in Of(mapName))
                count += piece.Triangles.Count;

            return count;
        }

        /// <summary>
        /// Whether a point touches the map's flowing lava: no more than <paramref name="above"/>
        /// over ground-like lava or <paramref name="below"/> under it, or within
        /// <paramref name="reach"/> of a fall's sheet.
        /// </summary>
        public static bool Touches(string mapName, Vector3 point, float above, float below, float reach)
        {
            var margin = Math.Max(Math.Max(above, below), reach);

            foreach (var piece in Of(mapName))
            {
                if (point.X < piece.Min.X - margin || point.X > piece.Max.X + margin
                    || point.Y < piece.Min.Y - margin || point.Y > piece.Max.Y + margin
                    || point.Z < piece.Min.Z - margin || point.Z > piece.Max.Z + margin)
                    continue;

                foreach (var triangle in piece.Triangles)
                    if (triangle.Steep ? Beside(triangle, point, reach) : Over(triangle, point, above, below))
                        return true;
            }

            return false;
        }

        /// <summary>Over a triangle that can be stood on, at the lava's height there: seen from above, inside it.</summary>
        private static bool Over(in Triangle t, Vector3 p, float above, float below)
        {
            var d = (t.B.Z - t.C.Z) * (t.A.X - t.C.X) + (t.C.X - t.B.X) * (t.A.Z - t.C.Z);

            if (Math.Abs(d) < 1e-6f)
                return false;

            var u = ((t.B.Z - t.C.Z) * (p.X - t.C.X) + (t.C.X - t.B.X) * (p.Z - t.C.Z)) / d;
            var v = ((t.C.Z - t.A.Z) * (p.X - t.C.X) + (t.A.X - t.C.X) * (p.Z - t.C.Z)) / d;
            var w = 1f - u - v;

            if (u < -Edge || v < -Edge || w < -Edge)
                return false;

            var lava = u * t.A.Y + v * t.B.Y + w * t.C.Y;

            return p.Y <= lava + above && p.Y >= lava - below;
        }

        /// <summary>Beside a fall's sheet: straight out from a point of it, no further than the reach.</summary>
        private static bool Beside(in Triangle t, Vector3 p, float reach)
        {
            var distance = Vector3.Dot(p - t.A, t.Normal);

            if (Math.Abs(distance) > reach)
                return false;

            // The foot of the perpendicular, and whether it is inside the triangle.
            var foot = p - distance * t.Normal;
            var v0 = t.B - t.A;
            var v1 = t.C - t.A;
            var v2 = foot - t.A;
            var d00 = Vector3.Dot(v0, v0);
            var d01 = Vector3.Dot(v0, v1);
            var d11 = Vector3.Dot(v1, v1);
            var d20 = Vector3.Dot(v2, v0);
            var d21 = Vector3.Dot(v2, v1);
            var d = d00 * d11 - d01 * d01;

            if (Math.Abs(d) < 1e-9f)
                return false;

            var v = (d11 * d20 - d01 * d21) / d;
            var w = (d00 * d21 - d01 * d20) / d;

            return v >= -Edge && w >= -Edge && v + w <= 1f + Edge;
        }

        /// <summary>How far outside a triangle, as a share of it, still counts as on its edge: neighbours share edges, and a point on one must be in one of them.</summary>
        private const float Edge = 1e-4f;

        private static Piece[] Of(string mapName)
        {
            if (mapName == null || !LavaFlows.ByMap.TryGetValue(mapName, out var flows))
                return Array.Empty<Piece>();

            return Fields.GetOrAdd(mapName, _ => Build(flows));
        }

        private static Piece[] Build(LavaFlow[] flows)
        {
            var pieces = new List<Piece>(flows.Length);

            foreach (var flow in flows)
            {
                if (!LavaFlows.Shapes.TryGetValue(flow.Shape, out var shape))
                    continue;

                var corners = new Vector3[shape.Corners.Length / 3];

                for (var i = 0; i < corners.Length; i++)
                    corners[i] = flow.ToWorld(new Vector3(shape.Corners[i * 3], shape.Corners[i * 3 + 1], shape.Corners[i * 3 + 2]));

                var piece = new Piece();

                foreach (var corner in corners)
                {
                    piece.Min = Vector3.Min(piece.Min, corner);
                    piece.Max = Vector3.Max(piece.Max, corner);
                }

                for (var i = 0; i + 2 < shape.Triangles.Length; i += 3)
                {
                    var a = corners[shape.Triangles[i]];
                    var b = corners[shape.Triangles[i + 1]];
                    var c = corners[shape.Triangles[i + 2]];

                    // A sliver with no area has no direction to be above.
                    if (Vector3.Cross(b - a, c - a).LengthSquared() > 1e-10f)
                        piece.Triangles.Add(new Triangle(a, b, c));
                }

                if (piece.Triangles.Count > 0)
                    pieces.Add(piece);
            }

            return pieces.ToArray();
        }
    }
}
