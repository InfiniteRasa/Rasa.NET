using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;

namespace Rasa.Test.Database
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context;
    using Rasa.Services.DbContext;

    /// <summary>
    /// Contexts on a live MySQL server, for the tests in the "MySql" category. The server comes from
    /// RASA_TEST_MYSQL, a MySqlConnector connection string without a database
    /// ("Server=127.0.0.1;Port=3306;User ID=root;Password=..."); the database names are the ones
    /// databasesettings.json and databasesettings.env.json give, which is where
    /// `dotnet ef database update` applied the migrations. Contexts are configured the way the servers configure them (server version
    /// detection, the bounded migration lock), not with the offline setup the model tests use.
    ///
    /// CI runs this category in its own job against mysql:8.0 and mysql:8.4 and leaves it out of
    /// the other lanes; without RASA_TEST_MYSQL the tests report inconclusive. docs/setup.md
    /// has the commands to run them locally.
    /// </summary>
    internal static class LiveMySql
    {
        internal const string Category = "MySql";
        internal const string ServerVariable = "RASA_TEST_MYSQL";

        internal static RasaDbContextBase CreateContext(Type contextType)
        {
            var server = Environment.GetEnvironmentVariable(ServerVariable);
            if (string.IsNullOrWhiteSpace(server))
                Assert.Inconclusive(
                    $"{ServerVariable} is not set. These tests need a MySQL server with the migrations applied; see docs/setup.md.");

            var builder = new MySqlConnectionStringBuilder(server);
            var databases = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("databasesettings.json", false, false)
                .AddJsonFile("databasesettings.env.json", true, false)
                .Build()
                .GetSection("Databases");

            DatabaseConnectionConfiguration Connection(string name) => new()
            {
                Host = builder.Server,
                Port = builder.Port,
                User = builder.UserID,
                Password = builder.Password,
                Database = databases[$"{name}:Database"],
                TimeoutInMilliseconds = 30000
            };

            var options = Options.Create(new DatabaseConfiguration
            {
                Provider = "MySql",
                Auth = Connection("Auth"),
                Char = Connection("Char"),
                World = Connection("World")
            });
            return (RasaDbContextBase)Activator.CreateInstance(
                contextType,
                options,
                new MySqlDbContextConfigurationService(new MySqlConnectionStringFactory()),
                new MySqlDbContextPropertyModifier());
        }
    }
}
