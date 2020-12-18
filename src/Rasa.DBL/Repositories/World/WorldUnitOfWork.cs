using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.World
{
    using Context.World;
    using UnitOfWork;

    public class WorldUnitOfWork : UnitOfWork, IWorldUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public WorldUnitOfWork(WorldContext dbContext,
            IItemTemplateItemClassRepository itemTemplateItemClassRepository,
            IPlayerRandomNameRepository randomNameRepository)
                : base(dbContext)
        {
            ItemTemplateItemClassRepository = itemTemplateItemClassRepository;
            RandomNameRepository = randomNameRepository;
        }

        public IItemTemplateItemClassRepository ItemTemplateItemClassRepository { get; }

        public IPlayerRandomNameRepository RandomNameRepository { get; }
    }
}