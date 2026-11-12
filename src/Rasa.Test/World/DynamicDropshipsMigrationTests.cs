using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    /// <summary>The dropship migrations of the Plateau, the Pools, the Marshes and the Crucible: Up, Down, Up again, and each Up safe to run twice.</summary>
    [TestClass, DoNotParallelize]
    public class DynamicDropshipsMigrationTests
    {
        private const string Plateau = "20271105000000_Plateau_dynamic_dropships";
        private const string Pools = "20271105000100_Pools_dynamic_dropships";
        private const string Marshes = "20271105000200_Marshes_dynamic_dropships";
        private const string Crucible = "20271109000000_Crucible_dynamic_dropships";

        /// <summary>The dropship migrations in the order they must run.</summary>
        private static readonly string[] Order = { Plateau, Pools, Marshes, Crucible };

        private static void WithWorld(Action<WorldContext> test)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var database = Path.Combine(directory, "world");
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());
                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                test(world);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static (int Creatures, int Actions, int Stats, int Appearances, int Pools, int Links) Rows(WorldContext world, uint min, uint max) =>
            (world.CreatureEntries.AsNoTracking().Count(r => r.Id >= min && r.Id <= max),
             world.CreatureActionEntries.AsNoTracking().Count(r => r.Id >= min && r.Id <= max),
             world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN {min} AND {max}").AsEnumerable().Single(),
             world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN {min} AND {max}").AsEnumerable().Single(),
             world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= min && r.Id <= max),
             world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ObjectId >= min && r.ObjectId <= max));

        private static void UpDownUp(string migration, uint min, uint max, int pools, Func<IEnumerable<string>> up)
        {
            WithWorld(world =>
            {
                var after = (3, 3, 3, 2, pools, pools);
                Assert.AreEqual(after, Rows(world, min, max), $"{migration}: after");

                // Safe to run twice: each insert only where its key is not there yet.
                foreach (var sql in up())
                    world.Database.ExecuteSqlRaw(sql);
                Assert.AreEqual(after, Rows(world, min, max), $"{migration}: its Up run again adds nothing");

                world.GetService<IMigrator>().Migrate(DynamicDropshipsMigrations.Previous(world, migration));
                Assert.AreEqual((0, 0, 0, 0, 0, 0), Rows(world, min, max), $"{migration}: down");
                world.GetService<IMigrator>().Migrate(migration);
                Assert.AreEqual(after, Rows(world, min, max), $"{migration}: up again");
            });
        }

        [TestMethod]
        public void ThePlateauMigrationAddsItsRowsOnceAndItsDownTakesThemOut() =>
            UpDownUp(Plateau, 692001, 692099, PlateauDynamicDropshipsContent.Garrison.Length, PlateauDynamicDropshipsContent.Up);

        [TestMethod]
        public void ThePoolsMigrationAddsItsRowsOnceAndItsDownTakesThemOut() =>
            UpDownUp(Pools, 692101, 692199, PoolsDynamicDropshipsContent.Garrison.Length, PoolsDynamicDropshipsContent.Up);

        [TestMethod]
        public void TheMarshesMigrationAddsItsRowsOnceAndItsDownTakesThemOut() =>
            UpDownUp(Marshes, MarshesDynamicDropshipsContent.IdMin, MarshesDynamicDropshipsContent.IdMax, MarshesDynamicDropshipsContent.Garrison.Length, MarshesDynamicDropshipsContent.Up);

        [TestMethod]
        public void TheCrucibleMigrationAddsItsRowsOnceAndItsDownTakesThemOut() =>
            UpDownUp(Crucible, CrucibleDynamicDropshipsContent.IdMin, CrucibleDynamicDropshipsContent.IdMax, CrucibleDynamicDropshipsContent.Garrison.Length, CrucibleDynamicDropshipsContent.Up);

        /// <summary>The newest migration on development when the dropship migrations were written: each sorts after it.</summary>
        private const string NewestWhenWritten = "20271103000200";

        /// <summary>
        /// The dropship migrations run in their stacked order (<see cref="Order"/>), and each sorts after
        /// <see cref="NewestWhenWritten"/>, so that it runs after the zones' rows it needs. Later
        /// migrations may sort after them.
        /// </summary>
        [TestMethod]
        public void TheDropshipMigrationsComeInOrderAfterTheNewestWhenTheyWereWritten()
        {
            WithWorld(world =>
            {
                var ids = world.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
                Assert.IsTrue(ids.Any(id => id.StartsWith(NewestWhenWritten + "_", StringComparison.Ordinal)), $"{NewestWhenWritten} is there");
                var dropships = ids.Where(id => id.EndsWith("_dynamic_dropships", StringComparison.Ordinal)).ToList();
                CollectionAssert.AreEqual(Order, dropships, "in their stacked order");
                foreach (var id in Order)
                    Assert.IsTrue(string.CompareOrdinal(id, NewestWhenWritten + "_") > 0, $"{id} after {NewestWhenWritten}");
            });
        }

        [TestMethod]
        public void EveryInsertIsGuardedAndEveryRowItsOwn()
        {
            foreach (var sql in PlateauDynamicDropshipsContent.Up().Concat(PoolsDynamicDropshipsContent.Up()).Concat(MarshesDynamicDropshipsContent.Up()).Concat(CrucibleDynamicDropshipsContent.Up()))
            {
                // The Crucible's three updates, each safe to run twice: Derge's move (the same row as the
                // Crucible quests'), the Thrax Commander's respawn, the boxed-in turret's move (guarded on its old place).
                if (sql.StartsWith("UPDATE spawnpool SET ", StringComparison.Ordinal))
                {
                    Assert.IsTrue(sql == "UPDATE spawnpool SET pos_x = 859, pos_y = 188.68, pos_z = 476 WHERE id = 520013;" ||
                        sql == "UPDATE spawnpool SET respown_time = 1200 WHERE id = 520014;" ||
                        sql == "UPDATE spawnpool SET pos_x = 415.27, pos_z = 338.56 WHERE id IN (551265, 551266) AND map_context_id = 1993 " +
                            "AND abs(pos_x - 416.27) < 0.01 AND abs(pos_y - 177.4) < 0.01 AND abs(pos_z - 339.56) < 0.01;", sql);
                    continue;
                }
                StringAssert.StartsWith(sql, "INSERT INTO ");
                StringAssert.Contains(sql, " FROM (SELECT 1 AS one) d WHERE NOT EXISTS (SELECT 1 FROM ");
                Assert.IsFalse(sql.Contains(" VALUES "), sql);
            }
        }
    }

    internal static class DynamicDropshipsMigrations
    {
        /// <summary>
        /// The migration directly before <paramref name="migration"/> (IMigrationsAssembly, ordinal order), the target that
        /// rolls it back: not a fixed id.
        /// </summary>
        internal static string Previous(DbContext world, string migration)
        {
            var ids = world.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, $"{migration} has a migration before it");
            return ids[index - 1];
        }
    }
}
