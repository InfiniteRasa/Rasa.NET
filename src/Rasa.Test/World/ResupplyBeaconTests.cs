using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// The Resupply Beacon (ACCOUNTREWARD_RESUPPLY 510, abilities.resupply) was refused as not
    /// implemented - and is sold on the promo vendor. Used now, it brings an AFS Resupply Bot to
    /// the player for 5 minutes: a vendor that sells every ammunition a weapon loads and repairs,
    /// that nothing fights. The beacon is kept for its hour's reuse.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ResupplyBeaconTests
    {
        private const int ResupplyAction = 510;
        private const uint BeaconTemplate = 130393;     // Resupply Beacon: 510 level 1
        private const uint VestTemplate = 13186;        // Motor Assist Armor Vest

        [TestMethod]
        public void TheBeaconBringsAVendorBotInFrontOfThePlayerAndIsKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var beacon = ToyTests.Grant(harness, BeaconTemplate);
            var manager = ToyTests.CreateManager(harness, ResupplyAction, 1, BeaconTemplate);
            LoadBotClass(harness);

            Assert.IsTrue(manager.TryGetAction(ActionId.AccountrewardResupply, 1, out var module, out var info));
            Assert.AreEqual(ResupplyBots.ResupplyModule, module);
            Assert.AreEqual(300, info.Get(AbilityProperty.Duration));

            try
            {
                harness.Drain();
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(ResupplyAction, 1, beacon.EntityId));
                Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any(), "no target wanted");
                ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.AccountrewardResupply));

                var bot = ResupplyBots.BotOf(player);
                Assert.IsNotNull(bot);
                Assert.AreEqual(ResupplyBots.BotClass, bot.EntityClass, "Creature_AccountReward_Resupply_Vendor_Bot");
                Assert.AreSame(bot, EntityManager.Instance.GetCreature(bot.EntityId), "in the world");

                var spot = player.Position + AbilityManager.FacingOf(player) * 3f;
                Assert.AreEqual(0f, Vector2.Distance(new(bot.Position.X, bot.Position.Z), new(spot.X, spot.Z)), 0.5f, "in front of them");
                Assert.AreEqual(TargetCategory.Friendly, bot.TargetCategory, "someone to talk to");

                Assert.AreEqual(ResupplyBots.VendorPackageId, bot.Npc.Vendor.VendorPackageId);
                CollectionAssert.AreEqual(ResupplyBots.Stock.ToList(), bot.Npc.Vendor.VendorItems);
                Assert.AreEqual(ResupplyBots.GreetingId, bot.Npc.GreetingId);
                Assert.IsNull(bot.Npc.Vendor.ItemPrice, "every item at its buy price");

                CollectionAssert.AreEqual(new[] { bot.EntityId },
                    harness.Drain().OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.AccountrewardResupply).Hits.Select(h => h.EntityId).ToArray());

                Assert.IsTrue(player.Inventory.PersonalInventory.Contains(beacon.EntityId), "kept");
                Assert.AreEqual(1U, beacon.StackSize);
                Assert.IsTrue(player.ActionReuseUntil[ActionId.AccountrewardResupply] - Environment.TickCount64 > 3_500_000, "an hour's reuse");

                // Straight away again: refused, and the bot that is out stays.
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(ResupplyAction, 1, beacon.EntityId));
                Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
                Assert.AreSame(bot, ResupplyBots.BotOf(player));
            }
            finally
            {
                ResupplyBots.Worker(map, long.MaxValue);
            }
        }

        [TestMethod]
        public void ItSellsAmmunitionAtItsBuyPriceAndRepairs()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var bot = Bring(harness);

            try
            {
                foreach (var template in ResupplyBots.Stock)
                    ToyTests.LoadTemplate(harness, template);

                Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1_000_000));
                var npcs = new NpcManager(harness.Context, harness.Manager);
                harness.Drain();

                npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = bot.EntityId });

                Assert.HasCount(1, harness.Drain().OfType<VendPacket>().ToList(), "the window opens");
                var stock = EntityManager.Instance.VendorItems[bot.EntityId];
                Assert.HasCount(ResupplyBots.Stock.Count, stock);

                // Elite cartridges.
                var elite = stock.Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplate.ItemTemplateId == 110906);
                var price = elite.ItemTemplate.BuyPrice;
                var credits = harness.Client.Player.Credits[CurencyType.Credits];

                npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket { VendorEntityId = bot.EntityId, ItemEntityId = elite.EntityId, Quantity = 10 });

                Assert.AreEqual(credits - 10 * price, harness.Client.Player.Credits[CurencyType.Credits]);
                Assert.AreEqual(10U, (uint)harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                    .Select(EntityManager.Instance.GetItem).Where(item => item.ItemTemplateId == 110906).Sum(item => item.StackSize));

                // And a worn vest put right.
                var vest = ToyTests.Grant(harness, VestTemplate);
                var max = Durability.MaxHitPointsOf(vest);
                vest.CurrentHitPoints = 10;
                var cost = Durability.RepairCost(vest);
                credits = harness.Client.Player.Credits[CurencyType.Credits];

                npcs.RequestVendorRepair(harness.Client, new RequestVendorRepairPacket { VendorEntityId = bot.EntityId, ItemEntitesId = { vest.EntityId } });

                Assert.AreEqual(max, vest.CurrentHitPoints);
                Assert.AreEqual(credits - cost, harness.Client.Player.Credits[CurencyType.Credits]);
            }
            finally
            {
                ResupplyBots.Worker(map, long.MaxValue);
            }
        }

        [TestMethod]
        public void NothingFightsItAndNothingHurtsIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var bot = Bring(harness);

            try
            {
                Assert.IsFalse(CreatureGameplayRules.CanParticipateInCombat(bot), "no aggro scan picks it out");
                Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(bot));

                ActorManager.Instance.Damage(map, bot, 500, harness.Client.Player);

                Assert.AreEqual(100, (int)bot.Attributes[Attributes.Health].Current);
                Assert.AreNotEqual(CharacterState.Dead, bot.State);
            }
            finally
            {
                ResupplyBots.Worker(map, long.MaxValue);
            }
        }

        [TestMethod]
        public void ItGoesAfterFiveMinutes()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var deployed = Environment.TickCount64;
            var bot = Bring(harness, deployed);

            ResupplyBots.Worker(map, deployed + 299_000);
            Assert.AreSame(bot, EntityManager.Instance.GetCreature(bot.EntityId), "still there at 4:59");

            ResupplyBots.Worker(map, deployed + 300_100);
            Assert.IsNull(EntityManager.Instance.GetCreature(bot.EntityId));
            Assert.IsNull(ResupplyBots.BotOf(harness.Client.Player));
        }

        [TestMethod]
        public void ItGoesWhenItsOwnerLeavesTheMap()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var bot = Bring(harness);

            player.MapChannel = new MapChannel();
            try
            {
                ResupplyBots.Worker(map);

                Assert.IsNull(EntityManager.Instance.GetCreature(bot.EntityId));
            }
            finally
            {
                player.MapChannel = map;
                ResupplyBots.Worker(map, long.MaxValue);
            }
        }

        [TestMethod]
        public void ASecondBotOfTheirsTakesThePlaceOfTheFirst()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var first = Bring(harness);

            try
            {
                var second = Bring(harness);

                // Its entity id is freed, and can be the second's.
                Assert.AreNotSame(first, EntityManager.Instance.GetCreature(first.EntityId), "taken away");
                Assert.IsFalse(map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(first)));
                Assert.AreSame(second, ResupplyBots.BotOf(harness.Client.Player));
            }
            finally
            {
                ResupplyBots.Worker(map, long.MaxValue);
            }
        }

        /// <summary>
        /// The counter a vendor was given is keyed by its entity id, which is freed as it goes: a
        /// bot's has to go with it, or the next creature given the id - a mob, or another vendor -
        /// was shown it.
        /// </summary>
        [TestMethod]
        public void ABotsCounterGoesWithIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var deployed = Environment.TickCount64;
            var bot = Bring(harness, deployed);

            foreach (var template in ResupplyBots.Stock)
                ToyTests.LoadTemplate(harness, template);

            new NpcManager(harness.Context, harness.Manager).RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = bot.EntityId });
            Assert.IsTrue(EntityManager.Instance.VendorItems.ContainsKey(bot.EntityId));

            ResupplyBots.Worker(map, deployed + 300_100);

            Assert.IsFalse(EntityManager.Instance.VendorItems.ContainsKey(bot.EntityId));
        }

        /// <summary>
        /// What it sells is every ammunition a weapon loads: one item for each Ammo_*_Grade class
        /// a weaponclass row names, and nothing else.
        /// </summary>
        [TestMethod]
        public void ItsStockIsEveryGradeOfAmmunitionAWeaponLoads()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var world = harness.WorldContext;

            var loaded = world.Set<WeaponClassEntry>().AsNoTracking().Select(w => w.AmmoClassId).Distinct().ToList();
            var names = world.Set<EntityClassEntry>().AsNoTracking().Where(c => loaded.Contains(c.Id)).ToDictionary(c => c.Id, c => c.ClassName);
            var graded = names.Where(n => n.Value.StartsWith("Ammo_") && n.Value.EndsWith("_Grade")).Select(n => n.Key).ToHashSet();
            var classOf = world.Set<ItemTemplateItemClassEntry>().AsNoTracking().ToList()
                .Where(link => ResupplyBots.Stock.Contains(link.ItemTemplateId)).ToDictionary(link => link.ItemTemplateId, link => link.ItemClass);

            Assert.HasCount(25, graded, "five types, five grades");
            CollectionAssert.AreEquivalent(graded.ToList(), ResupplyBots.Stock.Select(t => classOf[t]).ToList(), "one of each, and nothing else");
        }

        /// <summary>The player's bot, brought by the level as the client's tables have it.</summary>
        private static Creature Bring(BootcampRuntimeTestHarness.Harness harness, long now = 0)
        {
            LoadBotClass(harness);
            var manager = ToyTests.CreateManager(harness, ResupplyAction, 1, BeaconTemplate);
            Assert.IsTrue(manager.TryGetAction(ActionId.AccountrewardResupply, 1, out _, out var info));
            return ResupplyBots.Deploy(harness.BootcampMap, harness.Client.Player, info, now);
        }

        /// <summary>The server loads every class at start-up; the harness, what a test asks for.</summary>
        private static void LoadBotClass(BootcampRuntimeTestHarness.Harness harness)
        {
            ToyTests.LoadClass(harness, (uint)ResupplyBots.BotClass);
            Assert.IsTrue(EntityClassManager.Instance.LoadedEntityClasses[ResupplyBots.BotClass].Augmentations
                .Contains(AugmentationType.Vendor), "a vendor's class");
        }
    }
}
