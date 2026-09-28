using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using JetBrains.Annotations;

namespace Rasa.Context.World
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Services.DbContext;

    /// <summary>
    /// Used by EF Core to create and execute migrations.
    /// </summary>
    [UsedImplicitly]
    public class DesignTimeMySqlWorldContextFactory : DesignTimeContextFactoryBase, IDesignTimeDbContextFactory<MySqlWorldContext>
    {
        public MySqlWorldContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build();
            var databases = new DatabaseConfiguration();
            configuration.GetSection("Databases").Bind(databases);
            databases.Provider = DatabaseProvider.MySql.ToString();

            return new MySqlWorldContext(
                Options.Create(databases),
                new MySqlDbContextConfigurationService(
                    new MySqlConnectionStringFactory(),
                    _ => new MySqlServerVersion(new Version(8, 4, 0))),
                new MySqlDbContextPropertyModifier());
        }
    }
}