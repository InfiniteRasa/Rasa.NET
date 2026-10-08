using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Loot;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions;

namespace Rasa.Test.Gameplay
{
    [TestClass]
    [DoNotParallelize]
    public class OrdinaryCorpseLootTests
    {
        [TestMethod]
        public void PublishedOrdinaryBindingMaterializesWithoutAnActiveMissionOrPerKillReads()
        {
            using var fixture = new Fixture();
            var calls = fixture.Content.Calls;
            fixture.Context.BeforeSave = _ =>
            {
                Assert.AreEqual(0, fixture.Context.Map.LootDispensers.Count);
                Assert.AreEqual(0UL, fixture.Corpse.CorpseLootEntityId);
                Assert.IsFalse(EntityManager.Instance.Items.Values.Any(item => item.ItemTemplateId == 901));
            };

            var loot = fixture.Manager.Create(fixture.Context.Client, fixture.Corpse);

            Assert.AreEqual(calls, fixture.Content.Calls);
            Assert.AreEqual(0, fixture.Context.Client.Player.Missions.Count);
            Assert.AreEqual(1, loot.LootItems.Count, "The bound ordinary profile must create its configured drop.");
            var drop = loot.LootItems.Single();
            Assert.AreEqual(901U, drop.ItemTemplateId, "Resolve native creature DbId, not its entity class or pool ID.");
            Assert.AreEqual(2U, drop.ItemQuantity);
            Assert.AreSame(drop.Item, EntityManager.Instance.GetItem(drop.EntityId));
            Assert.AreEqual(EntityType.Item, EntityManager.Instance.GetEntityType(drop.EntityId));
            Assert.AreEqual(fixture.Context.Client.Player.EntityId, drop.ActorId);
            Assert.AreEqual(0U, drop.PartyId);
            Assert.AreEqual(0UL, drop.ReservedFor);
            Assert.AreEqual(LootQuality.Normal, loot.LootQuality);
            using var verify = fixture.Context.Open();
            var row = verify.ItemEntries.AsNoTracking().Single();
            Assert.AreEqual(drop.Item.Id, row.ItemId);
            Assert.AreEqual(901U, row.ItemTemplateId);
            Assert.AreEqual(2U, row.StackSize);
            Assert.AreEqual(0, verify.CharacterInventoryEntries.Count());
        }

        [TestMethod]
        [DataRow(PartyLootMethod.FreeForAll)]
        [DataRow(PartyLootMethod.Rotation)]
        [DataRow(PartyLootMethod.DiceRoll)]
        public void OrdinaryProfilesKeepSquadOwnershipAndExistingRollReservations(PartyLootMethod method)
        {
            using var fixture = new Fixture();
            var member = fixture.Context.CreateAdditionalClient(2);
            member.Player.Attributes[Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0);
            using var party = new PartyScope(method, fixture.Context.Client, member);
            party.Party.LootRotation = 1;

            fixture.Manager.Loot(fixture.Context.Client, fixture.Corpse);

            var loot = fixture.Context.Map.LootDispensers[fixture.Corpse.CorpseLootEntityId];
            Assert.AreEqual(1, loot.LootItems.Count);
            var drop = loot.LootItems.Single();
            Assert.AreEqual(901U, drop.ItemTemplateId);
            var owner = method == PartyLootMethod.Rotation ? member : fixture.Context.Client;
            Assert.AreEqual(owner.Player.EntityId, loot.Owner);
            CollectionAssert.AreEquivalent(new[] { fixture.Context.Client.Player.EntityId, member.Player.EntityId },
                loot.CreditSharers.ToArray());
            if (method == PartyLootMethod.DiceRoll)
            {
                Assert.IsTrue(loot.CreditSharers.Contains(drop.ReservedFor));
                Assert.AreEqual(drop.ReservedFor, drop.ActorId);
                Assert.AreEqual(0U, drop.PartyId);
                Assert.IsTrue(drop.MayTake(drop.ReservedFor));
                Assert.IsFalse(drop.MayTake(drop.ReservedFor == member.Player.EntityId
                    ? fixture.Context.Client.Player.EntityId : member.Player.EntityId));
            }
            else
            {
                Assert.AreEqual(owner.Player.EntityId, drop.ActorId);
                Assert.AreEqual(method == PartyLootMethod.FreeForAll ? party.Party.Id : 0U, drop.PartyId);
                Assert.AreEqual(0UL, drop.ReservedFor);
                Assert.AreEqual(method == PartyLootMethod.FreeForAll, drop.MayTake(member.Player.EntityId == owner.Player.EntityId
                    ? fixture.Context.Client.Player.EntityId : member.Player.EntityId));
            }
        }

