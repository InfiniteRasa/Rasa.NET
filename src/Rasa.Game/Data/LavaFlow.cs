using System.Numerics;

namespace Rasa.Data
{
    /// <summary>
    /// The lava of one mesh, in the mesh's own frame (LavaFlows.Shapes): its corners, x, y and z
    /// each, and its triangles, three corners each.
    /// </summary>
    public sealed class LavaShape
    {
        public float[] Corners { get; }
        public ushort[] Triangles { get; }

        public LavaShape(float[] corners, ushort[] triangles)
        {
            Corners = corners;
            Triangles = triangles;
        }
    }

    /// <summary>One place a map puts a mesh that has lava (LavaFlows.ByMap), as the client's .map file has it.</summary>
    public readonly struct LavaFlow
    {
        /// <summary>The mesh: a key of LavaFlows.Shapes.</summary>
        public string Shape { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Scale { get; }

        public LavaFlow(string shape, float x, float y, float z, float rotationX, float rotationY, float rotationZ, float rotationW, float scale)
        {
            Shape = shape;
            Position = new Vector3(x, y, z);
            Rotation = Quaternion.Normalize(new Quaternion(rotationX, rotationY, rotationZ, rotationW));
            Scale = scale;
        }

        /// <summary>A point of the mesh's frame, where the map puts it: scaled, turned, then moved, as the client does.</summary>
        public Vector3 ToWorld(Vector3 point) => Vector3.Transform(point * Scale, Rotation) + Position;
    }
}
