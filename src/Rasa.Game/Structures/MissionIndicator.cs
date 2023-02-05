using System.Numerics;

namespace Rasa.Structures
{
    public class MissionIndicator
    {
        public Vector3 Position { get; set; }
        public double Radius { get; set; }
        public uint IndicatorId { get; set; }
        public bool Show3DEffect { get; set; }
    }
}
