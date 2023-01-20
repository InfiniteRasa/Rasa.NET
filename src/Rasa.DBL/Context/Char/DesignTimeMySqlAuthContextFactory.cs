using Microsoft.EntityFrameworkCore.Design;

using JetBrains.Annotations;

namespace Rasa.Context.Char
{
    using Configuration;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeMySqlCharContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<MySqlCharContext>
    {
        public MySqlCharContext CreateDbContext(string[] args)
        {
            return CreateDbContext<MySqlCharContext>(DatabaseProvider.MySql);
        }
    }
}