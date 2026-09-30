using System;

namespace Rasa.Data
{
    /// <summary>
    /// One water plane on a map (WaterSurfaces): the height of its surface and the four world
    /// (x, z) corners of its footprint, in order around it.
    /// </summary>
    public readonly struct WaterSurface
    {
        public float SurfaceY { get; }
        public float X1 { get; }
        public float Z1 { get; }
        public float X2 { get; }
        public float Z2 { get; }
        public float X3 { get; }
        public float Z3 { get; }
        public float X4 { get; }
        public float Z4 { get; }

        public WaterSurface(float surfaceY, float x1, float z1, float x2, float z2, float x3, float z3, float x4, float z4)
        {
            SurfaceY = surfaceY;
            X1 = x1; Z1 = z1;
            X2 = x2; Z2 = z2;
            X3 = x3; Z3 = z3;
            X4 = x4; Z4 = z4;
        }

        /// <summary>Whether (x, z) is inside the footprint, or within <paramref name="margin"/> metres of its edge.</summary>
        public bool Covers(float x, float z, float margin = 0f)
        {
            if (Inside(x, z))
                return true;

            return margin > 0f
                && (EdgeDistance(x, z, X1, Z1, X2, Z2) <= margin || EdgeDistance(x, z, X2, Z2, X3, Z3) <= margin
                    || EdgeDistance(x, z, X3, Z3, X4, Z4) <= margin || EdgeDistance(x, z, X4, Z4, X1, Z1) <= margin);
        }

        /// <summary>Inside the quadrilateral: on the same side of all four edges, whichever way round they run.</summary>
        private bool Inside(float x, float z)
        {
            var a = Cross(X1, Z1, X2, Z2, x, z);
            var b = Cross(X2, Z2, X3, Z3, x, z);
            var c = Cross(X3, Z3, X4, Z4, x, z);
            var d = Cross(X4, Z4, X1, Z1, x, z);

            return (a >= 0 && b >= 0 && c >= 0 && d >= 0) || (a <= 0 && b <= 0 && c <= 0 && d <= 0);
        }

        private static float Cross(float ax, float az, float bx, float bz, float px, float pz) =>
            (bx - ax) * (pz - az) - (bz - az) * (px - ax);

        private static float EdgeDistance(float px, float pz, float ax, float az, float bx, float bz)
        {
            var dx = bx - ax;
            var dz = bz - az;
            var lengthSquared = dx * dx + dz * dz;
            var t = lengthSquared > 0 ? Math.Clamp(((px - ax) * dx + (pz - az) * dz) / lengthSquared, 0f, 1f) : 0f;
            var cx = ax + t * dx - px;
            var cz = az + t * dz - pz;

            return MathF.Sqrt(cx * cx + cz * cz);
        }
    }
}
