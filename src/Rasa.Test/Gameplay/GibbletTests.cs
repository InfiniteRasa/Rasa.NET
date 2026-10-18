using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Structures;
    using LootFixture = LootConsolidationTests.LootFixture;

    /// <summary>
    /// A corpse that goes with loot still on it leaves the loot on a gibblet where it lay
    /// (LootDispenserManager.LeaveGibblet, GibbletWorker): the client's Sys_Default_LootProxy, or
    /// LootableProxy_Mech for a mechanical creature, carrying the same loot dispenser.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GibbletTests
    {
        [TestMethod]
        public void TheLootStaysBehindOnAGibbletWhereTheCorpseLay()
        {
            using var context = new LootFixture();
            context.Corpse.Position = new Vector3(1, 0, 1);
            context.Corpse.Rotation = 1.25;
            context.Loot.OwnerClient = context.Client;
            context.Storage.World.AddClass(LootDispenserManager.GibbletClass);

            var gibblet = context.Manager.LeaveGibblet(context.Map, context.Corpse);

            Assert.IsNotNull(gibblet);
            Assert.AreEqual(LootDispenserManager.GibbletClass, gibblet.EntityClassId);
            Assert.AreEqual(DynamicObjectType.Gibblet, gibblet.DynamicObjectType);
            Assert.AreEqual(context.Corpse.Position, gibblet.Position);
            Assert.AreEqual(1.25, gibblet.Rotation, 1e-9);
            Assert.IsTrue(gibblet.IsInWorld);

            // The same dispenser, moved: off the corpse, onto the gibblet.
            Assert.AreSame(gibblet, context.Loot.Gibblet);
            Assert.AreEqual(gibblet.EntityId, context.Loot.AttachedTo);
            Assert.IsNull(context.Loot.Corpse);
            Assert.AreEqual(0UL, context.Corpse.CorpseLootEntityId);
            Assert.IsTrue(context.Loot.HasLoot);

            // The looter is shown the gibblet and told the loot is on it now.
            var sent = context.Drain();
            Assert.IsTrue(sent.OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == gibblet.EntityId &&
                packet.ClassId == LootDispenserManager.GibbletClass), "the gibblet");
            Assert.IsTrue(sent.OfType<AttachInfoPacket>().Any(), "the dispenser told what it is attached to");

            // The corpse going takes nothing with it now.
            CellManager.Instance.RemoveCreatureFromWorld(context.Map, context.Corpse);
            Assert.IsTrue(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
            Assert.IsNotNull(EntityManager.Instance.GetItem(context.Item.EntityId));
        }

        [TestMethod]
        public void AMechanicalCreatureLeavesTheScrapPile()
        {
            using var context = new LootFixture();
            context.Corpse.ExtraFlags.Add(CreatureFlag.Mechanical);

            Assert.AreEqual(LootDispenserManager.MechGibbletClass, context.Manager.LeaveGibblet(context.Map, context.Corpse).EntityClassId);
        }

        [TestMethod]
        public void AMachinaLeavesTheScrapPileToo()
        {
            // MACHINA alone, as the creature flags have it: not MECHANICAL as well.
            using var context = new LootFixture();
            context.Corpse.ExtraFlags.Add(CreatureFlag.Machina);

            Assert.AreEqual(LootDispenserManager.MechGibbletClass, context.Manager.LeaveGibblet(context.Map, context.Corpse).EntityClassId);
        }

        [TestMethod]
        public void NothingIsLeftWithNothingToLeave()
        {
            using var context = new LootFixture();
            context.Loot.FullyLooted = true;
            Assert.IsNull(context.Manager.LeaveGibblet(context.Map, context.Corpse), "cleared");
            context.Loot.FullyLooted = false;

            // Taken by a Filcher, eaten, revived: the dispenser is gone from the corpse.
            context.Corpse.CorpseLootEntityId = 0;
            Assert.IsNull(context.Manager.LeaveGibblet(context.Map, context.Corpse), "no dispenser");
            Assert.IsNull(context.Loot.Gibblet);
        }

        [TestMethod]
        public void TheGibbletIsLootedAsTheCorpseWasAndGoesOnceEmpty()
        {
            using var context = new LootFixture();
            var gibblet = context.Manager.LeaveGibblet(context.Map, context.Corpse);
            context.Drain();

            // Out of reach of the gibblet (2 m in the fixture), nothing.
            context.Client.Player.Position = new Vector3(2.5f, 0, 0);
            context.Manager.RequestCorpseLooting(context.Client, new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            Assert.AreEqual(0, context.Drain().OfType<LootCorpsePacket>().Count());

            context.Client.Player.Position = new Vector3(1.5f, 0, 0);
            context.Manager.RequestCorpseLooting(context.Client, new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            Assert.AreEqual(1, context.Drain().OfType<LootCorpsePacket>().Count(), "the window opens on it");

            // Walking over it picks up what the auto-loot takes, as over a corpse.
            context.Manager.RequestLootAllFromCorpse(context.Client, new RequestLootAllFromCorpsePacket { EntityId = context.Loot.EntityId });
            Assert.AreEqual(1, context.Client.Player.Inventory.PersonalInventory.Count(id => id == context.Item.EntityId));
            Assert.AreEqual(107, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsTrue(context.Loot.FullyLooted);
            context.Drain();

            context.Manager.GibbletWorker(context.Map, 1);

            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId), "the dispenser gone");
            Assert.IsFalse(gibblet.IsInWorld);
            Assert.IsNull(context.Loot.Gibblet);
            Assert.IsFalse(EntityManager.Instance.TryGetObject(gibblet.EntityId, out _), "the gibblet gone");
        }

        [TestMethod]
        public void AGibbletLiesAsLongAsALootableCorpseThenGoesWithWhatIsLeft()
        {
            using var context = new LootFixture();
            var gibblet = context.Manager.LeaveGibblet(context.Map, context.Corpse);

            context.Manager.GibbletWorker(context.Map, LootDispenserManager.LootableCorpseMs - 1);
            Assert.IsTrue(gibblet.IsInWorld, "still lying");

            context.Manager.GibbletWorker(context.Map, 1);
            Assert.IsFalse(gibblet.IsInWorld);
            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
            Assert.IsNull(EntityManager.Instance.GetItem(context.Item.EntityId), "the unclaimed item went with it");
            Assert.IsNull(context.Storage.Read(context.Item));
        }

        [TestMethod]
        public void SomeoneWithTheWindowOpenKeepsItLonger()
        {
            using var context = new LootFixture();
            var gibblet = context.Manager.LeaveGibblet(context.Map, context.Corpse);
            context.Manager.RequestCorpseLooting(context.Client, new RequestCorpseLootingPacket { EntityId = context.Loot.EntityId });
            Assert.AreEqual(context.Client.Player.EntityId, context.Loot.CurrentLooter);

            context.Manager.GibbletWorker(context.Map, LootDispenserManager.LootableCorpseMs);
            Assert.IsTrue(gibblet.IsInWorld);

            context.Manager.GibbletWorker(context.Map, LootDispenserManager.BeingLootedCorpseMs - LootDispenserManager.LootableCorpseMs);
            Assert.IsFalse(gibblet.IsInWorld);
        }

        [TestMethod]
        public void TheOwnerLeavingTakesTheGibbletWithTheLoot()
        {
            using var context = new LootFixture();
            var gibblet = context.Manager.LeaveGibblet(context.Map, context.Corpse);

            context.Manager.RemoveForOwner(context.Map, context.Client);

            Assert.IsFalse(gibblet.IsInWorld);
            Assert.IsFalse(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
        }

        [TestMethod]
        public void ACorpseWhoseTimeIsUpLeavesAGibbletAsItGoes()
        {
            using var context = new LootFixture();
            context.Corpse.Controller.DeadTime = LootDispenserManager.LootableCorpseMs;

            BehaviorManager.Instance.MapChannelThink(context.Map, 250); // one think (BehaviorManager.CreatureThinkInterval)

            Assert.IsFalse(context.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(context.Corpse)), "the corpse gone");
            Assert.IsNotNull(context.Loot.Gibblet, "its loot on a gibblet");
            Assert.IsTrue(context.Loot.Gibblet.IsInWorld);
            Assert.IsTrue(context.Map.LootDispensers.ContainsKey(context.Loot.EntityId));
            Assert.AreEqual(context.Corpse.Position, context.Loot.Gibblet.Position);
        }
    }
}
