namespace Rasa.Repositories.Char.Character
{
    public interface ICharacterChange
    {
        uint Id { get; }
        bool IsRunning { get; }
    }
}