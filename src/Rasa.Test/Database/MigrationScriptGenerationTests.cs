using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Auth;
using Rasa.Context.Char;
using Rasa.Context.World;

namespace Rasa.Test.Database
{
    /// <summary>
    /// Every migration's Up and Down can be turned into SQL on the model it runs against.
    /// EF builds the operations of a step on a model: the Up of M on M's target model, the Down
    /// of M on the target model of the migration before it (the nearest earlier one with a
    /// Designer file). A data operation without column types (InsertData("mission_trigger", ...)
    /// with no columnTypes) needs that model to map the table; when the model before it has no
    /// Designer file, the generation throws "There is no entity type mapped to the table ..."
    /// and every test that migrates past it fails. Such a migration passes its own tests on the
    /// branch it was written on and breaks only after a merge puts another migration before it,
    /// so this checks every step of every context, on both providers, at once.
    /// No database: GenerateScript only reads the migrations assembly and the models.
    /// </summary>
    [TestClass]
    public class MigrationScriptGenerationTests
    {
        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        [DataRow(typeof(SqliteCharContext))]
        [DataRow(typeof(MySqlCharContext))]
        [DataRow(typeof(SqliteAuthContext))]
        [DataRow(typeof(MySqlAuthContext))]
        public void EveryMigrationsUpAndDownGenerateOnTheModelTheyRunAgainst(Type contextType)
        {
            var watch = Stopwatch.StartNew();
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var migrator = context.GetService<IMigrator>();
            var ids = context.Database.GetMigrations().ToList();
            Assert.IsNotEmpty(ids);

            // No floor: every step back to the empty database (Migration.InitialDatabase, "0")
            // is checked. The consolidated schemas (ConsolidatedWorldSchema and the others) have
            // a real Down (MigrationConsolidationTests rolls World and Char back through them to
            // the preserved boundary and up again), and the restamped ids
            // (Forget_restamped_migration_ids) have a Down that generates too, so no step is
            // left out. Generating a script never runs it: a Down that would lose data still
            // has to be valid SQL.
            var failures = new List<string>();
            for (var index = 0; index < ids.Count; index++)
            {
                var migration = ids[index];
                var previous = index == 0 ? Migration.InitialDatabase : ids[index - 1];
                Generate(migrator, previous, migration, "Up", failures);
                Generate(migrator, migration, previous, "Down", failures);
            }

            Assert.IsEmpty(failures,
                $"{contextType.Name}: {failures.Count} migration step(s) cannot be generated:\n" +
                string.Join("\n", failures));
            Console.WriteLine($"{contextType.Name}: {ids.Count} migrations, Up and Down, in {watch.Elapsed}");
        }

        private static void Generate(IMigrator migrator, string from, string to, string direction, List<string> failures)
        {
            var migration = direction == "Up" ? to : from;
            try
            {
                migrator.GenerateScript(from, to);
            }
            catch (Exception exception)
            {
                failures.Add($"{migration} ({direction}): {exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}
