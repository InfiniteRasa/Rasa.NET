using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories
{
    public abstract class UnitOfWork
    {
        private readonly DbContext _dbContext;

        protected UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool AutoComplete { get; set; }

        public void Complete()
        {
            if (_dbContext.ChangeTracker.HasChanges())
            {
                _dbContext.SaveChanges();
            }
        }

        public void Reject()
        {
            if (_dbContext.ChangeTracker.HasChanges())
            {
                _dbContext.ChangeTracker.Clear();
            }
        }

        public void Dispose()
        {
            if (AutoComplete)
            {
                Complete();
            }
            Reject();
            _dbContext.Dispose();
        }
    }
}