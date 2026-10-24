using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.TestSupport;

    /// <summary>
    /// Logos_those_height: the stone "Those" of Minos Caverns lifted from 2.0 m under its pedestal's floor to 2.0 m over it.
    /// One column of one row; every other stone is as it was, and Down puts the old height back.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LogosThoseHeightTests
    {
        private const string Migration = "20270303000002_Logos_those_height";

        private static List<string[]> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string[]>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(Enumerable.Range(0, reader.FieldCount)
                    .Select(i => Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)).ToArray());

            return rows;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }

        [TestMethod]
        public void TheSeedChangesOneHeightOfOneStoneUnderAGuardAndBackAgain()
        {
            Assert.HasCount(1, LogosThoseHeightSeed.UpStatements);
            Assert.HasCount(1, LogosThoseHeightSeed.DownStatements);
            Assert.StartsWith("update logos set pos_y = -30.51 ", LogosThoseHeightSeed.UpStatements[0]);
            Assert.Contains("where id = 321 and map_context_id = 1347 and abs(pos_y - (-34.48828)) < 0.01", LogosThoseHeightSeed.UpStatements[0]);
            Assert.StartsWith("update logos set pos_y = -34.48828 ", LogosThoseHeightSeed.DownStatements[0]);
            Assert.Contains("where id = 321 and map_context_id = 1347 and abs(pos_y - (-30.51)) < 0.01", LogosThoseHeightSeed.DownStatements[0]);
        }

        /// <summary>
        /// A second witness besides the client's floor: the server's own walkable ground for Minos Caverns under the stone.
        /// The old height is under it; the new one is about 2 m over it, as the other stones stand over theirs.
        /// </summary>
        [TestMethod]
        public void TheStoneWasUnderTheServersGroundAndNowStandsOverIt()
        {
            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_foreas_concordia_divide_minoscaverns"));
            var ground = nav.NearestInColumn(new Vector3(26.132812f, -32.5f, -51.117188f), 4f);

            Assert.IsNotNull(ground, "walkable ground under the stone");
            Assert.IsLessThan(ground.Value.Y - 1f, (float)LogosThoseHeightSeed.OldHeight, "the old height is under the ground");
            Assert.AreEqual(2.0, LogosThoseHeightSeed.NewHeight - ground.Value.Y, 0.75, $"the new height over the ground at {ground.Value.Y:0.00}");
        }

        [TestMethod]
        public void TheMigrationLiftsThoseAndNothingElseAndDownPutsItBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                var all = context.Database.GetMigrations().ToList();
                CollectionAssert.Contains(all, Migration, "the migration is registered");
                var previous = all[all.IndexOf(Migration) - 1];
                migrator.Migrate(previous);

                const string All = "select id, class_id, map_context_id, pos_x, pos_y, pos_z, name from logos order by id";
                var before = Rows(context, All);
                var those = before.Single(r => r[0] == "321");
                Assert.AreEqual("Those", those[6]);
                Assert.AreEqual("1347", those[2]);
                Assert.AreEqual(-34.48828, double.Parse(those[4], CultureInfo.InvariantCulture), 0.001, "as the retail capture has it, 2 m under the pedestal's floor");

                migrator.Migrate(Migration);
                var after = Rows(context, All);

                Assert.HasCount(before.Count, after);
                var changed = before.Zip(after, (b, a) => (Before: b, After: a)).Where(pair => !pair.Before.SequenceEqual(pair.After)).ToList();
                Assert.HasCount(1, changed, "one stone changes");
                Assert.AreEqual("321", changed[0].After[0]);
                Assert.AreEqual(-30.51, double.Parse(changed[0].After[4], CultureInfo.InvariantCulture), 0.001);

                // Only the height: its place on the map, its class and its name are as they were.
                CollectionAssert.AreEqual(new[] { those[0], those[1], those[2], those[3], those[5], those[6] },
                    new[] { changed[0].After[0], changed[0].After[1], changed[0].After[2], changed[0].After[3], changed[0].After[5], changed[0].After[6] });

                // The guard: run again, nothing more moves.
                context.Database.ExecuteSqlRaw(LogosThoseHeightSeed.UpStatements[0]);
                CollectionAssert.AreEqual(after.Select(r => string.Join("|", r)).ToList(), Rows(context, All).Select(r => string.Join("|", r)).ToList(), "a second run changes nothing");

                migrator.Migrate(previous);
                var back = Rows(context, All);
                Assert.HasCount(before.Count, back);

                for (var i = 0; i < before.Count; i++)
                    CollectionAssert.AreEqual(before[i], back[i], $"stone {before[i][0]} after Down");
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }
    }
}
