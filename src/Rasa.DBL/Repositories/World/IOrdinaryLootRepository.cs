namespace Rasa.Repositories.World
{
    using Structures.World;

    public interface IOrdinaryLootRepository
    {
        OrdinaryLootContentSnapshot Get();
    }
}
