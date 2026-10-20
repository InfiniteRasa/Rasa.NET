using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Loot;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Game.Missions.World;
using Rasa.Packets;
using Rasa.Packets.ClientMethod.Server;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Game.Server;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.LootDispenser.Server;
using Rasa.Packets.Protocol;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Test.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessOrdinaryLootTests
    {
        private static readonly IReadOnlyDictionary<uint, uint> NativeDropClasses = new Dictionary<uint, uint>
        {
            [28] = 3147, [2285] = 10346, [11561] = 6470, [17130] = 27121, [17133] = 27122,
            [41666] = 20307, [42342] = 20923, [44917] = 21969, [48545] = 27624
        };

        [TestMethod]
        [DataRow(57U, 580023U)]
        [DataRow(88U, 580057U)]
        public void MissionlessNativeFamiliesPublishDifferentDropsAndCommitManualPickup(uint creatureId, uint poolId)
        {
            var thrax = creatureId == 57;
            var rolls = FamilyRolls(thrax);
            using var harness = CreatePinnedHarness(rolls.Next);
            Assert.AreEqual(0, harness.Client.Player.Missions.Count);
            var expected = thrax
                ? new[] { (41666U, 1U), (28U, 24U), (44917U, 1U), (17130U, 1U) }
                : new[] { (42342U, 1U), (44917U, 1U), (11561U, 1U) };
            Grant(harness, thrax ? 28U : 44917U, thrax ? 10U : 2U);
            var existingStack = harness.Client.Player.Inventory.PersonalInventory.Single(id => id != 0);
            DrainCalls(harness);

            var corpse = Spawn(harness, poolId, creatureId);
            var loot = Kill(harness, corpse);

            rolls.AssertExhausted();
            AssertDrops(harness, loot, expected);
            var publication = DrainCalls(harness);
            AssertQuality(publication, loot, thrax ? LootQuality.Uncommon : LootQuality.Normal);
            AssertLists(publication, harness, loot, expected, menu: false);
            Assert.IsFalse(publication.Any(call => call.Packet is GotLootPacket));
            Assert.AreEqual(0, harness.Client.Player.Missions.Count);
            AssertUnclaimed(harness, loot, expected.Length, 1);

            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            var menu = DrainCalls(harness);
            AssertLists(menu, harness, loot, expected, menu: true);
            Assert.AreEqual(harness.Client.Player.EntityId, loot.CurrentLooter);
            AssertUnclaimed(harness, loot, expected.Length, 1);
            var frozenMenu = menu.Select(call => Encode(call.Packet)).ToArray();
            var beforeCredits = harness.Client.Player.Credits[CurencyType.Credits];

            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.IsTrue(loot.FullyLooted);
            Assert.IsTrue(loot.LootItems.All(item => item.Taken));
            Assert.AreEqual(beforeCredits + 7, harness.Client.Player.Credits[CurencyType.Credits]);
            var acquired = DrainCalls(harness).Select(call => call.Packet).OfType<GotLootPacket>().Single();
            Assert.AreEqual(corpse.EntityId, acquired.CreatureEntityId);
            Assert.AreEqual(7, acquired.Credits);
            CollectionAssert.AreEqual(expected.Select(drop => drop.Item2).ToArray(),
                acquired.Entries.Select(entry => entry.Quantity).ToArray());
            CollectionAssert.AreEqual(thrax ? new uint[] { 20307, 3147, 21969, 27121 } :
                new uint[] { 20923, 21969, 6470 }, acquired.Entries.Select(entry => entry.ClassId).ToArray());
            foreach (var (template, quantity) in expected)
                AssertHeld(harness, template, quantity + (template == (thrax ? 28U : 44917U) ? (thrax ? 10U : 2U) : 0U));
            Assert.IsTrue(harness.Client.Player.Inventory.PersonalInventory.Contains(existingStack),
                "The preexisting persisted stack must retain its entity identity after merging.");
            AssertPersistedCredits(harness, beforeCredits + 7);
            using (var verify = harness.Context.Open())
            {
                Assert.AreEqual(expected.Length, verify.CharacterInventoryEntries.Count());
                Assert.AreEqual(expected.Length, verify.ItemEntries.Count(),
                    "A fully merged unowned source stack must not leave a persisted duplicate.");
            }
            for (var index = 0; index < menu.Count; index++)
                CollectionAssert.AreEqual(frozenMenu[index], Encode(menu[index].Packet),
                    "Queued corpse menu packets must retain their pre-claim payload.");
            AssertDuplicateRefused(harness, loot);
        }

        [TestMethod]
        public void NativeSingleStackPickupRefusesDuplicatesWhileOtherCorpseRowsRemain()
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            Grant(harness, 28, 10);
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            DrainCalls(harness);
            var ammo = loot.LootItems.Single(item => item.ItemTemplateId == 28);
            var request = new RequestLootItemFromCorpsePacket
            { EntityId = loot.EntityId, ItemId = ammo.EntityId, DestSlot = 50 };
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client, request);
            Assert.IsTrue(ammo.Taken);
            Assert.IsFalse(loot.FullyLooted);
            Assert.AreEqual(3, loot.Remaining().Count);
            Assert.AreEqual(7, loot.Credits);
            AssertHeld(harness, 28, 34);
            AssertPersistedCredits(harness, 100);
            Assert.IsNull(EntityManager.Instance.GetItem(ammo.EntityId));
            using (var verify = harness.Context.Open())
                Assert.IsFalse(verify.ItemEntries.Any(row => row.ItemId == ammo.Item.Id));
            var got = DrainCalls(harness).Select(call => call.Packet).OfType<GotLootPacket>().Single();
            Assert.AreEqual(0, got.Credits);
            Assert.AreEqual((3147U, 24U, ammo.EntityId), got.Entries.Single());
            var saves = harness.Context.SaveAttempts;
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client, request);
            Assert.AreEqual(saves, harness.Context.SaveAttempts);
            AssertHeld(harness, 28, 34);
            AssertPersistedCredits(harness, 100);
            var refused = DrainCalls(harness);
            Assert.IsTrue(refused.Any(call => call.Packet is TakenInfoPacket));
            Assert.IsFalse(refused.Any(call => call.Packet is GotLootPacket));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(loot.FullyLooted);
            AssertHeld(harness, 28, 34);
            AssertPersistedCredits(harness, 107);
            rolls.AssertExhausted();
        }

        [TestMethod]
        [DataRow("missing-row")]
        [DataRow("wrong-quantity")]
        [DataRow("wrong-menu-actor")]
        public void NativePacketOracleRejectsTruncatedOrMisaddressedOrWrongQuantityPayloads(string mutation)
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            var calls = DrainCalls(harness);
            var expected = new[] { (41666U, 1U), (28U, 24U), (44917U, 1U), (17130U, 1U) };
            AssertLists(calls, harness, loot, expected, menu: true);
            var index = calls.FindIndex(call => mutation == "wrong-menu-actor"
                ? call.Packet is LootCorpsePacket : call.Packet is LootInfoPacket);
            var copy = loot.LootItems.Select(LootItem.Capture).ToList();
            if (mutation == "missing-row")
                copy.RemoveAt(0);
            else if (mutation == "wrong-quantity")
                copy.Single(item => item.ItemTemplateId == 28).ItemQuantity = 3;
            PythonPacket mutated = mutation == "wrong-menu-actor"
                ? new LootCorpsePacket(loot.Corpse.EntityId, copy) : new LootInfoPacket(copy);
            calls[index] = new CallMethodMessage(loot.EntityId, mutated);
            Assert.ThrowsExactly<AssertFailedException>(() => AssertLists(calls, harness, loot, expected, menu: true),
                "The independent wire oracle must reject a plausible regression, not only accept current packets.");
            rolls.AssertExhausted();
        }

        [TestMethod]
        [DataRow(57U, 580023U, 799, true)]
        [DataRow(57U, 580023U, 800, false)]
        [DataRow(82U, 580014U, 6499, true)]
        [DataRow(82U, 580014U, 6500, false)]
        public void RealRegularAndNamedBossEquipmentUseExactAdmissionBoundaries(
            uint creatureId, uint poolId, int admission, bool admitted)
        {
            var boss = creatureId == 82;
            var rolls = GearOnlyRolls(boss, admission, boss ? 130 : 95);
            using var harness = CreatePinnedHarness(rolls.Next);
            Assert.AreEqual(boss ? "wilderness_v1_thrax_5_9_boss" : "wilderness_v1_thrax_5_9",
                OrdinaryLootCatalog.Current.Resolve(1220, creatureId).ProfileKey);
            Assert.AreEqual(0, harness.Client.Player.Missions.Count);
            var corpse = Spawn(harness, poolId, creatureId);
            Assert.AreEqual(boss ? 10504U : 9354U, (uint)corpse.EntityClass);
            var loot = Kill(harness, corpse);
            rolls.AssertExhausted();
            var expected = admitted ? new[] { (48545U, 1U) } : Array.Empty<(uint, uint)>();
            AssertDrops(harness, loot, expected);
            var calls = DrainCalls(harness);
            AssertQuality(calls, loot, admitted ? LootQuality.Rare : LootQuality.Junk);
            AssertLists(calls, harness, loot, expected, menu: false);
            AssertUnclaimed(harness, loot, expected.Length, 0);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot, expected, menu: true);
            if (admitted)
            {
                var drop = loot.LootItems.Single();
                LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                    new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = drop.EntityId, DestSlot = 0 });
                Assert.AreEqual(drop.EntityId, harness.Client.Player.Inventory.PersonalInventory[0]);
                using var verify = harness.Context.Open();
                var inventory = verify.CharacterInventoryEntries.AsNoTracking().Single();
                Assert.AreEqual(drop.Item.Id, inventory.ItemId);
                Assert.AreEqual(0U, inventory.SlotId);
                Assert.AreEqual((uint)InventoryType.Personal, inventory.InventoryType);
                AssertHeld(harness, 48545, 1);
            }
            else
            {
                Assert.IsFalse(loot.LootItems.Any(item => item.ItemTemplateId == 28),
                    "Failed ordinary admissions must not fall back to the historical three cartridges.");
                LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                    new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                using var verify = harness.Context.Open();
                Assert.AreEqual(0, verify.ItemEntries.Count());
                Assert.AreEqual(0, verify.CharacterInventoryEntries.Count());
            }
            Assert.IsTrue(loot.FullyLooted);
            AssertPersistedCredits(harness, 107);
            Assert.AreEqual(107, harness.Client.Player.Credits[CurencyType.Credits]);
            var got = DrainCalls(harness).Select(call => call.Packet).OfType<GotLootPacket>().Single();
            Assert.AreEqual(7, got.Credits);
            Assert.AreEqual(admitted ? 1 : 0, got.Entries.Count);
            if (admitted)
                AssertDuplicateRefused(harness, loot);
        }

        [TestMethod]
        public void FrozenRegularAndBossRarityIntervalsHaveExactIndependentWeights()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var boss in new[] { false, true })
            {
                var profile = OrdinaryLootCatalog.Current.Resolve(1220, boss ? 82U : 57U);
                var expected = boss
                    ? new[] { (17130U, 65, LootQuality.Uncommon), (13736U, 65, LootQuality.Uncommon), (48545U, 70, LootQuality.Rare) }
                    : new[] { (3057U, 35, LootQuality.Normal), (11561U, 35, LootQuality.Normal),
                        (17130U, 25, LootQuality.Uncommon), (48545U, 5, LootQuality.Rare) };
                var group = profile.Groups.Single(candidate => candidate.Key == "equipment");
                CollectionAssert.AreEqual(expected.Select(entry => (entry.Item1, entry.Item2)).ToArray(),
                    group.Candidates.Select(candidate => (candidate.TemplateId, candidate.Weight)).ToArray());
                var counts = new Dictionary<LootQuality, int>();
                var start = 0;
                foreach (var (template, weight, quality) in expected)
                {
                    for (var draw = start; draw < start + weight; draw++)
                    {
                        var rolls = GearOnlyRolls(boss, 0, draw, credits: false);
                        CollectionAssert.AreEqual(new[] { (template, 1U) }, profile.Roll(rolls.Next).ToArray(),
                            $"{profile.ProfileKey}: exact weighted interval at draw {draw}.");
                        rolls.AssertExhausted();
                        Assert.AreEqual((int)quality, ItemManager.Instance.GetItemTemplateById(template).QualityId);
                        counts[quality] = counts.GetValueOrDefault(quality) + 1;
                    }
                    start += weight;
                }
                Assert.AreEqual(boss ? 200 : 100, start);
                Assert.AreEqual(boss ? 0 : 70, counts.GetValueOrDefault(LootQuality.Normal));
                Assert.AreEqual(boss ? 130 : 25, counts.GetValueOrDefault(LootQuality.Uncommon));
                Assert.AreEqual(boss ? 70 : 5, counts.GetValueOrDefault(LootQuality.Rare));
                Assert.AreEqual(0, counts.GetValueOrDefault(LootQuality.Legendary));
                Assert.AreEqual(0, counts.GetValueOrDefault(LootQuality.Epic));
            }
        }

        [TestMethod]
        public void RealCreationAndPickupPublishOnlyAfterTheirWholeTransactionsCommit()
        {
            var rolls = FamilyRolls(thrax: true);
            WildernessRuntimeTestHarness current = null;
            var creations = 0;
            var claims = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: rolls.Next,
                beforeCorpsePublication: loot =>
                {
                    creations++;
                    Assert.AreEqual(0UL, loot.Corpse.CorpseLootEntityId);
                    Assert.AreEqual(0, current.Map.LootDispensers.Count);
                    using var verify = current.Context.Open();
                    Assert.AreEqual(4, verify.ItemEntries.Count());
                    Assert.AreEqual(0, verify.CharacterInventoryEntries.Count());
                    foreach (var drop in loot.LootItems)
                    {
                        Assert.AreSame(drop.Item, EntityManager.Instance.GetItem(drop.EntityId));
                        Assert.IsTrue(verify.ItemEntries.Any(row => row.ItemId == drop.Item.Id));
                    }
                    Assert.IsFalse(DrainCalls(current).Any(call =>
                        call.Packet is LootInfoPacket or OverallQualityPacket or GotLootPacket));
                }, beforeItemPublication: item =>
                {
                    claims++;
                    using var verify = current.Context.Open();
                    Assert.AreEqual(4, verify.CharacterInventoryEntries.Count());
                    Assert.AreEqual(107, verify.CharacterEntries.Single().Credit);
                    Assert.IsTrue(verify.CharacterInventoryEntries.Any(row => row.ItemId == item.Id));
                    Assert.IsFalse(DrainCalls(current).Any(call => call.Packet is GotLootPacket));
                });
            current = harness;
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            Assert.AreEqual(1, creations);
            Assert.AreEqual(0, claims, "The old publication hook remains pickup-only.");
            AssertUnclaimed(harness, loot, 4, 0);
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            Assert.AreEqual(0, claims);
            DrainCalls(harness);
            harness.Context.BeforeSave = _ =>
            {
                Assert.AreEqual(100, harness.Client.Player.Credits[CurencyType.Credits]);
                Assert.IsTrue(harness.Client.Player.Inventory.PersonalInventory.All(id => id == 0));
                Assert.IsTrue(loot.LootItems.All(item => !item.Taken));
                Assert.AreEqual(0, DrainCalls(harness).Count, "No pickup result may be queued by an uncommitted save.");
            };
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            harness.Context.BeforeSave = null;
            Assert.AreEqual(4, claims);
            Assert.IsTrue(loot.FullyLooted);
            rolls.AssertExhausted();
        }

        [TestMethod]
        [DataRow(41665U, 5000U, 200, 1)]
        [DataRow(28U, 50000U, 50, 2)]
        [DataRow(3057U, 1U, 0, 1)]
        public void FullPersistedNativeBagsLeaveWhatDoesNotFitAndRetryAfterSlotsAreFreed(
            uint fillerTemplate, uint nativeStack, int firstSlot, int slotsNeeded)
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            Grant(harness, fillerTemplate, nativeStack * 50);
            Assert.AreEqual(50, harness.Client.Player.Inventory.PersonalInventory.Count(id => id != 0));
            AssertHeld(harness, fillerTemplate, nativeStack * 50);
            using (var verify = harness.Context.Open())
            {
                Assert.AreEqual(50, verify.CharacterInventoryEntries.Count());
                Assert.IsTrue(verify.ItemEntries.All(row => row.ItemTemplateId == fillerTemplate && row.StackSize == nativeStack));
            }
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot,
                new[] { (41666U, 1U), (28U, 24U), (44917U, 1U), (17130U, 1U) }, menu: true);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            // What has no room - the filled category's - stays on the corpse; the rest is taken,
            // and Loot All pays the corpse's credits, the player told their inventory is full.
            var full = ItemManager.Instance.GetItemTemplateById(fillerTemplate).InventoryCategory;
            foreach (var item in loot.LootItems)
                Assert.AreEqual(item.Item.ItemTemplate.InventoryCategory != full, item.Taken, $"template {item.ItemTemplateId}");
            Assert.AreEqual(slotsNeeded, loot.LootItems.Count(item => !item.Taken));
            Assert.IsFalse(loot.FullyLooted);
            AssertPersistedCredits(harness, 107);
            Assert.AreEqual(107, harness.Client.Player.Credits[CurencyType.Credits]);
            var firstTake = DrainCalls(harness);
            Assert.IsTrue(firstTake.Any(call => call.Packet is GotLootPacket));
            Assert.AreEqual(1, firstTake.Count(call => call.Packet is DisplayClientMessagePacket { MsgId: PlayerMessage.PmInventoryFull }));
            for (var index = 0; index < slotsNeeded; index++)
            {
                var filler = EntityManager.Instance.GetItem(harness.Client.Player.Inventory.PersonalInventory[firstSlot + index]);
                Assert.AreEqual(fillerTemplate, filler.ItemTemplateId);
                InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                    new PersonalInventory_DestroyItemPacket { EntityId = filler.EntityId, Quantity = filler.StackSize });
            }
            DrainCalls(harness);
            if (firstSlot == 200)
            {
                var skull = loot.LootItems.Single(item => item.ItemTemplateId == 41666);
                LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                    new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = skull.EntityId, DestSlot = 200 });
                Assert.AreEqual(skull.EntityId, harness.Client.Player.Inventory.PersonalInventory[200]);
                Assert.IsTrue(loot.FullyLooted, "the skull was all that was left on it");
                AssertPersistedCredits(harness, 107);
            }
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(loot.FullyLooted);
            AssertHeld(harness, 41666, 1);
            AssertHeld(harness, fillerTemplate, nativeStack * (uint)(50 - slotsNeeded) + (fillerTemplate == 28 ? 24U : 0U));
            AssertPersistedCredits(harness, 107);
            DrainCalls(harness);
            AssertDuplicateRefused(harness, loot);
            rolls.AssertExhausted();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void OpenedNativeCorpseRevalidatesRangeOrExtendedExpiryBeforePickup(bool expire)
        {
            var rolls = JunkOnlyRolls();
            using var harness = CreatePinnedHarness(rolls.Next);
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot, new[] { (41666U, 1U) }, menu: true);
            if (expire)
                Assert.IsTrue(LootDispenserManager.Instance.AdvanceCorpseLifetime(harness.Map, loot.Corpse,
                    LootDispenserManager.BeingLootedCorpseMs));
            else
                harness.MoveTo(loot.Corpse.Position + new Vector3(6.01f, 0, 0));
            DrainCalls(harness);
            var saves = harness.Context.SaveAttempts;
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                new RequestLootItemFromCorpsePacket
                { EntityId = loot.EntityId, ItemId = loot.LootItems.Single().EntityId, DestSlot = 200 });
            Assert.AreEqual(saves, harness.Context.SaveAttempts);
            AssertUnclaimed(harness, loot, 1, 0);
            AssertPersistedCredits(harness, 100);
            Assert.IsFalse(DrainCalls(harness).Any(call => call.Packet is GotLootPacket or TakenInfoPacket));
            rolls.AssertExhausted();
        }

        [TestMethod]
        public void IntentionallyEmptyMigratedProfileStillUsesRealKillMenuAndCreditClaimWithoutFallback()
        {
            var rolls = new ScriptedRoll();
            rolls.Add(1, 10, 7);
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: rolls.Next,
                additionalContent: migration =>
                {
                    migration.Sql("DELETE FROM loot_item WHERE profile_key = 'wilderness_v1_thrax_5_9'");
                    migration.Sql("DELETE FROM ordinary_loot_group WHERE profile_key = 'wilderness_v1_thrax_5_9'");
                });
            var profile = OrdinaryLootCatalog.Current.Resolve(1220, 57);
            Assert.IsNotNull(profile);
            Assert.AreEqual(0, profile.Groups.Count);
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            Assert.AreEqual(0, loot.LootItems.Count);
            var empty = Array.Empty<(uint, uint)>();
            var published = DrainCalls(harness);
            AssertQuality(published, loot, LootQuality.Junk);
            AssertLists(published, harness, loot, empty, menu: false);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot, empty, menu: true);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(loot.FullyLooted);
            Assert.AreEqual(107, harness.Client.Player.Credits[CurencyType.Credits]);
            AssertPersistedCredits(harness, 107);
            using var verify = harness.Context.Open();
            Assert.AreEqual(0, verify.ItemEntries.Count());
            Assert.AreEqual(0, verify.CharacterInventoryEntries.Count());
            var got = DrainCalls(harness).Select(call => call.Packet).OfType<GotLootPacket>().Single();
            Assert.AreEqual(0, got.Entries.Count);
            Assert.AreEqual(7, got.Credits);
            AssertDuplicateRefused(harness, loot);
            rolls.AssertExhausted();
        }

        [TestMethod]
        [DataRow("expired")]
        [DataRow("out-of-range")]
        [DataRow("foreign-player")]
        [DataRow("foreign-map")]
        [DataRow("stale-corpse")]
        [DataRow("stale-item")]
        public void RealCorpseRequestsRejectExpiredForeignOutOfRangeAndStaleSources(string rejection)
        {
            var rolls = JunkOnlyRolls();
            using var harness = CreatePinnedHarness(rolls.Next);
            var corpse = Spawn(harness, 580023, 57);
            var loot = Kill(harness, corpse);
            var itemEntityId = loot.LootItems.Single().EntityId;
            DrainCalls(harness);
            var client = harness.Client;
            Client foreign = null;
            if (rejection == "expired")
                Assert.IsTrue(LootDispenserManager.Instance.AdvanceCorpseLifetime(harness.Map, corpse,
                    LootDispenserManager.LootableCorpseMs));
            else if (rejection == "out-of-range")
                harness.MoveTo(corpse.Position + new Vector3(0, 6.01f, 0));
            else if (rejection == "foreign-player")
            {
                foreign = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
                foreign.Player.Attributes[Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0);
                foreign.SetWorldPosition(corpse.Position, 0);
                CellManager.Instance.UpdateVisibility(foreign);
                MissionTestContext.Drain(foreign);
                client = foreign;
            }
            else if (rejection == "foreign-map")
                corpse.RuntimeMapChannel = new MapChannel { MapInfo = new MapInfo(1220, "different instance", 1, 0) };
            else if (rejection == "stale-corpse")
                Assert.IsTrue(CellManager.Instance.RemoveCreatureFromWorld(harness.Map, corpse));
            else
                Grant(harness, 41665, 1);
            DrainCalls(harness);
            var saves = harness.Context.SaveAttempts;
            var inventory = client.Player.Inventory.PersonalInventory.ToArray();
            var credits = client.Player.Credits[CurencyType.Credits];
            try
            {
                if (rejection == "stale-item")
                {
                    LootDispenserManager.Instance.RequestLootItemFromCorpse(client,
                        new RequestLootItemFromCorpsePacket
                        {
                            EntityId = loot.EntityId,
                            ItemId = client.Player.Inventory.PersonalInventory.First(id => id != 0), DestSlot = 201
                        });
                }
                else
                {
                    LootDispenserManager.Instance.RequestCorpseLooting(client,
                        new RequestCorpseLootingPacket { EntityId = loot.EntityId });
                    LootDispenserManager.Instance.RequestLootAllFromCorpse(client,
                        new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                    LootDispenserManager.Instance.RequestLootItemFromCorpse(client,
                        new RequestLootItemFromCorpsePacket
                        { EntityId = loot.EntityId, ItemId = itemEntityId, DestSlot = 200 });
                }
                Assert.AreEqual(saves, harness.Context.SaveAttempts);
                CollectionAssert.AreEqual(inventory, client.Player.Inventory.PersonalInventory.ToArray());
                Assert.AreEqual(credits, client.Player.Credits[CurencyType.Credits]);
                var packets = MissionTestContext.Drain(client);
                Assert.IsFalse(packets.Any(packet => packet is LootCorpsePacket or GotLootPacket));
                Assert.AreEqual(0UL, loot.CurrentLooter);
                AssertHeld(harness, 41666, 0);
                AssertPersistedCredits(harness, 100);
                using var verify = harness.Context.Open();
                Assert.AreEqual(rejection == "stale-corpse" ? 0 : rejection == "stale-item" ? 2 : 1,
                    verify.ItemEntries.Count());
            }
            finally
            {
                corpse.RuntimeMapChannel = harness.Map;
                if (foreign != null)
                {
                    CellManager.Instance.RemoveFromWorld(foreign);
                    harness.Map.ClientList.Remove(foreign);
                }
            }
            rolls.AssertExhausted();
        }

        [TestMethod]
        public void RealMultiItemClaimRollsBackPersistenceFailureAndRetriesWithoutRerolling()
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            Grant(harness, 28, 10);
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            rolls.AssertExhausted();
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            DrainCalls(harness);
            harness.Context.AfterSave = _ => throw new DbUpdateException("Task6 injected pickup transaction failure.");
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            harness.Context.AfterSave = null;
            AssertUnclaimed(harness, loot, 4, 1);
            AssertHeld(harness, 28, 10);
            AssertHeld(harness, 41666, 0);
            AssertPersistedCredits(harness, 100);
            Assert.AreEqual(100, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsFalse(DrainCalls(harness).Any(call => call.Packet is GotLootPacket or TakenInfoPacket));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(loot.FullyLooted);
            AssertHeld(harness, 28, 34);
            AssertHeld(harness, 41666, 1);
            AssertPersistedCredits(harness, 107);
            rolls.AssertExhausted();
            DrainCalls(harness);
            AssertDuplicateRefused(harness, loot);
        }

        [TestMethod]
        public void FailedRealCreationCleansCommittedBatchBeforeAnyCorpsePublication()
        {
            var rolls = FamilyRolls(thrax: true);
            LootDispenser rejected = null;
            LootItem[] rejectedItems = null;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: rolls.Next,
                beforeCorpsePublication: loot =>
                {
                    rejected = loot;
                    rejectedItems = loot.LootItems.ToArray();
                    throw new DbUpdateException("Task6 injected creation publication failure.");
                });
            var corpse = Spawn(harness, 580023, 57);
            Hit(harness, corpse, 100000);
            Assert.AreEqual(CharacterState.Dead, corpse.State);
            Assert.IsNotNull(rejected);
            Assert.AreEqual(4, rejectedItems.Length);
            Assert.AreEqual(0, rejected.LootItems.Count, "Retirement clears the failed creation's live item list.");
            Assert.AreEqual(0UL, corpse.CorpseLootEntityId);
            Assert.AreEqual(0, harness.Map.LootDispensers.Count);
            foreach (var drop in rejectedItems)
            {
                Assert.IsNull(EntityManager.Instance.GetItem(drop.EntityId));
                Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(drop.EntityId));
            }
            using var verify = harness.Context.Open();
            Assert.AreEqual(0, verify.ItemEntries.Count());
            Assert.AreEqual(0, verify.CharacterInventoryEntries.Count());
            Assert.IsFalse(DrainCalls(harness).Any(call =>
                call.Packet is LootInfoPacket or OverallQualityPacket or GotLootPacket or CanLootItemsPacket));
            rolls.AssertExhausted();
        }

        [TestMethod]
        public void RealMapDepartureRetiresUnclaimedItemsButPreservesCommittedSinglePickup()
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            var loot = Kill(harness, Spawn(harness, 580023, 57));
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            DrainCalls(harness);
            var skull = loot.LootItems.Single(item => item.ItemTemplateId == 41666);
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = skull.EntityId, DestSlot = 200 });
            Assert.IsTrue(skull.Taken);
            Assert.IsFalse(loot.FullyLooted);
            AssertHeld(harness, 41666, 1);
            var unclaimed = loot.LootItems.Where(item => !item.Taken)
                .Select(item => (item.EntityId, ItemId: item.Item.Id)).ToArray();
            Assert.AreEqual(3, unclaimed.Length, "Capture every unclaimed item before retirement clears the live list.");
            Assert.AreEqual(3, unclaimed.Select(item => item.EntityId).Distinct().Count());
            Assert.AreEqual(3, unclaimed.Select(item => item.ItemId).Distinct().Count());
            using (var beforeDeparture = harness.Context.Open())
                foreach (var item in unclaimed)
                {
                    Assert.IsTrue(item.ItemId > 0);
                    Assert.IsNotNull(EntityManager.Instance.GetItem(item.EntityId));
                    Assert.IsTrue(EntityManager.Instance.RegisteredEntities.ContainsKey(item.EntityId));
                    Assert.IsTrue(beforeDeparture.ItemEntries.Any(row => row.ItemId == item.ItemId));
                    Assert.IsFalse(beforeDeparture.CharacterInventoryEntries.Any(row => row.ItemId == item.ItemId));
                }
            var destination = new MapChannel
            {
                MapInfo = new MapInfo(1221, "test transfer destination", 1, 0), PlayerLimit = 128
            };
            harness.Maps.MapChannelArray.Add(1221, destination);
            Assert.IsTrue(harness.Maps.ChangeMap(harness.Client, 1221, Vector3.Zero, 0));
            Assert.IsFalse(harness.Map.LootDispensers.ContainsKey(loot.EntityId));
            Assert.AreEqual(0UL, loot.Corpse.CorpseLootEntityId);
            Assert.AreSame(skull.Item, EntityManager.Instance.GetItem(skull.EntityId));
            Assert.IsTrue(EntityManager.Instance.RegisteredEntities.ContainsKey(skull.EntityId));
            AssertHeld(harness, 41666, 1);
            using var verify = harness.Context.Open();
            foreach (var item in unclaimed)
            {
                Assert.IsNull(EntityManager.Instance.GetItem(item.EntityId));
                Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(item.EntityId),
                    "A captured unclaimed item entity must not survive map departure.");
                Assert.IsFalse(verify.ItemEntries.Any(row => row.ItemId == item.ItemId));
                Assert.IsFalse(verify.CharacterInventoryEntries.Any(row => row.ItemId == item.ItemId));
            }
            Assert.AreEqual(1, verify.ItemEntries.Count());
            Assert.AreEqual(1, verify.CharacterInventoryEntries.Count());
            Assert.AreEqual(skull.Item.Id, verify.ItemEntries.Single().ItemId);
            var ownedSkull = verify.CharacterInventoryEntries.Single();
            Assert.AreEqual(skull.Item.Id, ownedSkull.ItemId);
            Assert.AreEqual(harness.Client.Player.Id, ownedSkull.CharacterId);
            AssertPersistedCredits(harness, 100);
            DrainCalls(harness);
            AssertDuplicateRefused(harness, loot, skull.EntityId);
            rolls.AssertExhausted();
        }

        [TestMethod]
        public void RealOrdinaryThraxAndPersonalMissionHeartCoexistAndOnlyPickupAdvancesCollection()
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            WildernessElohPinholeTests.CompleteEarlierHistory(harness, 1069);
            harness.SpawnWorld(219);
            var apirka = harness.Npc(219);
            Assert.IsNotNull(apirka);
            harness.MoveTo(apirka.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, apirka.EntityId, 479));
            var objective = harness.Client.Player.Missions[479].Objectives[1];
            Assert.AreEqual(0U, objective.ItemCounters[10346]);
            var loot = Kill(harness, Spawn(harness, 580019, 3));
            var expected = new[] { (41666U, 1U), (28U, 24U), (44917U, 1U), (17133U, 1U), (2285U, 1U) };
            AssertDrops(harness, loot, expected);
            AssertQuality(DrainCalls(harness), loot, LootQuality.Mission);
            Assert.AreEqual(0U, objective.ItemCounters[10346], "A real player kill cannot replace collecting the heart.");
            AssertHeld(harness, 2285, 0);
            var heart = loot.LootItems.Single(item => item.ItemTemplateId == 2285);
            Assert.AreEqual(harness.Client.Player.EntityId, heart.ReservedFor);
            Assert.IsNull(heart.Item.MissionOwnership, "Personal ownership is persisted by pickup, not by the kill.");
            var foreign = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            foreign.Player.Attributes[Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0);
            foreign.SetWorldPosition(loot.Corpse.Position, 0);
            try
            {
                LootDispenserManager.Instance.RequestLootItemFromCorpse(foreign,
                    new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = heart.EntityId, DestSlot = 200 });
                Assert.IsFalse(heart.Taken);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(foreign);
                harness.Map.ClientList.Remove(foreign);
            }
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot, expected, menu: true);
            Assert.AreEqual(0U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(loot.FullyLooted);
            AssertHeld(harness, 2285, 1);
            Assert.AreEqual(harness.Client.Player.Id, heart.Item.OwnerId);
            Assert.IsNull(heart.Item.MissionOwnership,
                "This native collection uses personal corpse qualification, not assignment-owned inventory.");
            AssertHeld(harness, 28, 24);
            AssertHeld(harness, 17133, 1);
            Assert.AreEqual(1U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
            using var verify = harness.Context.Open();
            Assert.AreEqual(1U, verify.CharacterMissionObjectiveItemCounterEntries.AsNoTracking().Single(row =>
                row.CharacterId == harness.Client.Player.Id && row.MissionId == 479 && row.ObjectiveId == 1 &&
                row.ItemClassId == 10346).CounterValue);
            AssertPersistedCredits(harness, 107);
            DrainCalls(harness);
            AssertDuplicateRefused(harness, loot);
            Assert.AreEqual(1U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
            rolls.AssertExhausted();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RealMission479HeartRevalidatesAssignmentWhileOwnerCanClaimOrdinaryLoot(bool claimAll)
        {
            var rolls = FamilyRolls(thrax: true);
            using var harness = CreatePinnedHarness(rolls.Next);
            WildernessElohPinholeTests.CompleteEarlierHistory(harness, 1069);
            harness.SpawnWorld(219);
            var apirka = harness.Npc(219);
            Assert.IsNotNull(apirka);
            harness.MoveTo(apirka.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, apirka.EntityId, 479));
            var assignment = harness.Client.Player.Missions[479];
            var loot = Kill(harness, Spawn(harness, 580019, 3));
            var expected = new[] { (41666U, 1U), (28U, 24U), (44917U, 1U), (17133U, 1U), (2285U, 1U) };
            AssertDrops(harness, loot, expected);
            DrainCalls(harness);
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot, expected, menu: true);
            var heart = loot.LootItems.Single(item => item.ItemTemplateId == 2285);
            var ammo = loot.LootItems.Single(item => item.ItemTemplateId == 28);
            var heartRequest = new RequestLootItemFromCorpsePacket
            { EntityId = loot.EntityId, ItemId = heart.EntityId, DestSlot = 200 };

            // Change only fixture authority; abandoning would also retire the source and mask this later gate.
            using (var authority = harness.Context.Open())
            {
                var durable = authority.CharacterMissionEntries.Single(row =>
                    row.CharacterId == harness.Client.Player.Id && row.MissionId == 479);
                Assert.IsTrue(assignment.MatchesAssignment(durable.AssignmentId, durable.Generation, durable.ContentRevision));
                Assert.AreEqual((uint)MissionState.Active, durable.MissionState);
                durable.Generation++;
                authority.SaveChanges();
            }
            Assert.AreSame(assignment, harness.Client.Player.Missions[479]);
            Assert.AreEqual(MissionState.Active, assignment.State);
            Assert.AreEqual(harness.Client.Player.EntityId, loot.Owner);
            Assert.AreSame(harness.Client, loot.OwnerClient);
            Assert.AreSame(harness.Client.Player.MapChannel, loot.Map);
            Assert.AreSame(loot, harness.Map.LootDispensers[loot.EntityId]);
            Assert.AreSame(loot.Corpse, EntityManager.Instance.Creatures[loot.Corpse.EntityId]);
            Assert.AreEqual(loot.EntityId, loot.Corpse.CorpseLootEntityId);
            Assert.AreEqual(CharacterState.Dead, loot.Corpse.State);
            Assert.AreEqual(0, loot.Corpse.Attributes[Attributes.Health].Current);
            Assert.AreEqual(harness.Client.Player.MapContextId, loot.Corpse.MapContextId);
            Assert.AreEqual(0F, Vector3.Distance(harness.Client.Player.Position, loot.Corpse.Position));
            Assert.IsNull(harness.Client.PendingTransfer);
            Assert.AreEqual(harness.Client.Player.EntityId, heart.ReservedFor);
            Assert.IsTrue(heart.MayTake(harness.Client.Player.EntityId));
            LootDispenserManager.Instance.RequestCorpseLooting(harness.Client,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });
            AssertLists(DrainCalls(harness), harness, loot, expected, menu: true);
            Assert.AreEqual(harness.Client.Player.EntityId, loot.CurrentLooter,
                "The owner still passes real corpse/menu admission after authority changes.");
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                new RequestLootItemFromCorpsePacket { EntityId = loot.EntityId, ItemId = ammo.EntityId, DestSlot = 50 });
            Assert.IsTrue(ammo.Taken, "Ordinary pickup from this same corpse must remain admitted.");
            AssertHeld(harness, 28, 24);
            DrainCalls(harness);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var saves = harness.Context.SaveAttempts;
                var units = harness.Context.CharUnitsCreated;
                var inventory = harness.Client.Player.Inventory.PersonalInventory.ToArray();
                if (claimAll)
                    LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                        new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                else
                    LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client, heartRequest);
                Assert.IsFalse(heart.Taken, "The corpse-admitted owner must fail personal assignment revalidation.");
                Assert.AreEqual(units + 1, harness.Context.CharUnitsCreated,
                    "Rejection must reach the claim transaction, not stop at outer corpse or reservation admission.");
                Assert.AreEqual(saves, harness.Context.SaveAttempts);
                Assert.IsTrue(loot.IsLootable);
                Assert.IsFalse(loot.FullyLooted);
                Assert.AreEqual(7, loot.Credits);
                Assert.IsTrue(loot.LootItems.Where(item => item != ammo).All(item => !item.Taken));
                CollectionAssert.AreEqual(inventory, harness.Client.Player.Inventory.PersonalInventory.ToArray());
                AssertHeld(harness, 2285, 0);
                Assert.AreEqual(0U, heart.Item.OwnerId);
                Assert.IsNull(heart.Item.MissionOwnership);
                Assert.AreEqual(0U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
                Assert.AreEqual(100, harness.Client.Player.Credits[CurencyType.Credits]);
                AssertPersistedCredits(harness, 100);
                using var verify = harness.Context.Open();
                var durable = verify.CharacterMissionEntries.AsNoTracking().Single(row =>
                    row.CharacterId == harness.Client.Player.Id && row.MissionId == 479);
                Assert.AreEqual(assignment.Generation + 1, durable.Generation);
                Assert.AreEqual(assignment.AssignmentId, durable.AssignmentId);
                Assert.AreEqual(assignment.ContentRevision, durable.ContentRevision);
                Assert.AreEqual((uint)MissionState.Active, durable.MissionState);
                Assert.AreEqual(1U, verify.ItemEntries.Single(row => row.ItemId == heart.Item.Id).StackSize);
                Assert.IsFalse(verify.CharacterInventoryEntries.Any(row => row.ItemId == heart.Item.Id));
                Assert.AreEqual(1, verify.CharacterInventoryEntries.Count(), "Only the admitted ordinary ammunition is owned.");
                Assert.AreEqual(0U, verify.CharacterMissionObjectiveItemCounterEntries.Single(row =>
                    row.CharacterId == harness.Client.Player.Id && row.MissionId == 479 && row.ObjectiveId == 1 &&
                    row.ItemClassId == 10346).CounterValue);
                Assert.IsFalse(DrainCalls(harness).Any(call => call.Packet is GotLootPacket or TakenInfoPacket));
            }

            using (var authority = harness.Context.Open())
            {
                authority.CharacterMissionEntries.Single(row =>
                    row.CharacterId == harness.Client.Player.Id && row.MissionId == 479).Generation = assignment.Generation;
                authority.SaveChanges();
            }
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client, heartRequest);
            Assert.IsTrue(heart.Taken, "Restoring only assignment authority must admit the same native heart.");
            AssertHeld(harness, 2285, 1);
            Assert.AreEqual(harness.Client.Player.Id, heart.Item.OwnerId);
            Assert.AreEqual(1U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
            DrainCalls(harness);
            var duplicateSaves = harness.Context.SaveAttempts;
            LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client, heartRequest);
            Assert.AreEqual(duplicateSaves, harness.Context.SaveAttempts);
            Assert.AreEqual(1U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
            AssertHeld(harness, 2285, 1);
            Assert.IsFalse(DrainCalls(harness).Any(call => call.Packet is GotLootPacket));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            Assert.IsTrue(loot.FullyLooted);
            AssertPersistedCredits(harness, 107);
            DrainCalls(harness);
            AssertDuplicateRefused(harness, loot);
            AssertHeld(harness, 2285, 1);
            Assert.AreEqual(1U, harness.Client.Player.Missions[479].Objectives[1].ItemCounters[10346]);
            using var final = harness.Context.Open();
            Assert.AreEqual(1U, final.CharacterMissionObjectiveItemCounterEntries.Single(row =>
                row.CharacterId == harness.Client.Player.Id && row.MissionId == 479 && row.ObjectiveId == 1 &&
                row.ItemClassId == 10346).CounterValue);
            rolls.AssertExhausted();
        }

        private static WildernessRuntimeTestHarness CreatePinnedHarness(Func<int, int, int> next) =>
            WildernessRuntimeTestHarness.Create(lootRoll: next);

        private static ScriptedRoll GearOnlyRolls(bool boss, int admission, int selection, bool credits = true)
        {
            var rolls = new ScriptedRoll();
            for (var index = 0; index < 3; index++)
                rolls.Add(0, 10000, 9999);
            if (admission < (boss ? 6500 : 800))
                rolls.Group(admission, boss ? 200 : 100, selection);
            else
                rolls.Add(0, 10000, admission);
            if (credits)
                rolls.Add(1, 10, 7);
            return rolls;
        }

        private static ScriptedRoll JunkOnlyRolls()
        {
            var rolls = new ScriptedRoll();
            rolls.Group(0, 2, 1);
            for (var index = 0; index < 3; index++)
                rolls.Add(0, 10000, 9999);
            rolls.Add(1, 10, 7);
            return rolls;
        }

        private static ScriptedRoll FamilyRolls(bool thrax)
        {
            var rolls = new ScriptedRoll();
            rolls.Group(0, thrax ? 2 : 4, thrax ? 1 : 3);
            if (thrax)
                rolls.Group(0, 1, 0, 12, 25, 24);
            rolls.Group(0, 1, 0);
            rolls.Group(0, 100, thrax ? 70 : 35);
            rolls.Add(1, 10, 7);
            return rolls;
        }

        private static Creature Spawn(WildernessRuntimeTestHarness harness, uint poolId, uint creatureId)
        {
            harness.SpawnWorld(poolId);
            var creature = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .First(actor => actor.SpawnPool?.DbId == poolId && actor.DbId == creatureId &&
                    actor.State != CharacterState.Dead);
            Assert.AreEqual(TargetCategory.Hostile, creature.TargetCategory);
            Assert.IsNull(CreatureGameplayRules.Policy(creature).Loot, "Ordinary loot must not become an authored personal override.");
            Assert.AreSame(creature, EntityManager.Instance.Creatures[creature.EntityId]);
            Assert.AreSame(harness.Map, creature.RuntimeMapChannel);
            harness.MoveTo(creature.Position);
            DrainCalls(harness);
            return creature;
        }

        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, Creature creature)
        {
            var health = creature.Attributes[Attributes.Health].Current;
            var armor = creature.Attributes[Attributes.Armor].Current;
            Assert.IsTrue(health > 0);
            Hit(harness, creature, 1);
            Assert.IsTrue(creature.Attributes[Attributes.Health].Current +
                creature.Attributes[Attributes.Armor].Current <
                health + armor,
                "The first actual player missile must damage a living native actor.");
            Assert.AreNotEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(0UL, creature.CorpseLootEntityId);
            Hit(harness, creature, 100000);
            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(0, creature.Attributes[Attributes.Health].Current);
            Assert.AreNotEqual(0UL, creature.CorpseLootEntityId);
            return harness.Map.LootDispensers[creature.CorpseLootEntityId];
        }

        private static void Hit(WildernessRuntimeTestHarness harness, Creature creature, int damage)
        {
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 1, creature.EntityId, 0), damage);
            var missile = harness.Map.QueuedMissiles.Single();
            Assert.AreSame(harness.Client.Player, missile.Source);
            Assert.AreEqual(creature.EntityId, missile.TargetEntityId);
            missile.CritChance = 0;
            MissileManager.Instance.DoWork(harness.Map, 1000);
            Assert.AreEqual(0, harness.Map.QueuedMissiles.Count);
        }

        private static void Grant(WildernessRuntimeTestHarness harness, uint template, uint quantity)
        {
            using var grant = new InventoryManager.InventoryGrant();
            using var unit = harness.CreateChar();
            unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                new[] { new InventoryManager.InventoryItemGrant(template, quantity) }, unit));
            grant.Publish(harness.Client);
        }

        private static List<CallMethodMessage> DrainCalls(WildernessRuntimeTestHarness harness) =>
            WorldTestContext.Drain(harness.Client).Select(packet => packet.Message)
                .OfType<CallMethodMessage>().ToList();

        private static void AssertDrops(WildernessRuntimeTestHarness harness, LootDispenser loot,
            (uint Template, uint Quantity)[] expected)
        {
            CollectionAssert.AreEqual(expected, loot.LootItems.Select(item => (item.ItemTemplateId, item.ItemQuantity)).ToArray());
            Assert.IsTrue(loot.LootItems.Count(item => item.Item.ItemTemplate.QualityId == (int)LootQuality.Junk) <= 1,
                "Multiple native junk candidates must never yield more than one ordinary junk TYPE.");
            foreach (var drop in loot.LootItems)
            {
                Assert.IsTrue(drop.Item.Id > 0);
                Assert.AreSame(drop.Item, EntityManager.Instance.GetItem(drop.EntityId));
                Assert.AreEqual(EntityType.Item, EntityManager.Instance.GetEntityType(drop.EntityId));
                Assert.AreEqual(harness.Client.Player.EntityId, drop.ActorId);
                Assert.AreEqual(0U, drop.PartyId);
                using var verify = harness.Context.Open();
                var row = verify.ItemEntries.AsNoTracking().Single(item => item.ItemId == drop.Item.Id);
                Assert.AreEqual(drop.ItemTemplateId, row.ItemTemplateId);
                Assert.AreEqual(drop.ItemQuantity, row.StackSize);
            }
        }

        private static void AssertUnclaimed(WildernessRuntimeTestHarness harness, LootDispenser loot,
            int dropCount, int inventoryCount)
        {
            Assert.IsFalse(loot.FullyLooted);
            Assert.IsTrue(loot.LootItems.All(item => !item.Taken));
            using var verify = harness.Context.Open();
            Assert.AreEqual(inventoryCount, verify.CharacterInventoryEntries.Count());
            Assert.AreEqual(dropCount + inventoryCount, verify.ItemEntries.Count());
            Assert.IsFalse(verify.CharacterInventoryEntries.Any(row =>
                loot.LootItems.Select(drop => drop.Item.Id).Contains(row.ItemId)));
        }

        private static void AssertQuality(List<CallMethodMessage> calls, LootDispenser loot, LootQuality expected)
        {
            var call = calls.Single(message => message.Packet is OverallQualityPacket);
            Assert.AreEqual(loot.EntityId, call.EntityId);
            Assert.AreEqual(expected, ((OverallQualityPacket)call.Packet).OverallQuality);
            CollectionAssert.AreEqual(Encode(writer => { writer.WriteTuple(1); writer.WriteInt((int)expected); }),
                Encode(call.Packet));
        }

        private static void AssertLists(List<CallMethodMessage> calls, WildernessRuntimeTestHarness harness,
            LootDispenser loot, (uint Template, uint Quantity)[] expected, bool menu)
        {
            var info = calls.Single(call => call.Packet is LootInfoPacket);
            Assert.AreEqual(loot.EntityId, info.EntityId);
            CollectionAssert.AreEqual(Encode(writer =>
            {
                writer.WriteTuple(1);
                WriteExpectedItems(writer, harness, loot, expected);
            }), Encode(info.Packet));
            var canLoot = calls.Single(call => call.Packet is CanLootItemsPacket);
            Assert.AreEqual(loot.EntityId, canLoot.EntityId);
            Assert.IsTrue(((CanLootItemsPacket)canLoot.Packet).CanLootItems);
            CollectionAssert.AreEqual(Encode(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteBool(true);
                writer.WriteDictionary(expected.Length);
                foreach (var item in loot.LootItems)
                {
                    writer.WriteULong(item.EntityId);
                    writer.WriteTuple(1);
                    writer.WriteBool(true);
                }
            }), Encode(canLoot.Packet));
            if (!menu)
                return;
            for (var index = 0; index < expected.Length; index++)
            {
                var entityId = loot.LootItems[index].EntityId;
                var physical = calls.Select(call => call.Packet).OfType<CreatePhysicalEntityPacket>()
                    .Single(packet => packet.EntityId == entityId);
                Assert.AreEqual(NativeDropClasses[expected[index].Template], (uint)physical.ClassId,
                    "The corpse row must have an actual native Item entity the client's menu can resolve.");
                var infoCall = calls.Single(call => call.EntityId == entityId && call.Packet is ItemInfoPacket);
                Assert.AreEqual(expected[index].Template, ((ItemInfoPacket)infoCall.Packet).Item.ItemTemplateId);
                var stack = calls.Single(call => call.EntityId == entityId && call.Packet is SetStackCountPacket);
                Assert.AreEqual(expected[index].Quantity, ((SetStackCountPacket)stack.Packet).StackSize);
            }
            var opened = calls.Single(call => call.Packet is LootCorpsePacket);
            Assert.AreEqual(loot.EntityId, opened.EntityId);
            Assert.AreEqual(harness.Client.Player.EntityId, ((LootCorpsePacket)opened.Packet).ActorId);
            CollectionAssert.AreEqual(Encode(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteULong(harness.Client.Player.EntityId);
                WriteExpectedItems(writer, harness, loot, expected);
            }), Encode(opened.Packet));
        }

        private static void WriteExpectedItems(PythonWriter writer, WildernessRuntimeTestHarness harness,
            LootDispenser loot, (uint Template, uint Quantity)[] expected)
        {
            writer.WriteDictionary(expected.Length);
            for (var index = 0; index < expected.Length; index++)
            {
                writer.WriteULong(loot.LootItems[index].EntityId);
                writer.WriteTuple(4);
                writer.WriteUInt(expected[index].Template);
                writer.WriteUInt(expected[index].Quantity);
                writer.WriteULong(harness.Client.Player.EntityId);
                writer.WriteNoneStruct();
            }
        }

        private static byte[] Encode(PythonPacket packet) => Encode(packet.Write);

        private static byte[] Encode(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);
            using var writer = new PythonWriter(binary);
            write(writer);
            return stream.ToArray();
        }

        private static void AssertHeld(WildernessRuntimeTestHarness harness, uint template, uint quantity)
        {
            var memory = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Where(item => item.ItemTemplateId == template).ToArray();
            Assert.AreEqual(quantity, memory.Aggregate(0U, (total, item) => total + item.StackSize));
            using var verify = harness.Context.Open();
            var persisted = (from inventory in verify.CharacterInventoryEntries.AsNoTracking()
                join item in verify.ItemEntries.AsNoTracking() on inventory.ItemId equals item.ItemId
                where inventory.CharacterId == harness.Client.Player.Id &&
                    inventory.InventoryType == (uint)InventoryType.Personal && item.ItemTemplateId == template
                select item).ToArray();
            Assert.AreEqual(quantity, persisted.Aggregate(0U, (total, item) => total + item.StackSize));
            CollectionAssert.AreEquivalent(memory.Select(item => item.Id).ToArray(),
                persisted.Select(item => item.ItemId).ToArray());
            foreach (var item in memory)
            {
                var start = ((int)item.ItemTemplate.InventoryCategory - 1) * 50;
                Assert.IsTrue(item.OwnerSlotId >= start && item.OwnerSlotId < start + 50);
                Assert.AreEqual(item.EntityId, harness.Client.Player.Inventory.PersonalInventory[(int)item.OwnerSlotId]);
                Assert.AreEqual(harness.Client.Player.Id, item.OwnerId);
                Assert.AreEqual(item.OwnerSlotId, verify.CharacterInventoryEntries.AsNoTracking()
                    .Single(row => row.ItemId == item.Id).SlotId);
            }
        }

        private static void AssertPersistedCredits(WildernessRuntimeTestHarness harness, int credits)
        {
            using var verify = harness.Context.Open();
            Assert.AreEqual(credits, verify.CharacterEntries.AsNoTracking().Single(row => row.Id == harness.Client.Player.Id).Credit);
        }

        private static void AssertDuplicateRefused(WildernessRuntimeTestHarness harness, LootDispenser loot,
            ulong? staleItemEntityId = null)
        {
            var saves = harness.Context.SaveAttempts;
            var inventory = harness.Client.Player.Inventory.PersonalInventory.ToArray();
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
            if (staleItemEntityId.HasValue || loot.LootItems.Count != 0)
                LootDispenserManager.Instance.RequestLootItemFromCorpse(harness.Client,
                    new RequestLootItemFromCorpsePacket
                    { EntityId = loot.EntityId, ItemId = staleItemEntityId ?? loot.LootItems[0].EntityId });
            Assert.AreEqual(saves, harness.Context.SaveAttempts);
            CollectionAssert.AreEqual(inventory, harness.Client.Player.Inventory.PersonalInventory.ToArray());
            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsFalse(DrainCalls(harness).Any(call => call.Packet is GotLootPacket));
        }

        private sealed class ScriptedRoll
        {
            private readonly Queue<(int Minimum, int Maximum, int Value)> _draws = new();

            internal void Add(int minimum, int maximum, int value) => _draws.Enqueue((minimum, maximum, value));

            internal void Group(int admission, int total, int selection, int minimum = 1, int maximum = 2, int quantity = 1)
            {
                Add(0, 10000, admission);
                Add(1, 2, 1);
                Add(0, total, selection);
                Add(minimum, maximum, quantity);
            }

            internal int Next(int minimum, int maximum)
            {
                Assert.IsTrue(_draws.Count > 0, $"Unexpected RNG call [{minimum},{maximum}).");
                var expected = _draws.Dequeue();
                Assert.AreEqual((expected.Minimum, expected.Maximum), (minimum, maximum), "Frozen native RNG call order.");
                Assert.IsTrue(expected.Value >= minimum && expected.Value < maximum);
                return expected.Value;
            }

            internal void AssertExhausted() => Assert.AreEqual(0, _draws.Count, "Every pinned draw must actually be consumed.");
        }
    }
}
