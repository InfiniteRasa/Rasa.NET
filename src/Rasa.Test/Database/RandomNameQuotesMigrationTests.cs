using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    using Rasa.Managers;

    // Every name the character creation screen can hand out is one creation takes: the four
    // seeded family names with a double quote (O"Brien and the rest) are spelt without it
    // (Random_name_quotes), and the two-letter ones are within the minimum.
    [TestClass]
    public class RandomNameQuotesMigrationTests
    {
        private const string Migration = "20261223000000_Random_name_quotes";
        private const string Previous = "20261222000000_Boss_weapons";

        [TestMethod]
        public void EveryRandomNameIsOneCreationTakes()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var names = Names(database, "SELECT name AS Value FROM player_random_name");

            Assert.IsTrue(names.Length > 2500, $"{names.Length} names");
            CollectionAssert.AreEqual(new string[0], names.Where(name => !CharacterManager.IsValidName(name, out _)).ToArray());
            CollectionAssert.IsSubsetOf(new[] { "An", "Ed", "Li", "My", "Xu" }, names);
        }

        [TestMethod]
        public void TheQuotedNamesAreSpeltAsTheClientSendsThemAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(RandomNameQuotes.Names.Select(n => n.Was).ToArray(), Quoted(database));

            migrator.Migrate(Migration);
            Assert.AreEqual(0, Quoted(database).Length);
            foreach (var (_, name) in RandomNameQuotes.Names)
                Assert.AreEqual(1L, Count(database, $"SELECT COUNT(*) AS Value FROM player_random_name WHERE name = '{name}' AND type = 1 AND gender = 2"), name);

            foreach (var sql in RandomNameQuotes.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(4L, Count(database, "SELECT COUNT(*) AS Value FROM player_random_name WHERE name IN ('Obrien', 'Odonnell', 'Oleary', 'Orourke')"), "once");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(RandomNameQuotes.Names.Select(n => n.Was).ToArray(), Quoted(database));
            Assert.AreEqual(0L, Count(database, "SELECT COUNT(*) AS Value FROM player_random_name WHERE name IN ('Obrien', 'Odonnell', 'Oleary', 'Orourke')"));
        }

        private static string[] Quoted(OrdinaryLootTestDatabase database) =>
            Names(database, "SELECT name AS Value FROM player_random_name WHERE name LIKE '%\"%'");

        private static string[] Names(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).ToArray();

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).Single();
    }
}
