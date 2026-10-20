using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Services.Preloader.Missions.Wilderness;
    using Rasa.Test.Database;

    /// <summary>
    /// The Wilderness gear rewards are given with the module their source names them by
    /// (Wilderness_reward_modules, WildernessRewardModules).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessRewardModulesTests
    {
        private const string Migration = "20270116000000_Wilderness_reward_modules";
        private const string Previous = "20270115000000_Named_npc_barks";

        [TestMethod]
        public void EachNamedRewardHoldsItsModuleAndNothingElseChanges()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rewards(database);

            Assert.HasCount(40, WildernessRewardModules.Rows);
            foreach (var row in WildernessRewardModules.Rows)
                Assert.AreEqual($"{row.Template} 0 0 0 0", Row(database, row), $"{row.Mission}/{row.Item} plain before");

            migrator.Migrate(Migration);
            var after = Rewards(database);

            foreach (var row in WildernessRewardModules.Rows)
                Assert.AreEqual($"{row.Template} {row.Module} 0 0 0", Row(database, row), $"{row.Mission}/{row.Item}: {row.Name}");

            // Only those rows: every other reward row and the reward definitions as they were.
            var changed = WildernessRewardModules.Rows.Select(r => $"{r.Mission} {r.Item} ").ToArray();
            CollectionAssert.AreEqual(before.Where(r => !changed.Any(r.StartsWith)).ToList(), after.Where(r => !changed.Any(r.StartsWith)).ToList());
            Assert.AreEqual(before.Count, after.Count);

            // The ones left plain: several kinds of module to the name, a tool with none, no name.
            foreach (var (mission, item) in new[] { (421u, 1u), (429u, 1u), (429u, 2u), (430u, 1u), (431u, 2u), (433u, 2u), (682u, 2u), (696u, 2u), (700u, 1u), (860u, 1u), (428u, 1u), (451u, 1u), (508u, 2u), (1992u, 1u) })
                Assert.IsTrue(after.Single(r => r.StartsWith($"{mission} {item} ")).EndsWith(" 0 0 0 0"), $"{mission}/{item} plain");

            var evidence = Strings(database, $"SELECT mission_id || ' ' || owner_kind || ' ' || owner_id || ' ' || source_kind AS Value FROM mission_evidence " +
                $"WHERE evidence_id = {WildernessRewardModules.EvidenceId} AND content_revision = '{WildernessRewardModules.Revision}' ORDER BY mission_id");
            CollectionAssert.AreEqual(WildernessRewardModules.Missions.OrderBy(m => m).Select(m => $"{m} 4 1 3").ToList(), evidence);
        }

        [TestMethod]
        public void TheContentLoadsAndEveryOfferShowsTheItemsModule()
        {
            // Creating the harness loads the missions, which refuses any module the item cannot take.
            using var harness = WildernessRuntimeTestHarness.Create();

            foreach (var mission in WildernessRewardModules.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetRewardInfo(mission, out var info), $"mission {mission}");
                var offered = info.FixedReward.FixedItems.Concat(info.SelectableReward).ToList();

                foreach (var row in WildernessRewardModules.Rows.Where(r => r.Mission == mission))
                    Assert.IsTrue(offered.Any(item => item.ItemTemplateId == row.Template && item.ModuleIds.FirstOrDefault() == row.Module &&
                        item.ModuleIds.Skip(1).All(moduleId => moduleId == 0)), $"{mission}: {row.Name}");
            }
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedRunsTwiceAndTheDownEmptiesTheSlots()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM gave 442's AccuMax Rifle a module of their own by hand before the migration ran.
            var rifle = WildernessRewardModules.Rows.Single(r => r.Mission == 442 && r.Item == 1);
            database.Context.Database.ExecuteSqlRaw($"UPDATE mission_reward_item SET module_1 = 100057 WHERE mission_id = 442 AND content_revision = '{WildernessRewardModules.Revision}' AND reward_id = 1 AND item_id = 1");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual($"{rifle.Template} 100057 0 0 0", Row(database, rifle), "the GM's module kept");

            var once = Snapshot(database);
            foreach (var sql in WildernessRewardModules.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed, and leaves the GM's");
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in WildernessRewardModules.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in WildernessRewardModules.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>
        /// Whether an offered reward item carries the modules the migration gives it on this
        /// mission: its module in the first slot for a named one, none for every other.
        /// </summary>
        internal static bool OfferedAsAuthored(uint missionId, Rasa.Structures.RewardItem item)
        {
            var row = WildernessRewardModules.Rows.SingleOrDefault(r => r.Mission == missionId && r.Template == item.ItemTemplateId);

            return row == null
                ? item.ModuleIds.Count == 0
                : item.ModuleIds.SequenceEqual(new[] { row.Module, 0u, 0u, 0u });
        }

        private static string Row(OrdinaryLootTestDatabase database, WildernessRewardModules.Row row) =>
            Strings(database, $"SELECT item_template_id || ' ' || module_1 || ' ' || module_2 || ' ' || module_3 || ' ' || module_4 AS Value FROM mission_reward_item " +
                $"WHERE mission_id = {row.Mission} AND content_revision = '{WildernessRewardModules.Revision}' AND reward_id = 1 AND item_id = {row.Item}").Single();

        private static List<string> Rewards(OrdinaryLootTestDatabase database) =>
            Strings(database, "SELECT mission_id || ' ' || item_id || ' ' || content_revision || ' ' || reward_id || ' ' || kind || ' ' || item_template_id || ' ' || quantity || ' ' || " +
                "module_1 || ' ' || module_2 || ' ' || module_3 || ' ' || module_4 AS Value FROM mission_reward_item ORDER BY mission_id, content_revision, reward_id, item_id")
                .Concat(Strings(database, "SELECT 'def ' || mission_id || ' ' || content_revision || ' ' || reward_id || ' ' || experience || ' ' || credits || ' ' || selection_count AS Value " +
                    "FROM mission_reward_definition ORDER BY mission_id, content_revision, reward_id"))
                .ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Rewards(database)
                .Concat(Strings(database, $"SELECT 'evidence ' || mission_id || ' ' || evidence_id AS Value FROM mission_evidence WHERE evidence_id = {WildernessRewardModules.EvidenceId} ORDER BY mission_id"))
                .ToList();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();
    }
}
