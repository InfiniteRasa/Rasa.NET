using System.Numerics;

namespace Rasa.Repositories.Char.Character
{
    public interface ICharacterChange
    {
        uint Id { get; }
        bool IsRunning { get; }
        bool IsCrouching { get; }
        Vector3 Position { get; }
        double Rotation { get; }
        uint MapContextId { get; }
    }
}
