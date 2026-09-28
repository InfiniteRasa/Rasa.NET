using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class LootConsolidationTests
    {
        [TestMethod]
        [DataRow(2f, true)]
        [DataRow(2.001f, false)]
        [DataRow(float.NaN, false)]
        public void ManualCorpseOpeningUsesFiniteThreeDimensionalConfiguredDistance(float distance, bool allowed)
        {
            using var context = new LootFixture();
            context.Corpse.Position = new Vector3(distance, 0, 0);

            context.Manager.RequestCorpseLooting(context.Client,
                new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });

            var packets = context.Drain();
            Assert.AreEqual(allowed ? 1 : 0, packets.OfType<LootCorpsePacket>().Count());
            Assert.AreEqual(allowed ? context.Client.Player.EntityId : 0UL, context.Loot.CurrentLooter);
        }

        [TestMethod]
        public void ClaimRevalidatesRangeAndCommitsItemsAndCreditsBeforePublication()
        {
            using var context = new LootFixture();
            context.Manager.RequestCorpseLooting(context.Client,
                new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            context.Drain();
            context.Corpse.Position = new Vector3(3, 0, 0);

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(0, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsFalse(context.Loot.FullyLooted);
            Assert.AreEqual(0, context.Drain().Count);

            context.Corpse.Position = Vector3.Zero;
            context.Storage.BeforeSave = _ =>
            {
                Assert.AreEqual(0, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));
                Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
                Assert.AreEqual(0, context.Drain().Count);
            };
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(1, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));
            Assert.AreEqual(107, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.IsTrue(context.Drain().OfType<GotLootPacket>().Any());
            using var verify = context.Storage.Open();
            Assert.AreEqual(107, verify.CharacterEntries.AsNoTracking().Single().Credit);
            Assert.AreEqual(1, verify.CharacterInventoryEntries.AsNoTracking()
                .Count(row => row.ItemId == context.Item.Id));
        }

        [TestMethod]
        public void ExpiredCorpseCannotBeOpenedOrClaimedBeforeTheBehaviorSweep()
        {
            using var context = new LootFixture();
            context.Corpse.Controller.DeadTime = LootDispenserManager.LootableCorpseMs;
            var saves = context.Storage.SaveAttempts;

            context.Manager.RequestCorpseLooting(context.Client,
                new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(0UL, context.Loot.CurrentLooter);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, context.Client.Player.Inventory.PersonalInventory
                .Count(id => id == context.Item.EntityId));
            Assert.AreEqual(saves, context.Storage.SaveAttempts);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void CorpseLifetimeBoundaryIsCheckedUnderTheLootLock()
        {
            using var context = new LootFixture();
            context.Corpse.Controller.DeadTime = LootDispenserManager.LootableCorpseMs - 1;
            context.Manager.RequestCorpseLooting(context.Client,
                new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            Assert.AreEqual(context.Client.Player.EntityId, context.Loot.CurrentLooter);
            context.Drain();

            context.Corpse.Controller.DeadTime = LootDispenserManager.BeingLootedCorpseMs;
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.IsFalse(context.Loot.FullyLooted);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void CorpseExpiryAdvanceCannotBeExtendedByAConcurrentOpen()
        {
            using var context = new LootFixture();
            context.Corpse.Controller.DeadTime =
                LootDispenserManager.LootableCorpseMs - 1;
            using var openStarted = new ManualResetEventSlim();
            Task open;
            bool expired;

            lock (context.Map.LootSyncRoot)
            {
                expired = context.Manager.AdvanceCorpseLifetime(
                    context.Map, context.Corpse, 1);
                open = Task.Run(() =>
                {
                    openStarted.Set();
                    context.Manager.RequestCorpseLooting(context.Client,
                        new RequestCorpseLootingPacket
                        {
                            EntityId = context.Loot.EntityId
                        });
                });
                Assert.IsTrue(openStarted.Wait(5000));
                Assert.IsFalse(open.Wait(100));
            }

            Assert.IsTrue(open.Wait(5000));
            Assert.IsTrue(expired);
            Assert.AreEqual(LootDispenserManager.LootableCorpseMs,
                context.Corpse.Controller.DeadTime);
            Assert.AreEqual(0UL, context.Loot.CurrentLooter);
            Assert.AreEqual(0, context.Drain().OfType<LootCorpsePacket>().Count());
        }

        [TestMethod]
        public void PersistenceFailureRollsBackTheWholeLootClaimAndAllowsRetry()
        {
            using var context = new LootFixture();
            context.Storage.AfterSave = _ => throw new DbUpdateException("Injected loot failure.");

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));
            Assert.IsFalse(context.Loot.FullyLooted);
            Assert.AreEqual(0, context.Drain().Count);
            using (var verify = context.Storage.Open())
            {
                Assert.AreEqual(100, verify.CharacterEntries.AsNoTracking().Single().Credit);
                Assert.AreEqual(0, verify.CharacterInventoryEntries.AsNoTracking()
                    .Count(row => row.ItemId == context.Item.Id));
            }

            context.Storage.AfterSave = null;
            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });
            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.AreEqual(107, context.Client.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        public void OwnerDepartureReclaimsUnclaimedLootAndDispenserIdsAreNeverRecycled()
        {
            using var context = new LootFixture();
            var dispenserId = context.Loot.EntityId;
            var itemId = context.Item.EntityId;

            context.Manager.RemoveForOwner(context.Map, context.Client);
            var later = new LootDispenser();

            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(dispenserId));
            Assert.IsNull(EntityManager.Instance.GetItem(itemId));
            Assert.IsNull(context.Storage.Read(context.Item));
            Assert.AreNotEqual(dispenserId, later.EntityId);
        }

        [TestMethod]
        public void FailedDepartureCleanupRetainsIdOwnershipAndRetries()
        {
            using var context = new LootFixture();
            var itemEntityId = context.Item.EntityId;
            context.Storage.AfterSave = _ =>
                throw new DbUpdateException("Injected departure cleanup failure.");

            context.Manager.RemoveForOwner(context.Map, context.Client);
            var later = new Item();

            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
            Assert.IsNull(EntityManager.Instance.GetItem(itemEntityId));
            Assert.IsNotNull(context.Storage.Read(context.Item));
            Assert.AreNotEqual(itemEntityId, later.EntityId);

            context.Storage.AfterSave = null;
            context.Manager.RemoveForOwner(context.Map, context.Client);

            Assert.IsNull(context.Storage.Read(context.Item));
            EntityManager.Instance.FreeEntity(later.EntityId);
        }

        [TestMethod]
        public void FailedCreatureRemovalCleanupRetriesOnTheNextRemoval()
        {
            using var context = new LootFixture();
            context.Storage.AfterSave = _ =>
                throw new DbUpdateException("Injected removal cleanup failure.");

            context.Manager.RemoveForCreature(context.Map, context.Corpse);
            Assert.IsNotNull(context.Storage.Read(context.Item));

            context.Storage.AfterSave = null;
            context.Manager.RemoveForCreature(context.Map, context.Corpse);

            Assert.IsNull(context.Storage.Read(context.Item));
        }

        [TestMethod]
        public void FailedExpiryCleanupRetriesOnTheNextLifetimeCheck()
        {
            using var context = new LootFixture();
            context.Corpse.Controller.DeadTime = LootDispenserManager.LootableCorpseMs;
            context.Storage.AfterSave = _ =>
                throw new DbUpdateException("Injected expiry cleanup failure.");

            Assert.IsTrue(context.Manager.MayDespawn(
                context.Map, context.Corpse, context.Corpse.Controller.DeadTime));
            context.Manager.RemoveForCreature(context.Map, context.Corpse);
            Assert.IsNotNull(context.Storage.Read(context.Item));

            context.Storage.AfterSave = null;
            context.Manager.MayDespawn(
                context.Map, context.Corpse, context.Corpse.Controller.DeadTime);

            Assert.IsNull(context.Storage.Read(context.Item));
        }

        [TestMethod]
        public void LootMergesStacksAndPersistsTheRemainderInOneClaim()
        {
            using var context = new LootFixture();
            EntityClassManager.Instance.GetItemClassInfo(context.Item).StackSize = 5;
            var existing = context.Storage.AddAmmo(4);

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(5u, existing.StackSize);
            Assert.AreEqual(2u, context.Item.StackSize);
            Assert.AreEqual(2, context.Storage.ReadInventory()
                .Count(row => row.InventoryType == (uint)InventoryType.Personal));
            Assert.AreEqual(5u, context.Storage.Read(existing).StackSize);
            Assert.AreEqual(2u, context.Storage.Read(context.Item).StackSize);
        }

        [TestMethod]
        public void RepeatedLootTemplatesShareAPlannedStack()
        {
            using var context = new LootFixture();
            EntityClassManager.Instance.GetItemClassInfo(context.Item).StackSize = 5;
            context.Item.StackSize = 2;
            context.Loot.LootItems[0] = new LootItem(
                context.Item, context.Client.Player.EntityId, 0);
            using (var database = context.Storage.Open())
            {
                database.ItemEntries.Single(entry => entry.ItemId == context.Item.Id).StackSize = 2;
                database.SaveChanges();
            }
            var second = context.Storage.AddUnownedLoot(2);
            context.Loot.LootItems.Add(new LootItem(
                second, context.Client.Player.EntityId, 0));

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.AreEqual(1, context.Storage.ReadInventory()
                .Count(row => row.InventoryType == (uint)InventoryType.Personal));
            Assert.AreEqual(4u, context.Storage.Read(context.Item).StackSize);
            Assert.IsNull(context.Storage.Read(second));
        }

        [TestMethod]
        public void AutoLootThresholdLeavesBetterItemsButPaysCreditsOnlyOnce()
        {
            using var context = new LootFixture();
            context.Item.ItemTemplate.QualityId = (int)LootQuality.Normal;

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket
                {
                    EntityId = context.Loot.EntityId,
                    AutoLootOnly = true
                });

            Assert.IsFalse(context.Item.OwnerId == context.Client.Player.Id);
            Assert.IsFalse(context.Loot.FullyLooted);
            Assert.AreEqual(107, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0, context.Loot.Credits);

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });
            Assert.IsTrue(context.Loot.FullyLooted);
            Assert.AreEqual(107, context.Client.Player.Credits[CurencyType.Credits]);
        }

        [TestMethod]
        public void FullInventoryCategoryRejectsItemsAndCreditsAsOneBatch()
        {
            using var context = new LootFixture();
            EntityClassManager.Instance.GetItemClassInfo(context.Item).StackSize = 1;
            for (uint slot = 50; slot < 100; slot++)
                context.Storage.AddAmmo(1, slot);

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.IsFalse(context.Loot.FullyLooted);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(7, context.Loot.Credits);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        [DataRow(50U)]
        [DataRow(250U)]
        public void CorruptPersonalInventorySlotRejectsTheWholeClaimBeforeWrites(uint corruptSlot)
        {
            using var context = new LootFixture();
            context.Storage.AddAmmo(1, 50);
            context.Storage.AddCorruptPersonalInventoryRow(corruptSlot);
            var saves = context.Storage.SaveAttempts;

            context.Manager.RequestLootAllFromCorpse(context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });

            Assert.AreEqual(saves, context.Storage.SaveAttempts);
            Assert.IsFalse(context.Loot.FullyLooted);
            Assert.AreEqual(100, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(7, context.Loot.Credits);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void QueuedLootPacketsSnapshotBeforeClaimAndRetirementMutation()
        {
            using var context = new LootFixture();
            var item = context.Loot.LootItems.Single();
            var info = new LootInfoPacket(context.Loot.LootItems);
            var corpse = new LootCorpsePacket(context.Client.Player.EntityId, context.Loot.LootItems);
            var canLoot = new CanLootItemsPacket(true, context.Loot.LootItems);
            var actorGot = new ActorGotLootPacket(context.Loot);
            var taken = new TakenInfoPacket(context.Client.Player.EntityId, context.Loot.LootItems);
            var got = new GotLootPacket(context.Loot);
            var expectedInfo = Encode(new LootInfoPacket(context.Loot.LootItems));
            var expectedCorpse = Encode(new LootCorpsePacket(
                context.Client.Player.EntityId, context.Loot.LootItems));
            var expectedCanLoot = Encode(new CanLootItemsPacket(true, context.Loot.LootItems));
            var expectedActorGot = Encode(new ActorGotLootPacket(context.Loot));
            var expectedTaken = Encode(new TakenInfoPacket(
                context.Client.Player.EntityId, context.Loot.LootItems));
            var expectedGot = Encode(new GotLootPacket(context.Loot));

            item.ItemTemplateId++;
            item.ItemClassId++;
            item.ItemQuantity++;
            item.ActorId++;
            item.PartyId++;
            context.Loot.AttachedTo++;
            context.Loot.Credits++;
            context.Loot.LootItems.Clear();

            CollectionAssert.AreEqual(expectedInfo, Encode(info));
            CollectionAssert.AreEqual(expectedCorpse, Encode(corpse));
            CollectionAssert.AreEqual(expectedCanLoot, Encode(canLoot));
            CollectionAssert.AreEqual(expectedActorGot, Encode(actorGot));
            CollectionAssert.AreEqual(expectedTaken, Encode(taken));
            CollectionAssert.AreEqual(expectedGot, Encode(got));
        }

        [TestMethod]
        public void CancellationWaitsForAnAtomicClaim()
        {
            using var context = new LootFixture();
            context.Manager.RequestCorpseLooting(context.Client,
                new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            context.Drain();
            AssertConcurrentOperationWaitsForClaim(context, () =>
                context.Manager.CancelCorpseLooting(context.Client,
                    new CancelCorpseLootingPacket { EntityId = context.Loot.EntityId }));
        }

        [TestMethod]
        public void ExpiryCheckWaitsForAnAtomicClaim()
        {
            using var context = new LootFixture();
            AssertConcurrentOperationWaitsForClaim(context, () =>
                context.Manager.MayDespawn(context.Map, context.Corpse,
                    context.Corpse.Controller.DeadTime));
        }

        [TestMethod]
        public void DepartureWaitsForAnAtomicClaim()
        {
            using var context = new LootFixture();
            AssertConcurrentOperationWaitsForClaim(context, () =>
                context.Manager.RemoveForOwner(context.Map, context.Client));
        }

        [TestMethod]
        public void MapDepartureReclaimsOwnedCorpseLootBeforeTransfer()
        {
            using var context = new LootFixture();
            var destination = new MapChannel
            {
                MapInfo = new MapInfo(1221, "destination", 1, 0),
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128
            };
            var manager = new MapChannelManager(context.Storage);
            manager.MapChannelArray[destination.MapInfo.MapContextId] = destination;

            Assert.IsTrue(manager.ChangeMap(
                context.Client,
                destination.MapInfo.MapContextId,
                Vector3.Zero,
                0));

            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
            Assert.IsNull(EntityManager.Instance.GetItem(context.Item.EntityId));
            Assert.IsNull(context.Storage.Read(context.Item));
        }

        private sealed class LootFixture : System.IDisposable
        {
            internal WeaponAmmoContext Storage { get; } = new(characterId: 42);
            internal Rasa.Game.Client Client => Storage.Client;
            internal MapChannel Map => Storage.World.Map;
            internal LootDispenserManager Manager { get; }
            internal Creature Corpse { get; }
            internal LootDispenser Loot { get; }
            internal Item Item { get; }

            internal LootFixture()
            {
                Client.Player.Credits[CurencyType.Credits] = 100;
                Client.Player.Position = Vector3.Zero;
                Client.Player.Attributes[Attributes.Health] =
                    new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                using (var database = Storage.Open())
                {
                    database.CharacterEntries.Single().Credit = 100;
                    database.SaveChanges();
                }
                Item = Storage.AddUnownedLoot(3);
                Corpse = new Creature
                {
                    EntityClass = EntityClasses.HumanBaseMale,
                    MapContextId = Map.MapInfo.MapContextId,
                    Position = Vector3.Zero,
                    State = CharacterState.Dead,
                    Faction = Factions.Bane,
                    AppearanceData = new(),
                    Attributes = new Dictionary<Attributes, ActorAttributes>
                    {
                        [Attributes.Health] = new(Attributes.Health, 100, 100, 0, 0, 0),
                        [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                    }
                };
                CellManager.Instance.AddToWorld(Map, Corpse);
                Loot = new LootDispenser
                {
                    Owner = Client.Player.EntityId,
                    AttachedTo = Corpse.EntityId,
                    IsLootable = true,
                    Credits = 7,
                    UnitOfWorkFactory = Storage
                };
                Loot.LootItems.Add(new LootItem(Item, Client.Player.EntityId, 0));
                Corpse.CorpseLootEntityId = Loot.EntityId;
                Map.LootDispensers.Add(Loot.EntityId, Loot);
                Manager = new LootDispenserManager(Storage, _ => 2);
                Drain();
            }

            internal List<Rasa.Packets.PythonPacket> Drain() => WorldTestContext.Drain(Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).ToList();

            public void Dispose()
            {
                Manager.RemoveForOwner(Map, Client);
                CellManager.Instance.RemoveCreatureFromWorld(Map, Corpse);
                Storage.Dispose();
            }
        }

        private static void AssertConcurrentOperationWaitsForClaim(
            LootFixture context,
            System.Action concurrentOperation)
        {
            using var saveEntered = new ManualResetEventSlim();
            using var releaseSave = new ManualResetEventSlim();
            context.Storage.BeforeSave = _ =>
            {
                saveEntered.Set();
                Assert.IsTrue(releaseSave.Wait(5000));
            };
            var claim = Task.Run(() => context.Manager.RequestLootAllFromCorpse(
                context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId }));
            Assert.IsTrue(saveEntered.Wait(5000));
            using var concurrentStarted = new ManualResetEventSlim();
            var concurrent = Task.Run(() =>
            {
                concurrentStarted.Set();
                concurrentOperation();
            });
            Assert.IsTrue(concurrentStarted.Wait(5000));
            var completedDuringClaim = concurrent.Wait(100);
            releaseSave.Set();
            Assert.IsTrue(Task.WaitAll(new[] { claim, concurrent }, 5000));
            Assert.IsFalse(completedDuringClaim);
        }

        private static byte[] Encode(Rasa.Packets.PythonPacket packet)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            return stream.ToArray();
        }
    }
}
