using Microsoft.EntityFrameworkCore.Design;

namespace Rasa.Context.Auth
{
    using Configuration;

    public class DesignTimeMySqlAuthContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<MySqlAuthContext>
    {
        public MySqlAuthContext CreateDbContext(string[] args)
        {
            var options = CreateDbContextOptions("Auth", DatabaseProvider.MySql);

            return new MySqlAuthContext(options);
        }
    }
}