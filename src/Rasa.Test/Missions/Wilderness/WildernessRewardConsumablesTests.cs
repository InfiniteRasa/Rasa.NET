using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions.Wilderness;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>
    /// The Wilderness rewards get back the bombs, grenades, Res Trauma Kits and Adrenaline Booster
    /// their sources list (Wilderness_reward_consumables, WildernessRewardConsumables).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessRewardConsumablesTests
    {
        private const string Migration = "20270113000000_Wilderness_reward_consumables";
        private const string Previous = "20270112000000_Add_world_containers";

        [TestMethod]
        public void EachRewardRowHoldsItsSourcesItemAndNothingElseChanges()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rewards(database);

            foreach (var swap in WildernessRewardConsumables.Swaps)
                Assert.AreEqual($"{swap.MedPack} {swap.Quantity}", Row(database, swap), $"{swap.Mission}/{swap.Item} a med pack before");

            migrator.Migrate(Migration);
            var after = Rewards(database);

            foreach (var swap in WildernessRewardConsumables.Swaps)
                Assert.AreEqual($"{swap.Source} {swap.Quantity}", Row(database, swap), $"{swap.Mission}/{swap.Item}: {swap.What}");

            // Only those rows: every other reward row, its kind and the reward definitions as they were.
            var changed = WildernessRewardConsumables.Swaps.Select(s => $"{s.Mission} {s.Item} ").ToArray();
            CollectionAssert.AreEqual(before.Where(r => !changed.Any(r.StartsWith)).ToList(), after.Where(r => !changed.Any(r.StartsWith)).ToList());
            Assert.AreEqual(before.Count, after.Count);

            var evidence = Strings(database, $"SELECT mission_id || ' ' || owner_kind || ' ' || owner_id || ' ' || source_kind AS Value FROM mission_evidence " +
                $"WHERE evidence_id = {WildernessRewardConsumables.EvidenceId} AND content_revision = '{WildernessRewardConsumables.Revision}' ORDER BY mission_id");
            CollectionAssert.AreEqual(WildernessRewardConsumables.Missions.OrderBy(m => m).Select(m => $"{m} 4 1 3").ToList(), evidence);
        }

        [TestMethod]
        public void TheServerOffersThemAndCanUseEveryOne()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            foreach (var mission in WildernessRewardConsumables.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetRewardInfo(mission, out var info), $"mission {mission}");
                var offered = info.FixedReward.FixedItems.Concat(info.SelectableReward).Select(item => (item.ItemTemplateId, item.Quantity)).ToList();

                foreach (var swap in WildernessRewardConsumables.Swaps.Where(s => s.Mission == mission))
                    CollectionAssert.Contains(offered, (swap.Source, swap.Quantity), $"{mission}: {swap.What}");
            }

            var abilities = harness.LoadAbilities();
            var canResolve = typeof(AbilityManager).GetMethod("CanResolve", BindingFlags.Static | BindingFlags.NonPublic)!;
            var expected = new Dictionary<uint, ActionId>
            {
                [111247] = ActionId.ConsumableBomb, [111227] = ActionId.ConsumableBomb,
                [111022] = ActionId.ConsumableGrenade, [45125] = ActionId.ConsumableGrenade, [45441] = ActionId.ConsumableGrenade,
                [47593] = (ActionId)427, [47594] = (ActionId)427,
                [111136] = ActionId.ConsumableAdrenalineBoost,
            };

            foreach (var template in WildernessRewardConsumables.Swaps.Select(s => s.Source).Distinct())
            {
                Assert.IsTrue(abilities.TryGetItemAction(template, out var action, out var level), $"{template} has an action");
                Assert.AreEqual(expected[template], action, $"{template}");
                Assert.IsTrue(abilities.TryGetAction(action, out var definition));
                Assert.IsTrue(abilities.TryGetLevel(action, level, out var info), $"{template}: level {level}");
                Assert.IsTrue((bool)canResolve.Invoke(null, new object[] { definition, info }), $"{template} ({definition.Module} level {level}) resolves on the server");
            }

            // The Class I EMP Grenade is an EMP one: grenade levels 11-20 are EMP.
            Assert.IsTrue(abilities.TryGetItemAction(111022, out _, out var empLevel));
            Assert.IsTrue(empLevel >= 11 && empLevel <= 20, $"level {empLevel}");
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmChangedRunsTwiceAndTheDownPutsTheMedPacksBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);

            // A GM gave 422's first row something else by hand before the migration ran.
            database.Context.Database.ExecuteSqlRaw($"UPDATE mission_reward_item SET item_template_id = 28 WHERE mission_id = 422 AND content_revision = '{WildernessRewardConsumables.Revision}' AND reward_id = 1 AND item_id = 1");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual("28 1", Row(database, WildernessRewardConsumables.Swaps.First(s => s.Mission == 422)), "the GM's item kept");

            var once = Snapshot(database);
            foreach (var sql in WildernessRewardConsumables.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in WildernessRewardConsumables.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in WildernessRewardConsumables.Down())
                StringAssert.Contains(down, sql);
        }

        private static string Row(OrdinaryLootTestDatabase database, WildernessRewardConsumables.Swap swap) =>
            Strings(database, $"SELECT item_template_id || ' ' || quantity AS Value FROM mission_reward_item WHERE mission_id = {swap.Mission} " +
                $"AND content_revision = '{WildernessRewardConsumables.Revision}' AND reward_id = 1 AND item_id = {swap.Item}").Single();

        private static List<string> Rewards(OrdinaryLootTestDatabase database) =>
            Strings(database, "SELECT mission_id || ' ' || item_id || ' ' || content_revision || ' ' || reward_id || ' ' || kind || ' ' || item_template_id || ' ' || quantity AS Value " +
                "FROM mission_reward_item ORDER BY mission_id, content_revision, reward_id, item_id")
                .Concat(Strings(database, "SELECT 'def ' || mission_id || ' ' || content_revision || ' ' || reward_id || ' ' || experience || ' ' || credits || ' ' || selection_count AS Value " +
                    "FROM mission_reward_definition ORDER BY mission_id, content_revision, reward_id"))
                .ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Rewards(database)
                .Concat(Strings(database, $"SELECT 'evidence ' || mission_id || ' ' || evidence_id AS Value FROM mission_evidence WHERE evidence_id = {WildernessRewardConsumables.EvidenceId} ORDER BY mission_id"))
                .ToList();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();
    }
}
