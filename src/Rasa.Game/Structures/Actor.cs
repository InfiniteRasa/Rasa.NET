using System.Numerics;

namespace Rasa.Structures
{
    using Positioning;

    public class Actor : Entity, IHasPosition
    {
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
    }
}
