using System.Numerics;

namespace Rasa.Structures
{
    public sealed class MissionIndicator
    {
        public Vector3 Position { get; init; }
        public double Radius { get; init; }
        public uint IndicatorId { get; init; }
        public bool Show3DEffect { get; init; }
    }
}
