namespace Rasa.Repositories.World
{
    using Structures.World;

    public interface IOrdinaryLootRepository
    {
        OrdinaryLootContentSnapshot Get();

        /// <summary>Stages validated complete content inside the caller's World transaction. Never commits or publishes.</summary>
        void Replace(OrdinaryLootContentSnapshot content) =>
            throw new System.NotSupportedException("This ordinary loot repository is read-only.");
    }
}
