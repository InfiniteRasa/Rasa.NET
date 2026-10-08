using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.World;

    // A weapon's clip is the count in memory; its row is written when the weapon is reloaded,
    // put away, changed for another or taken out of the drawer, when its holder leaves, and by a
    // sweep every fifteen seconds - not by a shot (WeaponClips). Against a real SQLite database.
    [TestClass]
    [DoNotParallelize]
    public class WeaponClipTests
    {
        private long _now;

        [TestInitialize]
        public void StartTheClock()
        {
            _now = 1_000_000;
            WeaponClips.Reset();
            WeaponClips.Now = () => _now;
        }

        [TestCleanup]
        public void GiveTheClockBack()
        {
            WeaponClips.Reset();
            WeaponClips.Now = () => System.Environment.TickCount64;
        }

        [TestMethod]
        public void AShotTakesItsRoundFromTheClipAndWritesNothing()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var saves = context.SaveAttempts;

            Fire(context, manager, 3);

            Assert.AreEqual(4u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(7u, context.Read(context.Weapon).AmmoCount, "the row is as it was");
            Assert.AreEqual(saves, context.SaveAttempts, "nothing was written");
            Assert.IsTrue(WeaponClips.IsUnsaved(context.Weapon));
            Assert.AreEqual(7u, WeaponClips.RowCountOf(context.Weapon));
            Assert.AreEqual(3, context.World.Map.QueuedMissiles.Count);
        }

        [TestMethod]
        public void TheClientIsToldTheClipAfterEachShot()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            WorldTestContext.Drain(context.Client);
            Fire(context, manager, 2);

            CollectionAssert.AreEqual(new[] { 6u, 5u }, WorldTestContext.Drain(context.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<WeaponAmmoInfoPacket>()
                .Select(info => info.AmmoInfo).ToArray());
        }

        [TestMethod]
        public void AShotIsFiredWhateverTheDatabaseIsDoing()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.BeforeSave = _ => throw new DbUpdateException("Injected failure.");

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));

            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(1, context.World.Map.QueuedMissiles.Count);
        }

        [TestMethod]
        public void TheSweepWritesWhatIsWaitingEveryFifteenSeconds()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var other = context.AddWeapon(12, 1);

            WeaponClips.Worker();       // the sweep's clock starts here
            Fire(context, manager, 2);
            WeaponClips.Spend(context.Client, other, 5, context);

            _now += WeaponClips.SweepMs - 1;
            WeaponClips.Worker();

            Assert.AreEqual(7u, context.Read(context.Weapon).AmmoCount, "not yet");
            Assert.AreEqual(2, WeaponClips.UnsavedCount);

            // Both, in one transaction.
            var writes = 0;
            context.BeforeSave = database => Assert.IsNotNull(database.Database.CurrentTransaction);
            context.AfterSave = _ => writes++;

            _now += 1;
            WeaponClips.Worker();

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(7u, context.Read(other).AmmoCount);
            Assert.AreEqual(0, WeaponClips.UnsavedCount);
            Assert.AreEqual(2, writes, "a write each, inside the one transaction");

            // And nothing more until something is fired again.
            var saves = context.SaveAttempts;
            _now += WeaponClips.SweepMs;
            WeaponClips.Worker();

            Assert.AreEqual(saves, context.SaveAttempts);
        }

        [TestMethod]
        public void AWeaponFiredAgainAfterASaveWaitsAgainFromWhatWasWritten()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);
            WeaponClips.SaveAll();

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.IsFalse(WeaponClips.IsUnsaved(context.Weapon));

            Fire(context, manager, 1);

            Assert.IsTrue(WeaponClips.IsUnsaved(context.Weapon));
            Assert.AreEqual(5u, WeaponClips.RowCountOf(context.Weapon));
            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);

            WeaponClips.SaveAll();

            Assert.AreEqual(4u, context.Read(context.Weapon).AmmoCount);
        }

        [TestMethod]
        public void AReloadWritesTheClipWithTheRoundsFromThePack()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var reserve = context.AddAmmo(10);

            Fire(context, manager, 3);      // 4 in the clip, 7 in its row

            manager.RequestWeaponReload(context.Client, true);
            manager.WeaponReload(context.World.Map.PerformRecovery.Single());

            Assert.AreEqual(14u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(14u, context.Read(context.Weapon).AmmoCount);
            Assert.IsFalse(WeaponClips.IsUnsaved(context.Weapon), "the reload wrote it");
            Assert.IsNull(context.Read(reserve), "the stack went into the clip");
        }

        [TestMethod]
        public void AReloadIsRefusedWhenTheRowIsNotWhatTheServerLeftThere()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var reserve = context.AddAmmo(10);

            Fire(context, manager, 3);
            SetRow(context, context.Weapon, 20);    // not the 7 the shots were fired from

            manager.RequestWeaponReload(context.Client, true);
            manager.WeaponReload(context.World.Map.PerformRecovery.Single());

            Assert.AreEqual(4u, context.Weapon.CurrentAmmo, "nothing loaded");
            Assert.AreEqual(10u, context.Read(reserve).StackSize, "nothing taken from the pack");
        }

        [TestMethod]
        public void PuttingTheWeaponAwayWritesItsClip()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);
            manager.RequestWeaponStow(context.Client);

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.IsFalse(WeaponClips.IsUnsaved(context.Weapon));
        }

        [TestMethod]
        public void TakingAnotherWeaponInHandWritesTheFirstOnesClip()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            context.AddWeapon(12, 1);

            Fire(context, manager, 2);

            // What follows the save in the arming - the slot kept for the character, the weapon
            // shown - is not this test's, and wants managers this fixture has not got.
            try
            {
                manager.RequestArmWeapon(context.Client, 1);
            }
            catch (System.Exception)
            {
            }

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.IsFalse(WeaponClips.IsUnsaved(context.Weapon));
        }

        [TestMethod]
        public void LeavingTheWorldWritesTheClips()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);

            // The removal's other steps log what they cannot do here and go on (RemovalStep).
            new MapChannelManager(null).RemovePlayer(context.Client, true);

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(0, WeaponClips.UnsavedCount);
        }

        [TestMethod]
        public void TakingItOutOfTheDrawerWritesItsClip()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);
            new InventoryManager(context).RemoveItemBySlot(context.Client, InventoryType.WeaponDrawerInventory, 0);

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.IsFalse(WeaponClips.IsUnsaved(context.Weapon));
            Assert.AreEqual(0UL, context.Client.Player.Inventory.WeaponDrawer[0]);

            // The other way out of a drawer slot.
            var second = context.AddWeapon(9, 2);
            WeaponClips.Spend(context.Client, second, 4, context);

            new InventoryManager(context).FreeSlotIndex(context.Client.Player, InventoryType.WeaponDrawerInventory, 2);

            Assert.AreEqual(5u, context.Read(second).AmmoCount);
        }

        [TestMethod]
        public void AHolderLeavingHasTheirClipsWrittenAndNobodyElses()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var theirs = context.AddWeapon(12, 1);
            var someoneElses = context.AddWeapon(12, 2);
            var stranger = context.World.CreateClient(factory: context);
            stranger.Player.Id = 77;

            Fire(context, manager, 2);
            WeaponClips.Spend(context.Client, theirs, 1, context);
            WeaponClips.Spend(stranger, someoneElses, 6, context);

            WeaponClips.SaveFor(context.Client);

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(11u, context.Read(theirs).AmmoCount);
            Assert.AreEqual(12u, context.Read(someoneElses).AmmoCount, "still waiting");
            Assert.AreEqual(1, WeaponClips.UnsavedCount);
        }

        [TestMethod]
        public void AWeaponMadeFromItsRowBeforeTheSaveHasTheClipItWasFiredDownTo()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 3);      // 4 in memory, 7 in the row, nothing written

            // Back in before any save: the inventory is made again from its rows.
            var again = context.Relog();
            var weapon = EntityManager.Instance.GetItem(again.Player.Inventory.WeaponDrawer[0]);

            Assert.AreNotSame(context.Weapon, weapon);
            Assert.AreEqual(4u, weapon.CurrentAmmo, "not the row's 7");
            Assert.IsTrue(WeaponClips.IsUnsaved(weapon), "and it is the one the count is written from");

            WeaponClips.SaveAll();

            Assert.AreEqual(4u, context.Read(weapon).AmmoCount);
            Assert.AreEqual(0, WeaponClips.UnsavedCount);
        }

        [TestMethod]
        public void ASaveThatFailsLeavesTheClipWaiting()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);
            context.BeforeSave = _ => throw new DbUpdateException("Injected failure.");

            WeaponClips.Save(context.Weapon);

            Assert.IsTrue(WeaponClips.IsUnsaved(context.Weapon));
            Assert.AreEqual(7u, context.Read(context.Weapon).AmmoCount);
            Assert.AreEqual(5u, context.Weapon.CurrentAmmo);

            context.BeforeSave = null;
            WeaponClips.SaveAll();

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
            Assert.IsFalse(WeaponClips.IsUnsaved(context.Weapon));
        }

        [TestMethod]
        public void ARowChangedUnderneathIsWrittenOverWithWhatWasFiredFrom()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);
            SetRow(context, context.Weapon, 20);

            WeaponClips.Save(context.Weapon);

            Assert.AreEqual(5u, context.Read(context.Weapon).AmmoCount);
        }

        [TestMethod]
        public void AWeaponWhoseRowIsGoneIsForgotten()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var kept = context.AddWeapon(12, 1);

            Fire(context, manager, 2);
            WeaponClips.Spend(context.Client, kept, 3, context);

            using (var database = context.Open())
            {
                database.ItemEntries.Remove(database.ItemEntries.Single(row => row.ItemId == context.Weapon.Id));
                database.SaveChanges();
            }

            WeaponClips.SaveAll();

            Assert.AreEqual(0, WeaponClips.UnsavedCount);
            Assert.IsNull(context.Read(context.Weapon));
            Assert.AreEqual(9u, context.Read(kept).AmmoCount, "the others are written all the same");
        }

        [TestMethod]
        public void ARowThatIsAnotherItemsByNowIsLeftAlone()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);

            Fire(context, manager, 2);

            // The weapon's row deleted and its id given to something else before the save.
            using (var database = context.Open())
            {
                var row = database.ItemEntries.Single(entry => entry.ItemId == context.Weapon.Id);

                row.ItemTemplateId = 28;
                row.AmmoCount = 0;
                database.SaveChanges();
            }

            WeaponClips.SaveAll();

            Assert.AreEqual(0u, context.Read(context.Weapon).AmmoCount, "not written over");
            Assert.AreEqual(0, WeaponClips.UnsavedCount);
        }

        [TestMethod]
        public void AWeaponWithNoRowOnlyLosesItsRounds()
        {
            using var context = new WeaponAmmoContext();
            var creatures = new Item { CurrentAmmo = 10 };      // a polymorphed player's: no row

            WeaponClips.Spend(context.Client, creatures, 4, context);

            Assert.AreEqual(6u, creatures.CurrentAmmo);
            Assert.AreEqual(0, WeaponClips.UnsavedCount);

            // Never more than the clip holds.
            WeaponClips.Spend(context.Client, creatures, 9, context);

            Assert.AreEqual(0u, creatures.CurrentAmmo);
        }

        private static void Fire(WeaponAmmoContext context, ManifestationManager manager, int shots)
        {
            for (var shot = 0; shot < shots; shot++)
            {
                context.Client.Player.NextShotAt = 0;
                Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client), $"shot {shot + 1}");
            }
        }

        private static void SetRow(WeaponAmmoContext context, Item weapon, uint count)
        {
            using var database = context.Open();

            database.ItemEntries.Single(row => row.ItemId == weapon.Id).AmmoCount = count;
            database.SaveChanges();
        }
    }
}
