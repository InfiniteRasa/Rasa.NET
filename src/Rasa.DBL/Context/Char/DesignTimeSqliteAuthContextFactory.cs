using Microsoft.EntityFrameworkCore.Design;

using JetBrains.Annotations;

namespace Rasa.Context.Char
{
    using Configuration;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeSqliteCharContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<SqliteCharContext>
    {
        public SqliteCharContext CreateDbContext(string[] args)
        {
            return CreateDbContext<SqliteCharContext>(DatabaseProvider.Sqlite);
        }
    }
}