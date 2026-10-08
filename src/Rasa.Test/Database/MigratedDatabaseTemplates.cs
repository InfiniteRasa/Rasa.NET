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
    /// once, per context type. A full World migration takes many seconds (the preloaded item
    /// templates, the seed, every data migration after it), and the Bootcamp harnesses make a
    /// new World database for every test. Tests that are about migrating keep calling Migrate
    /// themselves.
    ///
    /// The migrated copy is kept in the TestDatabases folder from one test run to the next, so
    /// a run of a single class does not start by migrating a World either. It is named by what
    /// made it - the context, its list of migrations, and the build of the assembly they are
    /// in - so a change to any migration or preloader makes a new one, and the ones before it
    /// are deleted.
    /// </summary>
    internal static class MigratedDatabaseTemplates
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<string, string> Templates = new();

        /// <param name="migrate">How the caller migrates (Database.Migrate, RasaDbContextBase.Initialize); run once per context type and build.</param>
        internal static void Migrate(DbContext context, Action migrate)
        {
            var connection = context.Database.GetDbConnection();
            var path = new SqliteConnectionStringBuilder(connection.ConnectionString).DataSource;
            var key = context.GetType().FullName + "|" + string.Join(",", context.Database.GetMigrations())
                + "|" + context.GetType().Assembly.ManifestModule.ModuleVersionId;

            lock (Gate)
            {
                var folder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, ".."));
                var prefix = $"template-{context.GetType().Name}-";

                if (!Templates.TryGetValue(key, out var template))
                    template = Path.Combine(folder, prefix + NameOf(key) + ".db");

                if (File.Exists(template) && !context.Database.GetAppliedMigrations().Any())
                {
                    Templates[key] = template;
                    Release(context, connection);
                    File.Copy(template, path, overwrite: true);
                    return;
                }

                migrate();

                Release(context, connection);

                if (File.Exists(template))
                    return;

                // Whole or not at all: another test process may be looking for it this moment.
                var partial = template + "." + Environment.ProcessId + ".tmp";

                File.Copy(path, partial, overwrite: true);

                try
                {
                    File.Move(partial, template, overwrite: false);
                }
                catch (IOException)
                {
                    // Another process made the same one first.
                    File.Delete(partial);
                }

                Templates[key] = template;
                DeleteOthers(folder, prefix, template);
            }
        }

        /// <summary>A file name for a key: the first sixteen hex digits of its SHA-256.</summary>
        private static string NameOf(string key) =>
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key))).Substring(0, 16).ToLowerInvariant();

        /// <summary>
        /// Deletes the templates of this context that an older build or list of migrations made.
        /// One made in the last two hours is left for a later run to delete: a test process of
        /// another build may be running beside this one.
        /// </summary>
        private static void DeleteOthers(string folder, string prefix, string keep)
        {
            foreach (var other in Directory.EnumerateFiles(folder, prefix + "*.db"))
            {
                if (string.Equals(Path.GetFullPath(other), Path.GetFullPath(keep), StringComparison.OrdinalIgnoreCase)
                    || File.GetLastWriteTimeUtc(other) > DateTime.UtcNow.AddHours(-2))
                    continue;

                try
                {
                    File.Delete(other);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
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
