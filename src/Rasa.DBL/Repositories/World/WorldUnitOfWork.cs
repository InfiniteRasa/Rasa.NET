using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.World
{
    using UnitOfWork;

    public class WorldUnitOfWork : UnitOfWork, IWorldUnitOfWork
    {
        public WorldUnitOfWork(DbContext dbContext) : base(dbContext)
        {
        }
    }
}