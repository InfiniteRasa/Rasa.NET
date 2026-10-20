using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The voiced barks of the named NPCs (NamedNpcBarks): Major McAllister's own line goes in where
    /// he stands, once, and Down takes out only it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NamedNpcBarkTests
    {
        private const string Migration = "20270115000000_Named_npc_barks";
        private const string Before = "20270114000000_Bootcamp_van_valkenberg";
        private const uint McAllister = NamedNpcBarks.MajorMcAllister;
        private const uint HisLine = NamedNpcBarks.McAllisterAlienBuddies;

        [TestCleanup]
        public void Cleanup() => NpcBarks.Load(null);

        [TestMethod]
        public void McAllisterIsTheBootcampMajor()
        {
            Assert.AreEqual(510203U, McAllister);
            Assert.AreEqual(852U, HisLine);
        }

        [TestMethod]
        public void McAllisterGetsHisLineOnceAndDownTakesOutOnlyIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            var before = Rows(database);
            Assert.IsFalse(before.Contains((McAllister, HisLine)));

            migrator.Migrate(Migration);
            var after = Rows(database);
            CollectionAssert.AreEquivalent(before.Append((McAllister, HisLine)).ToList(), after);

            foreach (var sql in NamedNpcBarks.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(1, Rows(database).Count(row => row == (McAllister, HisLine)), "a second run adds nothing");

            migrator.Migrate(Before);
            CollectionAssert.AreEquivalent(before, Rows(database));
        }

        [TestMethod]
        public void NoRowGoesInWhereTheCreatureIsNot()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            database.Context.Database.ExecuteSqlRaw($"DELETE FROM creature WHERE id = {McAllister};");
            migrator.Migrate(Migration);

            Assert.IsFalse(Rows(database).Any(row => row.Creature == McAllister));
        }

        [TestMethod]
        public void TheServerLoadsMcAllistersLine()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            CollectionAssert.Contains(NpcBarks.BarksOf(harness.Creatures.LoadedCreatures[McAllister]).ToArray(), HisLine);
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, $"INSERT INTO npc_bark (creature_id, bark_id) SELECT {McAllister}, {HisLine} FROM creature WHERE id = {McAllister} AND NOT EXISTS");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, $"DELETE FROM npc_bark WHERE creature_id = {McAllister} AND bark_id = {HisLine};");
        }

        private static System.Collections.Generic.List<(uint Creature, uint Bark)> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.NpcBarkEntries.AsNoTracking().AsEnumerable().Select(row => (row.CreatureId, row.BarkId)).ToList();
    }
}
