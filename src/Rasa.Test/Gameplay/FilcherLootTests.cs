using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using LootFixture = LootConsolidationTests.LootFixture;

    /// <summary>
    /// A Filcher that loots a corpse carries what it took until it dies, and its corpse has it
    /// (LootDispenserManager.Steal, TakeOverCarried). It used to retire the corpse's dispenser:
    /// the loot was deleted, the Filcher's sparkle was the take's own effect and went, and a
    /// player who killed it found nothing of what it had taken.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FilcherLootTests
    {
        [TestMethod]
        public void AFilcherTakesTheStackablesOffTheCorpseAndCarriesThem()
        {
            using var context = new LootFixture();
            Watched(context);
            var filcher = Filcher(context);

            Assert.IsTrue(CreatureHabits.IsWorthStealing(context.Map, context.Corpse));
            Assert.IsTrue(context.Manager.Steal(context.Map, context.Corpse, filcher));

            // The ammunition is the Filcher's to carry: off the corpse, kept, not deleted.
            Assert.IsFalse(context.Loot.LootItems.Any(item => item.EntityId == context.Item.EntityId), "off the corpse");
            Assert.AreSame(context.Item, filcher.CarriedLoot.LootItems.Single().Item);
            Assert.IsNotNull(EntityManager.Instance.GetItem(context.Item.EntityId), "still an item");
            Assert.IsNotNull(context.Storage.Read(context.Item), "and still saved");

            // The corpse keeps what a Filcher does not take.
            Assert.AreEqual(7, context.Loot.Credits);
            Assert.IsTrue(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));

            Remove(context, filcher);
        }

        /// <summary>The robbed see the loot sparkle on the Filcher: a dispenser attached to it that their clients show as lootable.</summary>
        [TestMethod]
        public void TheRobbedSeeTheLootOnTheFilcher()
        {
            using var context = new LootFixture();
            Watched(context);
            var filcher = Filcher(context);

            context.Manager.Steal(context.Map, context.Corpse, filcher);
            var sent = context.Drain();

            Assert.IsTrue(sent.OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == filcher.CarriedLoot.EntityId), "the dispenser shown");
            Assert.IsTrue(sent.OfType<AttachInfoPacket>().Any(packet => packet.AttachedToEnityId == filcher.EntityId), "on the Filcher");
            Assert.IsTrue(sent.OfType<CanLootItemsPacket>().Any(packet => packet.CanLootItems), "sparkling, as lootable");
            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(filcher.CarriedLoot.EntityId), "and on no table: nobody loots a living Filcher");

            Remove(context, filcher);
        }

        /// <summary>With nothing else on it, the corpse goes as an empty one.</summary>
        [TestMethod]
        public void ACorpseLeftWithNothingLosesItsDispenser()
        {
            using var context = new LootFixture();
            Watched(context);
            context.Loot.Credits = 0;
            var filcher = Filcher(context);

            Assert.IsTrue(context.Manager.Steal(context.Map, context.Corpse, filcher));

            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
            Assert.AreEqual(0UL, context.Corpse.CorpseLootEntityId);
            Assert.IsNotNull(context.Storage.Read(context.Item), "what it took is not deleted with the dispenser");
            Assert.IsFalse(CreatureHabits.IsWorthStealing(context.Map, context.Corpse), "nothing more to take");

            Remove(context, filcher);
        }

        /// <summary>A squad roll's winnings or a mission's drop is one player's: a Filcher leaves it.</summary>
        [TestMethod]
        public void WhatIsOnePlayersStaysOnTheCorpse()
        {
            using var context = new LootFixture();
            Watched(context);
            context.Loot.LootItems.Single().ReservedFor = context.Client.Player.EntityId;
            var filcher = Filcher(context);

            Assert.IsFalse(CreatureHabits.IsWorthStealing(context.Map, context.Corpse));
            Assert.IsFalse(context.Manager.Steal(context.Map, context.Corpse, filcher));
            Assert.IsTrue(context.Loot.LootItems.Any(item => item.EntityId == context.Item.EntityId));
            Assert.IsNull(filcher.CarriedLoot);

            Remove(context, filcher);
        }

        /// <summary>Killed, its corpse has what it took on it with its own drops, for its killer; the sparkle comes off it.</summary>
        [TestMethod]
        public void KilledItsCorpseHasWhatItTook()
        {
            using var context = new LootFixture();
            Watched(context);
            var filcher = Filcher(context);
            context.Manager.Steal(context.Map, context.Corpse, filcher);
            var carriedId = filcher.CarriedLoot.EntityId;
            context.Drain();

            Die(filcher);

            // Nothing of its own rolled: what is on it is what it took.
            var manager = new LootDispenserManager(context.Storage, _ => 2, lootRoll: (min, max) => 0, ordinaryLootResolver: (map, creature) => null);
            var loot = manager.Create(context.Client, filcher);

            var stolen = loot.LootItems.Single(item => item.EntityId == context.Item.EntityId);
            Assert.AreSame(context.Item, stolen.Item);
            Assert.IsTrue(stolen.MayTake(context.Client.Player.EntityId), "the killer's");
            Assert.IsNull(filcher.CarriedLoot);
            Assert.IsTrue(context.Drain().OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == carriedId), "the sparkle off it");

            manager.RemoveForCreature(context.Map, filcher);
            Remove(context, filcher);
        }

        /// <summary>One that leaves the world alive takes its loot with it: deleted as a corpse's unclaimed loot is.</summary>
        [TestMethod]
        public void OneThatLeavesTheWorldTakesItWithIt()
        {
            using var context = new LootFixture();
            Watched(context);
            var filcher = Filcher(context);
            context.Manager.Steal(context.Map, context.Corpse, filcher);
            var carriedId = filcher.CarriedLoot.EntityId;
            context.Drain();

            context.Manager.DiscardCarried(context.Map, filcher);

            Assert.IsNull(filcher.CarriedLoot);
            Assert.IsNull(EntityManager.Instance.GetItem(context.Item.EntityId));
            Assert.IsNull(context.Storage.Read(context.Item), "its row deleted");
            Assert.IsTrue(context.Drain().OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == carriedId));

            Remove(context, filcher);
        }

        #region Fixture

        /// <summary>The corpse's owner in the map and shown the corpse, as a killer is.</summary>
        private static void Watched(LootFixture context)
        {
            context.Loot.OwnerClient = context.Client;
            context.Loot.Map = context.Map;
            context.Loot.Corpse = context.Corpse;

            if (!context.Map.ClientList.Contains(context.Client))
                context.Map.ClientList.Add(context.Client);

            context.Drain();
        }

        private static Creature Filcher(LootFixture context)
        {
            var filcher = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map,
                Position = new Vector3(2, 0, 0),
                State = CharacterState.Idle,
                TargetCategory = TargetCategory.Hostile,
                AppearanceData = new(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0),
                    [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(context.Map, filcher);
            return filcher;
        }

        private static void Die(Creature creature)
        {
            creature.State = CharacterState.Dead;
            creature.Attributes[Attributes.Health].Current = 0;
        }

        private static void Remove(LootFixture context, Creature filcher) =>
            CellManager.Instance.RemoveCreatureFromWorld(context.Map, filcher);

        #endregion
    }
}
