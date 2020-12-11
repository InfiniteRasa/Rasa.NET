using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Rasa.Context
{
    using Configuration;
    using Configuration.ConnectionStrings;

    public class DesignTimeAuthContextFactory : IDesignTimeDbContextFactory<AuthContext>
    {
        public AuthContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build();

            var authConnection = configuration
                .GetSection("Databases:Auth")
                .Get<DatabaseConnectionConfiguration>();

            var connectionStringFactory = new MySqlConnectionStringFactory();
            var connectionString = connectionStringFactory.Create(authConnection);

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new AuthContext(optionsBuilder.Options);
        }
    }
}