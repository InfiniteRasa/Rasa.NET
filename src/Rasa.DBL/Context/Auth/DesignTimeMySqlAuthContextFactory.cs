using Microsoft.EntityFrameworkCore.Design;

using JetBrains.Annotations;

namespace Rasa.Context.Auth
{
    using Configuration;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeMySqlAuthContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<MySqlAuthContext>
    {
        public MySqlAuthContext CreateDbContext(string[] args)
        {
            return CreateDbContext<MySqlAuthContext>(DatabaseProvider.MySql);
        }
    }
}