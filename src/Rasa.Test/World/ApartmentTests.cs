extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Config;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.Char.Apartment;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;

    /// <summary>
    /// Apartments (Managers.Apartments) with the character database behind them: going in and
    /// out, who may, the items and their rules, plot changes, the copies' lives, a login inside
    /// and a death inside.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ApartmentTests
    {
        private const uint Arena = 2259;        // adv_wargame_indoorarena: plots 1, 4, 5, 11
        private const uint Nexus = 1737;        // test_nexus2: plots 2, 6, 7
        private const uint FloorLamp = 8075, Lighting2 = 8076, Chair = 8064, Footlocker = 21030, Station = 9595, Bed = 29690, Pod = 11258;

        private static readonly Vector3 Outside = new Vector3(30, 2, 40);

        private ApartmentConfig _config;

        [TestInitialize]
        public void KeepTheConfig() => _config = Apartments.Config;

        [TestCleanup]
        public void RestoreTheConfig() => Apartments.Config = _config;

        #region Going in and out

        [TestMethod]
        public void TheFirstApartmentIsMadeOnTheFirstPlotAndEnteredAtItsEntry()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Command(owner, ".apartment");

            var copy = owner.PendingTransfer.DestinationMap;
            var plot = f.Apartments.Catalogue.PlotOf(1);

            Assert.IsTrue(copy.IsApartment);
            Assert.IsTrue(copy.IsCopy);
            Assert.IsFalse(copy.IsPrivateInstance, "nobody's to the rest of the server");
            Assert.AreEqual(Arena, copy.MapInfo.MapContextId);
            Assert.AreNotEqual(1u, copy.InstanceId);
            Assert.AreEqual(plot.Entry, owner.PendingTransfer.DestinationPosition);
            Assert.AreEqual(0, copy.SpawnPools.Count + copy.DynamicObjects.Count, "blank");

            var row = f.Repository(r => r.FindByOwner(ApartmentEntry.OwnerKindCharacter, owner.Player.Id));
            var back = f.Repository(r => r.GetReturn(owner.Player.Id));

            Assert.AreEqual(1u, row.PlotId);
            Assert.AreEqual(copy.ApartmentId, row.Id);
            Assert.AreEqual(f.World.Map.MapInfo.MapContextId, back.MapContextId, "the way back is where they stood");
            Assert.AreEqual(Outside.X, back.PosX, 0.01);

            f.Arrive(owner);
            f.Apartments.Worker();

            Assert.AreSame(copy, owner.Player.MapChannel);
            StringAssert.Contains(f.Said(owner).Last(), "Welcome home");

            // .apartment again inside: no second trip.
            f.Command(owner, ".apartment");
            StringAssert.Contains(f.Said(owner).Last(), "You are in");
            Assert.IsNull(owner.PendingTransfer);
        }

        [TestMethod]
        public void LeaveGoesBackAndTheReturnPointIsLetGoOnceOutside()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            f.Command(owner, ".leave");

            Assert.AreSame(f.World.Map, owner.PendingTransfer.DestinationMap);
            Assert.AreEqual(Outside, owner.PendingTransfer.DestinationPosition);

            f.Arrive(owner);
            Assert.IsNotNull(f.Repository(r => r.GetReturn(owner.Player.Id)), "kept until they are seen outside");
            f.Apartments.Worker();
            Assert.IsNull(f.Repository(r => r.GetReturn(owner.Player.Id)));

            f.Command(owner, ".leave");
            StringAssert.Contains(f.Said(owner).Last(), "not in an apartment");
        }

        [TestMethod]
        public void NobodyTravelsDeadFightingOrFromAPrivateInstance()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            owner.Player.InCombat = true;
            f.Command(owner, ".apartment");
            StringAssert.Contains(f.Said(owner).Last(), "combat");

            owner.Player.InCombat = false;
            var alive = owner.Player.State;
            owner.Player.State = CharacterState.Dead;
            f.Command(owner, ".apartment");
            StringAssert.Contains(f.Said(owner).Last(), "dead");

            owner.Player.State = alive;
            f.World.Map.IsPrivateInstance = true;
            f.Command(owner, ".apartment");
            StringAssert.Contains(f.Said(owner).Last(), "from here");
            f.World.Map.IsPrivateInstance = false;

            Apartments.Config = new ApartmentConfig { Enabled = false };
            f.Command(owner, ".apartment");
            StringAssert.Contains(f.Said(owner).Last(), "not open");
            Assert.IsNull(owner.PendingTransfer);
        }

        [TestMethod]
        public void TheCopyStaysWhileAnyoneIsInItAndClosesAfterTheIdleTime()
        {
            using var f = new Fixture();
            Apartments.Config = new ApartmentConfig { IdleCloseSeconds = 60 };
            var owner = f.Player("Ann");
            var guest = f.Player("Bob");

            f.Into(owner);
            f.Command(owner, ".decor place floor lamp");
            var copy = owner.Player.MapChannel;
            var lamp = f.Live(owner).Items.Values.Single().Object;

            Assert.IsTrue(EntityManager.Instance.TryGetObject(lamp.EntityId, out _));

            f.Command(owner, ".apartment invite Bob");
            f.Command(guest, ".apartment visit Ann");
            f.Arrive(guest);

            Assert.AreSame(copy, guest.Player.MapChannel, "the visitor is in the owner's copy, the one copy of it");
            Assert.AreEqual(1, f.Maps.ApartmentCopies().Count);

            // The owner goes; the visitor holds it open.
            f.Out(owner);
            f.Now += 3600_000;
            f.Apartments.Worker();
            Assert.AreEqual(1, f.Maps.ApartmentCopies().Count);

            // Empty: it stands for the idle time, then closes, its items with it.
            f.Out(guest);
            f.Apartments.Worker();
            f.Now += 59_000;
            f.Apartments.Worker();
            Assert.AreEqual(1, f.Maps.ApartmentCopies().Count, "not before its minute");
            f.Now += 1_000;
            f.Apartments.Worker();
            Assert.AreEqual(0, f.Maps.ApartmentCopies().Count);
            Assert.IsFalse(EntityManager.Instance.TryGetObject(lamp.EntityId, out _), "its items are out of the world");
            Assert.IsNull(f.Maps.FindByContextAndInstance(Arena, copy.InstanceId));

            // Opened again, its items are put down again from the database.
            f.Into(owner);
            var again = f.Live(owner).Items.Values.Single();

            Assert.AreNotSame(copy, owner.Player.MapChannel);
            Assert.AreEqual(FloorLamp, (uint)again.Object.EntityClassId);
            Assert.AreEqual(lamp.Position, again.Object.Position);
        }

        [TestMethod]
        public void NoMoreCopiesOpenThanMayAndTheClientIsToldItsOwnMessage()
        {
            using var f = new Fixture();
            Apartments.Config = new ApartmentConfig { MaxOpenCopies = 1 };
            var first = f.Player("Ann");
            var second = f.Player("Bob");

            f.Into(first);
            f.Command(second, ".apartment");

            Assert.IsNull(second.PendingTransfer);
            CollectionAssert.Contains(f.Told(second), PlayerMessage.PmFailedToCreateApartment);
            StringAssert.Contains(f.Said(second).Last(), "Too many apartments");
        }

        #endregion

        #region Who may come in

        [TestMethod]
        public void OnlyTheInvitedComeInAndOnlyThoseWhoMayBuildBuild()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");
            var guest = f.Player("Bob");

            f.Command(guest, ".apartment visit Ann");
            StringAssert.Contains(f.Said(guest).Last(), "has no apartment");

            f.Into(owner);
            f.Command(guest, ".apartment visit Ann");
            StringAssert.Contains(f.Said(guest).Last(), "hasn't invited you");
            Assert.IsNull(guest.PendingTransfer);

            f.Command(owner, ".apartment invite Bob");
            StringAssert.Contains(f.Said(guest).Last(), "invited you");
            f.Command(guest, ".apartment visit ann");
            f.Arrive(guest);
            f.Apartments.Worker();
            StringAssert.Contains(f.Said(guest).Last(), "You are in Ann's apartment");

            f.Command(guest, ".decor place floor lamp");
            StringAssert.Contains(f.Said(guest).Last(), "may not build");

            f.Command(owner, ".apartment invite Bob build");
            f.Command(guest, ".decor place floor lamp");
            StringAssert.Contains(f.Said(guest).Last(), "Placed #1 Floor Lamp");

            f.Command(guest, ".apartment list");
            StringAssert.Contains(f.Said(guest).Last(), "Ann's, you may build");

            // Uninvited, they are sent back within the second.
            f.Command(owner, ".apartment uninvite Bob");
            f.Apartments.Worker();
            Assert.AreSame(f.World.Map, guest.PendingTransfer?.DestinationMap);
            StringAssert.Contains(f.Said(guest).Last(), "no longer be in");
        }

        [TestMethod]
        public void APasscodeLetsAnyoneInWrongOnesLockTheAccountOutAndANewOneSendsTheHoldersOut()
        {
            using var f = new Fixture();
            Apartments.Config = new ApartmentConfig { PasscodeTries = 3, PasscodeLockoutSeconds = 30 };
            var owner = f.Player("Ann");
            var guest = f.Player("Bob");

            f.Into(owner);
            f.Command(owner, ".apartment passcode Tulip");
            var id = owner.Player.MapChannel.ApartmentId;

            StringAssert.Contains(f.Said(owner).Last(), $".apartment code tulip-{id}");
            Assert.AreEqual(Apartments.PasscodeHash(id, "tulip"), f.Repository(r => r.Get(id)).PasscodeHash, "the hash is kept, not the word");

            for (var i = 0; i < 3; i++)
                f.Command(guest, $".apartment code rose-{id}");

            f.Command(guest, $".apartment code tulip-{id}");
            StringAssert.Contains(f.Said(guest).Last(), "Too many wrong passcodes");
            Assert.IsNull(guest.PendingTransfer);

            f.Now += 30_000;
            f.Command(guest, $".apartment code TULIP-{id}");
            f.Arrive(guest);
            f.Apartments.Worker();
            Assert.AreSame(owner.Player.MapChannel, guest.Player.MapChannel);

            // A new code: those who came in by the old one must leave.
            f.Command(owner, ".apartment passcode lily1");
            f.Apartments.Worker();
            Assert.AreSame(f.World.Map, guest.PendingTransfer?.DestinationMap);
        }

        [TestMethod]
        public void TheOwnersFriendsComeInWhileFriendsMay()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");
            var friend = f.Player("Bob");

            f.Apartments.IsFriendOf = (ownerAccount, visitorAccount) => ownerAccount == owner.AccountEntry.Id && visitorAccount == friend.AccountEntry.Id;
            f.Into(owner);

            f.Command(friend, ".apartment visit Ann");
            Assert.IsNull(friend.PendingTransfer);

            f.Command(owner, ".apartment friends on");
            f.Command(friend, ".apartment visit Ann");
            f.Arrive(friend);
            Assert.AreSame(owner.Player.MapChannel, friend.Player.MapChannel);

            f.Command(owner, ".apartment friends off");
            f.Live(owner).Friends.Clear();
            f.Apartments.Worker();
            Assert.AreSame(f.World.Map, friend.PendingTransfer?.DestinationMap);
        }

        #endregion

        #region Items

        [TestMethod]
        public void AnItemIsPlacedInFrontOfThePlayerSavedAndShownAsTargetableScenery()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            WorldTestContext.Drain(owner);
            f.Command(owner, ".decor place 8075 3");

            var live = f.Live(owner);
            var placed = live.Items[1];
            var expected = owner.Player.Position + Apartments.Forward(owner.Player.Rotation) * 3;

            StringAssert.Contains(f.Said(owner).Last(), "Placed #1 Floor Lamp (Lighting 1/24, 1/150 items)");
            Assert.AreEqual(0, Vector3.Distance(expected, placed.Object.Position), 0.01);
            Assert.AreEqual(Apartments.Wrap(owner.Player.Rotation + Math.PI), placed.Object.Rotation, 1e-6, "facing the player");
            Assert.AreEqual(DynamicObjectType.ApartmentItem, placed.Object.DynamicObjectType);

            var row = f.Repository(r => r.GetItems(live.Row.Id)).Single();

            Assert.AreEqual(1u, row.Number);
            Assert.AreEqual(owner.Player.Id, row.PlacedBy);
            Assert.AreEqual(expected.Z, row.PosZ, 0.01);

            var create = Sent<CreatePhysicalEntityPacket>(owner).Single(p => p.EntityId == placed.Object.EntityId);
            Assert.IsTrue(create.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable);
            Assert.AreEqual(1, create.EntityData.OfType<WorldLocationDescriptorPacket>().Count());
            Assert.AreEqual(0, create.EntityData.OfType<BodyAttributesPacket>().Count(), "scale 1 is not sent");

            f.Command(owner, ".decor scale 2");
            create = Sent<CreatePhysicalEntityPacket>(owner).Single();
            Assert.AreEqual(2.0, create.EntityData.OfType<BodyAttributesPacket>().Single().Scale, 1e-6);
        }

        [TestMethod]
        public void TheChosenItemIsMovedTurnedSizedCopiedDeletedAndUndone()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            f.Command(owner, ".decor place floor lamp");
            var live = f.Live(owner);
            Vector3 At(uint n) => live.Items[n].Object.Position;
            var start = At(1);

            f.Command(owner, ".decor move 1 0.5 -2");
            Assert.AreEqual(start + new Vector3(1, 0.5f, -2), At(1));
            StringAssert.Contains(f.Said(owner).Last(), "Moved #1 Floor Lamp");

            f.Command(owner, ".decor turn 90");
            Assert.AreEqual(Apartments.Wrap(owner.Player.Rotation + Math.PI + Math.PI / 2), live.Items[1].Row.Rotation, 1e-6);

            f.Command(owner, ".decor undo");
            Assert.AreEqual(Apartments.Wrap(owner.Player.Rotation + Math.PI), live.Items[1].Row.Rotation, 1e-6, "the turn undone");

            f.Command(owner, ".decor scale 9");
            StringAssert.Contains(f.Said(owner).Last(), "goes from scale 0.5 to 3");

            f.Command(owner, ".decor face");
            Assert.AreEqual(Apartments.YawTowards(At(1), owner.Player.Position), live.Items[1].Row.Rotation, 1e-6);

            f.Command(owner, ".decor here");
            Assert.AreEqual(owner.Player.Position, At(1));

            f.Command(owner, ".decor copy 4");
            Assert.AreEqual(2, live.Items.Count);
            StringAssert.Contains(f.Said(owner).Last(), "Copied as #2 Floor Lamp");

            // The chosen one is the copy now; by number back to the first.
            f.Command(owner, ".decor sel 1");
            f.Command(owner, ".decor del");
            Assert.IsFalse(live.Items.ContainsKey(1));
            Assert.AreEqual(1, f.Repository(r => r.GetItems(live.Row.Id)).Count);

            f.Command(owner, ".decor undo");
            Assert.AreEqual(2, live.Items.Count, "the deleted one is back");
            Assert.IsTrue(live.Items.ContainsKey(3), "under a new number");
            Assert.AreEqual(owner.Player.Position, At(3));

            f.Command(owner, ".decor near 50");
            StringAssert.Contains(f.Said(owner).Last(), "#3 Floor Lamp 0.0 m");
        }

        [TestMethod]
        public void TheTargetIsTheItemMeant()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            f.Command(owner, ".decor place floor lamp");
            f.Command(owner, ".decor place 8076");
            var live = f.Live(owner);

            owner.Player.Target = live.Items[1].Object.EntityId;
            f.Command(owner, ".decor del");

            Assert.IsFalse(live.Items.ContainsKey(1), "the targeted one, not the last placed");
            Assert.IsTrue(live.Items.ContainsKey(2));
        }

        [TestMethod]
        public void TheBuildBoxBudgetCapsReachAndRateAreHeldTo()
        {
            using var f = new Fixture();
            Apartments.Config = new ApartmentConfig { ItemBudget = 3, EditBurst = 100 };
            var owner = f.Player("Ann");

            f.Into(owner);

            // Plot 1's box is 32 m across; the entry is 2 m inside its south wall, facing in.
            f.Command(owner, ".decor place floor lamp 40");
            StringAssert.Contains(f.Said(owner).Last(), "within 30 metres");
            f.Command(owner, ".decor place floor lamp 29.9");
            StringAssert.Contains(f.Said(owner).Last(), "outside the apartment's build box");
            f.Command(owner, ".decor place floor lamp 27");
            StringAssert.Contains(f.Said(owner).Last(), "Placed #1");
            f.Command(owner, ".decor push 3");
            StringAssert.Contains(f.Said(owner).Last(), "outside the apartment's build box");
            f.Command(owner, ".decor up 20");
            StringAssert.Contains(f.Said(owner).Last(), "outside the apartment's build box");

            // Medical: one at a budget of 3 (plot 1's own cap is 1).
            f.Command(owner, ".decor place 29690");
            f.Command(owner, ".decor place 11258");
            StringAssert.Contains(f.Said(owner).Last(), "holds 1 Medical items");

            f.Command(owner, ".decor place 8076");
            StringAssert.Contains(f.Said(owner).Last(), "holds 1 Lighting items", "24 at 150 is 1 at 3");
            f.Command(owner, ".decor place 8064");
            f.Command(owner, ".decor place 8064");
            StringAssert.Contains(f.Said(owner).Last(), "holds 3 items, and has them all");
            Assert.AreEqual(3, f.Live(owner).Items.Count);

            // The rate: 2 at once, then one every two seconds.
            Apartments.Config = new ApartmentConfig { EditBurst = 2, EditsPerSecond = 0.5 };
            f.Command(owner, ".decor turn 10");
            f.Command(owner, ".decor turn 10");
            f.Command(owner, ".decor turn 10");
            StringAssert.Contains(f.Said(owner).Last(), "Slow down");
            f.Now += 2_000;
            f.Command(owner, ".decor turn 10");
            StringAssert.Contains(f.Said(owner).Last(), "Turned");
        }

        [TestMethod]
        public void WorkingItemsAreAFootlockerAndACraftingStationAndAMedbayIsWhereTheDeadGetUp()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            var copy = owner.Player.MapChannel;
            var plot = f.Apartments.Catalogue.PlotOf(1);

            Assert.AreEqual(plot.Entry, InstanceEntrances.Of(copy, owner.Player).Value.Position, "with no medbay, the entry");

            f.Command(owner, ".decor place 21030");
            f.Command(owner, ".decor place 9595 5");
            f.Command(owner, ".decor place 29690 8");

            var live = f.Live(owner);

            Assert.AreEqual(DynamicObjectType.Lockbox, live.Items[1].Object.DynamicObjectType, "used as a world footlocker is: the vault");
            Assert.AreEqual(DynamicObjectType.Kraftwerks, live.Items[2].Object.DynamicObjectType);
            Assert.AreEqual(Apartments.StationIdOf(live.Items[2].Row), live.Items[2].Object.KraftwerksId);
            Assert.AreSame(live.Items[2].Object, copy.Kraftwerks[live.Items[2].Object.KraftwerksId]);
            Assert.IsTrue(KraftwerksManager.Instance.TryGet(live.Items[2].Object.KraftwerksId, out _));

            var bed = live.Items[3].Row;
            var entrance = InstanceEntrances.Of(copy, owner.Player).Value;

            Assert.AreNotEqual(plot.Entry, entrance.Position);
            Assert.IsTrue(Vector3.Distance(entrance.Position, new Vector3((float)bed.PosX, (float)bed.PosY, (float)bed.PosZ)) < 4, "beside the bed");

            // Deleted, the station goes off the crafting table.
            var station = live.Items[2].Object.KraftwerksId;
            f.Command(owner, ".decor sel 2");
            f.Command(owner, ".decor del");
            Assert.IsFalse(KraftwerksManager.Instance.TryGet(station, out _));
            Assert.IsFalse(copy.Kraftwerks.ContainsKey(station));
        }

        #endregion

        #region Plots

        [TestMethod]
        public void RepackKeepsWhatFitsInTheOrderPlacedAndPacksTheRest()
        {
            var catalogue = ApartmentCatalogueTests.Shipped();
            var arenaFloor = catalogue.PlotOf(11);  // 120 m across
            var loft = catalogue.PlotOf(1);         // 32 m across, Medical 1
            ApartmentItemEntry Item(uint id, uint classId, float x, long at) => new ApartmentItemEntry { Id = id, Number = id, ClassId = classId, PosX = x, PosY = 0.05, PosZ = 0, Scale = 1, PlacedAt = at };
            var items = new[] { Item(1, FloorLamp, 0, 10), Item(2, FloorLamp, 40, 20), Item(3, Bed, 5, 30), Item(4, Pod, -5, 25), Item(5, 1, 0, 40) };

            var onFloor = Apartments.Repack(catalogue, arenaFloor, items, 150);
            CollectionAssert.AreEqual(new[] { false, false, true, false, true }, items.Select(i => onFloor[i.Id]).ToArray(),
                "all but the unknown class and the bed: the pod, placed first, has the one Medical place left at 150 of 800");

            var inLoft = Apartments.Repack(catalogue, loft, items, 150);
            Assert.IsFalse(inLoft[1]);
            Assert.IsTrue(inLoft[2], "outside the loft's box");
            Assert.IsFalse(inLoft[4], "the pod was placed before the bed: it keeps the one medical place");
            Assert.IsTrue(inLoft[3]);

            var tight = Apartments.Repack(catalogue, arenaFloor, items, 2);
            Assert.AreEqual(2, tight.Count(e => !e.Value), "held to the budget");
            Assert.IsFalse(tight[1]);
            Assert.IsTrue(tight[2], "Lighting is 1 at a budget of 2");
            Assert.IsFalse(tight[4]);
        }

        [TestMethod]
        public void APlotOnTheSameMapPacksWhatIsOutsideItAndBringsItBackOnTheWayBack()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            f.Command(owner, ".decor place floor lamp");
            var live = f.Live(owner);
            var copy = owner.Player.MapChannel;

            // Northwest Keep: elsewhere on the same map.
            f.Command(owner, ".apartment plot 4");
            StringAssert.Contains(f.Said(owner).Last(), "Northwest Keep now: 0 items put down, 1 packed");
            Assert.AreSame(copy, owner.Player.MapChannel, "the same copy");
            Assert.AreEqual(0, live.Items.Count);
            Assert.AreEqual(1, live.Packed.Count);
            Assert.AreEqual(f.Apartments.Catalogue.PlotOf(4).Entry, owner.Player.Position, "taken to the new entry");
            Assert.IsTrue(f.Repository(r => r.GetItems(live.Row.Id)).Single().Packed);

            f.Command(owner, ".apartment plot 1");
            StringAssert.Contains(f.Said(owner).Last(), "1 items put down, 0 packed");
            Assert.AreEqual(1, live.Items.Count);
            Assert.IsFalse(f.Repository(r => r.GetItems(live.Row.Id)).Single().Packed);
        }

        [TestMethod]
        public void APlotOnAnotherMapSendsEveryoneOutAndTakesTheOwnerIntoTheNewCopy()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");
            var guest = f.Player("Bob");

            f.Into(owner);
            f.Command(owner, ".apartment invite Bob");
            f.Command(guest, ".apartment visit Ann");
            f.Arrive(guest);

            f.Command(owner, ".apartment plot 2");
            StringAssert.Contains(f.Said(owner).Last(), "You will be taken there");

            f.Apartments.Worker();
            Assert.AreSame(f.World.Map, owner.PendingTransfer.DestinationMap);
            Assert.AreSame(f.World.Map, guest.PendingTransfer.DestinationMap);
            StringAssert.Contains(f.Said(guest).Last(), "moved to another plot");

            f.Arrive(owner);
            f.Arrive(guest);
            f.Apartments.Worker();

            Assert.AreEqual(Nexus, owner.PendingTransfer?.DestinationMap.MapInfo.MapContextId, "into the new copy");
            Assert.IsNull(guest.PendingTransfer, "the visitor is not");
            Assert.AreEqual(1, f.Maps.ApartmentCopies().Count);

            f.Arrive(owner);
            Assert.AreEqual(Nexus, owner.Player.MapChannel.MapInfo.MapContextId);
            Assert.IsTrue(owner.Player.MapChannel.IsApartment);
            Assert.AreEqual(f.World.Map.MapInfo.MapContextId, f.Repository(r => r.GetReturn(owner.Player.Id)).MapContextId, "the first way back is kept");
        }

        #endregion

        #region Logging in inside

        [TestMethod]
        public void ALoginInsideGoesBackInOrToTheReturnPointWhenItMayNot()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");
            var guest = f.Player("Bob");

            f.Into(owner);
            f.Command(owner, ".apartment invite Bob");
            f.Command(guest, ".apartment visit Ann");
            f.Arrive(guest);
            var copy = owner.Player.MapChannel;
            var guestAt = guest.Player.Position;

            // Both leave the world inside; the copy stands empty and closes.
            f.Relog(owner);
            f.Relog(guest);
            f.Apartments.Worker();
            f.Now += 600_000;
            f.Apartments.Worker();
            Assert.AreEqual(0, f.Maps.ApartmentCopies().Count);

            // The owner comes back: into a copy opened again, where they stood.
            f.Apartments.PlaceLogin(owner);
            Assert.IsTrue(owner.Player.MapChannel.IsApartment);
            Assert.AreNotSame(copy, owner.Player.MapChannel);
            Assert.AreEqual(copy.ApartmentId, owner.Player.MapChannel.ApartmentId);

            // The guest was uninvited meanwhile: back where they came from, told why on arrival.
            f.Command(owner, ".apartment uninvite Bob");
            f.Apartments.PlaceLogin(guest);
            Assert.AreSame(f.World.Map, guest.Player.MapChannel);
            Assert.AreEqual(Outside, guest.Player.Position);
            StringAssert.Contains(guest.ArrivalNotice, "may no longer go into");
            Assert.IsNull(f.Repository(r => r.GetReturn(guest.Player.Id)));
            Assert.AreNotEqual(guestAt, guest.Player.Position);
        }

        [TestMethod]
        public void TheMapsOwnLoginPlacingLeavesAnApartmentCopyAlone()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Into(owner);
            var copy = owner.Player.MapChannel;

            f.Maps.PlaceLogin(owner);
            Assert.AreSame(copy, owner.Player.MapChannel);
        }

        #endregion

        #region Commands and the database

        [TestMethod]
        public void CommandsSayHowTheyAreUsed()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");

            f.Command(owner, ".apartment help");
            StringAssert.Contains(f.Said(owner).Last(), ".apartment visit <name>");
            f.Command(owner, ".decor");
            StringAssert.Contains(f.Said(owner).Last(), ".decor place <class id or name> [metres]");
            f.Command(owner, ".apartment nonsense");
            StringAssert.Contains(f.Said(owner).Last(), "No such apartment command");
            f.Command(owner, ".decor place floor lamp");
            StringAssert.Contains(f.Said(owner).Last(), "You are not in an apartment");
            f.Command(owner, ".decor cats");
            StringAssert.Contains(f.Said(owner).Last(), "Lighting (63)");
            f.Command(owner, ".decor list lighting 2");
            StringAssert.Contains(f.Said(owner).Last(), "Lighting, page 2 of 6");
            f.Command(owner, ".decor find hospital bed");
            StringAssert.Contains(f.Said(owner).Last(), "revive point");

            f.Into(owner);
            f.Command(owner, ".decor place lamp");
            StringAssert.Contains(f.Said(owner).Last(), "place one by its id");
            f.Command(owner, ".decor move 1 2");
            StringAssert.Contains(f.Said(owner).Last(), "Choose an item first");
            f.Command(owner, ".decor place floor lamp");
            f.Command(owner, ".decor move 1 2");
            StringAssert.Contains(f.Said(owner).Last(), "usage: .decor move");
            f.Command(owner, ".apartment name The Den");
            f.Command(owner, ".apartment info");
            StringAssert.Contains(f.Said(owner).Last(), "\"The Den\"");
            StringAssert.Contains(f.Said(owner).Last(), "1/150 put down");
            f.Command(owner, ".apartment plot");
            StringAssert.Contains(f.Said(owner).Last(), "1 Arena Loft");
            StringAssert.Contains(f.Said(owner).Last(), "(yours)");
        }

        [TestMethod]
        public void AGameMasterLooksInGoesInAndResets()
        {
            using var f = new Fixture();
            var owner = f.Player("Ann");
            var gm = f.Player("Gil", GmLevel.GameMaster);

            f.Into(owner);
            f.Command(owner, ".decor place floor lamp");
            f.Command(owner, ".apartment passcode secret");

            f.Command(gm, ".aptadmin Ann");
            StringAssert.Contains(f.Said(gm).Last(), "1/150 put down");
            StringAssert.Contains(f.Said(gm).Last(), "Passcode: on");

            f.Command(gm, ".aptadmin Ann goto");
            f.Arrive(gm);
            Assert.AreSame(owner.Player.MapChannel, gm.Player.MapChannel);

            var id = owner.Player.MapChannel.ApartmentId;

            f.Command(gm, ".aptadmin Ann reset");
            StringAssert.Contains(f.Said(gm).Last(), "1 items deleted");
            Assert.AreEqual(0, f.Repository(r => r.GetItems(id)).Count);
            Assert.IsNull(f.Repository(r => r.Get(id)).PasscodeHash);
            Assert.AreSame(f.World.Map, owner.PendingTransfer?.DestinationMap, "everyone out");

            var said = f.Said(owner).Count;
            f.Command(owner, ".aptadmin Ann");
            Assert.AreEqual(said, f.Said(owner).Count, "a player is told it is an unknown command (by the dispatcher), and nothing is done");
        }

        [TestMethod]
        public void TheRowsGoRoundTheDatabaseAndGoWithTheirCharacter()
        {
            using var f = new Fixture();

            var row = f.Repository(r => r.Create(ApartmentEntry.OwnerKindCharacter, 41, 1, 5));
            Assert.AreEqual(row.Id, f.Repository(r => r.Create(ApartmentEntry.OwnerKindCharacter, 41, 2, 6)).Id, "one an owner");

            var first = f.Repository(r => r.AddItem(row.Id, new ApartmentItemEntry { ClassId = FloorLamp, PosX = 1, PosY = 2, PosZ = 3, Rotation = 0.5, Scale = 1.5, PlacedBy = 41 }));
            var second = f.Repository(r => r.AddItem(row.Id, new ApartmentItemEntry { ClassId = Footlocker, Scale = 1 }));

            Assert.AreEqual(1u, first.Number);
            Assert.AreEqual(2u, second.Number);
            Assert.IsTrue(f.Repository(r => r.RemoveItem(second.Id)));
            Assert.AreEqual(3u, f.Repository(r => r.AddItem(row.Id, new ApartmentItemEntry { ClassId = Station, Scale = 1 })).Number, "a number is never given twice");

            var read = f.Repository(r => r.GetItems(row.Id)).First();
            Assert.AreEqual(1.5, read.Scale);
            Assert.AreEqual(0.5, read.Rotation);
            Assert.AreEqual(3, read.PosZ);

            f.Repository(r => { r.SetPacked(row.Id, new Dictionary<uint, bool> { [first.Id] = true }); return 0; });
            Assert.IsTrue(f.Repository(r => r.GetItems(row.Id)).First().Packed);

            f.Repository(r => { r.SetVisitor(row.Id, 42, false, 1); r.SetVisitor(row.Id, 42, true, 2); r.SetVisitor(row.Id, 43, false, 3); return 0; });
            Assert.IsTrue(f.Repository(r => r.GetVisitors(row.Id)).Single(v => v.CharacterId == 42).MayBuild);
            Assert.AreEqual(row.Id, f.Repository(r => r.InvitedInto(42)).Single().Id);

            f.Repository(r => { r.SetReturn(new ApartmentReturnEntry { CharacterId = 42, ApartmentId = row.Id, MapContextId = 1220, PosX = 9 }); return 0; });
            f.Repository(r => { r.SetReturn(new ApartmentReturnEntry { CharacterId = 42, ApartmentId = row.Id, MapContextId = 1148, PosX = 8 }); return 0; });
            Assert.AreEqual(1148u, f.Repository(r => r.GetReturn(42)).MapContextId);

            // The visitor deleted: their invitation and way back go; the apartment stays.
            Assert.AreEqual(0u, f.Repository(r => r.DeleteForCharacter(42)));
            Assert.IsNull(f.Repository(r => r.GetReturn(42)));
            Assert.AreEqual(1, f.Repository(r => r.GetVisitors(row.Id)).Count);

            // The owner deleted: the apartment, its items and its visitors go.
            Assert.AreEqual(row.Id, f.Repository(r => r.DeleteForCharacter(41)));
            Assert.IsNull(f.Repository(r => r.Get(row.Id)));
            Assert.AreEqual(0, f.Repository(r => r.GetItems(row.Id)).Count);
            Assert.AreEqual(0, f.Repository(r => r.GetVisitors(row.Id)).Count);
        }

        #endregion

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();

        /// <summary>
        /// A world of three maps - the players' own, the indoor arena (plots 1, 4, 5, 11) and the
        /// Nexus (plots 2, 6, 7) - a character database, and an apartment manager in the server's
        /// place with its own clock, its own chat and the shipped tables.
        /// </summary>
        internal sealed class Fixture : IDisposable
        {
            private readonly string _directory;
            private readonly string _database;
            private readonly Apartments _previous;
            private readonly List<Client> _clients = new List<Client>();
            private readonly Dictionary<Client, List<string>> _said = new Dictionary<Client, List<string>>();
            private readonly Dictionary<Client, List<PlayerMessage>> _told = new Dictionary<Client, List<PlayerMessage>>();
            private readonly ChatCommandsManager _commands;

            internal long Now = 1000;
            internal WorldTestContext World { get; } = new WorldTestContext();
            internal MapChannelManager Maps { get; }
            internal Apartments Apartments { get; }

            internal Fixture()
            {
                _directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_directory);
                _database = Path.Combine(_directory, "database");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database))
                    context.Database.Migrate();

                Maps = new MapChannelManager(null, () => Now, (_, _, _) => { }, privateInstances: new PrivateMapInstanceService(),
                    refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { })
                {
                    InstancePolicy = _ => null,
                    SquadMapPolicy = _ => false,
                    MapLinks = () => Array.Empty<MapLink>()
                };
                Maps.MapChannelArray.Add(World.Map.MapInfo.MapContextId, World.Map);
                Maps.MapChannelArray.Add(Arena, new MapChannel { MapInfo = new MapInfo(Arena, "adv_wargame_indoorarena", 35, 0), ClientList = new List<Client>(), PlayerLimit = 128 });
                Maps.MapChannelArray.Add(Nexus, new MapChannel { MapInfo = new MapInfo(Nexus, "test_nexus2", 147, 0), ClientList = new List<Client>(), PlayerLimit = 128 });

                foreach (var classId in new[] { FloorLamp, Lighting2, Chair, Footlocker, Station, Bed, Pod })
                    World.AddClass((EntityClasses)classId);

                Apartments = new Apartments
                {
                    Catalogue = ApartmentCatalogueTests.Shipped(),
                    Store = new FileStore(_database),
                    Maps = () => Maps,
                    Clock = () => Now,
                    UtcNowMs = () => Now,
                    Online = () => _clients.Where(c => c.Player != null).ToList(),
                    Say = (client, text) => SaidOf(client).Add(text),
                    Tell = (client, message) => TellOf(client).Add(message)
                };
                _previous = Apartments.Swap(Apartments);
                _commands = new ChatCommandsManager(null) { Audit = null };
                _commands.RegisterChatCommands();
            }

            private List<string> SaidOf(Client client) => _said.TryGetValue(client, out var said) ? said : _said[client] = new List<string>();
            private List<PlayerMessage> TellOf(Client client) => _told.TryGetValue(client, out var told) ? told : _told[client] = new List<PlayerMessage>();

            internal List<string> Said(Client client) => SaidOf(client);
            internal List<PlayerMessage> Told(Client client) => TellOf(client);

            /// <summary>A player on the fixture's map, with a character row on an account of their own.</summary>
            internal Client Player(string name, GmLevel level = GmLevel.Player)
            {
                var client = World.CreateClient(Outside.X, Outside.Z);
                var accountId = 100 + client.Player.Id;

                client.Player.Name = name;
                client.Player.FamilyName = "Fam" + name;
                client.Player.PlaceAt(Outside);
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client,
                    new GameAccountEntry { Id = accountId, SelectedSlot = 1, Level = (byte)level });

                using (var database = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database))
                {
                    var account = new GameAccountEntry { Id = accountId, Name = $"account{accountId}", Email = $"a{accountId}@example.invalid", FamilyName = client.Player.FamilyName };

                    database.GameAccountEntries.Add(account);
                    database.CharacterEntries.Add(new CharacterEntry { Id = client.Player.Id, GameAccount = account, Name = name, Level = 1 });
                    database.SaveChanges();
                }

                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);
                _clients.Add(client);

                return client;
            }

            internal void Command(Client client, string text) => _commands.ProcessCommand(client, text);

            internal void Arrive(Client client)
            {
                Maps.MapLoaded(client);
                WorldTestContext.Drain(client);
            }

            /// <summary>Into their own apartment and arrived, greeted.</summary>
            internal void Into(Client client)
            {
                Command(client, ".apartment");
                Assert.IsNotNull(client.PendingTransfer, string.Join("\n", Said(client)));
                Arrive(client);
                Apartments.Worker();
            }

            /// <summary>Out by .leave and arrived, the way back let go.</summary>
            internal void Out(Client client)
            {
                Command(client, ".leave");
                Assert.IsNotNull(client.PendingTransfer, string.Join("\n", Said(client)));
                Arrive(client);
                Apartments.Worker();
            }

            internal Apartments.Live Live(Client client) => Apartments.LiveOf(client.Player.MapChannel);

            /// <summary>Out of the world as a logout takes them, and back to where a login stands before it is placed.</summary>
            internal void Relog(Client client)
            {
                var map = client.Player.MapChannel;

                CellManager.Instance.RemoveFromWorld(client);
                map.ClientList.Remove(client);
                _clients.Remove(client);
                Apartments.Worker();
                _clients.Add(client);
                client.Player.MapChannel = Maps.MapChannelArray[map.MapInfo.MapContextId];
                WorldTestContext.Drain(client);
            }

            internal T Repository<T>(Func<IApartmentRepository, T> action) => Apartments.Store.With(action);

            public void Dispose()
            {
                Apartments.Reset();
                Apartments.Swap(_previous);

                foreach (var map in Maps.MapChannelArray.Values)
                {
                    map.ClientList.Clear();
                    map.MapCellInfo.Cells.Clear();
                }

                World.Dispose();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>The store the server has, over a database file: a context for each thing done.</summary>
        private sealed class FileStore : IApartmentStore
        {
            private readonly string _database;

            internal FileStore(string database) => _database = database;

            public T With<T>(Func<IApartmentRepository, T> action)
            {
                using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), _database);
                return action(new ApartmentRepository(context));
            }
        }
    }
}
