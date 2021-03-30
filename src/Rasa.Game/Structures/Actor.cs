using System.Numerics;

namespace Rasa.Structures
{
    using System;
    using Data;
    using Positioning;

    public class Actor : Entity
    {
        public Vector3 Position { get; set; }

        public double Rotation { get; set; }
        
        public bool IsCrouching { get; set; }

        public void SetIsCrouching(Posture posture)
        {
            switch (posture)
            {
                case Posture.Crouching:
                    IsCrouching = true;
                    break;
                case Posture.Standing:
                    IsCrouching = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(posture), posture, null);
            }
        }

        public Posture GetPosture()
        {
            return IsCrouching
                ? Posture.Crouching
                : Posture.Standing;
        }
    }
}
