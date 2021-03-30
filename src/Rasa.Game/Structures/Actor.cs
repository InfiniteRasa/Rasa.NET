using System.Numerics;

namespace Rasa.Structures
{
    public class Actor : Entity
    {
        public Vector3 Position { get; set; }

        public double Rotation { get; set; }
        
        public bool IsCrouching { get; set; }
    }
}