        [TestMethod]
        public void ExplicitAuthoredOverrideBypassesUninitializedCatalogAndStaysPersonal()
        {
            using var fixture = new Fixture();
            using var unpublished = new OrdinaryLootCatalogScope((OrdinaryLootCatalog)null);
            var member = fixture.Context.CreateAdditionalClient(2);
            using var party = new PartyScope(PartyLootMethod.Rotation, fixture.Context.Client, member);
            party.Party.LootRotation = 1;

            fixture.Manager.Loot(fixture.Context.Client, fixture.Corpse, new ActorGameplayPolicy
            {
                Loot = new AuthoredLootProfile(new[] { new LootDrop(902, 100, 1, 1) })
            });

            var loot = fixture.Context.Map.LootDispensers[fixture.Corpse.CorpseLootEntityId];
            Assert.AreEqual(fixture.Context.Client.Player.EntityId, loot.Owner);
            Assert.AreEqual(902U, loot.LootItems.Single().ItemTemplateId);
            Assert.AreEqual(0U, loot.LootItems.Single().PartyId);
            Assert.AreEqual(0, loot.CreditSharers.Count);
            Assert.AreEqual(LootQuality.Rare, loot.LootQuality);
        }

        [TestMethod]
        public void MissingManagedBindingRejectsWithoutPublishingOrFallingBack()
        {
            using var fixture = new Fixture();
            fixture.Corpse.DbId = 503;

            var error = Assert.ThrowsExactly<GameplayRejectionException>(() =>
                fixture.Manager.Create(fixture.Context.Client, fixture.Corpse));

            StringAssert.Contains(error.Message, "1220");
            StringAssert.Contains(error.Message, "503");
            fixture.AssertNoLoot();
        }

        [TestMethod]
        public void OrdinaryCreationCannotSilentlyUseAnUninitializedCatalog()
        {
            using var fixture = new Fixture();
            using var unpublished = new OrdinaryLootCatalogScope((OrdinaryLootCatalog)null);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                fixture.Manager.Create(fixture.Context.Client, fixture.Corpse));

            fixture.AssertNoLoot();
        }

