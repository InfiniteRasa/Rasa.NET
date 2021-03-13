namespace Rasa.Repositories.Char.Character
{
    using System.Numerics;

    public interface ICharacterChange
    {
        uint Id { get; }
        bool IsRunning { get; }
        bool IsCrouching { get; }
        Vector3 Position { get; }
        double Rotation { get; }    }
}