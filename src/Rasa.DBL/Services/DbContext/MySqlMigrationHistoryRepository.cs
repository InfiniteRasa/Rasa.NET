using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Pomelo.EntityFrameworkCore.MySql.Migrations.Internal;

namespace Rasa.Services.DbContext
{
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

        internal static string BoundLockName(string name)
        {
            if (name.Length <= 64)
                return name;

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name.ToLowerInvariant())));
        }
    }
}
