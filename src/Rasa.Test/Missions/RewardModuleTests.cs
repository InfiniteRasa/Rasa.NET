using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// Modules on mission reward items (mission_reward_item.module_1 to module_4): checked as
    /// the content loads, offered with the item, and made with it - into the backpack, onto no
    /// stack of other items, and into the inbox when the backpack is full.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class RewardModuleTests
    {
        private const string Migration = "20270115000000_Add_mission_reward_item_modules";
        private const string Before = "20270114000000_Creature_flags";

        // Weapon modules (class set 1212): Crit Hit Bonus [1] (variant 15), Crit Hit Bonus [2]
        // (variant 15), Threat Reduction [1] (variant 16); and a tool module (class set 1273).
        private const uint CritBonus1 = 100056, CritBonus2 = 100057, ThreatReduction1 = 100052, ToolThreatReduction1 = 900508;

        private static readonly uint[] Titan = { CritBonus1, ThreatReduction1, 0, 0 };

        [TestMethod]
        public void TheChosenRewardIsMadeWithItsModulesAndTheOthersWithout()
        {
            using var context = MissionTestContext.WithCompletableMission(429, customReward: Reward(
                fixedItems: new[] { new MissionRewardItem(28, 3) },
                selectable: new[] { new MissionRewardItem(29, 1, Titan), new MissionRewardItem(29, 1) }));

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 429, 0, null));

            var weapon = Owned(context, 29).Single();
            CollectionAssert.AreEqual(Titan, weapon.ModuleIds.ToArray(), "in the world");
            CollectionAssert.AreEqual(new uint[4], Owned(context, 28).Single().ModuleIds.ToArray(), "the fixed item has none");

            using var unit = context.CreateChar();
            CollectionAssert.AreEqual(Titan, unit.Items.GetItem(weapon.Id).Modules, "saved");
        }

        [TestMethod]
        public void TheOtherChoiceIsMadeWithout()
        {
            using var context = MissionTestContext.WithCompletableMission(429, customReward: Reward(
                fixedItems: Array.Empty<MissionRewardItem>(),
                selectable: new[] { new MissionRewardItem(29, 1, Titan), new MissionRewardItem(29, 1) }));

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 429, 1, null));

            CollectionAssert.AreEqual(new uint[4], Owned(context, 29).Single().ModuleIds.ToArray());
        }

        [TestMethod]
        public void ItemsWithModulesAndWithoutAreNeverOneStack()
        {
            // Both of one template, which stacks (to 50000 here): two stacks, one of each.
            using var context = MissionTestContext.WithCompletableMission(429, customReward: Reward(
                fixedItems: new[] { new MissionRewardItem(29, 2), new MissionRewardItem(29, 1, Titan), new MissionRewardItem(29, 4) },
                selectable: Array.Empty<MissionRewardItem>()));

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 429, null, null));

            var stacks = Owned(context, 29);
            Assert.HasCount(2, stacks);
            Assert.AreEqual(6u, stacks.Single(item => item.ModuleIds[0] == 0).StackSize, "the plain ones together");
            var titan = stacks.Single(item => item.ModuleIds[0] != 0);
            Assert.AreEqual(1u, titan.StackSize);
            CollectionAssert.AreEqual(Titan, titan.ModuleIds.ToArray());
        }

        [TestMethod]
        public void ARewardTheBackpackCannotTakeGoesToTheInboxWithItsModules()
        {
            using var context = MissionTestContext.WithCompletableMission(429, customReward: Reward(
                fixedItems: Array.Empty<MissionRewardItem>(),
                selectable: new[] { new MissionRewardItem(29, 1, Titan), new MissionRewardItem(29, 1) }));
            context.FillRewardCategory();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, 429, 0, null));

            var inboxed = context.Client.Player.Inventory.InboxItems
                .Select(EntityManager.Instance.GetItem)
                .Single(item => item.ItemTemplate.ItemTemplateId == 29);
            CollectionAssert.AreEqual(Titan, inboxed.ModuleIds.ToArray());

            using var unit = context.CreateChar();
            CollectionAssert.AreEqual(Titan, unit.Items.GetItem(inboxed.Id).Modules);
        }

        [TestMethod]
        public void TheOfferShowsTheModulesAndTheQualityTheItemIsMadeIn()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)3147].ItemTemplates[29].QualityId = 4;

            var info = Reward(
                fixedItems: new[] { new MissionRewardItem(28, 3) },
                selectable: new[] { new MissionRewardItem(29, 1, new uint[] { CritBonus1, 0, ThreatReduction1 }), new MissionRewardItem(29, 1) })
                .CreateInfo();

            var offered = info.SelectableReward[0];
            CollectionAssert.AreEqual(new uint[] { CritBonus1, 0, ThreatReduction1, 0 }, offered.ModuleIds);
            Assert.AreEqual(4, offered.QualityId, "the template's");
            Assert.IsEmpty(info.SelectableReward[1].ModuleIds);
            Assert.IsEmpty(info.FixedReward.FixedItems[0].ModuleIds);

            // On the wire as an item's lootModuleIds: by slot, None for an empty one, up to the last full one.
            var reader = Written(offered, out var stream);
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual(29L, reader.ReadLong());
            Assert.AreEqual(3147u, reader.ReadUInt());
            Assert.AreEqual(1u, reader.ReadUInt());
            reader.ReadNoneStruct();
            Assert.AreEqual(3, reader.ReadList());
            Assert.AreEqual(CritBonus1, reader.ReadUInt());
            reader.ReadNoneStruct();
            Assert.AreEqual(ThreatReduction1, reader.ReadUInt());
            Assert.AreEqual(4, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ARewardLootedFromACrateIsMadeWithItsModules()
        {
            // The Proving Grounds' gear crate (mission 1992, reward 58): its boots (13066, class
            // set 212) given Body Bonus [1].
            const uint bodyBonus1 = 100060;
            using var harness = BootcampRuntimeTestHarness.Create();
            harness.WorldContext.Database.ExecuteSqlRaw(
                "update mission_reward_item set module_1 = {0} where mission_id = 1992 and reward_id = 58 and item_template_id = 13066",
                bodyBonus1);
            var report = harness.Manager.LoadMissions();
            Assert.IsFalse(report.BlocksReadiness, string.Join("; ", report.Diagnostics.Select(diagnostic => diagnostic.Message)));
            harness.SpawnWorldNpcs();

            var mcAllister = BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap, BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.SeedMission(harness.Client.Player.Id, 1990, (uint)MissionState.Completed, true);
            harness.MovePlayerTo(mcAllister);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, mcAllister.EntityId, 1992));
            var delessio = BootcampRuntimeTestHarness.FindNpcByPackage(harness.BootcampMap, BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delessio.EntityId, 1992, 4, 1));

            var crate = BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-equipment-crate");
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new Rasa.Packets.LootDispenser.Client.RequestLootAllFromCorpsePacket { EntityId = crate.LootDispenserEntityId });

            var items = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).ToList();
            CollectionAssert.AreEqual(new[] { bodyBonus1, 0u, 0u, 0u }, items.Single(item => item.ItemTemplateId == 13066).ModuleIds.ToArray());
            CollectionAssert.AreEqual(new uint[4], items.Single(item => item.ItemTemplateId == 13096).ModuleIds.ToArray(), "the rest as they were");

            using var unit = harness.Context.CreateChar();
            CollectionAssert.AreEqual(new[] { bodyBonus1, 0u, 0u, 0u },
                unit.Items.GetItem(items.Single(item => item.ItemTemplateId == 13066).Id).Modules);
        }

        [TestMethod]
        public void TheContentLoadsTheModulesOfAReward()
        {
            var fixture = Fixture();
            SetModules(fixture.RewardItems[1], Titan);

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(snapshot, fixture.CreateWorldUnitOfWork());

            Assert.IsFalse(report.Diagnostics.Any(diagnostic => diagnostic.Code.Contains("module")),
                string.Join("; ", report.Diagnostics.Select(diagnostic => diagnostic.Message)));

            var reward = MissionDefinitionCatalog.CreateRewardPackages(snapshot, report)[321][40];
            CollectionAssert.AreEqual(Titan, reward.SelectableItems.Single().ModuleIds.ToArray());
            Assert.IsFalse(reward.FixedItems.Single().HasModules);
        }

        [TestMethod]
        [DataRow(new uint[] { 999999, 0, 0, 0 }, "missing-reward-module", DisplayName = "no such module")]
        [DataRow(new uint[] { ToolThreatReduction1, 0, 0, 0 }, "wrong-reward-module-kind", DisplayName = "a tool module in a weapon")]
        [DataRow(new uint[] { CritBonus1, CritBonus2, 0, 0 }, "duplicate-reward-module-variant", DisplayName = "two Crit Hit Bonus modules")]
        [DataRow(new uint[] { 0, CritBonus1, 0, 0 }, "reward-module-slot-gap", DisplayName = "an empty first slot")]
        public void AModuleTheItemCannotTakeIsRefused(uint[] modules, string expected)
        {
            var fixture = Fixture();
            SetModules(fixture.RewardItems[1], modules);

            var report = new MissionContentValidator().Validate(
                new MissionContentLoader().Load(fixture.CreateRepository()), fixture.CreateWorldUnitOfWork());

            CollectionAssert.Contains(report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), expected);
            Assert.IsTrue(report.BlocksReadiness);
        }

        [TestMethod]
        public void AnItemThatTakesNoModulesIsGivenNone()
        {
            var fixture = Fixture();
            SetModules(fixture.RewardItems[0], new uint[] { CritBonus1, 0, 0, 0 });   // template 28: its class takes none

            var report = new MissionContentValidator().Validate(
                new MissionContentLoader().Load(fixture.CreateRepository()), fixture.CreateWorldUnitOfWork());

            CollectionAssert.Contains(report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), "reward-item-takes-no-modules");
        }

        [TestMethod]
        public void TheColumnsGoInAtZeroAndComeOutWithTheRowsKept()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var rows = new MissionContentRepository(database.Context).GetRewardItems();
            Assert.IsNotEmpty(rows);
            Assert.IsTrue(rows.All(row => row.Modules.All(moduleId => moduleId == 0)), "no reward has a module until one is given one");

            var first = rows.OrderBy(row => row.MissionId).ThenBy(row => row.RewardId).ThenBy(row => row.ItemId).First();
            database.Context.Database.ExecuteSqlRaw(
                "update mission_reward_item set module_1 = {0}, module_2 = {1} where mission_id = {2} and content_revision = {3} and reward_id = {4} and item_id = {5}",
                CritBonus1, ThreatReduction1, first.MissionId, first.ContentRevision, first.RewardId, first.ItemId);
            var read = new MissionContentRepository(database.Context).GetRewardItems()
                .Single(row => row.MissionId == first.MissionId && row.ContentRevision == first.ContentRevision &&
                    row.RewardId == first.RewardId && row.ItemId == first.ItemId);
            CollectionAssert.AreEqual(Titan, read.Modules);

            migrator.Migrate(Before);
            Assert.AreEqual(0, database.Context.Database
                .SqlQueryRaw<int>("select count(*) as Value from pragma_table_info('mission_reward_item') where name like 'module_%'")
                .AsEnumerable().Single());
            Assert.AreEqual(rows.Count, database.Context.Database
                .SqlQueryRaw<int>("select count(*) as Value from mission_reward_item").AsEnumerable().Single());
        }

        [TestMethod]
        public void OnMySqlTheColumnsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            foreach (var column in new[] { "module_1", "module_2", "module_3", "module_4" })
            {
                StringAssert.Contains(up, $"ALTER TABLE `mission_reward_item` ADD `{column}` int(11) unsigned NOT NULL DEFAULT 0");
                StringAssert.Contains(down, $"ALTER TABLE `mission_reward_item` DROP COLUMN `{column}`");
            }
        }

        private static MissionRewardDefinition Reward(MissionRewardItem[] fixedItems, MissionRewardItem[] selectable) =>
            new MissionRewardDefinition(100,
                new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7, [CurencyType.Prestige] = 3 },
                fixedItems, selectable);

        private static List<Item> Owned(MissionTestContext context, uint templateId) =>
            context.Client.Player.Inventory.PersonalInventory
                .Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem)
                .Where(item => item?.ItemTemplate?.ItemTemplateId == templateId)
                .ToList();

        /// <summary>The valid content fixture with weapon modules: its template 29 a weapon (class 3315's 12147, set 1212), 28 not.</summary>
        private static MissionContentFixture Fixture()
        {
            var fixture = MissionContentFixture.CreateValid();

            fixture.ItemTemplateClasses[29] = 12147;
            fixture.EntityClassIds.Add(12147);
            fixture.ModifiableClasses.Add(new ModifiableClassEntry { Id = 12147, ClassSetId = 1212 });
            fixture.ModuleClasses.AddRange(new[]
            {
                new ModuleClassEntry { Id = CritBonus1, VariantId = 15, Level = 1, ClassSetId = 1212 },
                new ModuleClassEntry { Id = CritBonus2, VariantId = 15, Level = 2, ClassSetId = 1212 },
                new ModuleClassEntry { Id = ThreatReduction1, VariantId = 16, Level = 1, ClassSetId = 1212 },
                new ModuleClassEntry { Id = ToolThreatReduction1, VariantId = 16, Level = 1, ClassSetId = 1273 }
            });

            return fixture;
        }

        private static void SetModules(MissionRewardItemEntry row, uint[] modules)
        {
            row.Module1 = modules[0];
            row.Module2 = modules[1];
            row.Module3 = modules[2];
            row.Module4 = modules[3];
        }

        private static PythonReader Written(RewardItem item, out MemoryStream stream)
        {
            stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                item.Write(new PythonWriter(writer));
            stream.Position = 0;
            return new PythonReader(new BinaryReader(stream));
        }
    }
}
