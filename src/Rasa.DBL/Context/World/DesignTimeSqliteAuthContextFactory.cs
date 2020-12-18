using Microsoft.EntityFrameworkCore.Design;

using JetBrains.Annotations;

namespace Rasa.Context.World
{
    using Configuration;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeSqliteWorldContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<SqliteWorldContext>
    {
        public SqliteWorldContext CreateDbContext(string[] args)
        {
            return CreateDbContext<SqliteWorldContext>(DatabaseProvider.Sqlite);
        }
    }
}