using System.Numerics;

namespace Rasa.Extensions
{
    using Structures.Positioning;
    public static class HasPositionExtensions
    {
        public static bool IsNear2m(this IHasPosition first, IHasPosition second)
        {
            return Vector3.Distance(first.Position, second.Position) < 2.0f;
        }

        public static bool IsNear5m(this IHasPosition first, IHasPosition second)
        {
            return Vector3.Distance(first.Position, second.Position) < 5.0f;
        }

        public static bool IsNear10m(this IHasPosition first, IHasPosition second)
        {
            return Vector3.Distance(first.Position, second.Position) < 10.0f;
        }
    }
}