using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Missions.Persistence;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.LootDispenser.Server;
using Rasa.Packets.Party.Server;
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
            corpse.TargetCategory = TargetCategory.Hostile;
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
        [DataRow(PartyLootMethod.FreeForAll)]
        [DataRow(PartyLootMethod.Rotation)]
        [DataRow(PartyLootMethod.DiceRoll)]
        public void SquadAllocationKeepsQualifiedLootWithTheKillerAndOutOfRolls(PartyLootMethod method)
        {
            using var fixture = new Fixture(questTemplateId: 30);
            var context = fixture.Context;
            var member = context.CreateAdditionalClient(2);
            member.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            using var party = new PartyScope(method, context.Client, member);
            using var items = new ItemManagerScope(context);
            party.Party.LootRotation = 1;
            ItemManager.Instance.GetItemTemplateById(28).QualityId = (int)LootQuality.Normal;
            ItemManager.Instance.GetItemTemplateById(30).QualityId = (int)LootQuality.Epic;

            var loot = fixture.Spawn(squadLoot: true);
            var quest = loot.LootItems.Single(item => item.ItemTemplateId == 30);
            var expectedOwner = method == PartyLootMethod.Rotation ? member : context.Client;
            Assert.AreEqual(expectedOwner.Player.EntityId, loot.Owner);
            Assert.AreEqual(context.Client.Player.EntityId, quest.ActorId);
            Assert.AreEqual(context.Client.Player.EntityId, quest.ReservedFor);
            Assert.AreEqual(0U, quest.PartyId);
            Assert.IsTrue(loot.Looters.Contains(context.Client.Player.EntityId),
                "Rotation must still show the killer their personal quest drop.");
            CollectionAssert.AreEquivalent(new[] { context.Client.Player.EntityId, member.Player.EntityId },
                loot.CreditSharers.ToArray());
            Assert.IsTrue(LootDispenserManager.LootableBy(loot, context.Client.Player.EntityId).Contains(quest));
            Assert.IsFalse(LootDispenserManager.LootableBy(loot, member.Player.EntityId).Contains(quest));
            foreach (var ordinary in loot.LootItems.Where(item => item != quest))
                if (method == PartyLootMethod.DiceRoll)
                {
                    Assert.AreNotEqual(0UL, ordinary.ReservedFor);
                    Assert.AreEqual(ordinary.ReservedFor, ordinary.ActorId);
                    Assert.AreEqual(0U, ordinary.PartyId);
                }
                else
                {
                    Assert.AreEqual(expectedOwner.Player.EntityId, ordinary.ActorId);
                    Assert.AreEqual(method == PartyLootMethod.FreeForAll ? party.Party.Id : 0U, ordinary.PartyId);
                    Assert.AreEqual(0UL, ordinary.ReservedFor);
                }
            var packets = context.Drain();
            Assert.IsTrue(packets.OfType<CanLootItemsPacket>().Any());
            Assert.IsFalse(packets.OfType<PartyMemberRollPacket>().Any(packet => packet.ItemClassId == quest.ItemClassId));
            Assert.IsFalse(MissionTestContext.Drain(member).OfType<PartyMemberRollPacket>()
                .Any(packet => packet.ItemClassId == quest.ItemClassId));
            Assert.AreEqual(0U, fixture.Counter);
            Assert.AreEqual(0U, fixture.HeldQuantity);

            LootRolls.Distribute(loot, party.Party, new List<Client> { context.Client, member }, () => 100);
            Assert.AreEqual(context.Client.Player.EntityId, quest.ReservedFor,
                "A qualified item must also remain personal if squad distribution is repeated.");
            Assert.IsFalse(context.Drain().OfType<PartyMemberRollPacket>()
                .Any(packet => packet.ItemClassId == quest.ItemClassId));

            fixture.Manager.RequestLootItemFromCorpse(member,
                new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = quest.EntityId, DestSlot = 50 });
            Assert.IsFalse(quest.Taken);
            Assert.AreEqual(0U, fixture.Counter);
            fixture.Manager.RequestLootItemFromCorpse(context.Client,
                new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = quest.EntityId, DestSlot = 50 });
            Assert.IsTrue(quest.Taken);
            Assert.AreEqual(1U, fixture.HeldQuantity);
            Assert.AreEqual(1U, fixture.Counter);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void OrdinaryItemsAllocatedToAnotherMemberDoNotFillTheKillersQualifiedDeficit(bool rolled)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var member = context.CreateAdditionalClient(2);
            using var party = new PartyScope(rolled ? PartyLootMethod.FreeForAll : PartyLootMethod.Rotation,
                context.Client, member);
            party.Party.LootThreshold = PartyLootThreshold.Junk;
            ItemManager.Instance.GetItemTemplateById(28).QualityId = (int)LootQuality.Normal;
            var looters = rolled ? new List<Client> { context.Client, member } : new List<Client> { member };
            var loot = fixture.Manager.Create(context.Client, fixture.CreateCorpse(), looters,
                rolled ? party.Party.Id : 0, new ActorGameplayPolicy
                {
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(28, 100, 2, 2) })
                });
            if (rolled)
            {
                var dice = new Queue<int>(new[] { 1, 100 });
                LootRolls.Distribute(loot, party.Party, new List<Client> { context.Client, member }, dice.Dequeue);
            }
            var ordinary = loot.LootItems.Single();
            Assert.IsFalse(ordinary.MayTake(context.Client.Player.EntityId));
            Assert.IsTrue(ordinary.MayTake(member.Player.EntityId));

            using var unit = context.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                var drops = MissionLootPlanner.Plan(context.Client, loot, context.Manager, unit, (minimum, maximum) => minimum);
                Assert.AreEqual(1, drops.Count);
                Assert.AreEqual(1U, drops[0].Quantity);
                Assert.AreEqual(context.Client.Player.Id, drops[0].CharacterId);
            });
            Assert.AreEqual(0U, fixture.Counter);
            Assert.AreEqual(0U, fixture.HeldQuantity);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SharedOrdinaryPickupLeavesQualifiedLootPersonalAndSplitsCreditsOnce(bool failFirstClaim)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var member = context.CreateAdditionalClient(2);
            member.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            using var party = new PartyScope(PartyLootMethod.FreeForAll, context.Client, member);
            var loot = fixture.Spawn();
            var ordinary = loot.LootItems.Single(item => item.ItemTemplateId == 29);
            var quest = loot.LootItems.Single(item => item.ItemTemplateId == 28);
            ordinary.PartyId = party.Party.Id;
            loot.Looters.Add(member.Player.EntityId);
            loot.CreditSharers.AddRange(new[] { context.Client.Player.EntityId, member.Player.EntityId });
            loot.Credits = 7;
            var request = new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId };
            if (failFirstClaim)
            {
                var injected = false;
                context.AfterSave = database =>
                {
                    if (database.CharacterInventoryEntries.Local.Any(row => row.ItemId == ordinary.Item.Id))
                    {
                        injected = true;
                        throw new DbUpdateException("Injected shared ordinary loot inventory failure.");
                    }
                };

                fixture.Manager.RequestLootAllFromCorpse(member, request);

                context.AfterSave = null;
                Assert.IsTrue(injected, "Shared loot must reach the inventory transaction before failing.");
                Assert.IsFalse(ordinary.Taken);
                Assert.IsFalse(quest.Taken);
                Assert.IsTrue(member.Player.Inventory.PersonalInventory.All(id => id == 0));
                using var verifyRollback = context.CreateChar();
                Assert.IsFalse(verifyRollback.CharacterInventories.GetItems(member.AccountEntry.Id)
                    .Any(row => row.ItemId == ordinary.Item.Id));
            }

            fixture.Manager.RequestLootAllFromCorpse(member, request);

            Assert.IsTrue(ordinary.Taken, "PR105 Free For All loot must be claimable by a squad member other than ActorId.");
            Assert.IsFalse(quest.Taken);
            Assert.IsFalse(loot.FullyLooted);
            Assert.AreEqual(0U, fixture.Counter);
            Assert.AreEqual(7, loot.Credits);
            using (var verify = context.CreateChar())
                Assert.AreEqual(member.Player.Id, verify.CharacterInventories.GetItems(member.AccountEntry.Id)
                    .Single(row => row.ItemId == ordinary.Item.Id).CharacterId);

            fixture.Take(loot);

            Assert.IsTrue(quest.Taken);
            Assert.IsTrue(loot.FullyLooted);
            Assert.AreEqual(1U, fixture.HeldQuantity);
            Assert.AreEqual(1U, fixture.Counter);
            Assert.AreEqual(104, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(103, member.Player.Credits[CurencyType.Credits]);
            using (var verify = context.CreateChar())
            {
                Assert.AreEqual(104, verify.Characters.Find(context.Client.Player.Id).Credit);
                Assert.AreEqual(103, verify.Characters.Find(member.Player.Id).Credit);
            }
            fixture.Take(loot);
            fixture.Manager.RequestLootAllFromCorpse(member, request);
            Assert.AreEqual(1U, fixture.Counter);
            Assert.AreEqual(104, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(103, member.Player.Credits[CurencyType.Credits]);
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
        [DataRow(false)]
        [DataRow(true)]
        public void MissionDropCreationRollsBackWhenTheRegisteredCorpseChangesBeforeCommit(bool retired)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var corpse = fixture.CreateCorpse();
            var injected = false;
            context.AfterSave = database =>
            {
                if (!database.ItemEntries.Local.Any(item => item.ItemTemplateId == 28))
                    return;
                injected = true;
                if (retired)
                    context.Map.LootDispensers[corpse.CorpseLootEntityId].IsLootable = false;
                else
                    corpse.CorpseLootEntityId = 0;
            };

            var loot = fixture.Manager.Create(context.Client, corpse, new ActorGameplayPolicy
            {
                Loot = new AuthoredLootProfile(new[] { new LootDrop(29, 100, 1, 1) })
            });

            context.AfterSave = null;
            Assert.IsTrue(injected);
            CollectionAssert.AreEqual(new uint[] { 29 }, loot.LootItems.Select(item => item.ItemTemplateId).ToArray());
            Assert.AreEqual(0U, fixture.Counter);
            Assert.AreEqual(0U, fixture.HeldQuantity);
            using var verify = context.Open();
            Assert.AreEqual(0, verify.ItemEntries.Count(item => item.ItemTemplateId == 28));
            Assert.AreEqual(1, verify.ItemEntries.Count(item => item.ItemTemplateId == 29));
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
            internal uint QuestTemplateId { get; }
            internal uint Counter => Context.Client.Player.Missions[901].Objectives[1].ItemCounters[3147];
            internal uint HeldQuantity => (uint)Context.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Where(item => item.ItemTemplate.ItemTemplateId == QuestTemplateId)
                .Sum(item => (long)item.StackSize);

            internal Fixture(int chance = 100, uint quantity = 1, bool accept = true, bool authorDrop = true,
                int percentile = 0, bool issueProtected = false, uint questTemplateId = 28)
            {
                QuestTemplateId = questTemplateId;
                var objective = new MissionObjectiveDefinition(1, 1001, 1002, new uint?[] { null, null, null }, 1,
                    MissionObjectiveState.Incomplete, true, null,
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                    {
                        [3147] = new MissionObjectiveItemCounterDefinition(3147, 0, 2)
                    }, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(), MissionProgressRule.IncrementItemCounterOnExactSubject(
                        MissionProgressEventKind.ItemAcquired, 3147, 0, 2));
                var binding = new MissionItemBinding("collected-item", questTemplateId, MissionItemScope.CharacterOwned, 2,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: 2,
                    Drop: authorDrop ? new MissionItemDropDefinition(new uint[] { 3 }, 1, 1220, chance, quantity) : null);
                var bindings = new List<MissionItemBinding> { binding };
                if (issueProtected)
                    bindings.Add(new MissionItemBinding("protected", questTemplateId, MissionItemScope.AssignmentIssued, 2,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove));
                var mission = new Mission(901, "Corpse collection", 901, 77, 88, 1, 1, 1, false, false,
                    new[] { objective }, true, items: bindings, acceptanceItems: issueProtected
                        ? new[] { new IssueMissionItemIntent("accept-protected", 901, "protected", questTemplateId, 2) } : null);
                Context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [901] = mission },
                    new Dictionary<uint, MissionRewardDefinition>
                    {
                        [901] = new MissionRewardDefinition(0, new Dictionary<CurencyType, int>
                            { [CurencyType.Credits] = 7 }, null, null)
                    });
                Context.AddRewardTemplate(questTemplateId, 3147);
                if (questTemplateId != 28)
                    Context.AddRewardTemplate(28, 3149);
                Context.AddRewardTemplate(29, 3148);
                Context.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                Manager = new LootDispenserManager(Context, missionManager: Context.Manager,
                    lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
                if (accept)
                    Assert.IsTrue(Context.Manager.AcceptOfferedMission(Context.Client, Context.AddNpc(77).EntityId, 901));
                Context.Drain();
            }

            internal Creature CreateCorpse(uint creatureId = 3)
            {
                var corpse = Context.AddNpc(creatureId);
                corpse.Npc = null;
                corpse.TargetCategory = TargetCategory.Hostile;
                corpse.State = CharacterState.Dead;
                corpse.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 0, 0, 0);
                CellManager.Instance.UpdateVisibility(Context.Client);
                return corpse;
            }

            internal LootDispenser Spawn(uint creatureId = 3, bool squadLoot = false)
            {
                var corpse = CreateCorpse(creatureId);
                Manager.Loot(Context.Client, corpse, new ActorGameplayPolicy
                {
                    Loot = squadLoot ? null : new AuthoredLootProfile(new[] { new LootDrop(29, 100, 1, 1) })
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
                foreach (var loot in Context.Map.LootDispensers.Values.ToArray())
                    Manager.RemoveForCreature(Context.Map, loot.Corpse);
                Context.Dispose();
            }
        }

        private sealed class PartyScope : IDisposable
        {
            private readonly Client[] _clients;
            internal Party Party { get; }

            internal PartyScope(PartyLootMethod method, params Client[] clients)
            {
                _clients = clients;
                var id = PartyManager.Instance.GetPartyId;
                Party = new Party(id, clients[0].AccountEntry.Id, clients.Select(client => new PartyMember(client)).ToList())
                {
                    LootMethod = method,
                    LootThreshold = PartyLootThreshold.Prototype
                };
                PartyManager.Instance.Parties.Add(id, Party);
                foreach (var client in clients)
                    client.Player.PartyId = id;
                lock (Server.Clients)
                    Server.Clients.AddRange(clients);
            }

            public void Dispose()
            {
                foreach (var client in _clients)
                    client.Player.PartyId = 0;
                lock (Server.Clients)
                    foreach (var client in _clients)
                        Server.Clients.Remove(client);
                PartyManager.Instance.Parties.Remove(Party.Id);
                PartyManager.Instance.FreePartyId(Party.Id);
            }
        }

        private sealed class ItemManagerScope : IDisposable
        {
            private readonly FieldInfo _field = typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly ItemManager _previous = ItemManager.Instance;

            internal ItemManagerScope(MissionTestContext context)
            {
                var items = (ItemManager)Activator.CreateInstance(typeof(ItemManager),
                    BindingFlags.Instance | BindingFlags.NonPublic, binder: null, args: new object[] { context }, culture: null)!;
                items.ItemTemplateItemClass = new Dictionary<uint, EntityClasses>(_previous.ItemTemplateItemClass);
                _field.SetValue(null, items);
            }

            public void Dispose() => _field.SetValue(null, _previous);
        }
    }
}
