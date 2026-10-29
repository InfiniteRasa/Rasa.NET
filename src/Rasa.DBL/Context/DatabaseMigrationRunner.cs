using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Context
{
    /// <summary>
    /// Explicit, one-shot MySQL migration entry point. Normal server startup never
    /// migrates MySQL automatically; the Compose migration jobs invoke this first.
    /// </summary>
    public static class DatabaseMigrationRunner
    {
        public static void MigrateMySql(RasaDbContextBase context)
        {
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true)
                throw new InvalidOperationException("--migrate requires the MySql database provider.");

            var name = context.GetType().Name;
            var pending = context.Database.GetPendingMigrations().ToList();
            if (pending.Count == 0)
            {
                Console.WriteLine($"{name}: schema is up to date, nothing to apply.");
                return;
            }

            // One targetless Migrate() applies every pending migration in order. Migrating to each
            // pending id in turn is wrong: a migration dated before ones already applied (named
            // "...Before..." so it runs ahead of them on a fresh database) is an older target, and
            // EF reaches an older target by rolling back everything applied after it.
            Console.WriteLine($"{name}: applying {pending.Count} MySQL migration(s): {string.Join(", ", pending)}");
            var timer = Stopwatch.StartNew();
            context.Database.Migrate();
            Console.WriteLine($"{name}: done in {timer.ElapsedMilliseconds} ms");
            Console.WriteLine($"{name}: all MySQL migrations applied.");
        }
    }
}
