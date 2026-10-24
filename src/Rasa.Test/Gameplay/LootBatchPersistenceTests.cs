using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions;
    using Rasa.Missions.Definitions;
    using Rasa.Repositories.Char.Items;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// A corpse's drops are saved in one write (IItemRepository.CreateItems) and its unclaimed
    /// items deleted in one (DeleteItemsWithInventory): each item keeps its own id and row, and
    /// an item a mission owns is still refused.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LootBatchPersistenceTests
    {
        private static readonly MethodInfo Materialize = typeof(LootDispenserManager)
            .GetMethod("MaterializeItems", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo Retire = typeof(LootDispenserManager)
            .GetMethod("Retire", BindingFlags.Instance | BindingFlags.NonPublic)!;

        [TestMethod]
        public void EachItemOfABatchHasItsOwnIdAndRow()
        {
            using var context = Start();
            var items = new List<IItemChange>
            {
                new Item { ItemTemplateId = 901, StackSize = 1, Crafter = "" },
                new Item { ItemTemplateId = 902, StackSize = 3, Crafter = "" },
                new Item { ItemTemplateId = 901, StackSize = 7, Crafter = "" }
            };

            IReadOnlyList<uint> ids;
            using (var unit = context.CreateChar())
                ids = unit.Items.CreateItems(items);

            Assert.AreEqual(3, ids.Count);
            Assert.AreEqual(3, ids.Distinct().Count());
            Assert.IsTrue(ids.All(id => id != 0));
            using var verify = context.Open();
            for (var i = 0; i < items.Count; i++)
            {
                var row = verify.ItemEntries.AsNoTracking().Single(entry => entry.ItemId == ids[i]);
                Assert.AreEqual(items[i].ItemTemplateId, row.ItemTemplateId, $"item {i}");
                Assert.AreEqual(items[i].StackSize, row.StackSize, $"item {i}");
            }
        }

        [TestMethod]
        public void TheItemsAndTheirInventoryRowsGoAndNothingElse()
        {
            using var context = Start();
            var ids = Create(context, 3);
            using (var database = context.Open())
            {
                database.CharacterInventoryEntries.Add(new CharacterInventoryEntry(1, 1, (uint)InventoryType.Personal, 50, ids[0]));
                database.CharacterInventoryEntries.Add(new CharacterInventoryEntry(1, 1, (uint)InventoryType.Personal, 51, ids[2]));
                database.SaveChanges();
            }

            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.Items.DeleteItemsWithInventory(new[] { ids[0], ids[1] }));

            using var verify = context.Open();
            CollectionAssert.AreEqual(new[] { ids[2] }, verify.ItemEntries.Select(entry => entry.ItemId).ToArray());
            CollectionAssert.AreEqual(new[] { ids[2] }, verify.CharacterInventoryEntries.Select(entry => entry.ItemId).ToArray());
        }

        [TestMethod]
        public void AnItemAMissionOwnsIsRefusedAndNoneOfThemGo()
        {
            using var context = Start();
            var ids = Create(context, 2);
            // WithProgressMission's mission, 321.
            const uint missionId = 321;
            using (var database = context.Open())
                database.Database.ExecuteSqlInterpolated(
                    $"INSERT INTO character_mission_item (character_id, mission_id, assignment_id, generation, item_key, item_id, quantity) VALUES (1, {missionId}, 'owned', 1, 'owned', {ids[1]}, 1)");

            using (var unit = context.CreateChar())
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    unit.ExecuteTransaction(() => unit.Items.DeleteItemsWithInventory(ids)));

            using var verify = context.Open();
            Assert.AreEqual(2, verify.ItemEntries.Count());
        }

        /// <summary>A corpse of three drops: one save to make them, one to delete them unclaimed, and the right rows each time.</summary>
        [TestMethod]
        public void ACorpseIsOneSaveToMakeAndOneToClearAway()
        {
            using var context = Start();
            context.AddRewardTemplate(901, 2001);
            context.AddRewardTemplate(902, 2002);
            var manager = LootDispenserManager.Instance;
            var loot = new LootDispenser { Map = context.Map, UnitOfWorkFactory = context };
            var saves = context.SaveAttempts;

            Materialize.Invoke(manager, new object[]
            {
                context.Client, loot, new List<(uint, uint)> { (901, 1), (902, 3), (901, 2) }, 0u, null
            });

            Assert.AreEqual(saves + 1, context.SaveAttempts, "one save for the three");
            Assert.AreEqual(3, loot.LootItems.Count);
            Assert.AreEqual(3, loot.LootItems.Select(item => item.Item.Id).Distinct().Count());
            using (var verify = context.Open())
                foreach (var item in loot.LootItems)
                {
                    var row = verify.ItemEntries.AsNoTracking().Single(entry => entry.ItemId == item.Item.Id);
                    Assert.AreEqual(item.ItemTemplateId, row.ItemTemplateId);
                    Assert.AreEqual(item.ItemQuantity, row.StackSize);
                    Assert.AreSame(item.Item, EntityManager.Instance.GetItem(item.EntityId));
                }

            saves = context.SaveAttempts;
            Retire.Invoke(manager, new object[] { context.Map, loot, false, true });

            Assert.AreEqual(saves + 1, context.SaveAttempts, "one save to delete them");
            using (var verify = context.Open())
                Assert.AreEqual(0, verify.ItemEntries.Count());
        }

        private static MissionTestContext Start() =>
            MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));

        private static uint[] Create(MissionTestContext context, int count)
        {
            using var unit = context.CreateChar();
            return unit.Items.CreateItems(Enumerable.Range(0, count)
                .Select(i => (IItemChange)new Item { ItemTemplateId = 901, StackSize = 1, Crafter = "" }).ToList()).ToArray();
        }
    }
}
