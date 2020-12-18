using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.World
{
    using UnitOfWork;

    public class WorldUnitOfWork : UnitOfWork, IWorldUnitOfWork
    {
        public WorldUnitOfWork(DbContext dbContext,
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