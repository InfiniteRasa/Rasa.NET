using Microsoft.EntityFrameworkCore.Design;

using JetBrains.Annotations;

namespace Rasa.Context.World
{
    using Configuration;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeMySqlWorldContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<MySqlWorldContext>
    {
        public MySqlWorldContext CreateDbContext(string[] args)
        {
            return CreateDbContext<MySqlWorldContext>(DatabaseProvider.MySql);
        }
    }
}