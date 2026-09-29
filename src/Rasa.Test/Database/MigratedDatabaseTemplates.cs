using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Test.Database
{
    /// <summary>
    /// A fresh SQLite database brought to the latest migration by copying one that was migrated
    /// once in this test run, per context type. A full World migration takes seconds (the
    /// preloaded item templates, the seed, every data migration after it), and the Bootcamp
    /// harnesses make a new World database for every test. Tests that are about migrating keep
    /// calling Migrate themselves.
    /// </summary>
    internal static class MigratedDatabaseTemplates
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<string, string> Templates = new();

        /// <param name="migrate">How the caller migrates (Database.Migrate, RasaDbContextBase.Initialize); run once per context type.</param>
        internal static void Migrate(DbContext context, Action migrate)
        {
            var connection = context.Database.GetDbConnection();
            var path = new SqliteConnectionStringBuilder(connection.ConnectionString).DataSource;
            var key = context.GetType().FullName + "|" + string.Join(",", context.Database.GetMigrations());

            lock (Gate)
            {
                if (Templates.TryGetValue(key, out var template) && File.Exists(template)
                    && !context.Database.GetAppliedMigrations().Any())
                {
                    Release(context, connection);
                    File.Copy(template, path, overwrite: true);
                    return;
                }

                migrate();

                Release(context, connection);
                var copy = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "..",
                    $"template-{context.GetType().Name}-{Environment.ProcessId}-{Templates.Count}.db");
                File.Copy(path, copy, overwrite: true);
                Templates[key] = copy;
            }
        }

        private static void Release(DbContext context, System.Data.Common.DbConnection connection)
        {
            context.Database.CloseConnection();

            if (connection is SqliteConnection sqlite)
                SqliteConnection.ClearPool(sqlite);
        }
    }
}
