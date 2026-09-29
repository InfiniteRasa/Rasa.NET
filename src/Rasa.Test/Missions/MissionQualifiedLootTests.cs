using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionQualifiedLootTests
    {
        [TestMethod]
        public void DropMetadataSnapshotsSourcesAndRoundTripsWithRequiredFields()
        {
            var sources = new List<uint> { 3 };
            var drop = new MissionItemDropDefinition(sources, 1, 1220, 100, 1);
            sources[0] = 4;
            CollectionAssert.AreEqual(new uint[] { 3 }, drop.CreatureIds.ToArray());
            Assert.ThrowsExactly<NotSupportedException>(() => ((IList<uint>)drop.CreatureIds).Add(5));
            var json = JsonSerializer.Serialize(drop, MissionContentCodec.Options);
            var restored = JsonSerializer.Deserialize<MissionItemDropDefinition>(json, MissionContentCodec.Options);
            CollectionAssert.AreEqual(new uint[] { 3 }, restored.CreatureIds.ToArray());
            Assert.AreEqual(100, restored.ChancePercent);
            Assert.AreEqual(1U, restored.Quantity);
            Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MissionItemDropDefinition>(
                json.Replace("\"chancePercent\":100,", "", StringComparison.Ordinal), MissionContentCodec.Options));
        }

        [TestMethod]
        public void EligiblePublicCorpseAddsMissionItemAlongsideOrdinaryLootAndOnlyPickupAdvancesProgress()
        {
            const uint missionId = 901;
            const uint questTemplateId = 28;
            const uint questClassId = 3147;
            var objective = new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { null, null, null }, 1,
                MissionObjectiveState.Incomplete, true, null,
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                {
                    [questClassId] = new MissionObjectiveItemCounterDefinition(questClassId, 0, 2)
                },
                Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(), MissionProgressRule.IncrementItemCounterOnExactSubject(
                    MissionProgressEventKind.ItemAcquired, questClassId, 0, 2));
            var binding = new MissionItemBinding("collected-item", questTemplateId, MissionItemScope.CharacterOwned, 2,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                MissionItemCleanupDisposition.Retain, TurnInQuantity: 2,
                Drop: new MissionItemDropDefinition(new uint[] { 3 }, 1, 1220, 100, 1));
            var mission = new Mission(missionId, "Corpse collection", missionId, 77, 88, 1, 1, 1, false, false,
                new[] { objective }, true, items: new[] { binding });
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [missionId] = mission });
            context.AddRewardTemplate(questTemplateId, questClassId);
            context.AddRewardTemplate(29, 3148);
            context.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, context.AddNpc(77).EntityId, missionId));
            var corpse = context.AddNpc(3);
            corpse.Npc = null;
            corpse.Faction = Factions.Bane;
            corpse.State = CharacterState.Dead;
            corpse.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 0, 0, 0);
            CellManager.Instance.UpdateVisibility(context.Client);
            var manager = new LootDispenserManager(context, missionManager: context.Manager,
                lootRoll: (minimum, maximum) => minimum);
            try
            {
                manager.Loot(context.Client, corpse, new ActorGameplayPolicy
                {
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(29, 100, 1, 1) })
                });

                var loot = context.Map.LootDispensers[corpse.CorpseLootEntityId];
                Assert.AreEqual(context.Client.Player.EntityId, loot.Owner);
                Assert.IsNotNull(loot.LootItems.SingleOrDefault(item => item.ItemTemplateId == 29),
                    "Mission-qualified drops must not replace ordinary corpse loot.");
                var questItem = loot.LootItems.SingleOrDefault(item => item.ItemTemplateId == questTemplateId);
                Assert.IsNotNull(questItem, "An eligible public corpse must include the active mission's authored item.");
                Assert.AreEqual(1U, questItem.ItemQuantity);
                Assert.AreEqual(0L, context.ReadRewardTotals().ItemCount);
                Assert.AreEqual(0U, context.Client.Player.Missions[missionId].Objectives[1].ItemCounters[questClassId]);
                Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[missionId].Objectives[1].State);

                var request = new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId };
                manager.RequestLootAllFromCorpse(context.Client, request);

                Assert.IsTrue(loot.FullyLooted);
                Assert.IsTrue(questItem.Taken);
                Assert.AreEqual(2L, context.ReadRewardTotals().ItemCount);
                Assert.AreEqual(1U, context.Client.Player.Missions[missionId].Objectives[1].ItemCounters[questClassId]);
                Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[missionId].Objectives[1].State);
                manager.RequestLootAllFromCorpse(context.Client, request);
                Assert.AreEqual(2L, context.ReadRewardTotals().ItemCount);
                Assert.AreEqual(1U, context.Client.Player.Missions[missionId].Objectives[1].ItemCounters[questClassId]);
                using var verify = context.CreateChar();
                Assert.AreEqual(context.Client.Player.Id, verify.CharacterInventories.GetItems(context.Client.AccountEntry.Id)
                    .Single(row => row.ItemId == questItem.Item.Id).CharacterId);
                Assert.AreEqual(1U, verify.Items.GetItem(questItem.Item.Id).StackSize);
            }
            finally
            {
                manager.RemoveForOwner(context.Map, context.Client);
            }
        }

        [TestMethod]
        [DataRow("unaccepted")]
        [DataRow("no-metadata")]
        [DataRow("wrong-creature")]
        [DataRow("wrong-map")]
        [DataRow("private-map")]
        [DataRow("held-enough")]
        [DataRow("zero-chance")]
        [DataRow("failed")]
        public void UnqualifiedDeathsKeepOnlyOrdinaryLoot(string reason)
        {
            using var fixture = new Fixture(accept: reason != "unaccepted", authorDrop: reason != "no-metadata",
                chance: reason == "zero-chance" ? 0 : 100);
            if (reason == "wrong-map")
            {
                fixture.Context.Map.MapInfo = new MapInfo(1221, "other-public-map", 1, 0);
                fixture.Context.Client.Player.MapContextId = 1221;
            }
            if (reason == "private-map")
                fixture.Context.Map.IsPrivateInstance = true;
            if (reason == "held-enough")
                fixture.Grant(28, 2);
            if (reason == "failed")
                Assert.IsTrue(fixture.Context.Manager.TryFailMission(fixture.Context.Client, 901));

            var loot = fixture.Spawn(reason == "wrong-creature" ? 4U : 3U);

            CollectionAssert.AreEqual(new uint[] { 29 }, loot.LootItems.Select(item => item.ItemTemplateId).ToArray());
        }

        [TestMethod]
        [DataRow(49, true)]
        [DataRow(50, false)]
        public void MissionDropChanceHasAnExactPercentileBoundary(int percentile, bool drops)
        {
            using var fixture = new Fixture(chance: 50, percentile: percentile);
            Assert.AreEqual(drops, fixture.Spawn().LootItems.Any(item => item.ItemTemplateId == 28));
        }

        [TestMethod]
        public void DropQuantityIsClampedToAuthoritativeHeldDeficit()
        {
            using var fixture = new Fixture(quantity: 2);
            fixture.Grant(28, 1, observeAcquisition: true);

            var loot = fixture.Spawn();
            Assert.AreEqual(1U, loot.LootItems.Single(item => item.ItemTemplateId == 28).ItemQuantity);
            fixture.Take(loot);

            Assert.AreEqual(2U, fixture.HeldQuantity);
            Assert.AreEqual(2U, fixture.Counter);
            Assert.IsTrue(fixture.Context.Client.Player.Missions[901].Completeable);
            Assert.IsFalse(fixture.Spawn().LootItems.Any(item => item.ItemTemplateId == 28));
        }

        [TestMethod]
        [DataRow("destroy")]
        [DataRow("bank")]
        [DataRow("consume")]
        public void CompletedCollectionCanReacquireLostTurnInItemsThroughRealInventoryPaths(string loss)
        {
            using var fixture = new Fixture(quantity: 2);
            var context = fixture.Context;
            fixture.Take(fixture.Spawn());
            Assert.AreEqual(2U, fixture.HeldQuantity);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[901].Objectives[1].State);
            var item = context.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplate.ItemTemplateId == 28);
            var inventory = new InventoryManager(context, context.Manager);
            if (loss == "destroy")
                inventory.PersonalInventory_DestroyItem(context.Client,
                    new PersonalInventory_DestroyItemPacket { EntityId = item.EntityId, Quantity = 2 });
            else if (loss == "bank")
            {
                context.Client.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, 480).ToList();
                context.Client.Player.LockboxTabs = 1;
                inventory.RequestMoveItemToHomeInventory(context.Client,
                    new RequestMoveItemToHomeInventoryPacket { SrcSlot = item.OwnerSlotId, DestSlot = 0, Quantity = 2 });
                Assert.AreEqual(item.EntityId, context.Client.Player.Inventory.HomeInventory[0]);
            }
            else
            {
                var consumption = new InventoryManager.InventoryConsumption();
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => consumption.PlanAndSave(context.Client,
                        new Dictionary<ulong, uint> { [item.EntityId] = 2 }, unit));
                consumption.Publish(context.Client);
            }
            Assert.AreEqual(0U, fixture.HeldQuantity, "The existing inventory operation must really remove the collection.");
            Assert.AreEqual(2U, fixture.Counter);
            var receiver = context.AddNpc(88);
            Assert.IsFalse(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 901, null));

            var recovery = fixture.Spawn();
            Assert.AreEqual(2U, recovery.LootItems.Single(item => item.ItemTemplateId == 28).ItemQuantity);
            fixture.Take(recovery);

            Assert.AreEqual(2U, fixture.HeldQuantity);
            Assert.AreEqual(2U, fixture.Counter, "Recovery must not reset or replay the completed collection.");
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[901].Objectives[1].State);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, 901, null));
            Assert.AreEqual(0U, fixture.HeldQuantity);
            Assert.IsFalse(fixture.Spawn().LootItems.Any(item => item.ItemTemplateId == 28));
            if (loss == "bank")
                Assert.AreEqual(item.EntityId, context.Client.Player.Inventory.HomeInventory[0],
                    "Turn-in must not spend the unrelated banked copy.");
        }

        [TestMethod]
        [DataRow("abandon")]
        [DataRow("generation")]
        [DataRow("revision")]
        [DataRow("held-enough")]
        [DataRow("foreign")]
        [DataRow("range")]
        public void ClaimRejectsStaleOrForeignMissionLootWithoutTakingOrdinaryItems(string change)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var loot = fixture.Spawn();
            Assert.IsTrue(loot.LootItems.Any(item => item.ItemTemplateId == 28));
            if (change == "abandon")
                Assert.IsTrue(context.Manager.TryAbandon(context.Client, 901));
            else if (change is "generation" or "revision")
            {
                using var database = context.Open();
                var assignment = database.CharacterMissionEntries.Single(entry => entry.MissionId == 901);
                if (change == "generation")
                    assignment.Generation++;
                else
                    assignment.ContentRevision = "changed-content";
                database.SaveChanges();
            }
            else if (change == "held-enough")
                fixture.Grant(28, 2);
            else if (change == "range")
                loot.Corpse.Position = new System.Numerics.Vector3(100, 0, 0);
            var before = context.ReadRewardTotals();
            var claimant = change == "foreign" ? context.CreateAdditionalClient(2) : context.Client;

            fixture.Manager.RequestLootAllFromCorpse(claimant, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.IsFalse(loot.FullyLooted);
            Assert.IsTrue(loot.LootItems.All(item => !item.Taken));
            Assert.AreEqual(before, context.ReadRewardTotals());
        }

        [TestMethod]
        public void FullBagRollsBackQualifiedLootAndRetryClaimsItOnce()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var loot = fixture.Spawn();
            context.AddRewardTemplate(30, 3149);
            var itemClass = EntityClassManager.Instance.GetClassInfo((EntityClasses)3149);
            var original = itemClass.ItemClassInfo;
            try
            {
                itemClass.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { StackSize = 1 });
                fixture.Grant(30, 50);
                var before = context.ReadRewardTotals();

                fixture.Take(loot);

                Assert.AreEqual(before, context.ReadRewardTotals());
                Assert.AreEqual(0U, fixture.Counter);
                Assert.IsTrue(loot.LootItems.All(item => !item.Taken));
                var inventory = new InventoryManager(context, context.Manager);
                foreach (var id in context.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Take(2).ToArray())
                    inventory.PersonalInventory_DestroyItem(context.Client,
                        new PersonalInventory_DestroyItemPacket { EntityId = id, Quantity = 1 });
                fixture.Take(loot);
                Assert.IsTrue(loot.FullyLooted);
                Assert.AreEqual(1U, fixture.HeldQuantity);
                Assert.AreEqual(1U, fixture.Counter);
                var after = context.ReadRewardTotals();
                fixture.Take(loot);
                Assert.AreEqual(after, context.ReadRewardTotals());
            }
            finally
            {
                itemClass.ItemClassInfo = original;
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PostWriteFailureOrSourceChangeRollsBackInventoryAndProgressBeforeRetry(bool changeSource)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var loot = fixture.Spawn();
            var questItem = loot.LootItems.Single(item => item.ItemTemplateId == 28);
            var before = context.ReadRewardTotals();
            var injected = false;
            context.AfterSave = database =>
            {
                if (database.CharacterInventoryEntries.Local.Any(row => row.ItemId == questItem.Item.Id))
                {
                    injected = true;
                    if (changeSource)
                        context.Map.IsPrivateInstance = true;
                    else
                        throw new DbUpdateException("Injected failure after mission corpse inventory write.");
                }
            };

            fixture.Take(loot);

            context.AfterSave = null;
            context.Map.IsPrivateInstance = false;
            Assert.IsTrue(injected);
            Assert.AreEqual(before, context.ReadRewardTotals());
            Assert.AreEqual(0U, fixture.Counter);
            Assert.IsFalse(questItem.Taken);
            Assert.IsFalse(loot.FullyLooted);
            fixture.Take(loot);
            Assert.AreEqual(1U, fixture.HeldQuantity);
            Assert.AreEqual(1U, fixture.Counter);
            Assert.IsTrue(loot.FullyLooted);
        }

        [TestMethod]
        public void AssignmentIssuedItemsCannotBeAuthoredAsUnboundCorpseDrops()
        {
            var binding = new MissionItemBinding("issued", 28, MissionItemScope.AssignmentIssued, 2,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                MissionItemCleanupDisposition.Remove, Drop: new MissionItemDropDefinition(new uint[] { 3 }, 1, 1220, 100, 1));
            StringAssert.Contains(MissionItemValidation.BindingError(binding), "CharacterOwned");
        }

        [TestMethod]
        public void ProtectedStacksDoNotSatisfyOrMergeWithCharacterOwnedDropsEvenWithoutTheRuntimeMarker()
        {
            using var fixture = new Fixture(issueProtected: true);
            var protectedItem = fixture.Context.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Single();
            protectedItem.MissionOwnership = null;

            var loot = fixture.Spawn();
            Assert.AreEqual(1U, loot.LootItems.Single(item => item.ItemTemplateId == 28).ItemQuantity);
            fixture.Take(loot);

            Assert.AreEqual(2U, protectedItem.StackSize);
            Assert.IsNotNull(protectedItem.MissionOwnership);
            var unbound = fixture.Context.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem)
                .Single(item => item.ItemTemplate.ItemTemplateId == 28 && item.MissionOwnership == null);
            Assert.AreEqual(1U, unbound.StackSize);
            Assert.AreEqual(1U, fixture.Counter);
        }

        private sealed class Fixture : IDisposable
        {
            internal MissionTestContext Context { get; }
            internal LootDispenserManager Manager { get; }
            internal uint Counter => Context.Client.Player.Missions[901].Objectives[1].ItemCounters[3147];
            internal uint HeldQuantity => (uint)Context.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Where(item => item.ItemTemplate.ItemTemplateId == 28)
                .Sum(item => (long)item.StackSize);

            internal Fixture(int chance = 100, uint quantity = 1, bool accept = true, bool authorDrop = true,
                int percentile = 0, bool issueProtected = false)
            {
                var objective = new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { null, null, null }, 1,
                    MissionObjectiveState.Incomplete, true, null,
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                    {
                        [3147] = new MissionObjectiveItemCounterDefinition(3147, 0, 2)
                    }, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(), MissionProgressRule.IncrementItemCounterOnExactSubject(
                        MissionProgressEventKind.ItemAcquired, 3147, 0, 2));
                var binding = new MissionItemBinding("collected-item", 28, MissionItemScope.CharacterOwned, 2,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: 2,
                    Drop: authorDrop ? new MissionItemDropDefinition(new uint[] { 3 }, 1, 1220, chance, quantity) : null);
                var bindings = new List<MissionItemBinding> { binding };
                if (issueProtected)
                    bindings.Add(new MissionItemBinding("protected", 28, MissionItemScope.AssignmentIssued, 2,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove));
                var mission = new Mission(901, "Corpse collection", 901, 77, 88, 1, 1, 1, false, false,
                    new[] { objective }, true, items: bindings, acceptanceItems: issueProtected
                        ? new[] { new IssueMissionItemIntent("accept-protected", 901, "protected", 28, 2) } : null);
                Context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [901] = mission },
                    new Dictionary<uint, MissionRewardDefinition>
                    {
                        [901] = new MissionRewardDefinition(0, new Dictionary<CurencyType, int>
                            { [CurencyType.Credits] = 7 }, null, null)
                    });
                Context.AddRewardTemplate(28, 3147);
                Context.AddRewardTemplate(29, 3148);
                Context.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                Manager = new LootDispenserManager(Context, missionManager: Context.Manager,
                    lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
                if (accept)
                    Assert.IsTrue(Context.Manager.AcceptOfferedMission(Context.Client, Context.AddNpc(77).EntityId, 901));
                Context.Drain();
            }

            internal LootDispenser Spawn(uint creatureId = 3)
            {
                var corpse = Context.AddNpc(creatureId);
                corpse.Npc = null;
                corpse.Faction = Factions.Bane;
                corpse.State = CharacterState.Dead;
                corpse.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 0, 0, 0);
                CellManager.Instance.UpdateVisibility(Context.Client);
                Manager.Loot(Context.Client, corpse, new ActorGameplayPolicy
                {
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(29, 100, 1, 1) })
                });
                return Context.Map.LootDispensers[corpse.CorpseLootEntityId];
            }

            internal void Take(LootDispenser loot) => Manager.RequestLootAllFromCorpse(Context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            internal void Grant(uint templateId, uint quantity, bool observeAcquisition = false)
            {
                using var grant = new InventoryManager.InventoryGrant();
                Action publishProgress = null;
                using (var unit = Context.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        grant.PlanAndSave(Context.Client, new[] { new InventoryManager.InventoryItemGrant(templateId, quantity) }, unit);
                        if (observeAcquisition)
                        {
                            var progress = Context.Manager.PlanProgress(Context.Client,
                                new[] { MissionProgressEvent.ItemAcquired(3147, quantity) }, unit);
                            publishProgress = () => progress.Publish(Context.Client);
                        }
                    });
                grant.Publish(Context.Client);
                publishProgress?.Invoke();
            }

            public void Dispose()
            {
                Context.AfterSave = null;
                Manager.RemoveForOwner(Context.Map, Context.Client);
                Context.Dispose();
            }
        }
    }
}
