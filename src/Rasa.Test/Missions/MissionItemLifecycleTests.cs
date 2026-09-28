extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Game.Missions.Persistence;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Structures.World;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Clan.Client;
using Rasa.Packets.Trade.Client;
using Rasa.Packets.Trade.Server;
using Rasa.Missions.Content;
using System.Text.Json;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionItemLifecycleTests
    {
        private const uint MissionId = 901;
        private const uint TemplateId = 28;
        private const string ItemKey = "survey-key";

        [TestMethod]
        public void GenericIssueCommitsBeforePublicationAndRetainsAssignmentProvenance()
        {
            using var context = Create();
            MissionItemPlanner plan = null;
            string assignmentId = null;
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, MissionId);
                    assignmentId = assignment.AssignmentId;
                    plan = new MissionItemPlanner(context.Client, unit, context.Manager);
                    plan.Apply(new IssueMissionItemIntent("recover-key", MissionId, ItemKey, TemplateId, 2),
                        assignment.AssignmentId, assignment.Generation);
                    Assert.AreEqual(0, Owned(context).Length, "Uncommitted items must not reach runtime.");
                });

            plan.Publish(context.Client);

            var item = Owned(context).Single();
            Assert.AreEqual(2U, item.StackSize);
            Assert.AreEqual(assignmentId, item.MissionOwnership.AssignmentId);
            using var verify = context.CreateChar();
            var ownership = verify.CharacterMissionItems.GetOwned(1).Single();
            Assert.AreEqual(item.Id, ownership.ItemId);
            Assert.AreEqual(2U, ownership.Quantity);
            Assert.AreEqual(ItemKey, ownership.ItemKey);
            Assert.AreEqual(assignmentId, ownership.AssignmentId);
        }

        [TestMethod]
        public void GrantThenConsumeInOneTransactionConvergesOnceAndRetriesNeverReissue()
        {
            using var context = Create();
            var issue = new IssueMissionItemIntent("recover-key", MissionId, ItemKey, TemplateId, 2);
            var consume = new ConsumeMissionItemIntent("use-key", MissionId, ItemKey, 2, MissionItemScope.AssignmentIssued);

            Apply(context, issue, consume);

            Assert.AreEqual(0, Owned(context).Length);
            Assert.AreEqual(0, context.Drain().OfType<Rasa.Packets.Inventory.Server.InventoryAddItemPacket>().Count(),
                "An item issued and fully consumed in one transaction must never appear in the client.");
            Apply(context, issue, consume);
            Assert.AreEqual(0, Owned(context).Length, "Both receipts survive consumption and replay.");
            using var verify = context.CreateChar();
            Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(1).Count);
        }

        [TestMethod]
        public void CleanupPreservesOtherAssignmentsAndUnboundCopiesOfTheSameTemplate()
        {
            using var context = Create(MissionId, 902);
            Apply(context, new IssueMissionItemIntent("recover-key", MissionId, ItemKey, TemplateId, 2),
                new IssueMissionItemIntent("recover-key", 902, ItemKey, TemplateId, 1));
            GrantUnbound(context, 3);
            Assert.AreEqual(3, Owned(context).Length, "Rewards must not merge into either protected stack.");

            Apply(context, new RemoveMissionItemsIntent("cleanup-key", MissionId, ItemKey));

            var items = Owned(context);
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual(1U, items.Single(item => item.MissionOwnership?.MissionId == 902).StackSize);
            Assert.AreEqual(3U, items.Single(item => item.MissionOwnership == null).StackSize);
            using var verify = context.CreateChar();
            Assert.AreEqual(902U, verify.CharacterMissionItems.GetOwned(1).Single().MissionId);
        }

        [TestMethod]
        public void AcceptanceIssuesOnlyExplicitlyAuthoredItems()
        {
            using var context = Create(true, MissionId);
            Assert.AreEqual(1U, Owned(context).Single().StackSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TerminalCleanupRemovesOnlyTheEndingAssignment(bool abandon)
        {
            using var context = Create(true, MissionId, 902);
            GrantUnbound(context, 2);

            Assert.IsTrue(abandon ? context.Manager.TryAbandon(context.Client, MissionId)
                : context.Manager.TryFailMission(context.Client, MissionId));

            Assert.AreEqual(2, Owned(context).Length);
            Assert.IsFalse(Owned(context).Any(item => item.MissionOwnership?.MissionId == MissionId));
            using var verify = context.CreateChar();
            Assert.AreEqual(902U, verify.CharacterMissionItems.GetOwned(1).Single().MissionId);
        }

        [TestMethod]
        public void TypedSceneItemsComposeAndRetriedSceneInputDoesNotRepeatTheCost()
        {
            using var context = Create();
            context.Manager.Scenes.Bind(MissionId, "data.sequence", new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(characterIntents: new CharacterIntent[]
                    {
                        new IssueMissionItemIntent("scene-key", MissionId, ItemKey, TemplateId, 2),
                        new ConsumeMissionItemIntent("scene-cost", MissionId, ItemKey, 1, MissionItemScope.AssignmentIssued)
                    }),
                    [1] = new(characterIntents: new CharacterIntent[]
                    { new RemoveMissionItemsIntent("scene-cleanup", MissionId, ItemKey) })
                }));

            context.Manager.Scenes.Execute(context.Client, MissionId, 0, started: true);

            Assert.AreEqual(1U, Owned(context).Single().StackSize);
            context.Manager.Scenes.Execute(context.Client, MissionId, 0);
            Assert.AreEqual(1U, Owned(context).Single().StackSize);
            context.Manager.Scenes.Execute(context.Client, MissionId, 1);
            Assert.AreEqual(0, Owned(context).Length);
        }

        [TestMethod]
        public void SceneReceiptsCannotHideAChangedItemOperationPayload()
        {
            using var context = Create();
            SceneBindings Bindings(uint quantity) => new("unversioned",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(characterIntents: new CharacterIntent[]
                    { new IssueMissionItemIntent("scene-issue", MissionId, ItemKey, TemplateId, quantity) })
                });
            context.Manager.Scenes.Bind(MissionId, "data.sequence", Bindings(2));
            context.Manager.Scenes.Execute(context.Client, MissionId, 0, started: true);
            string runId;
            using (var unit = context.CreateChar())
            {
                var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, MissionId);
                runId = unit.CharacterMissions.Runtime.AssignmentScene(assignment.AssignmentId).RunId;
            }
            context.Manager.Scenes.Attach(context.Client, runId, Bindings(1));
            Assert.IsFalse(context.Manager.Scenes.Submit(runId, new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 0)));
            Assert.AreEqual(2U, Owned(context).Single().StackSize);
        }

        [TestMethod]
        public void BootcampContinueAndPlantUseTheGenericAssignmentLedger()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ConradCorpseDialogueTests.FindMissingSoldiers(harness);
            harness.UseObjectAndRecover(ConradCorpseDialogueTests.Corpse(harness));
            using (var verify = harness.Context.CreateChar())
            {
                var item = verify.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Single();
                Assert.AreEqual(1995U, item.MissionId);
                Assert.AreEqual(1U, item.Quantity);
                Assert.AreEqual(11519U, verify.Items.GetItem(item.ItemId).ItemTemplateId);
                Assert.AreEqual(harness.UtcNow.AddSeconds(600),
                    verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995).DueAtUtc);
            }

            harness.UseObjectAndRecover(BootcampRuntimeTestHarness.FindScenarioObject(harness.BootcampMap, "bootcamp-dropship-debris"));

            using var after = harness.Context.CreateChar();
            Assert.AreEqual(0, after.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Count);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[1].State,
                "Consumption commits before the existing fuse completes the objective.");
        }

        [TestMethod]
        public void InventoryReloadRestoresProvenanceWithoutIssuingOrRevivingConsumedItems()
        {
            using var context = Create(true, MissionId);
            var original = Owned(context).Single();
            var ownership = original.MissionOwnership;
            var inventory = new InventoryManager(context, context.Manager);

            inventory.InitCharacterInventory(context.Client);

            var reloaded = Owned(context).Single();
            Assert.AreEqual(original.Id, reloaded.Id);
            Assert.AreEqual(ownership, reloaded.MissionOwnership);
            Apply(context, new ConsumeMissionItemIntent("consume-after-relog", MissionId, ItemKey, 1, MissionItemScope.AssignmentIssued));
            inventory.InitCharacterInventory(context.Client);
            Assert.AreEqual(0, Owned(context).Length);
            Apply(context, new IssueMissionItemIntent("accept-key", MissionId, ItemKey, TemplateId, 1));
            Assert.AreEqual(0, Owned(context).Length);
        }

        [TestMethod]
        [DataRow("destroy")]
        [DataRow("bank")]
        [DataRow("clan-slot")]
        [DataRow("clan-tab")]
        [DataRow("vendor")]
        [DataRow("auction")]
        [DataRow("central-remove")]
        [DataRow("central-add")]
        [DataRow("vendor-no-cache")]
        [DataRow("auction-no-cache")]
        [DataRow("bank-no-cache")]
        public void AssignmentItemsRejectActualPlayerMutationHandlers(string operation)
        {
            using var context = Create();
            Apply(context, new IssueMissionItemIntent("protected-key", MissionId, ItemKey, TemplateId, 3));
            var item = Owned(context).Single();
            if (operation.EndsWith("-no-cache", StringComparison.Ordinal))
            {
                item.MissionOwnership = null;
                operation = operation.Replace("-no-cache", "");
            }
            item.ItemTemplate.HasSellableFlag = true;
            item.ItemTemplate.SellPrice = 10;
            var inventory = new InventoryManager(context, context.Manager);
            context.Client.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, 480).ToList();
            context.Client.Player.Inventory.ClanInventory = Enumerable.Repeat(0UL, 500).ToList();
            context.Client.Player.LockboxTabs = 1;
            if (operation.StartsWith("clan-", StringComparison.Ordinal))
                context.CreateClanForPlayer();
            var originalFactory = Rasa.Game.Server.GameUnitOfWorkFactory;
            Rasa.Game.Server.GameUnitOfWorkFactory = context;
            try
            {
                switch (operation)
                {
                    case "destroy":
                        inventory.PersonalInventory_DestroyItem(context.Client,
                            new PersonalInventory_DestroyItemPacket { EntityId = item.EntityId, Quantity = 1 });
                        break;
                    case "bank":
                        inventory.RequestMoveItemToHomeInventory(context.Client,
                            new RequestMoveItemToHomeInventoryPacket { SrcSlot = item.OwnerSlotId, DestSlot = 0 });
                        break;
                    case "clan-slot":
                        inventory.ClanLockbox_DepositItemInSlot(context.Client,
                            new ClanLockbox_DepositItemInSlotPacket { SrcSlot = item.OwnerSlotId, DestSlot = 0 });
                        break;
                    case "clan-tab":
                        inventory.ClanLockbox_DepositItemInTab(context.Client,
                            new ClanLockbox_DepositItemInTabPacket { SrcSlot = (int)item.OwnerSlotId, NoTabNamed = true });
                        break;
                    case "vendor":
                        new NpcManager(context, context.Manager).RequestVendorSale(context.Client,
                            new RequestVendorSalePacket { ItemEntityId = item.EntityId, Quantity = 1 });
                        break;
                    case "auction":
                        var auctions = (AuctionHouseManager)Activator.CreateInstance(typeof(AuctionHouseManager),
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                            null, new object[] { context }, null);
                        auctions.RequestCreateAuction(context.Client,
                            new RequestCreateAuctionPacket { ItemEntityId = item.EntityId, Price = 10, Duration = 0 });
                        break;
                    case "central-remove":
                        inventory.RemoveItemBySlot(context.Client, InventoryType.Personal, item.OwnerSlotId);
                        break;
                    case "central-add":
                        inventory.AddItemBySlot(context.Client, InventoryType.HomeInventory, item.EntityId, 0, true);
                        break;
                }
            }
            finally { Rasa.Game.Server.GameUnitOfWorkFactory = originalFactory; }

            Assert.AreEqual(3U, item.StackSize);
            Assert.AreEqual(1, Owned(context).Count(candidate => candidate.Id == item.Id));
            Assert.AreEqual(0, context.Client.Player.Inventory.HomeInventory.Count(id => id != 0));
            Assert.AreEqual(0, context.Client.Player.Inventory.ClanInventory.Count(id => id != 0));
            Assert.AreEqual(0, context.Client.Player.Inventory.AuctionItems.Count);
            Assert.AreEqual(0, context.Client.Player.Inventory.BuybackItems.Count);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            using var verify = context.CreateChar();
            Assert.AreEqual((uint)InventoryType.Personal, verify.CharacterInventories.FindByItemId(item.Id).InventoryType);
            Assert.AreEqual(3U, verify.Items.GetItem(item.Id).StackSize);
        }

        [TestMethod]
        public void OrdinaryConsumptionCannotSpendAssignmentItems()
        {
            using var context = Create(true, MissionId);
            var item = Owned(context).Single();
            using var unit = context.CreateChar();
            var consumption = new InventoryManager.InventoryConsumption();
            Assert.ThrowsExactly<GameplayRejectionException>(() => unit.ExecuteTransaction(() =>
                consumption.PlanAndSave(context.Client, new Dictionary<ulong, uint> { [item.EntityId] = 1 }, unit)));
            Assert.AreEqual(1U, item.StackSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CloneCreditUseRejectsAssignmentItemsWithoutGrantingOnRetry(bool clearRuntimeOwnership)
        {
            using var context = Create(true, MissionId);
            var item = Owned(context).Single();
            var ownership = item.MissionOwnership;
            if (clearRuntimeOwnership)
                item.MissionOwnership = null;
            WithCloneCreditHandler(context, item, handler =>
            {
                var request = new Rasa.Packets.Manifestation.Client.RequestUseCloneCreditPacket { EntityId = item.EntityId };
                context.Drain();
                handler.RequestUseCloneCredit(context.Client, request);
                handler.RequestUseCloneCredit(context.Client, request);
                Assert.AreEqual(0U, context.Client.Player.CloneCredits, "Rejected consumption must never grant clone credits.");
                Assert.AreEqual(1U, item.StackSize);
                Assert.IsTrue(context.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId));
                Assert.IsFalse(context.Drain().OfType<Rasa.Packets.Manifestation.Server.CloneCreditsPacket>().Any());
                using var verify = context.CreateChar();
                Assert.AreEqual(0U, verify.Characters.Get(context.Client.Player.Id).CloneCredits);
                Assert.AreEqual(1U, verify.Items.GetItem(item.Id).StackSize);
                Assert.AreEqual(ownership.AssignmentId, verify.CharacterMissionItems.GetOwner(item.Id).AssignmentId);
            });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void CloneCreditConsumptionAndCreditCommitBeforePublicationAndRetry(int failAtSave)
        {
            using var context = Create();
            GrantUnbound(context, 2);
            var item = Owned(context).Single();
            WithCloneCreditHandler(context, item, handler =>
            {
                var request = new Rasa.Packets.Manifestation.Client.RequestUseCloneCreditPacket { EntityId = item.EntityId };
                var saves = 0;
                context.Drain();
                context.BeforeSave = database =>
                {
                    Assert.AreEqual(2U, item.StackSize, "Consumption must not publish before the credit transaction commits.");
                    Assert.AreEqual(0U, context.Client.Player.CloneCredits);
                    Assert.AreEqual(0, context.Drain().Count);
                    Assert.IsNotNull(database.Database.CurrentTransaction);
                    if (++saves == failAtSave)
                        throw new DbUpdateException("Injected clone-credit persistence failure.");
                };

                handler.RequestUseCloneCredit(context.Client, request);

                context.BeforeSave = null;
                if (failAtSave != 0)
                {
                    Assert.AreEqual(failAtSave, saves, "The requested persistence boundary must be exercised.");
                    Assert.AreEqual(0U, context.Client.Player.CloneCredits);
                    Assert.AreEqual(2U, item.StackSize);
                    Assert.AreEqual(0, context.Drain().Count);
                    using (var verify = context.CreateChar())
                    {
                        Assert.AreEqual(0U, verify.Characters.Get(context.Client.Player.Id).CloneCredits);
                        Assert.AreEqual(2U, verify.Items.GetItem(item.Id).StackSize);
                    }
                    handler.RequestUseCloneCredit(context.Client, request);
                }
                Assert.AreEqual(1U, context.Client.Player.CloneCredits);
                Assert.AreEqual(1U, item.StackSize);
                Assert.AreEqual(1U, context.Drain().OfType<Rasa.Packets.Manifestation.Server.CloneCreditsPacket>().Single().CloneCredits);
                using (var verify = context.CreateChar())
                {
                    Assert.AreEqual(1U, verify.Characters.Get(context.Client.Player.Id).CloneCredits);
                    Assert.AreEqual(1U, verify.Items.GetItem(item.Id).StackSize);
                }

                handler.RequestUseCloneCredit(context.Client, request);
                handler.RequestUseCloneCredit(context.Client, request);

                Assert.AreEqual(2U, context.Client.Player.CloneCredits, "The removed item cannot be redeemed again.");
                Assert.IsFalse(context.Client.Player.Inventory.PersonalInventory.Contains(item.EntityId));
                Assert.AreEqual(2U, context.Drain().OfType<Rasa.Packets.Manifestation.Server.CloneCreditsPacket>().Single().CloneCredits);
                using var after = context.CreateChar();
                Assert.AreEqual(2U, after.Characters.Get(context.Client.Player.Id).CloneCredits);
                Assert.IsNull(after.Items.GetItem(item.Id));
            });
        }

        [TestMethod]
        public void RewardGrantAndOrdinaryConsumptionShareOneUnpublishedShadowStack()
        {
            using var context = Create();
            GrantUnbound(context, 2);
            var item = Owned(context).Single();
            using var grant = new InventoryManager.InventoryGrant();
            var consumption = new InventoryManager.InventoryConsumption();
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    grant.PlanAndSave(context.Client, new[] { new InventoryManager.InventoryItemGrant(TemplateId, 2) }, unit);
                    consumption.PlanAndSave(context.Client, new Dictionary<ulong, uint> { [item.EntityId] = 1 }, unit);
                    Assert.AreEqual(2U, item.StackSize);
                });
            consumption.Publish(context.Client);
            grant.Publish(context.Client);
            Assert.AreEqual(3U, item.StackSize);
            using var verify = context.CreateChar();
            Assert.AreEqual(3U, verify.Items.GetItem(item.Id).StackSize);
        }

        [TestMethod]
        public void LootCannotMergeProtectedStacksAndComposesWithAnExplicitCharacterOwnedCost()
        {
            using var context = Create(true, MissionId);
            var loot = new LootItem(context.CreateInventoryItem(TemplateId, 3147, 3), context.Client.Player.EntityId, 0);
            var grant = new InventoryManager.LootGrant();
            MissionItemPlanner plan = null;
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    grant.PlanAndSave(context.Client, new[] { loot }, unit);
                    var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, MissionId);
                    plan = new MissionItemPlanner(context.Client, unit, context.Manager);
                    plan.Apply(new ConsumeMissionItemIntent("authored-loot-cost", MissionId, "owned-cost", 1,
                        MissionItemScope.CharacterOwned), assignment.AssignmentId, assignment.Generation);
                });
            plan.Publish(context.Client);
            grant.Publish(context.Client);
            Assert.IsTrue(loot.Taken);
            Assert.AreEqual(2, Owned(context).Length);
            Assert.AreEqual(1U, Owned(context).Single(item => item.MissionOwnership != null).StackSize);
            Assert.AreEqual(2U, Owned(context).Single(item => item.MissionOwnership == null).StackSize);
        }

        [TestMethod]
        [DataRow("move")]
        [DataRow("discard")]
        [DataRow("delete")]
        [DataRow("stack")]
        [DataRow("clan")]
        public void DurableInventoryBoundariesRejectUnownedMutationEvenWithoutTheRuntimeMarker(string operation)
        {
            using var context = Create(true, MissionId);
            var item = Owned(context).Single();
            item.MissionOwnership = null;
            using var unit = context.CreateChar();
            Assert.ThrowsExactly<InvalidOperationException>(() => unit.ExecuteTransaction(() =>
            {
                switch (operation)
                {
                    case "move": unit.CharacterInventories.MoveInvItem(1, 2, (uint)InventoryType.Personal, 50, item.Id); break;
                    case "discard": unit.CharacterInventories.DeleteInvItemByItemId(item.Id); break;
                    case "delete": unit.Items.DeleteItem(item.Id); break;
                    case "stack": unit.Items.UpdateItemStackSize(new Item(TemplateId, 2, 0, 0) { Id = item.Id }); break;
                    case "clan": unit.ClanInventories.AddInvItem(1, 0, item.Id); break;
                }
            }));
            Assert.AreEqual(1U, unit.CharacterMissionItems.GetOwned(1).Single().Quantity);
            Assert.AreEqual(1U, unit.Items.GetItem(item.Id).StackSize);
        }

        [TestMethod]
        public void AnInventoryChangeDuringFinalWritesRollsBackTheStagedItemAndReceipt()
        {
            using var context = Create();
            GrantUnbound(context, 2);
            var existing = Owned(context).Single();
            var triggered = false;
            context.AfterSave = database =>
            {
                if (!triggered && database.ItemEntries.Local.Any())
                {
                    triggered = true;
                    existing.StackSize = 9;
                }
            };
            try
            {
                Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(context,
                    new IssueMissionItemIntent("late-stack", MissionId, ItemKey, TemplateId, 1)));
            }
            finally
            {
                context.AfterSave = null;
                existing.StackSize = 2;
            }
            Assert.IsTrue(triggered);
            Assert.AreEqual(1, Owned(context).Length);
            using var verify = context.CreateChar();
            Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(1).Count);
            var assignment = verify.CharacterMissions.GetByCharacterAndMission(1, MissionId);
            Assert.IsNull(verify.CharacterMissionItems.GetReceipt(1, assignment.AssignmentId, "late-stack"));
        }

        [TestMethod]
        public void PersonalReorderingPreservesTheConcreteItemAndItsAssignmentProvenance()
        {
            using var context = Create(true, MissionId);
            var item = Owned(context).Single();
            var source = item.OwnerSlotId;
            new InventoryManager(context, context.Manager).PersonalInventory_MoveItem(context.Client,
                new PersonalInventory_MoveItemPacket { SrcSlot = (int)source, DestSlot = (int)source + 1 });
            Assert.AreEqual(0UL, context.Client.Player.Inventory.PersonalInventory[(int)source]);
            Assert.AreEqual(item.EntityId, context.Client.Player.Inventory.PersonalInventory[(int)source + 1]);
            Assert.AreEqual(source + 1, item.OwnerSlotId);
            using var verify = context.CreateChar();
            Assert.AreEqual(source + 1, verify.CharacterInventories.FindByItemId(item.Id).SlotId);
            Assert.AreEqual(item.Id, verify.CharacterMissionItems.GetOwned(1).Single().ItemId);
        }

        [TestMethod]
        [DataRow(false, "consume")]
        [DataRow(true, "consume")]
        [DataRow(false, "loot")]
        [DataRow(true, "loot")]
        public void OrdinaryMoveOrSwapKeepsUnrelatedConsumptionAndLootUsable(bool swap, string followingAction)
        {
            using var context = Create();
            context.AddRewardTemplate(29, 3147);
            context.AddRewardTemplate(30, 3147);
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                        swap
                            ? new[] { new InventoryManager.InventoryItemGrant(28, 2),
                                new InventoryManager.InventoryItemGrant(29, 2), new InventoryManager.InventoryItemGrant(30, 2) }
                            : new[] { new InventoryManager.InventoryItemGrant(28, 2), new InventoryManager.InventoryItemGrant(29, 2) },
                        unit));
                grant.Publish(context.Client);
            }
            var moved = Owned(context).Single(item => item.ItemTemplate.ItemTemplateId == 28);
            var swapped = Owned(context).SingleOrDefault(item => item.ItemTemplate.ItemTemplateId == 30);
            var unrelated = Owned(context).Single(item => item.ItemTemplate.ItemTemplateId == 29);
            new InventoryManager(context, context.Manager).PersonalInventory_MoveItem(context.Client,
                new PersonalInventory_MoveItemPacket { SrcSlot = 50, DestSlot = 52 });
            Assert.AreEqual(swapped?.EntityId ?? 0UL, context.Client.Player.Inventory.PersonalInventory[50]);

            if (followingAction == "consume")
            {
                WithCloneCreditHandler(context, unrelated, handler =>
                    handler.RequestUseCloneCredit(context.Client,
                        new Rasa.Packets.Manifestation.Client.RequestUseCloneCreditPacket { EntityId = unrelated.EntityId }));
                Assert.AreEqual(1U, context.Client.Player.CloneCredits, "A normal move must not invalidate unrelated consumption.");
                Assert.AreEqual(1U, unrelated.StackSize);
            }
            else
            {
                var corpse = context.AddNpc(99);
                corpse.State = CharacterState.Dead;
                corpse.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 0, 0, 0);
                context.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                var incoming = context.CreateInventoryItem(31, 3147, 3);
                var loot = new LootDispenser
                {
                    Owner = context.Client.Player.EntityId, AttachedTo = corpse.EntityId,
                    IsLootable = true, UnitOfWorkFactory = context
                };
                loot.LootItems.Add(new LootItem(incoming, context.Client.Player.EntityId, 0));
                corpse.CorpseLootEntityId = loot.EntityId;
                context.Map.LootDispensers.Add(loot.EntityId, loot);
                var looting = new LootDispenserManager(context, _ => 2, missionManager: context.Manager);
                try
                {
                    looting.RequestLootAllFromCorpse(context.Client,
                        new Rasa.Packets.LootDispenser.Client.RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                    Assert.IsTrue(loot.FullyLooted, "A normal move must not invalidate an unrelated corpse-loot claim.");
                    Assert.IsTrue(context.Client.Player.Inventory.PersonalInventory.Contains(incoming.EntityId));
                }
                finally { looting.RemoveForOwner(context.Map, context.Client); }
            }

            Assert.AreEqual(52U, moved.OwnerSlotId);
            Assert.AreEqual(moved.EntityId, context.Client.Player.Inventory.PersonalInventory[52]);
            using var verify = context.CreateChar();
            Assert.AreEqual(52U, verify.CharacterInventories.FindByItemId(moved.Id).SlotId);
            if (swapped != null)
            {
                Assert.AreEqual(50U, swapped.OwnerSlotId);
                Assert.AreEqual(50U, verify.CharacterInventories.FindByItemId(swapped.Id).SlotId);
            }
        }

        [TestMethod]
        public void InventoryValidationQueryCountDoesNotGrowWithOccupiedSlots()
        {
            var counts = new Dictionary<int, (int Total, int Items, int Owners)>();
            foreach (var occupiedSlots in new[] { 100, 250 })
            {
                using var context = Create();
                var source = FillPersonalInventory(context, occupiedSlots)[50];
                var selects = new List<string>();
                context.BeforeCommand = command =>
                {
                    if (command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        selects.Add(command);
                };

                WithCloneCreditHandler(context, source, handler =>
                    handler.RequestUseCloneCredit(context.Client,
                        new Rasa.Packets.Manifestation.Client.RequestUseCloneCreditPacket { EntityId = source.EntityId }));

                context.BeforeCommand = null;
                Assert.AreEqual(1U, context.Client.Player.CloneCredits);
                Assert.AreEqual(1U, source.StackSize);
                Assert.AreEqual(occupiedSlots, Owned(context).Length, "The measured operation consumes only part of one stack.");
                using var verify = context.CreateChar();
                Assert.AreEqual(1U, verify.Characters.Get(context.Client.Player.Id).CloneCredits);
                Assert.AreEqual(1U, verify.Items.GetItem(source.Id).StackSize);
                counts[occupiedSlots] = (selects.Count,
                    selects.Count(command => command.Contains("FROM \"items\"", StringComparison.Ordinal)),
                    selects.Count(command => command.Contains("FROM \"character_mission_item\"", StringComparison.Ordinal)));
                Console.WriteLine($"{occupiedSlots} occupied slots: {counts[occupiedSlots].Total} SELECTs, " +
                    $"{counts[occupiedSlots].Items} item reads, {counts[occupiedSlots].Owners} provenance reads.");
            }

            Assert.AreEqual(counts[100].Total, counts[250].Total, "Validation reads must not scale with occupied slots.");
            Assert.AreEqual(counts[100].Items, counts[250].Items);
            Assert.AreEqual(counts[100].Owners, counts[250].Owners);
            Assert.IsTrue(counts[250].Items >= 3, "Initial, prepare and final validation must each read authoritative items.");
            Assert.IsTrue(counts[250].Total <= 40, "A single-stack handler must use bounded batched validation.");
        }

        [TestMethod]
        [DataRow(false, 1)]
        [DataRow(true, 2)]
        public void OrdinaryMoveOrSwapFailureKeepsBothSlotsUntilCommit(bool swap, int failAtSave)
        {
            using var context = Create();
            GrantUnbound(context, 2);
            if (swap)
            {
                context.AddRewardTemplate(29, 3147);
                using var grant = new InventoryManager.InventoryGrant();
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                        new[] { new InventoryManager.InventoryItemGrant(29, 2) }, unit));
                grant.Publish(context.Client);
            }
            var items = Owned(context);
            var slots = context.Client.Player.Inventory.PersonalInventory.ToArray();
            var saves = 0;
            context.Drain();
            context.BeforeSave = database =>
            {
                Assert.IsNotNull(database.Database.CurrentTransaction);
                CollectionAssert.AreEqual(slots, context.Client.Player.Inventory.PersonalInventory);
                Assert.AreEqual(50U, items[0].OwnerSlotId);
                if (swap)
                    Assert.AreEqual(51U, items[1].OwnerSlotId);
                Assert.AreEqual(0, context.Drain().Count);
                if (++saves == failAtSave)
                    throw new DbUpdateException("Injected ordinary move failure.");
            };
            var handler = new InventoryManager(context, context.Manager);
            var request = new PersonalInventory_MoveItemPacket { SrcSlot = 50, DestSlot = 51 };

            handler.PersonalInventory_MoveItem(context.Client, request);

            context.BeforeSave = null;
            Assert.AreEqual(failAtSave, saves);
            CollectionAssert.AreEqual(slots, context.Client.Player.Inventory.PersonalInventory);
            Assert.AreEqual(0, context.Drain().Count);
            using (var verify = context.CreateChar())
                foreach (var item in items)
                    Assert.AreEqual(item.OwnerSlotId, verify.CharacterInventories.FindByItemId(item.Id).SlotId);

            handler.PersonalInventory_MoveItem(context.Client, request);

            Assert.AreEqual(51U, items[0].OwnerSlotId);
            Assert.AreEqual(items[0].EntityId, context.Client.Player.Inventory.PersonalInventory[51]);
            if (swap)
            {
                Assert.AreEqual(50U, items[1].OwnerSlotId);
                Assert.AreEqual(items[1].EntityId, context.Client.Player.Inventory.PersonalInventory[50]);
            }
        }

        [TestMethod]
        [DataRow(100, "stack")]
        [DataRow(250, "stack")]
        [DataRow(100, "slot")]
        [DataRow(250, "slot")]
        [DataRow(100, "owner")]
        [DataRow(250, "owner")]
        [DataRow(100, "foreign-owner")]
        [DataRow(250, "foreign-owner")]
        [DataRow(100, "orphan-owner")]
        [DataRow(250, "orphan-owner")]
        [DataRow(100, "resurrected-item")]
        [DataRow(250, "resurrected-item")]
        public void BatchedFinalValidationRejectsLateDurableChangesBeforeClonePublication(int occupiedSlots, string change)
        {
            using var context = Create();
            context.SeedCharacter(2, 0, 2);
            var items = FillPersonalInventory(context, occupiedSlots);
            var source = items[50];
            var other = items.Last();
            var originalSlots = context.Client.Player.Inventory.PersonalInventory.ToArray();
            var sourceCount = change == "resurrected-item" ? 1U : 2U;
            if (sourceCount == 1)
            {
                source.StackSize = 1;
                using var seed = context.CreateChar();
                seed.Items.UpdateItemStackSize(source);
            }
            var saves = 0;
            var injected = false;
            context.AfterSave = database =>
            {
                if (++saves != (sourceCount == 1 ? 3 : 2))
                    return;
                Assert.IsNotNull(database.Database.CurrentTransaction);
                Assert.AreEqual(sourceCount, source.StackSize);
                Assert.AreEqual(0U, context.Client.Player.CloneCredits);
                switch (change)
                {
                    case "stack":
                        database.Database.ExecuteSqlInterpolated($"UPDATE items SET stack_size = 3 WHERE item_id = {other.Id}");
                        break;
                    case "slot":
                        database.Database.ExecuteSqlInterpolated($"UPDATE character_inventory SET slot_id = 999 WHERE item_id = {other.Id}");
                        break;
                    case "owner":
                    case "foreign-owner":
                    case "orphan-owner":
                        var ownerId = change == "foreign-owner" ? 2U : context.Client.Player.Id;
                        var itemId = change == "orphan-owner" ? uint.MaxValue : other.Id;
                        var assignmentId = Guid.NewGuid().ToString("N");
                        database.Database.ExecuteSqlInterpolated(
                            $"INSERT INTO character_mission_item (character_id, mission_id, assignment_id, generation, item_key, item_id, quantity) VALUES ({ownerId}, {MissionId}, {assignmentId}, 1, 'late-owner', {itemId}, 2)");
                        break;
                    case "resurrected-item":
                        database.Database.ExecuteSqlInterpolated(
                            $"INSERT INTO items (item_id, item_template_id, stack_size, current_hp, color, ammo_count, crafter_name, created_at) VALUES ({source.Id}, {source.ItemTemplate.ItemTemplateId}, 1, 0, 0, 0, '', {DateTime.UtcNow})");
                        break;
                }
                injected = true;
            };
            context.Drain();

            WithCloneCreditHandler(context, source, handler =>
                handler.RequestUseCloneCredit(context.Client,
                    new Rasa.Packets.Manifestation.Client.RequestUseCloneCreditPacket { EntityId = source.EntityId }));

            context.AfterSave = null;
            Assert.IsTrue(injected, "A real durable mutation must land after the final write, not fail in the injection.");
            Assert.AreEqual(0U, context.Client.Player.CloneCredits);
            Assert.AreEqual(sourceCount, source.StackSize);
            CollectionAssert.AreEqual(originalSlots, context.Client.Player.Inventory.PersonalInventory);
            Assert.AreEqual(0, context.Drain().Count, "No inventory or credit packet can precede final authoritative validation.");
            using (var verify = context.CreateChar())
            {
                Assert.AreEqual(0U, verify.Characters.Get(context.Client.Player.Id).CloneCredits);
                Assert.AreEqual(sourceCount, verify.Items.GetItem(source.Id).StackSize);
                Assert.AreEqual(2U, verify.Items.GetItem(other.Id).StackSize);
                Assert.AreEqual(other.OwnerSlotId, verify.CharacterInventories.FindByItemId(other.Id).SlotId);
                Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(context.Client.Player.Id).Count);
                Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(2).Count);
            }

            WithCloneCreditHandler(context, source, handler =>
                handler.RequestUseCloneCredit(context.Client,
                    new Rasa.Packets.Manifestation.Client.RequestUseCloneCreditPacket { EntityId = source.EntityId }));
            Assert.AreEqual(1U, context.Client.Player.CloneCredits, "The rejected transaction must leave a retryable ordinary item.");
        }

        [TestMethod]
        public void CharacterDeletionClearsDurableOwnershipReceiptsAndRegisteredAssignmentItems()
        {
            using var context = Create(true, MissionId);
            var item = Owned(context).Single();
            var assignmentId = item.MissionOwnership.AssignmentId;
            context.Client.State = RasaGame::Rasa.Data.ClientState.CharacterSelection;
            context.BeforeSave = _ => throw new DbUpdateException("Injected character deletion failure.");
            new CharacterManager(context).RequestDeleteCharacterInSlot(context.Client,
                new Rasa.Packets.Game.Client.RequestDeleteCharacterInSlotPacket { Slot = 0 });
            using (var failed = context.CreateChar())
            {
                Assert.IsNotNull(failed.Characters.Find(1));
                Assert.AreEqual(1, failed.CharacterMissionItems.GetOwned(1).Count);
                Assert.AreSame(item, EntityManager.Instance.GetItem(item.EntityId));
            }
            context.BeforeSave = null;
            new CharacterManager(context).RequestDeleteCharacterInSlot(context.Client,
                new Rasa.Packets.Game.Client.RequestDeleteCharacterInSlotPacket { Slot = 0 });
            using var verify = context.CreateChar();
            Assert.IsNull(verify.Characters.Find(1));
            Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(1).Count);
            Assert.IsNull(verify.CharacterMissionItems.GetReceipt(1, assignmentId, "accept-key"));
            Assert.IsNull(verify.Items.GetItem(item.Id));
            Assert.IsNull(EntityManager.Instance.GetItem(item.EntityId));
        }

        [TestMethod]
        public void CompletingAndClearingCleansTemporaryItemsButKeepsNormalRewards()
        {
            using var context = Create(true, MissionId);
            GrantUnbound(context, 2);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.AddNpc(88).EntityId, MissionId, null));
            Assert.AreEqual(4U, Owned(context).Single().StackSize);
            Assert.IsNull(Owned(context).Single().MissionOwnership);
            Assert.IsTrue(context.Manager.TryClear(context.Client, MissionId));
            using var verify = context.CreateChar();
            Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(1).Count);
            Assert.AreEqual(4U, Owned(context).Single().StackSize);
        }

        [TestMethod]
        public void RemovingTheActiveRowDoesNotCascadeTheCleanupLedger()
        {
            using var context = Create(true, MissionId);
            var item = Owned(context).Single();
            using var unit = context.CreateChar();
            unit.ExecuteTransaction(() => unit.CharacterMissions.Remove(1, MissionId));
            Assert.AreEqual(item.Id, unit.CharacterMissionItems.GetOwned(1).Single().ItemId);
            Assert.IsNotNull(unit.Items.GetItem(item.Id));
        }

        [TestMethod]
        public void FullMissionInventoryRejectsConradContinueWithoutProgressDeadlineOrLostSession()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ConradCorpseDialogueTests.FindMissingSoldiers(harness);
            var template = ItemManager.Instance.GetItemTemplateById(11519);
            var quantity = checked(EntityClassManager.Instance.GetClassInfo(template.Class).ItemClassInfo.StackSize * 50);
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(11519, quantity) }, unit));
            grant.Publish(harness.Client);
            Assert.AreEqual(50, harness.Client.Player.Inventory.PersonalInventory.Skip(150).Take(50).Count(id => id != 0));
            var corpse = ConradCorpseDialogueTests.Corpse(harness);
            harness.MovePlayerTo(corpse);
            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = corpse.EntityId });
            var session = harness.Client.MissionConversation;
            npcs.CompleteNPCObjective(harness.Client, ConradCorpseDialogueTests.Continue(corpse));
            Assert.AreSame(session, harness.Client.MissionConversation);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[1995].Objectives[3].State);
            Assert.AreEqual(50, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            using var verify = harness.Context.CreateChar();
            Assert.IsNull(verify.CharacterMissionDeadlines.Get(harness.Client.Player.Id, 1995));
            Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Count);
        }

        [TestMethod]
        public void ItemWriteFailureRollsBackOwnershipReceiptAndInventoryAndAllowsRetry()
        {
            using var context = Create();
            var entities = EntityManager.Instance.Items.Keys.ToArray();
            context.AfterSave = database =>
            {
                if (database.ItemEntries.Local.Any())
                    throw new DbUpdateException("Injected staged item write failure.");
            };
            var intent = new IssueMissionItemIntent("rollback-key", MissionId, ItemKey, TemplateId, 1);
            Assert.ThrowsExactly<DbUpdateException>(() => Apply(context, intent));
            Assert.AreEqual(0, Owned(context).Length);
            CollectionAssert.AreEquivalent(entities, EntityManager.Instance.Items.Keys.ToArray());
            using (var verify = context.CreateChar())
            {
                var assignment = verify.CharacterMissions.GetByCharacterAndMission(1, MissionId);
                Assert.IsNull(verify.CharacterMissionItems.GetReceipt(1, assignment.AssignmentId, "rollback-key"));
                Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(1).Count);
            }
            context.AfterSave = null;
            Apply(context, intent);
            Assert.AreEqual(1U, Owned(context).Single().StackSize);
        }

        [TestMethod]
        [DataRow("runtime-stack")]
        [DataRow("durable-stack")]
        [DataRow("slot")]
        [DataRow("identity")]
        [DataRow("provenance")]
        public void StaleInventoryCannotIssueAnItemOrSaveItsReceipt(string stale)
        {
            using var context = Create();
            GrantUnbound(context, 2);
            var item = Owned(context).Single();
            var itemId = item.Id;
            var slot = item.OwnerSlotId;
            if (stale == "runtime-stack") item.StackSize = 3;
            if (stale == "slot") item.OwnerSlotId++;
            if (stale == "identity") item.Id = uint.MaxValue;
            if (stale == "provenance") item.MissionOwnership = new(1, MissionId, Guid.NewGuid().ToString("N"), 1, ItemKey);
            if (stale == "durable-stack")
            {
                using var unit = context.CreateChar();
                unit.Items.UpdateItemStackSize(new Item(TemplateId, 3, 0, 0) { Id = item.Id });
            }
            try
            {
                Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(context,
                    new IssueMissionItemIntent("stale-key", MissionId, ItemKey, TemplateId, 1)));
            }
            finally
            {
                item.Id = itemId;
                item.StackSize = 2;
                item.OwnerSlotId = slot;
                item.MissionOwnership = null;
            }
            using var verify = context.CreateChar();
            Assert.AreEqual(0, verify.CharacterMissionItems.GetOwned(1).Count);
        }

        [TestMethod]
        public void DuplicateIssueConsumeRetryAndOperationKeyCollisionsAreAssignmentScoped()
        {
            using var context = Create();
            var issue = new IssueMissionItemIntent("once", MissionId, ItemKey, TemplateId, 2);
            Apply(context, issue);
            var itemId = Owned(context).Single().Id;
            Apply(context, issue);
            Assert.AreEqual(itemId, Owned(context).Single().Id);
            Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(context, issue with { Quantity = 1 }));
            Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(context, issue with { OperationKey = "too-many" }));
            var consume = new ConsumeMissionItemIntent("once-consume", MissionId, ItemKey, 1, MissionItemScope.AssignmentIssued);
            Apply(context, consume);
            Apply(context, consume, issue);
            Assert.AreEqual(1U, Owned(context).Single().StackSize);
            using var verify = context.CreateChar();
            Assert.AreEqual(1U, verify.CharacterMissionItems.GetOwned(1).Single().Quantity);
        }

        [TestMethod]
        public void CleanupRejectsAChangedAssignmentGenerationInsteadOfSilentlyLeavingItsItem()
        {
            using var context = Create(true, MissionId);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(1, MissionId).Generation++);
            Assert.IsFalse(context.Manager.TryFailMission(context.Client, MissionId));
            Assert.AreEqual(MissionState.Active, context.Client.Player.Missions[MissionId].State);
            Assert.AreEqual(1U, Owned(context).Single().StackSize);
        }

        [TestMethod]
        public void TheActualTradeOfferRejectsAssignmentOwnershipEvenWithAMissingRuntimeMarker()
        {
            using var context = Create(true, MissionId);
            var partner = context.CreateAdditionalClient(2);
            var item = Owned(context).Single();
            item.MissionOwnership = null;
            var trades = (TradeManager)Activator.CreateInstance(typeof(TradeManager), nonPublic: true);
            var clients = Rasa.Game.Server.Clients;
            var addOwner = !clients.Contains(context.Client);
            var addPartner = !clients.Contains(partner);
            var factory = Rasa.Game.Server.GameUnitOfWorkFactory;
            if (addOwner) clients.Add(context.Client);
            if (addPartner) clients.Add(partner);
            Rasa.Game.Server.GameUnitOfWorkFactory = context;
            try
            {
                trades.RequestTrade(context.Client, new RequestTradePacket { TargetEntityId = (long)partner.Player.EntityId });
                trades.RequestAcceptTradeRequest(partner, new RequestAcceptTradeRequestPacket());
                Assert.AreEqual(1, context.Drain().OfType<TradeCreatePacket>().Count());
                trades.RequestAddItemToTrade(context.Client,
                    new RequestAddItemToTradePacket { ItemEntityId = (long)item.EntityId, ConfigurationId = 1 });
                Assert.AreEqual(0, context.Drain().OfType<TradeAddItemPacket>().Count());
                Assert.AreEqual(item.Id, Owned(context).Single().Id);
            }
            finally
            {
                Rasa.Game.Server.GameUnitOfWorkFactory = factory;
                if (addOwner) clients.Remove(context.Client);
                if (addPartner) clients.Remove(partner);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WithdrawingAnOrdinaryItemCannotSwapAProtectedItemIntoStorage(bool clan)
        {
            using var context = Create(true, MissionId);
            var protectedItem = Owned(context).Single();
            var incoming = context.CreateInventoryItem(TemplateId, 3147, 1);
            incoming.OwnerId = 0;
            incoming.OwnerSlotId = 0;
            context.Client.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, 480).ToList();
            context.Client.Player.Inventory.ClanInventory = Enumerable.Repeat(0UL, 500).ToList();
            if (clan)
                context.CreateClanForPlayer();
            using (var unit = context.CreateChar())
            {
                if (clan)
                    unit.ClanInventories.AddInvItem(context.Client.Player.ClanId, 0, incoming.Id);
                else
                    unit.CharacterInventories.AddInvItem(1, 0, (uint)InventoryType.HomeInventory, 0, incoming.Id);
            }
            var storage = clan ? context.Client.Player.Inventory.ClanInventory : context.Client.Player.Inventory.HomeInventory;
            storage[0] = incoming.EntityId;
            var inventory = new InventoryManager(context, context.Manager);
            if (clan)
                inventory.ClanLockbox_WithdrawItem(context.Client,
                    new ClanLockbox_WithdrawItemPacket { SrcSlot = 0, DestSlot = protectedItem.OwnerSlotId, ManagePersonalSlot = false });
            else
                inventory.RequestTakeItemFromHomeInventory(context.Client,
                    new RequestTakeItemFromHomeInventoryPacket { SrcSlot = 0, DestSlot = protectedItem.OwnerSlotId });
            Assert.AreEqual(protectedItem.EntityId, context.Client.Player.Inventory.PersonalInventory[(int)protectedItem.OwnerSlotId]);
            Assert.AreEqual(incoming.EntityId, storage[0]);
            using var verify = context.CreateChar();
            Assert.AreEqual((uint)InventoryType.Personal, verify.CharacterInventories.FindByItemId(protectedItem.Id).InventoryType);
        }

        [TestMethod]
        public void RetryAcceptanceIssuesANewAssignmentItemAndTimeoutPreservesAnUnboundBomb()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            ConradCorpseDialogueTests.FindMissingSoldiers(harness);
            harness.UseObjectAndRecover(ConradCorpseDialogueTests.Corpse(harness));
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                    new[] { new InventoryManager.InventoryItemGrant(11519, 1) }, unit));
            grant.Publish(harness.Client);
            harness.UtcNow = harness.UtcNow.AddSeconds(601);
            Assert.IsTrue(harness.Manager.EvaluateDeadlines(harness.Client));
            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            var giver = BootcampRuntimeTestHarness.FindCreature(harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 2005));
            uint issuedItemId;
            using (var unit = harness.Context.CreateChar())
            {
                var owned = unit.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Single();
                Assert.AreEqual(2005U, owned.MissionId);
                issuedItemId = owned.ItemId;
            }
            harness.ReconnectFresh();
            Assert.AreEqual(2, harness.ReadOwnedTemplateCounts(11519).GetValueOrDefault(11519U));
            using var verify = harness.Context.CreateChar();
            Assert.AreEqual(issuedItemId, verify.CharacterMissionItems.GetOwned(harness.Client.Player.Id).Single().ItemId);
        }

        [TestMethod]
        public void ItemBindingsAndAcceptanceMetadataSurviveEveryMissionCopyPath()
        {
            using var context = Create(true, MissionId);
            Assert.IsTrue(context.Manager.TryGetOperationalMission(MissionId, out var original));
            var copies = new[] { original.WithWorldMetadata(original), original.DisableOperational("fixture disabled"),
                original.WithDialogue(Array.Empty<MissionDialogueTopicDefinition>()),
                original.WithPolicies(new Dictionary<uint, Rasa.Missions.Runtime.MissionCreditPolicy>(), null) };
            foreach (var copy in copies)
            {
                Assert.AreEqual(original.Items[ItemKey], copy.Items[ItemKey]);
                Assert.AreEqual(original.AcceptanceItems.Single(), copy.AcceptanceItems.Single());
            }
            Assert.ThrowsExactly<NotSupportedException>(() =>
                ((IDictionary<string, MissionItemBinding>)original.Items).Clear());
        }

        [TestMethod]
        public void FrozenScenesOmitAbsentItemMetadataAndCleanupDispositionsAreRequiredInNewBindings()
        {
            var old = JsonSerializer.Serialize(Rasa.Services.Preloader.Missions.BootcampMissionDataV1.Mission1995(),
                MissionContentCodec.Options);
            Assert.IsFalse(old.Contains("\"items\"", StringComparison.Ordinal));
            Assert.IsFalse(old.Contains("\"acceptanceItems\"", StringComparison.Ordinal));
            Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MissionSceneDefinition>(
                "{\"items\":[{\"itemKey\":\"key\",\"itemTemplateId\":28,\"scope\":\"AssignmentIssued\",\"maximumQuantity\":1}]}",
                MissionContentCodec.Options));
            Assert.AreEqual(18, typeof(MissionActionEntry).GetProperties().Count(property =>
                Attribute.IsDefined(property, typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute))));
        }

        [TestMethod]
        public void ContentLoadRejectsAnItemActionWhoseAuthoredBindingWasRemoved()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            Content.MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes => scenes[1995].Items = null);
            var error = Assert.ThrowsExactly<Rasa.Missions.Runtime.MissionRuleException>(() => harness.Manager.LoadMissions());
            StringAssert.Contains(error.Message, "item binding");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NativeChoicePaysOnlyItsExplicitCharacterOwnedCostBeforeCommittingTheBranch(bool affordable)
        {
            var transitions = Enumerable.Range(1, 3).Select(index => new MissionObjectiveExecutableTransition(
                (uint)index, (uint)index, MissionObjectiveState.Completed,
                new[] { new MissionObjectiveConversation(586, 1, MissionObjectiveConversationType.Completion) },
                null, null, null, index == 1 ? new[] { new MissionActionDefinition
                {
                    MissionId = MissionId, ObjectiveId = 1, TransitionId = 1, ActionId = 1,
                    Kind = MissionActionKind.ConsumeMissionItem, Sequence = 1,
                    ItemIntent = new ConsumeMissionItemIntent("pay-choice", MissionId, "fee", 1, MissionItemScope.CharacterOwned)
                } } : Array.Empty<MissionActionDefinition>())).ToArray();
            var objective = new MissionObjectiveDefinition(1, 901, 901, Array.Empty<uint?>(), 1,
                MissionObjectiveState.Incomplete, true, null, null, transitions.SelectMany(transition => transition.Conversations),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(), executableTransitions: transitions);
            var mission = new Mission(MissionId, "Survey choice", 901, 77, 88, 1, 1, 1, false, false,
                new[] { objective }, true,
                dialogue: new[] { new MissionDialogueTopicDefinition(1, 586, 1, MissionDialogueKind.Choice,
                    choices: new Dictionary<int, uint> { [1] = 1, [2] = 2, [3] = 3 }) },
                items: new[] { new MissionItemBinding("fee", TemplateId, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain) });
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [MissionId] = mission });
            context.AddRewardTemplate(TemplateId, 3147);
            context.SeedMission(1, MissionId, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            if (affordable)
                GrantUnbound(context, 2);
            var npc = context.AddNpc(77, npcPackageId: 586);
            var manager = new NpcManager(context, context.Manager);
            manager.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            var session = context.Client.MissionConversation;
            Assert.IsNotNull(session);
            manager.PerformNPCChoice(context.Client, new PerformNPCChoicePacket
            { EntityId = npc.EntityId, MissionId = MissionId, ObjectiveId = 1, PlayerFlagId = 1, ChoiceIdx = 1 });
            Assert.AreEqual(affordable ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[MissionId].Objectives[1].State);
            if (affordable)
                Assert.AreEqual(1U, Owned(context).Single().StackSize);
            else
                Assert.AreSame(session, context.Client.MissionConversation);
        }

        [TestMethod]
        [DataRow("assignment")]
        [DataRow("generation")]
        [DataRow("revision")]
        public void PlannerRequiresTheExactAssignmentGenerationAndCompatibleRevision(string mismatch)
        {
            using var context = Create();
            using var unit = context.CreateChar();
            var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, MissionId);
            if (mismatch == "revision")
                assignment.ContentRevision = "other-release";
            Assert.ThrowsExactly<GameplayRejectionException>(() => unit.ExecuteTransaction(() =>
                new MissionItemPlanner(context.Client, unit, context.Manager).Apply(
                    new IssueMissionItemIntent("stale-owner", MissionId, ItemKey, TemplateId, 1),
                    mismatch == "assignment" ? Guid.NewGuid().ToString("N") : assignment.AssignmentId,
                    mismatch == "generation" ? assignment.Generation + 1 : assignment.Generation)));
            Assert.AreEqual(0, Owned(context).Length);
            Assert.AreEqual(0, unit.CharacterMissionItems.GetOwned(1).Count);
        }

        [TestMethod]
        [DataRow("unknown-key")]
        [DataRow("template")]
        [DataRow("quantity")]
        [DataRow("scope")]
        [DataRow("cost-cleanup")]
        [DataRow("operation-key")]
        public void InvalidItemOperationsAreRejectedBeforeWriting(string invalid)
        {
            using var context = Create();
            var issue = new IssueMissionItemIntent("invalid", MissionId, ItemKey, TemplateId, 1);
            CharacterIntent intent = invalid switch
            {
                "unknown-key" => issue with { ItemKey = "unbound" },
                "template" => issue with { ItemTemplateId = 29 },
                "quantity" => issue with { Quantity = 4 },
                "scope" => new ConsumeMissionItemIntent("invalid", MissionId, ItemKey, 1, MissionItemScope.CharacterOwned),
                "cost-cleanup" => new RemoveMissionItemsIntent("invalid", MissionId, "owned-cost"),
                _ => issue with { OperationKey = "" }
            };
            Assert.ThrowsExactly<GameplayRejectionException>(() => Apply(context, intent));
            Assert.AreEqual(0, Owned(context).Length);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void OrdinaryDialogueIssuesAtomicallyAndFullInventoryRetainsTheOpenedSession(bool full, bool failedItemWrite)
        {
            var transition = new MissionObjectiveExecutableTransition(11, 1, MissionObjectiveState.Completed,
                new[] { new MissionObjectiveConversation(586, 1, MissionObjectiveConversationType.Completion) },
                null, null, null, new[] { new MissionActionDefinition
                {
                    MissionId = MissionId, ObjectiveId = 1, TransitionId = 11, ActionId = 1,
                    Kind = MissionActionKind.IssueMissionItem, Sequence = 1,
                    ItemIntent = new IssueMissionItemIntent("dialogue-key", MissionId, ItemKey, TemplateId, 1)
                } });
            var objective = new MissionObjectiveDefinition(1, 901, 901, Array.Empty<uint?>(), 1,
                MissionObjectiveState.Incomplete, true, null, null, transition.Conversations,
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                executableTransitions: new[] { transition });
            var mission = new Mission(MissionId, "Survey dialogue", 901, 77, 88, 1, 1, 1, false, false,
                new[] { objective }, true, items: new[] { Binding() });
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [MissionId] = mission });
            context.AddRewardTemplate(TemplateId, 3147);
            context.SeedMission(1, MissionId, (uint)MissionState.Active, false);
            context.ReloadPlayerMissions();
            if (full)
                context.FillRewardCategory();
            var npc = context.AddNpc(77, npcPackageId: 586);
            var manager = new NpcManager(context, context.Manager);
            manager.RequestNpcConverse(context.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            var session = context.Client.MissionConversation;
            Assert.IsNotNull(session);
            if (failedItemWrite)
                context.AfterSave = database =>
                {
                    if (database.ItemEntries.Local.Any())
                        throw new DbUpdateException("Injected dialogue item creation failure.");
                };

            manager.CompleteNPCObjective(context.Client, new CompleteNPCObjectivePacket
            { EntityId = npc.EntityId, MissionId = MissionId, ObjectiveId = 1, PlayerFlagId = 1 });

            Assert.AreEqual(full || failedItemWrite ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                context.Client.Player.Missions[MissionId].Objectives[1].State);
            if (full || failedItemWrite)
                Assert.AreSame(session, context.Client.MissionConversation);
            else
                Assert.AreEqual(1U, Owned(context).Single().StackSize);
            if (failedItemWrite)
            {
                context.AfterSave = null;
                manager.CompleteNPCObjective(context.Client, new CompleteNPCObjectivePacket
                { EntityId = npc.EntityId, MissionId = MissionId, ObjectiveId = 1, PlayerFlagId = 1 });
                Assert.AreEqual(1U, Owned(context).Single().StackSize);
            }
        }

        private static MissionItemBinding Binding() => new(ItemKey, TemplateId, MissionItemScope.AssignmentIssued, 3,
            MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove);

        private static Item[] FillPersonalInventory(MissionTestContext context, int occupiedSlots)
        {
            var templates = new Dictionary<int, ItemTemplate>();
            for (var category = 0; category < InventoryManager.PersonalCategoryCount; category++)
            {
                var templateId = (uint)(1000 + category);
                context.AddRewardTemplate(templateId, 3147);
                var template = ItemManager.Instance.GetItemTemplateById(templateId);
                template.InventoryCategory = (InventoryCategory)(category + 1);
                templates[category] = template;
            }
            var items = new List<Item>();
            using var database = context.Open();
            for (var slot = 0; slot < occupiedSlots; slot++)
            {
                var item = ItemManager.StageItem(templates[slot / InventoryManager.PersonalCategorySize], 2, "");
                item.Id = (uint)slot + 1;
                item.OwnerId = context.Client.Player.Id;
                item.OwnerSlotId = (uint)slot;
                database.ItemEntries.Add(new Rasa.Structures.Char.ItemEntry(item) { ItemId = item.Id });
                database.CharacterInventoryEntries.Add(new Rasa.Structures.Char.CharacterInventoryEntry(
                    context.Client.AccountEntry.Id, context.Client.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id));
                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                context.Client.Player.Inventory.PersonalInventory[slot] = item.EntityId;
                items.Add(item);
            }
            database.SaveChanges();
            return items.ToArray();
        }

        private static void WithCloneCreditHandler(MissionTestContext context, Item item, Action<ManifestationManager> action)
        {
            var classInfo = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class);
            var augmentations = classInfo.Augmentations;
            classInfo.Augmentations = augmentations.Append(AugmentationType.CloneCredit).ToList();
            try { action(new ManifestationManager(context, context.Manager)); }
            finally { classInfo.Augmentations = augmentations; }
        }

        private static void GrantUnbound(MissionTestContext context, uint quantity)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(context.Client,
                    new[] { new InventoryManager.InventoryItemGrant(TemplateId, quantity) }, unit));
            grant.Publish(context.Client);
        }

        private static void Apply(MissionTestContext context, params CharacterIntent[] intents)
        {
            MissionItemPlanner plan = null;
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    plan = new MissionItemPlanner(context.Client, unit, context.Manager);
                    foreach (var intent in intents)
                    {
                        var missionId = intent switch
                        {
                            IssueMissionItemIntent issue => issue.MissionId,
                            ConsumeMissionItemIntent consume => consume.MissionId,
                            RemoveMissionItemsIntent remove => remove.MissionId,
                            _ => throw new AssertFailedException("Unexpected test intent.")
                        };
                        var assignment = unit.CharacterMissions.GetByCharacterAndMission(1, missionId);
                        plan.Apply(intent, assignment.AssignmentId, assignment.Generation);
                    }
                });
            plan.Publish(context.Client);
            plan.Publish(context.Client);
        }

        private static MissionTestContext Create(params uint[] missionIds) => Create(false, missionIds);

        private static MissionTestContext Create(bool acceptanceItems, params uint[] missionIds)
        {
            if (missionIds.Length == 0)
                missionIds = new[] { MissionId };
            var definitions = missionIds.ToDictionary(id => id, id => new Mission(id, "Survey fixture", 901, 77, 88, 1, 1, 1, false, false,
                Array.Empty<MissionObjectiveDefinition>(), true,
                items: new[] { new MissionItemBinding(ItemKey, TemplateId, MissionItemScope.AssignmentIssued, 3,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding("owned-cost", TemplateId, MissionItemScope.CharacterOwned, 3,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain) },
                acceptanceItems: acceptanceItems
                    ? new[] { new IssueMissionItemIntent("accept-key", id, ItemKey, TemplateId, 1) } : null));
            var context = MissionTestContext.WithCustomDefinitions(definitions, definitions.ToDictionary(entry => entry.Key,
                _ => new MissionRewardDefinition(0, null, new[] { new MissionRewardItem(TemplateId, 2) }, null)));
            context.AddRewardTemplate(TemplateId, 3147);
            var giver = context.AddNpc(77);
            foreach (var id in missionIds)
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, id));
            context.Drain();
            return context;
        }

        private static Item[] Owned(MissionTestContext context) => context.Client.Player.Inventory.PersonalInventory
            .Where(id => id != 0).Select(EntityManager.Instance.GetItem).ToArray();
    }
}
