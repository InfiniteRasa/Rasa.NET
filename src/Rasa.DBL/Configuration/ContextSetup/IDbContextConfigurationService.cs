using Microsoft.EntityFrameworkCore;

namespace Rasa.Configuration.ContextSetup
{
    public interface IDbContextConfigurationService
    {
        void Configure(DbContextOptionsBuilder dbContextOptionsBuilder, DatabaseConnectionConfiguration configuration);
    }
}