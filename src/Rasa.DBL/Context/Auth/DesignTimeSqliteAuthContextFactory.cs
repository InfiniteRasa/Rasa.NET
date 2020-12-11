using Microsoft.EntityFrameworkCore.Design;

namespace Rasa.Context.Auth
{
    using Configuration;

    public class DesignTimeSqliteAuthContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<SqliteAuthContext>
    {
        public SqliteAuthContext CreateDbContext(string[] args)
        {
            var options = CreateDbContextOptions("Auth", DatabaseProvider.Sqlite);

            return new SqliteAuthContext(options);
        }
    }
}