using Microsoft.EntityFrameworkCore.Design;

using JetBrains.Annotations;

namespace Rasa.Context.Auth
{
    using Configuration;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeSqliteAuthContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<SqliteAuthContext>
    {
        public SqliteAuthContext CreateDbContext(string[] args)
        {
            return CreateDbContext<SqliteAuthContext>(DatabaseProvider.Sqlite);
        }
    }
}