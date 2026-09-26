using System.Linq;
using System.Numerics;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class BootcampCrateLootTests
    {
        private static readonly uint[] TemplateIds = { 13066, 13096, 13156, 13186, 13713, 28 };

        [TestMethod]
        public void RightClickOpensLootMenuWithoutGrantingItemsOrRemovingCrate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            harness.Drain();

            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);

            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<LootCorpsePacket>().Count(),
                "Right-click must open the existing loot menu.");
            var menuIndex = packets.ToList().FindIndex(packet => packet is LootCorpsePacket);
            var recoveryIndex = packets.ToList().FindIndex(packet =>
                packet is PerformRecoveryPacket recovery &&
                recovery.ActionId == ActionId.UseObject &&
                recovery.ActionArgId == DynamicObjectManager.FootlockerUseArgId);
            Assert.IsTrue(recoveryIndex >= 0 && recoveryIndex < menuIndex,
                "Complete the client's Use action before opening the loot menu.");
            var loot = harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId];
            foreach (var item in loot.LootItems)
            {
                var itemIndex = packets.ToList().FindIndex(packet =>
                    packet is CreatePhysicalEntityPacket created && created.EntityId == item.EntityId);
                Assert.IsTrue(itemIndex >= 0 && itemIndex < menuIndex,
                    "Every loot row must resolve to an introduced item before the window opens.");
            }
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum(),
                "Opening the crate must not transfer its contents.");
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(crate),
                "Opening the crate must leave the prop in the world.");
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1992].Objectives[1].State);
        }

        [TestMethod]
        public void TakingOneItemLeavesTheOtherItemsInTheCrate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            var selected = loot.LootItems.Single(item => item.ItemTemplateId == 13066);

            LootDispenserManager.Instance.RequestLootItemFromCorpse(
                harness.Client,
                new RequestLootItemFromCorpsePacket
                {
                    EntityId = loot.EntityId,
                    ItemId = selected.EntityId,
                    DestSlot = 0
                });

            Assert.AreEqual(1, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum(),
                "Selecting one row must not grant the entire reward package.");
            Assert.AreEqual(5, loot.Remaining().Count);
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(crate));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1992].Objectives[1].State);
        }

        [TestMethod]
        public void LootAllCollectsTheContentsAndKeepsTheEmptyCrate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);

            LootDispenserManager.Instance.RequestLootAllFromCorpse(
                harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.AreEqual(6, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(crate),
                "Collecting the contents must not despawn the crate.");
            Assert.IsTrue(crate.IsInWorld);
            Assert.IsTrue(loot.FullyLooted);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[1].State);
        }

        [TestMethod]
        [DataRow(true, 50U)]
        [DataRow(false, 50U)]
        [DataRow(true, 51U)]
        public void UnownedInventoryIsIgnoredWithoutBlockingCrateLoot(bool orphanFirst, uint ownedSlot)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            uint orphanItemId = 0;
            var owners = orphanFirst
                ? new[] { 0U, harness.Client.Player.Id }
                : new[] { harness.Client.Player.Id, 0U };
            using (var database = harness.Context.Open())
            {
                foreach (var owner in owners)
                {
                    var item = new ItemEntry
                    {
                        ItemTemplateId = 28,
                        StackSize = 1000,
                        CrafterName = ""
                    };
                    database.ItemEntries.Add(item);
                    database.SaveChanges();
                    database.CharacterInventoryEntries.Add(new CharacterInventoryEntry(
                        harness.Client.AccountEntry.Id, owner,
                        (uint)InventoryType.Personal, owner == 0 ? 50U : ownedSlot, item.ItemId));
                    database.SaveChanges();
                    if (owner == 0)
                        orphanItemId = item.ItemId;
                }
            }

            new InventoryManager(harness.Context, harness.Manager).InitCharacterInventory(harness.Client);
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            var claimedAmmo = loot.LootItems.Single(item => item.ItemTemplateId == 28).ItemQuantity;

            LootDispenserManager.Instance.RequestLootAllFromCorpse(
                harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.IsTrue(loot.FullyLooted,
                "Loading an orphaned item must not create duplicate personal slots that block the crate claim.");
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[1992].Objectives[1].State);
            using var verify = harness.Context.Open();
            Assert.AreEqual(0U,
                verify.CharacterInventoryEntries.Single(
                    row => row.ItemId == orphanItemId).CharacterId,
                "Loading inventory must never assign an unknown owner's item to this character.");
            Assert.AreEqual(1000U, verify.ItemEntries.Single(
                item => item.ItemId == orphanItemId).StackSize);
            Assert.AreEqual(2000L + claimedAmmo,
                verify.ItemEntries.Where(item => item.ItemTemplateId == 28)
                    .AsEnumerable().Sum(item => (long)item.StackSize),
                "Neither loading nor looting may discard existing ammunition.");
        }

        [TestMethod]
        public void ProximityAutoLootCannotCollectTheCrate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId];

            LootDispenserManager.Instance.RequestLootAllFromCorpse(
                harness.Client,
                new RequestLootAllFromCorpsePacket
                {
                    EntityId = loot.EntityId,
                    AutoLootOnly = true
                });

            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum(),
                "Walking near the crate must not collect its contents.");
            Assert.AreEqual(6, loot.Remaining().Count);
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(crate));
        }

        [TestMethod]
        public void ClosingAndReopeningKeepsAllUnclaimedItems()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);

            LootDispenserManager.Instance.CancelCorpseLooting(harness.Client,
                new CancelCorpseLootingPacket { EntityId = loot.EntityId });
            Assert.AreEqual(0UL, loot.CurrentLooter);
            harness.Drain();
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);

            Assert.AreEqual(1, harness.Drain().OfType<LootCorpsePacket>().Count());
            Assert.AreEqual(6, loot.Remaining().Count);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        public void PartialCollectionSurvivesFreshReconnectWithoutRefillingTheCrate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                new RequestLootItemFromCorpsePacket
                {
                    EntityId = loot.EntityId,
                    ItemId = loot.LootItems.Single(item => item.ItemTemplateId == 13066).EntityId,
                    DestSlot = 0
                });

            harness.ReconnectFresh();
            harness.Manager.PublishInitialState(harness.Client);
            var rebuilt = BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(rebuilt);
            harness.MovePlayerTo(rebuilt);
            var remaining = OpenLoot(harness, rebuilt);

            Assert.AreEqual(5, remaining.Remaining().Count);
            Assert.IsFalse(remaining.Remaining().Any(item => item.ItemTemplateId == 13066));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = remaining.EntityId });
            var owned = harness.ReadOwnedTemplateCounts(TemplateIds);
            Assert.AreEqual(6, owned.Count);
            Assert.IsTrue(owned.Values.All(count => count == 1));
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(rebuilt));
        }

        [TestMethod]
        public void EmptyCrateSurvivesFreshReconnectAndCannotGrantAgain()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            harness.ReconnectFresh();
            harness.Manager.PublishInitialState(harness.Client);
            var rebuilt = BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(rebuilt, "An emptied crate must be rebuilt as an empty prop.");
            harness.MovePlayerTo(rebuilt);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.AreEqual((EntityClasses)29877, rebuilt.EntityClassId);
            Assert.AreEqual(UseObjectState.TdStateOpened, rebuilt.StateId);
            Assert.IsFalse(rebuilt.IsEnabled);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            harness.UseObjectAndRecover(rebuilt, actionArgId: DynamicObjectManager.FootlockerUseArgId);

            Assert.AreEqual(6, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(7)]
        public void FailedCollectionKeepsTheLootAvailableAndRetryGrantsExactlyOnce(int failingSave)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            harness.Drain();
            var saves = 0;
            harness.Context.AfterSave = _ =>
            {
                if (++saves == failingSave)
                    throw new DbUpdateException("Injected crate claim failure.");
            };

            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.AreEqual(failingSave, saves);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
            Assert.AreEqual(6, loot.Remaining().Count);
            Assert.IsTrue(loot.IsLootable);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[1992].Objectives[1].State);
            Assert.AreEqual(0, harness.Drain().OfType<ActorGotLootPacket>().Count());
            harness.Context.AfterSave = null;

            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.AreEqual(6, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
            Assert.IsTrue(loot.FullyLooted);
            Assert.IsTrue(harness.BootcampMap.DynamicObjects.Contains(crate));
        }

        [TestMethod]
        public void CrateUsesObjectRangeRatherThanTheShorterCorpseRange()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            harness.MovePlayerTo(crate.Position + new Vector3(3, 0, 0));
            harness.Drain();

            DynamicObjectManager.Instance.RequestUseObjectPacket(harness.Client,
                new RequestUseObjectPacket
                {
                    EntityId = crate.EntityId,
                    ActionId = ActionId.UseObject,
                    ActionArgId = DynamicObjectManager.FootlockerUseArgId
                });
            harness.AdvanceRecovery(crate.WindupTime);

            Assert.AreEqual(1, harness.Drain().OfType<LootCorpsePacket>().Count());
            Assert.AreEqual(0, harness.BootcampMap.PerformRecovery.Count);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        public void CrateIsIntroducedBeforeItsDispenserAndReattachesWhenItBecomesVisibleAgain()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness, introduce: false);
            Assert.AreEqual(0UL, crate.LootDispenserEntityId);
            harness.Drain();

            CellManager.Instance.UpdateVisibility(harness.Client);

            var packets = harness.Drain().ToList();
            var crateIndex = packets.FindIndex(packet =>
                packet is CreatePhysicalEntityPacket created && created.EntityId == crate.EntityId);
            var dispenserIndex = packets.FindIndex(packet =>
                packet is CreatePhysicalEntityPacket created && created.EntityId == crate.LootDispenserEntityId);
            var attachmentIndex = packets.FindIndex(packet =>
                packet is AttachInfoPacket attached && attached.AttachedToEnityId == crate.EntityId);
            Assert.IsTrue(crateIndex >= 0 && dispenserIndex > crateIndex && attachmentIndex > dispenserIndex);
            var dispenserId = crate.LootDispenserEntityId;

            harness.MovePlayerTo(Vector3.Zero);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();
            harness.MovePlayerTo(crate);
            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.AreEqual(dispenserId, crate.LootDispenserEntityId);
            Assert.AreEqual(1, harness.BootcampMap.LootDispensers.Count);
            Assert.AreEqual(1, harness.Drain().OfType<AttachInfoPacket>()
                .Count(packet => packet.AttachedToEnityId == crate.EntityId));
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        public void CompletedMissionKeepsItsEmptyCrateAfterReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            using (var context = harness.Context.Open())
            {
                context.CharacterMissionEntries.Single(entry =>
                    entry.CharacterId == harness.Client.Player.Id && entry.MissionId == 1992)
                    .MissionState = (uint)MissionState.Completed;
                context.SaveChanges();
            }

            harness.ReconnectFresh();
            var rebuilt = BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(rebuilt);
            harness.MovePlayerTo(rebuilt);
            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.IsFalse(rebuilt.IsEnabled);
            Assert.AreEqual(UseObjectState.TdStateOpened, rebuilt.StateId);
            Assert.AreEqual(6, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        public void CompletedObjectiveWithoutLootReceiptsDoesNotRefillLegacyCrate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            using (var unit = harness.Context.CreateChar())
                unit.CharacterMissionScenario.RemoveByPrefix(
                    harness.Client.Player.Id, 1992, "reward-loot:58:");

            harness.ReconnectFresh();
            var rebuilt = BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(rebuilt);
            harness.MovePlayerTo(rebuilt);
            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.IsFalse(rebuilt.IsEnabled);
            Assert.AreEqual(0, harness.BootcampMap.LootDispensers[rebuilt.LootDispenserEntityId].Remaining().Count);
            Assert.AreEqual(6, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        public void FailedInitialAttachmentRollsBackItsItemsAndCanRetryOnUse()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness, introduce: false);
            var itemCount = harness.Context.ReadRewardTotals().ItemCount;
            var saves = 0;
            harness.Context.AfterSave = _ =>
            {
                if (++saves == 2)
                    throw new DbUpdateException("Injected crate attachment failure.");
            };

            CellManager.Instance.UpdateVisibility(harness.Client);

            Assert.AreEqual(0UL, crate.LootDispenserEntityId);
            Assert.AreEqual(itemCount, harness.Context.ReadRewardTotals().ItemCount);
            Assert.AreEqual(0, harness.BootcampMap.LootDispensers.Count);
            harness.Context.AfterSave = null;
            harness.UseObjectAndRecover(crate, actionArgId: DynamicObjectManager.FootlockerUseArgId);

            Assert.AreEqual(6, harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId].Remaining().Count);
            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
        }

        [TestMethod]
        public void RemovingCrateReclaimsOnlyItsUnclaimedItems()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            var selected = loot.LootItems.Single(item => item.ItemTemplateId == 13066);
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                new RequestLootItemFromCorpsePacket
                {
                    EntityId = loot.EntityId,
                    ItemId = selected.EntityId,
                    DestSlot = 0
                });
            var remaining = loot.Remaining().Select(item => item.Item).ToArray();

            CellManager.Instance.RemoveFromWorld(harness.BootcampMap, crate);
            harness.BootcampMap.DynamicObjects.Remove(crate);

            Assert.AreEqual(0UL, crate.LootDispenserEntityId);
            Assert.IsFalse(harness.BootcampMap.LootDispensers.ContainsKey(loot.EntityId));
            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(selected.Item.Id));
            Assert.IsNotNull(EntityManager.Instance.GetItem(selected.EntityId));
            foreach (var item in remaining)
            {
                Assert.IsNull(unit.Items.GetItem(item.Id));
                Assert.IsNull(EntityManager.Instance.GetItem(item.EntityId));
            }
        }

        [TestMethod]
        [DataRow(20.001f)]
        [DataRow(float.NaN)]
        public void ClaimRechecksFiniteObjectRange(float distance)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var crate = ReachCrate(harness);
            var loot = OpenLoot(harness, crate);
            harness.MovePlayerTo(crate.Position + new Vector3(distance, 0, 0));
            harness.Drain();

            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.AreEqual(0, harness.ReadOwnedTemplateCounts(TemplateIds).Values.Sum());
            Assert.AreEqual(6, loot.Remaining().Count);
            Assert.AreEqual(0, harness.Drain().Count);
        }

        private static DynamicObject ReachCrate(
            BootcampRuntimeTestHarness.Harness harness,
            bool introduce = true)
        {
            var mcAllister = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            var delessio = harness.AddNpc(
                BootcampRuntimeTestHarness.CaptainDelessioCreatureId,
                BootcampRuntimeTestHarness.CaptainDelessioPackageId);
            harness.SeedMission(harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionInitiation, (uint)MissionState.Completed, false);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client,
                mcAllister.EntityId, BootcampRuntimeTestHarness.MissionGearingUp));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client,
                delessio.EntityId, BootcampRuntimeTestHarness.MissionGearingUp, 4, 1));
            var crate = BootcampRuntimeTestHarness.FindScenarioObject(
                harness.BootcampMap, "bootcamp-equipment-crate");
            Assert.IsNotNull(crate);
            harness.MovePlayerTo(crate);
            if (introduce)
                CellManager.Instance.UpdateVisibility(harness.Client);
            return crate;
        }

        private static LootDispenser OpenLoot(
            BootcampRuntimeTestHarness.Harness harness,
            DynamicObject crate)
        {
            CellManager.Instance.UpdateVisibility(harness.Client);
            var loot = harness.BootcampMap.LootDispensers[crate.LootDispenserEntityId];
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            Assert.AreEqual(harness.Client.Player.EntityId, loot.CurrentLooter);
            return loot;
        }
    }
}
