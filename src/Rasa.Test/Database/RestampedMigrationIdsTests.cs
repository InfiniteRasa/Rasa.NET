using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    /// <summary>
    /// The old ids of the restamped Logos_shrine_heights and Wilderness_urn_enigma_regions taken
    /// off the history (Forget_restamped_migration_ids, RestampedMigrationIds).
    /// </summary>
    [TestClass]
    public class RestampedMigrationIdsTests
    {
        private const string Migration = "20270901000000_Forget_restamped_migration_ids";
        private const string Logos = "20270224000000_Logos_shrine_heights", BeforeLogos = "20270223000000_Foreas_base_wormhole_region";
        private const string Wilderness = "20270601000000_Wilderness_urn_enigma_regions", BeforeWilderness = "20270316000000_Donn_fithik_abilities";

        [TestMethod]
        public void AWorldThatRanThemUnderTheirOldIdsEndsAsAnyOtherWithOnlyTheNewHistoryRows()
        {
            using var fresh = new OrdinaryLootTestDatabase();
            fresh.Context.GetService<IMigrator>().Migrate();

            // A World that ran each under its old ids: their statements run, and those ids on the
            // history, just before the new ids come up.
            using var old = new OrdinaryLootTestDatabase();
            var migrator = old.Context.GetService<IMigrator>();
            migrator.Migrate(BeforeLogos);
            foreach (var sql in LogosShrineHeights.Up())
                old.Context.Database.ExecuteSqlRaw(sql);
            Remember(old, RestampedMigrationIds.OldIds.Where(id => id.EndsWith("_Logos_shrine_heights")));
            CollectionAssert.Contains(old.Context.Database.GetPendingMigrations().ToList(), Logos, "the old ids are not the new one");
            migrator.Migrate(BeforeWilderness);
            foreach (var sql in WildernessUrnEnigmaRegions.Up())
                old.Context.Database.ExecuteSqlRaw(sql);
            Remember(old, RestampedMigrationIds.OldIds.Where(id => id.EndsWith("_Wilderness_urn_enigma_regions")));

            CollectionAssert.IsSubsetOf(new[] { Wilderness, Migration }, old.Context.Database.GetPendingMigrations().ToList());
            migrator.Migrate();

            // Run again under the new ids, as no-ops: the same shrines and regions as a World that never had the old ids.
            CollectionAssert.AreEqual(Rows(fresh, "logos"), Rows(old, "logos"));
            CollectionAssert.AreEqual(Rows(fresh, "map_region"), Rows(old, "map_region"));

            var history = old.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.IsSubsetOf(new[] { Logos, Wilderness, Migration }, history);
            foreach (var id in RestampedMigrationIds.OldIds)
                CollectionAssert.DoesNotContain(history, id);
            CollectionAssert.AreEqual(fresh.Context.Database.GetAppliedMigrations().ToList(), history);
        }

        [TestMethod]
        public void AWorldWithoutThemIsUnchangedAndDownPutsNothingBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var previous = PreviousTo(database.Context);

            migrator.Migrate(previous);
            var before = database.Context.Database.GetAppliedMigrations().ToList();

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(before.Append(Migration).ToList(), database.Context.Database.GetAppliedMigrations().ToList());

            migrator.Migrate(previous);
            CollectionAssert.AreEqual(before, database.Context.Database.GetAppliedMigrations().ToList());
        }

        [TestMethod]
        public void TheMigrationMakesTheSameStatementsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(PreviousTo(context), Migration);

            foreach (var id in RestampedMigrationIds.OldIds)
                StringAssert.Contains(up, $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{id}';");
            StringAssert.Contains(up, $"'{Migration}'");
        }

        /// <summary>
        /// The migration just before it, whichever that is now: migrations dated after the
        /// restamp's own day but before it keep coming (the region work of 2027-06), and a fixed
        /// id had the Up apply them all along with it.
        /// </summary>
        private static string PreviousTo(DbContext context)
        {
            var all = context.Database.GetMigrations().ToList();

            return all[all.IndexOf(Migration) - 1];
        }

        private static void Remember(OrdinaryLootTestDatabase database, IEnumerable<string> ids)
        {
            foreach (var id in ids)
                database.Context.Database.ExecuteSqlRaw(
                    $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{id}', '9.0.20')");
        }

        private static List<string> Rows(OrdinaryLootTestDatabase database, string table)
        {
            var connection = database.Context.Database.GetDbConnection();
            database.Context.Database.OpenConnection();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "null" : reader.GetValue(i).ToString())));
                rows.Sort(System.StringComparer.Ordinal);
                return rows;
            }
            finally
            {
                database.Context.Database.CloseConnection();
            }
        }
    }
}
