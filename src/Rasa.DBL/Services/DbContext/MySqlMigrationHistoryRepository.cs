using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Pomelo.EntityFrameworkCore.MySql.Migrations.Internal;

namespace Rasa.Services.DbContext
{
    // MySQL's GET_LOCK refuses names over 64 characters, and Pomelo builds the migration lock name
    // from the database name. The lock name can only be changed by overriding GetDatabaseLockName
    // on Pomelo's internal MySqlHistoryRepository: there's no option for it, and implementing
    // IHistoryRepository on the public HistoryRepository base would mean copying Pomelo's locking
    // and history-table SQL. So this deliberately uses the internal API (EF1001). A Pomelo upgrade
    // that changes it breaks the build here, and MySqlPlatformCompatibilityTests checks both the
    // registration and the lock names.
#pragma warning disable EF1001
    internal sealed class MySqlMigrationHistoryRepository : MySqlHistoryRepository
    {
        public MySqlMigrationHistoryRepository(HistoryRepositoryDependencies dependencies)
            : base(dependencies)
        {
        }

        protected override string GetDatabaseLockName(string databaseName)
        {
            return BoundLockName(base.GetDatabaseLockName(databaseName));
        }
#pragma warning restore EF1001

        internal static string BoundLockName(string name)
        {
            if (name.Length <= 64)
                return name;

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name.ToLowerInvariant())));
        }
    }
}
