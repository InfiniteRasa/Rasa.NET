using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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

            Console.WriteLine($"{name}: applying {pending.Count} MySQL migration(s)...");
            var migrator = context.GetService<IMigrator>();
            for (var i = 0; i < pending.Count; i++)
            {
                var timer = Stopwatch.StartNew();
                Console.WriteLine($"{name}: [{i + 1}/{pending.Count}] {pending[i]} - starting");
                migrator.Migrate(pending[i]);
                Console.WriteLine($"{name}: [{i + 1}/{pending.Count}] {pending[i]} - done in {timer.ElapsedMilliseconds} ms");
            }
            Console.WriteLine($"{name}: all MySQL migrations applied.");
        }
    }
}
