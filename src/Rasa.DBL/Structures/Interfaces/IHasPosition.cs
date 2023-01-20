using System.Numerics;

namespace Rasa.Structures.Interfaces
{
    public interface IHasPosition
    {
        Vector3 Position { get; }
        double Rotation { get; }
    }
}