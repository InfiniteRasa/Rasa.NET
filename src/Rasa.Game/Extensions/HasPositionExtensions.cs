using System.Numerics;

namespace Rasa.Extensions
{
    using Structures.Positioning;
    public static class HasPositionExtensions
    {
        public static bool IsNear(this IHasPosition first, IHasPosition second)
        {
            return Vector3.Distance(first.Position, second.Position) < 2.0f;
        }
    }
}