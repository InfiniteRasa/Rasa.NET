using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Rasa.Context
{
    using Configuration;

    public class DesignTimeAuthContextFactory : IDesignTimeDbContextFactory<AuthContext>
    {
        public AuthContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build();

            var authConnection = configuration
                .GetSection("Auth")
                .Get<DatabaseConnectionConfiguration>();

            var connectionStringFactory = new ConnectionStringFactory();
            var connectionString = connectionStringFactory.Create(authConnection);

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new AuthContext(optionsBuilder.Options);
        }
    }
}