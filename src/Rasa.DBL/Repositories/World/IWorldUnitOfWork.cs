namespace Rasa.Repositories.World
{
    using UnitOfWork;

    public interface IWorldUnitOfWork : IUnitOfWork
    {
        IItemTemplateItemClassRepository ItemTemplateItemClassRepository { get; }

        IPlayerRandomNameRepository RandomNameRepository { get; }
    }
}