        [TestMethod]
        [DataRow("foreign-map")]
        [DataRow("foreign-copy")]
        [DataRow("context")]
        [DataRow("identity")]
        public void ForeignOrNoncanonicalCorpseCannotBeMisclassifiedAsTheKillersSource(string change)
        {
            using var fixture = new Fixture();
            if (change == "context")
                fixture.Corpse.MapContextId = 1148;
            else if (change == "identity")
                EntityManager.Instance.Creatures[fixture.Corpse.EntityId] = null;
            else
                fixture.Corpse.RuntimeMapChannel = new MapChannel
                {
                    MapInfo = new MapInfo(change == "foreign-map" ? 1148U : 1220U, "foreign", 1556, 0)
                };

            Assert.ThrowsExactly<GameplayRejectionException>(() =>
                fixture.Manager.Create(fixture.Context.Client, fixture.Corpse));

            fixture.AssertNoLoot();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void EmptyOrdinaryOutcomesDoNotFallBackOrInventQuality(bool emptyProfile)
        {
            using var fixture = new Fixture(content =>
            {
                if (emptyProfile)
                {
                    content.Groups.Clear();
                    content.Items.Clear();
                }
                else
                    content.Groups[0].ChanceBasisPoints = 0;
            });

            var loot = fixture.Manager.Create(fixture.Context.Client, fixture.Corpse);

            Assert.AreEqual(0, loot.LootItems.Count);
            Assert.AreEqual(LootQuality.Junk, loot.LootQuality);
            Assert.AreEqual(1, loot.Credits);
            using var verify = fixture.Context.Open();
            Assert.AreEqual(0, verify.ItemEntries.Count());
        }

        [TestMethod]
        public void QualityUsesRankAcrossAllMaterializedItemsAndSupportsInclusiveQuantities()
        {
            using var fixture = new Fixture(content =>
            {
                content.Groups.Add(new OrdinaryLootGroupEntry { ProfileKey = "regular", GroupKey = "equipment",
                    Kind = LootGroupKind.Equipment, ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1, SortOrder = 2 });
                content.Items.Add(new LootItemEntry { ProfileKey = "regular", GroupKey = "equipment", ItemTemplateId = 902,
                    Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1 });
            }, next: (minimum, maximum) => maximum == 10000 ? 0 : maximum - 1);
            ItemManager.Instance.GetItemTemplateById(901).QualityId = (int)LootQuality.Junk;

            var loot = fixture.Manager.Create(fixture.Context.Client, fixture.Corpse);

            CollectionAssert.AreEquivalent(new uint[] { 901, 902 }, loot.LootItems.Select(item => item.ItemTemplateId).ToArray());
            Assert.AreEqual(4U, loot.LootItems.Single(item => item.ItemTemplateId == 901).ItemQuantity);
            Assert.AreEqual(LootQuality.Rare, loot.LootQuality, "Junk's numeric ID must not outrank Rare.");
            Assert.AreEqual(9, loot.Credits);
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(0, 9)]
        [DataRow(1, 1)]
        [DataRow(1, 9)]
        public void UnconfiguredOtherMapPreservesLegacyItemAndCreditBoundsUsingInjectedRng(int admission, int credits)
        {
            var draws = new List<(int Minimum, int Maximum)>();
            using var fixture = new Fixture(next: (minimum, maximum) =>
            {
                draws.Add((minimum, maximum));
                return maximum == 2 ? admission : maximum == 10 ? credits :
                    throw new AssertFailedException($"Unexpected legacy draw [{minimum}, {maximum}).");
            });
            fixture.Context.Map.MapInfo = new MapInfo(1148, "legacy", 1556, 0);
            fixture.Context.Client.Player.MapContextId = 1148;
            fixture.Corpse.MapContextId = 1148;

            var loot = fixture.Manager.Create(fixture.Context.Client, fixture.Corpse);

            CollectionAssert.AreEqual(new[] { (0, 2), (1, 10) }, draws.ToArray());
            Assert.AreEqual(credits, loot.Credits);
            Assert.AreEqual(admission, loot.LootItems.Count);
            if (admission != 0)
            {
                Assert.AreEqual(28U, loot.LootItems.Single().ItemTemplateId);
                Assert.AreEqual(3U, loot.LootItems.Single().ItemQuantity);
                Assert.AreSame(loot.LootItems.Single().Item, EntityManager.Instance.GetItem(loot.LootItems.Single().EntityId));
            }
            Assert.AreEqual(admission == 0 ? LootQuality.Junk : LootQuality.Normal, loot.LootQuality);
        }

        [TestMethod]
        public void ExplicitBindingOptsAnOtherwiseLegacyMapIntoOrdinaryProfiles()
        {
            using var fixture = new Fixture(content => content.Bindings.Add(new CreatureLootBindingEntry
            {
                MapContextId = 1148, CreatureId = 501, ProfileKey = "regular"
            }));
            fixture.Context.Map.MapInfo = new MapInfo(1148, "opted-in", 1556, 0);
            fixture.Context.Client.Player.MapContextId = 1148;
            fixture.Corpse.MapContextId = 1148;

            var loot = fixture.Manager.Create(fixture.Context.Client, fixture.Corpse);

            Assert.AreEqual(901U, loot.LootItems.Single().ItemTemplateId);
            Assert.AreEqual(2U, loot.LootItems.Single().ItemQuantity);
            Assert.AreEqual(LootQuality.Normal, loot.LootQuality);
        }

        [TestMethod]
        public void OrdinarySharedPickupCommitsTheRolledItemAndCreditsOnlyOnce()
        {
            using var fixture = new Fixture(next: (minimum, maximum) => maximum == 10 ? 9 : minimum);
            var member = fixture.Context.CreateAdditionalClient(2);
            member.Player.Attributes[Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0);
            using var party = new PartyScope(PartyLootMethod.FreeForAll, fixture.Context.Client, member);
            fixture.Manager.Loot(fixture.Context.Client, fixture.Corpse);
            var loot = fixture.Context.Map.LootDispensers[fixture.Corpse.CorpseLootEntityId];
            var drop = loot.LootItems.Single();
            var request = new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId };

            fixture.Manager.RequestLootAllFromCorpse(member, request);
            fixture.Manager.RequestLootAllFromCorpse(fixture.Context.Client, request);
            fixture.Manager.RequestLootAllFromCorpse(member, request);

            Assert.IsTrue(loot.FullyLooted);
            Assert.IsTrue(drop.Taken);
            Assert.AreEqual(1, member.Player.Inventory.PersonalInventory.Count(id => id == drop.EntityId));
            Assert.AreEqual(104, fixture.Context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(105, member.Player.Credits[CurencyType.Credits]);
            using var verify = fixture.Context.Open();
            Assert.AreEqual(1, verify.ItemEntries.Count());
            Assert.AreEqual(member.Player.Id, verify.CharacterInventoryEntries.Single().CharacterId);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MaterializationFailureRollsBackEveryItemAndReleasesStagedAndDispenserIds(bool authored)
        {
            using var fixture = new Fixture(content =>
            {
                content.Groups[0].MinimumRolls = 2;
                content.Groups[0].MaximumRolls = 2;
            });
            var freeBefore = FreeIds();
            var allocatorBefore = NextUnrecycledId();
            var saves = 0;
            fixture.Context.AfterSave = _ =>
            {
                if (++saves == 2)
                    throw new DbUpdateException("Injected second corpse item write failure.");
            };

            Assert.ThrowsExactly<DbUpdateException>(() =>
                fixture.Manager.Create(fixture.Context.Client, fixture.Corpse, authored ? new ActorGameplayPolicy
                {
                    Loot = new AuthoredLootProfile(new[] { new LootDrop(901, 100, 2, 2), new LootDrop(902, 100, 1, 1) })
                } : null));

            fixture.Context.AfterSave = null;
            fixture.AssertNoLoot();
            var allocated = Enumerable.Range(0, checked((int)(NextUnrecycledId() - allocatorBefore)))
                .Select(index => allocatorBefore + (ulong)index);
            Assert.IsTrue(allocated.All(id => FreeIds().Contains(id)), "Every newly allocated creation ID must be released.");
            Assert.IsTrue(freeBefore.All(id => FreeIds().Contains(id)), "Every staged recycled ID must be returned.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailedCreationPublicationRetiresPersistedItemsAndHoldsIdsUntilCleanupRetry(bool failCleanup)
        {
            using var fixture = new Fixture();
            LootDispenser attempted = null;
            Item[] items = null;
            var manager = CreationManager(fixture, loot =>
            {
                attempted = loot;
                items = loot.LootItems.Select(drop => drop.Item).ToArray();
                Assert.AreEqual(0, fixture.Context.Map.LootDispensers.Count);
                Assert.IsTrue(items.All(item => item.Id > 0 && ReferenceEquals(item, EntityManager.Instance.GetItem(item.EntityId))));
                if (failCleanup)
                    fixture.Context.AfterSave = _ => throw new DbUpdateException("Injected corpse retirement failure.");
                throw new GameplayRejectionException("Injected corpse creation publication failure.");
            });

            Assert.ThrowsExactly<GameplayRejectionException>(() => manager.Create(fixture.Context.Client, fixture.Corpse));

            Assert.IsNotNull(attempted);
            Assert.AreEqual(0, fixture.Context.Map.LootDispensers.Count);
            Assert.AreEqual(0UL, fixture.Corpse.CorpseLootEntityId);
            Assert.IsTrue(items.All(item => EntityManager.Instance.GetItem(item.EntityId) == null &&
                EntityManager.Instance.GetEntityType(item.EntityId) == 0));
            using (var verify = fixture.Context.Open())
                Assert.AreEqual(failCleanup ? 1 : 0, verify.ItemEntries.Count());
            var ids = items.Select(item => item.EntityId).Append(attempted.EntityId).ToArray();
            Assert.AreEqual(!failCleanup, ids.All(id => FreeIds().Contains(id)));
            if (failCleanup)
            {
                manager.RemoveForCreature(fixture.Context.Map, fixture.Corpse);
                Assert.IsTrue(ids.All(id => !FreeIds().Contains(id)));
                fixture.Context.AfterSave = null;
                manager.RemoveForCreature(fixture.Context.Map, fixture.Corpse);
                Assert.IsTrue(ids.All(id => FreeIds().Contains(id)));
                fixture.AssertNoLoot();
            }
        }

        [TestMethod]
        [DataRow("context")]
        [DataRow("map")]
        [DataRow("creature")]
        [DataRow("registered")]
        [DataRow("map-info")]
        public void SourceChangesAtTheCommitBoundaryRollBackOrdinaryCreation(string change)
        {
            using var fixture = new Fixture();
            fixture.Context.AfterSave = _ =>
            {
                if (change == "context")
                    fixture.Corpse.MapContextId = 1148;
                else if (change == "map")
                    fixture.Corpse.RuntimeMapChannel = new MapChannel { MapInfo = fixture.Context.Map.MapInfo };
                else if (change == "creature")
                    fixture.Corpse.DbId = 503;
                else if (change == "map-info")
                {
                    fixture.Context.Map.MapInfo = new MapInfo(1148, "changed", 1556, 0);
                    fixture.Context.Client.Player.MapContextId = 1148;
                    fixture.Corpse.MapContextId = 1148;
                }
                else
                    EntityManager.Instance.Creatures[fixture.Corpse.EntityId] = null;
            };

            Assert.ThrowsExactly<GameplayRejectionException>(() =>
                fixture.Manager.Create(fixture.Context.Client, fixture.Corpse));

            fixture.Context.AfterSave = null;
            fixture.AssertNoLoot();
        }

        [TestMethod]
        public void SourceChangeAfterPersistenceRetiresInsteadOfPublishingAStaleCorpse()
        {
            using var fixture = new Fixture();
            var manager = CreationManager(fixture, loot => fixture.Corpse.DbId = 503);

            Assert.ThrowsExactly<GameplayRejectionException>(() => manager.Create(fixture.Context.Client, fixture.Corpse));

            fixture.AssertNoLoot();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InvalidRuntimeStackRollsBackTheWholeGeneratedSequence(bool legacy)
        {
            using var fixture = new Fixture(content =>
            {
                content.Groups[0].MinimumRolls = 2;
                content.Groups[0].MaximumRolls = 2;
            }, next: (minimum, maximum) => maximum == 2 ? 1 : minimum);
            if (legacy)
            {
                fixture.Context.Map.MapInfo = new MapInfo(1148, "legacy", 1556, 0);
                fixture.Context.Client.Player.MapContextId = 1148;
                fixture.Corpse.MapContextId = 1148;
            }
            var template = ItemManager.Instance.GetItemTemplateById(legacy ? 28U : 901U);
            EntityClassManager.Instance.GetClassInfo(template.Class).ItemClassInfo = new ItemClassInfo(
                new ItemClassEntry { StackSize = 1 });

            Assert.ThrowsExactly<GameplayRejectionException>(() =>
                fixture.Manager.Create(fixture.Context.Client, fixture.Corpse));

            fixture.AssertNoLoot();
        }

        [TestMethod]
        public void ClaimPublicationHookStillFiresOnlyForCommittedPickup()
        {
            using var fixture = new Fixture();
            var calls = 0;
            var manager = new LootDispenserManager(fixture.Context, missionManager: fixture.Context.Manager,
                beforeItemPublication: _ => calls++, lootRoll: (minimum, maximum) => minimum);

            var loot = manager.Create(fixture.Context.Client, fixture.Corpse);
            Assert.AreEqual(0, calls);
            manager.RequestLootAllFromCorpse(fixture.Context.Client,
                new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.IsTrue(loot.FullyLooted);
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FullyMigratedHarnessesScopeTheValidatedProductionCatalog(bool wilderness)
        {
            using var unpublished = new OrdinaryLootCatalogScope((OrdinaryLootCatalog)null);
            if (wilderness)
            {
                using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create();
                Assert.IsNotNull(OrdinaryLootCatalog.Current.Resolve(1220, 3));
            }
            else
            {
                using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
                Assert.IsNotNull(OrdinaryLootCatalog.Current.Resolve(1220, 3));
            }
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = OrdinaryLootCatalog.Current);
        }

        [TestMethod]
        public void HistoricalWorldHarnessExplicitlyRetainsPreOrdinarySchemaBehavior()
        {
            using var unpublished = new OrdinaryLootCatalogScope((OrdinaryLootCatalog)null);
            using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261117000000_Hold_captive_pierre");
            var corpse = harness.Context.AddNpc(3);
            corpse.Npc = null;
            corpse.TargetCategory = TargetCategory.Hostile;
            corpse.State = CharacterState.Dead;
            corpse.Attributes[Attributes.Health].Current = 0;

            var loot = LootDispenserManager.Instance.Create(harness.Client, corpse);

            Assert.IsTrue(loot.LootItems.All(item => item.ItemTemplateId == 28 && item.ItemQuantity == 3));
            Assert.IsTrue(loot.Credits is >= 1 and <= 9);
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = OrdinaryLootCatalog.Current);
        }

        private static LootDispenserManager CreationManager(Fixture fixture, Action<LootDispenser> hook)
            => new(fixture.Context, missionManager: fixture.Context.Manager,
                lootRoll: (minimum, maximum) => minimum, beforeCorpsePublication: hook);

        private static HashSet<ulong> FreeIds() => new((HashSet<ulong>)typeof(EntityManager)
            .GetField("_freeEntityIdSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(EntityManager.Instance));

        private static ulong NextUnrecycledId() => (ulong)typeof(EntityManager)
            .GetField("_entityId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(EntityManager.Instance);

        private sealed class Fixture : IDisposable
        {
            internal MissionTestContext Context { get; } =
                MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());
            internal OrdinaryLootCatalogTests.Content Content { get; } = new();
            internal Creature Corpse { get; }
            internal LootDispenserManager Manager { get; }
            private readonly OrdinaryLootCatalogScope _catalog;
            private readonly System.Reflection.FieldInfo _items = typeof(ItemManager)
                .GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            private readonly object _previousItems;
            private readonly Dictionary<EntityClasses, ItemClassInfo> _itemClasses = new();

            internal Fixture(Action<OrdinaryLootCatalogTests.Content> configure = null, Func<int, int, int> next = null)
            {
                Content.Items[0].MinimumQuantity = 2;
                Content.Items[0].MaximumQuantity = 4;
                Content.Templates[1].QualityId = (byte)LootQuality.Rare;
                configure?.Invoke(Content);
                _catalog = new OrdinaryLootCatalogScope(Content.World);
                _previousItems = _items.GetValue(null);
                _items.SetValue(null, Activator.CreateInstance(typeof(ItemManager),
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null, new object[] { Context }, null));
                Context.AddRewardTemplate(901, 2001);
                Context.AddRewardTemplate(902, 2002);
                Context.AddRewardTemplate(28, 3147);
                ItemManager.Instance.GetItemTemplateById(901).QualityId = (int)LootQuality.Normal;
                ItemManager.Instance.GetItemTemplateById(902).QualityId = (int)LootQuality.Rare;
                ItemManager.Instance.GetItemTemplateById(28).QualityId = (int)LootQuality.Normal;
                foreach (var id in new uint[] { 901, 902, 28 })
                {
                    var classId = ItemManager.Instance.GetItemTemplateById(id).Class;
                    _itemClasses[classId] = EntityClassManager.Instance.GetClassInfo(classId).ItemClassInfo;
                }
                Context.Client.Player.Attributes[Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0);
                Corpse = Context.AddNpc(501);
                Corpse.Npc = null;
                Corpse.TargetCategory = TargetCategory.Hostile;
                Corpse.State = CharacterState.Dead;
                Corpse.Attributes[Attributes.Health].Current = 0;
                Manager = new LootDispenserManager(Context, missionManager: Context.Manager,
                    lootRoll: next ?? ((minimum, maximum) => minimum));
                Context.Drain();
            }

            internal void AssertNoLoot()
            {
                Assert.AreEqual(0, Context.Map.LootDispensers.Count);
                Assert.AreEqual(0UL, Corpse.CorpseLootEntityId);
                Assert.IsFalse(EntityManager.Instance.Items.Values.Any(item => item.ItemTemplateId is 901 or 902 or 28));
                using var verify = Context.Open();
                Assert.AreEqual(0, verify.ItemEntries.Count());
                Assert.AreEqual(0, verify.CharacterInventoryEntries.Count());
            }

            public void Dispose()
            {
                Context.BeforeSave = null;
                Context.AfterSave = null;
                foreach (var loot in Context.Map.LootDispensers.Values.ToArray())
                    Manager.RemoveForCreature(Context.Map, loot.Corpse);
                EntityManager.Instance.Creatures[Corpse.EntityId] = Corpse;
                _catalog.Dispose();
                foreach (var itemClass in _itemClasses)
                    EntityClassManager.Instance.GetClassInfo(itemClass.Key).ItemClassInfo = itemClass.Value;
                Context.Dispose();
                _items.SetValue(null, _previousItems);
            }
        }

        private sealed class PartyScope : IDisposable
        {
            private readonly Client[] _clients;
            internal Party Party { get; }

            internal PartyScope(PartyLootMethod method, params Client[] clients)
            {
                _clients = clients;
                Party = new Party(PartyManager.Instance.GetPartyId, clients[0].AccountEntry.Id,
                    clients.Select(client => new PartyMember(client)).ToList())
                {
                    LootMethod = method, LootThreshold = PartyLootThreshold.Prototype
                };
                PartyManager.Instance.Parties.Add(Party.Id, Party);
                foreach (var client in clients)
                    client.Player.PartyId = Party.Id;
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
    }
}
