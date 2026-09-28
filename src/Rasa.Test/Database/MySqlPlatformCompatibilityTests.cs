using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Services.DbContext;

    [TestClass]
    public class MySqlPlatformCompatibilityTests
    {
        [TestMethod]
        public void ConfigurationCachesVersionsPerConnectionStringAndInstallsBoundedLocks()
        {
            var detections = new Dictionary<string, int>();
            var service = new MySqlDbContextConfigurationService(
                new TestConnectionStringFactory(),
                connectionString =>
                {
                    detections.TryGetValue(connectionString, out var count);
                    detections[connectionString] = count + 1;
                    return new MySqlServerVersion(new Version(8, 4, 0));
                });

            var first = Configure(service, "first");
            Configure(service, "first");
            Configure(service, "second");

            Assert.AreEqual(1, detections["Server=127.0.0.1;Database=first"]);
            Assert.AreEqual(1, detections["Server=127.0.0.1;Database=second"]);

            using var context = new DbContext(first);
            Assert.IsInstanceOfType<MySqlMigrationHistoryRepository>(
                context.GetService<IHistoryRepository>());
        }

        [TestMethod]
        public void MigrationLockNamesPreserveLegacyNamesAndBoundLongNames()
        {
            const string legacy = "__short_EFMigrationsLock";
            const string longName = "__AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA_EFMigrationsLock";
            const string expectedHash = "94FD040F12EEA6E8A6E2884120F399BCFA36FE6E3B86C7B1A2B00AA59279302F";

            Assert.AreEqual(legacy, MySqlMigrationHistoryRepository.BoundLockName(legacy));
            Assert.AreEqual(expectedHash, MySqlMigrationHistoryRepository.BoundLockName(longName));
            Assert.AreEqual(expectedHash, MySqlMigrationHistoryRepository.BoundLockName(longName.ToLowerInvariant()));
            Assert.AreNotEqual(
                MySqlMigrationHistoryRepository.BoundLockName(longName + "x"),
                MySqlMigrationHistoryRepository.BoundLockName(longName + "y"));
        }

        private static DbContextOptions Configure(
            MySqlDbContextConfigurationService service,
            string database)
        {
            var builder = new DbContextOptionsBuilder();
            service.Configure(builder, new DatabaseConnectionConfiguration { Database = database });
            return builder.Options;
        }

        private sealed class TestConnectionStringFactory : IConnectionStringFactory
        {
            public string Create(DatabaseConnectionConfiguration configuration)
            {
                return $"Server=127.0.0.1;Database={configuration.Database}";
            }
        }
    }
}
