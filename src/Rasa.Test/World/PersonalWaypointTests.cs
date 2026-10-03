extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    // The Personal Waypoints (PersonalWaypoints): CONSUMABLE_PORTABLE_WAYPOINT, the object it
    // puts down, the waypoint window at it, the way out by SelectWaypoint and the way back by
    // ReturnToWormhole, and the three items at the medical vendors.
    [TestClass]
    [DoNotParallelize]
    public class PersonalWaypointTests
    {
        private const uint OneWay = 1;
        private const uint TwoWay = 2;
        private const uint Squad = 3;

        private const long Start = 5_000_000;

        #region The object

        [TestMethod]
        public void ItIsPutDownInServiceAndShownUsableToItsOwnerAlone()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);

            var obj = f.Deploy(owner, OneWay);

            Assert.AreEqual(PersonalWaypoints.WaypointClass, obj.EntityClassId);
            Assert.AreEqual(DynamicObjectType.PersonalWaypoint, obj.DynamicObjectType);
            Assert.AreEqual(UseObjectState.WhState0, obj.StateId);
            Assert.IsTrue(obj.IsEnabled, "a use of it is looked at");
            Assert.AreEqual(owner.Player.Position, obj.Position);

            // Each client around is shown it as it is for them.
            Assert.IsTrue(Created(owner, obj).Enabled);
            Assert.IsFalse(Created(stranger, obj).Enabled);
            Assert.IsTrue(PersonalWaypoints.MayUse(owner, obj));
            Assert.IsFalse(PersonalWaypoints.MayUse(stranger, obj));

            // And told nothing more while nothing changes.
            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Assert.AreEqual(0, Packets(owner).OfType<SetUsablePacket>().Count());
            Assert.AreEqual(0, Packets(stranger).OfType<SetUsablePacket>().Count());
        }

        [TestMethod]
        public void WithinReachItsOwnerGetsTheMapsWaypoints()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);
            f.Gain(owner, 10, 20);

            var obj = f.Deploy(owner, OneWay);
            Drain(owner);
            Drain(stranger);

            PersonalWaypoints.Worker(f.World.Map, Start + 500);

            var window = Packets(owner).OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(WaypointType.Waypoint, window.WaypointTypeId);
            Assert.AreEqual(1220u, window.CurrentMapId, "the map's row, which its rows are listed under");
            Assert.AreEqual(PersonalWaypoints.WindowNameId, window.CurrentWaypointId);
            CollectionAssert.AreEquivalent(new uint[] { 10, 20 }, window.MapWaypointInfoList[1220].Waypoints.Select(w => w.WaypointId).ToArray());
            Assert.IsNull(window.TempWormholes, "not a way to itself");
            Assert.AreEqual(0, Packets(stranger).OfType<EnteredWaypointPacket>().Count());

            // Once: standing there does not open it again.
            PersonalWaypoints.Worker(f.World.Map, Start + 1000);
            Assert.AreEqual(0, Packets(owner).OfType<EnteredWaypointPacket>().Count());

            // Walking off closes it, and coming back opens it.
            f.Move(owner, obj.Position + new Vector3(PersonalWaypoints.Reach + 1, 0, 0));
            PersonalWaypoints.Worker(f.World.Map, Start + 1500);
            Assert.AreEqual(1, Packets(owner).OfType<ExitedWaypointPacket>().Count());
            Assert.IsFalse(PersonalWaypoints.IsNearUsable(owner, f.World.Map));

            f.Move(owner, obj.Position);
            PersonalWaypoints.Worker(f.World.Map, Start + 2000);
            Assert.AreEqual(1, Packets(owner).OfType<EnteredWaypointPacket>().Count());
            Assert.IsTrue(PersonalWaypoints.IsNearUsable(owner, f.World.Map));
        }

        [TestMethod]
        public void UsingItOpensTheWindowForItsOwnerAndNobodyElse()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);
            var obj = f.Deploy(owner, OneWay);
            Drain(owner);
            Drain(stranger);

            var use = new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = obj.EntityId };

            PersonalWaypoints.Use(owner, obj, use);
            Assert.AreEqual(1, Packets(owner).OfType<EnteredWaypointPacket>().Count());

            PersonalWaypoints.Use(stranger, obj, use);
            Assert.AreEqual(0, Packets(stranger).OfType<EnteredWaypointPacket>().Count());
        }

        #endregion

        #region One way: out

        [TestMethod]
        public void SelectWaypointLeavesFromItForWhoeverMayUseIt()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);
            f.Gain(owner, 20);
            f.Gain(stranger, 20);

            f.Deploy(owner, OneWay);
            Drain(owner);
            Drain(stranger);

            // No waypoint is near: the Personal Waypoint is the departure station, for its owner.
            f.Manager.SelectWaypoint(stranger, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreEqual(ClientState.Ingame, stranger.State);
            Assert.AreEqual(1, Packets(stranger).OfType<TeleportFailedPacket>().Count());

            f.Manager.SelectWaypoint(owner, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreEqual(ClientState.Teleporting, owner.State);
            Assert.AreEqual(f.Far.Position + new Vector3(0, 1, 0), owner.Player.Position);

            f.Manager.TeleportAcknowledge(owner);
            Assert.AreEqual(ClientState.Ingame, owner.State);
        }

        [TestMethod]
        public void ItIsNoWayOntoADropshipOrFromFurtherThanItsReach()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Gain(owner, 20);
            WaypointTravelTests.AddWaypoint(f.Manager, f.World.Map, 30, new Vector3(250, 0, 0), type: WaypointType.Dropship);
            owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(owner.Player.Id, 30, (byte)WaypointType.Dropship));

            var obj = f.Deploy(owner, OneWay);
            Drain(owner);

            f.Manager.SelectWaypoint(owner, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 30 });
            Assert.AreEqual(ClientState.Ingame, owner.State);

            f.Move(owner, obj.Position + new Vector3(PersonalWaypoints.Reach + 1, 0, 0));
            f.Manager.SelectWaypoint(owner, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.AreEqual(2, Packets(owner).OfType<TeleportFailedPacket>().Count());
        }

        #endregion

        #region Two ways: back

        [TestMethod]
        public void ATwoWayOneIsListedAtAWaypointAndReturnedTo()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Gain(owner, 10, 20);

            var obj = f.Deploy(owner, TwoWay);

            // At the waypoint at the origin: its window lists the way back.
            f.Move(owner, f.Near.Position);
            Drain(owner);
            f.Manager.PlayerEnterWaypoint(f.Near);

            var window = Packets(owner).OfType<EnteredWaypointPacket>().Single();
            var listed = window.TempWormholes.Single();
            Assert.AreEqual(obj.EntityId, listed.Id);
            Assert.AreEqual(obj.Position, listed.Position);
            Assert.AreEqual("Player1 Fixture", listed.OwnerName);
            Assert.IsFalse(f.Manager.Teleporters.ContainsKey((uint)listed.Id), "the window keeps waypoints and wormholes in one list by id");

            f.Manager.ReturnToWormhole(owner, obj.EntityId);

            Assert.AreEqual(ClientState.Teleporting, owner.State);
            Assert.AreEqual(obj.Position + new Vector3(0, 1, 0), owner.Player.Position);
            var packets = Packets(owner);
            Assert.AreEqual(owner.Player.Position, packets.OfType<TeleportPacket>().Single().Position);
            Assert.IsTrue(packets.FindIndex(p => p is BeginTeleportPacket) < packets.FindIndex(p => p is TeleportPacket), "or the client never acknowledges it");

            f.Manager.TeleportAcknowledge(owner);
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.IsNull(owner.PendingTransfer);
        }

        [TestMethod]
        public void AOneWayOneIsNotListedAndNotReturnedTo()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Gain(owner, 10);

            var obj = f.Deploy(owner, OneWay);

            f.Move(owner, f.Near.Position);
            Drain(owner);
            f.Manager.PlayerEnterWaypoint(f.Near);
            Assert.IsNull(Packets(owner).OfType<EnteredWaypointPacket>().Single().TempWormholes);

            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.AreEqual(1, Packets(owner).OfType<TeleportFailedPacket>().Count());
        }

        [TestMethod]
        public void ReturningTakesAWaypointUnderfootAndAWormholeThatIsThere()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(0, 0);
            var obj = f.Deploy(owner, TwoWay);
            Drain(owner);
            Drain(stranger);

            // From nowhere: the window that lists it opens at a waypoint.
            f.Move(owner, new Vector3(120, 0, 120));
            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);

            // Somebody else's, from the waypoint.
            f.Manager.ReturnToWormhole(stranger, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, stranger.State);

            // One that is not there, and no id at all.
            f.Move(owner, f.Near.Position);
            f.Manager.ReturnToWormhole(owner, obj.EntityId + 1000);
            f.Manager.ReturnToWormhole(owner, 0);
            Assert.AreEqual(ClientState.Ingame, owner.State);

            // A waypoint the Bane hold opens for nobody.
            ((WaypointInfo)f.Near.ObjectData).Contested = true;
            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.AreEqual(4, Packets(owner).OfType<TeleportFailedPacket>().Count());
            Assert.AreEqual(1, Packets(stranger).OfType<TeleportFailedPacket>().Count());

            // Gone: no way back.
            ((WaypointInfo)f.Near.ObjectData).Contested = false;
            PersonalWaypoints.Worker(f.World.Map, Start + 300_000);
            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);
        }

        [TestMethod]
        public void AnotherPersonalWaypointIsAWayToOne()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(60, 60);
            owner.Player.PartyId = 7;
            mate.Player.PartyId = 7;

            var theirs = f.Deploy(owner, Squad);
            var mine = f.Deploy(mate, TwoWay);
            Drain(mate);

            // At their own, the mate's window lists the squad's - and not the one they stand at.
            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            var window = Packets(mate).OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(theirs.EntityId, window.TempWormholes.Single().Id);

            f.Manager.ReturnToWormhole(mate, mine.EntityId);
            Assert.AreEqual(ClientState.Ingame, mate.State, "the one underfoot is no way to itself");

            f.Manager.ReturnToWormhole(mate, theirs.EntityId);
            Assert.AreEqual(ClientState.Teleporting, mate.State);
            Assert.AreEqual(theirs.Position + new Vector3(0, 1, 0), mate.Player.Position);
        }

        #endregion

        #region Whose

        [TestMethod]
        public void TheSquadsOneIsTheSquadsAndTheOthersAreTheirOwnersAlone()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(31, 30);
            owner.Player.PartyId = 7;
            mate.Player.PartyId = 7;

            var own = f.Deploy(owner, TwoWay);
            Assert.IsFalse(Created(mate, own).Enabled);
            Assert.IsFalse(PersonalWaypoints.MayUse(mate, own));
            Assert.IsNull(PersonalWaypoints.ReturnPoints(mate, f.World.Map));

            var squads = f.Deploy(owner, Squad);
            Assert.IsTrue(Created(mate, squads).Enabled);
            Assert.IsTrue(PersonalWaypoints.MayUse(mate, squads));
            Assert.AreEqual(squads.EntityId, PersonalWaypoints.ReturnPoints(mate, f.World.Map).Single().Id);

            // Out of the squad while it stands, and in again: told each time, once.
            mate.Player.PartyId = 0;
            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Assert.IsFalse(Packets(mate).OfType<SetUsablePacket>().Single().IsEnabled);
            Assert.IsFalse(PersonalWaypoints.MayUse(mate, squads));

            mate.Player.PartyId = 7;
            PersonalWaypoints.Worker(f.World.Map, Start + 1000);
            Assert.IsTrue(Packets(mate).OfType<SetUsablePacket>().Single().IsEnabled);

            PersonalWaypoints.Worker(f.World.Map, Start + 1500);
            Assert.AreEqual(0, Packets(mate).OfType<SetUsablePacket>().Count());
        }

        [TestMethod]
        public void TheDeadDoNotUseIt()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var obj = f.Deploy(owner, TwoWay);

            owner.Player.State = CharacterState.Dead;

            Assert.IsFalse(PersonalWaypoints.MayUse(owner, obj));
            Assert.IsNull(PersonalWaypoints.ReturnPoints(owner, f.World.Map));
        }

        #endregion

        #region How long

        [TestMethod]
        public void ItGoesAfterFiveMinutes()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var obj = f.Deploy(owner, TwoWay);

            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Drain(owner);

            PersonalWaypoints.Worker(f.World.Map, Start + 299_999);
            Assert.AreEqual(1, f.Placed().Length);

            PersonalWaypoints.Worker(f.World.Map, Start + 300_000);

            Assert.AreEqual(0, f.Placed().Length);
            Assert.IsNull(obj.RuntimeMapChannel);
            Assert.IsFalse(obj.IsEnabled);

            var packets = Packets(owner);
            Assert.AreEqual(1, packets.OfType<ExitedWaypointPacket>().Count(), "the open window is closed");
            Assert.AreEqual(obj.EntityId, packets.OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
            Assert.IsFalse(PersonalWaypoints.MayUse(owner, obj));
        }

        [TestMethod]
        public void PuttingDownAnotherTakesTheFirstAway()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var first = f.Deploy(owner, TwoWay);

            f.Move(owner, new Vector3(60, 0, 60));
            var second = f.Deploy(owner, TwoWay);

            Assert.AreSame(second, f.Placed().Single());
            Assert.IsNull(first.RuntimeMapChannel);
            Assert.AreEqual(second.EntityId, PersonalWaypoints.ReturnPoints(owner, f.World.Map).Single().Id);
        }

        [TestMethod]
        public void ItGoesWithItsOwner()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(31, 30);
            owner.Player.PartyId = 7;
            mate.Player.PartyId = 7;
            f.Deploy(owner, Squad);

            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Assert.AreEqual(1, f.Placed().Length);

            // Off the map: another one, or out of the game.
            f.World.Map.ClientList.Remove(owner);
            PersonalWaypoints.Worker(f.World.Map, Start + 1000);

            Assert.AreEqual(0, f.Placed().Length);
            Assert.IsNull(PersonalWaypoints.ReturnPoints(mate, f.World.Map));
        }

        [TestMethod]
        public void InAnOperationATwoWayOneStays()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(60, 60);

            Assert.IsFalse(PersonalWaypoints.Stays(f.World.Map, TwoWay), "the open world");

            f.World.Map.IsSquadInstance = true;

            Assert.IsTrue(PersonalWaypoints.Stays(f.World.Map, TwoWay));
            Assert.IsTrue(PersonalWaypoints.Stays(f.World.Map, Squad));
            Assert.IsFalse(PersonalWaypoints.Stays(f.World.Map, OneWay), "\"Duration is 5 minutes\"");

            f.Deploy(owner, TwoWay);
            f.Deploy(mate, OneWay);

            PersonalWaypoints.Worker(f.World.Map, Start + 24 * 3_600_000L);

            Assert.AreEqual(owner.Player.Position, f.Placed().Single().Position);
        }

        #endregion

        #region The item

        [TestMethod]
        [DataRow(118781u, 1u)]
        [DataRow(118782u, 2u)]
        [DataRow(118783u, 3u)]
        public void TheItemPutsOneDownAndIsUsedUp(uint template, uint level)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var entry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == (uint)PersonalWaypoints.WaypointClass);
            EntityClassManager.Instance.LoadedEntityClasses[PersonalWaypoints.WaypointClass] = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                entry.ClassCollisionRole, entry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0);

            try
            {
                Assert.AreEqual("UsableAbilityTemporaryWormhole", entry.ClassName);
                Assert.AreEqual(((uint)AugmentationType.Wormhole).ToString(), entry.AugList);

                var item = ToyTests.Grant(harness, template);
                var manager = ToyTests.CreateManager(harness, 487, level, template);
                harness.Drain();

                manager.RequestPerformAbility(harness.Client, ToyTests.Request(487, level, item.EntityId));

                var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumablePortableWaypoint);
                Assert.AreEqual(8000, pending.WaitTime, "the level's windup");
                Assert.IsEmpty(Placed(harness.BootcampMap), "not until it has run");

                ToyTests.Land(harness, manager, pending);

                var obj = Placed(harness.BootcampMap).Single();
                Assert.AreEqual(PersonalWaypoints.WaypointClass, obj.EntityClassId);
                Assert.IsLessThan(0.5f, Vector3.Distance(new Vector3(harness.Client.Player.Position.X, 0, harness.Client.Player.Position.Z),
                    new Vector3(obj.Position.X, 0, obj.Position.Z)));
                Assert.IsTrue(PersonalWaypoints.MayUse(harness.Client, obj));
                Assert.AreEqual(level >= PersonalWaypoints.TwoWayLevel,
                    PersonalWaypoints.ReturnPoints(harness.Client, harness.BootcampMap) != null);
                Assert.HasCount(1, harness.Drain().OfType<AbilityRecoveryPacket>().ToArray());

                using var unit = harness.Context.CreateChar();
                Assert.IsNull(unit.Items.GetItem(item.Id), "used up");
                Assert.IsFalse(harness.Client.Player.ActionReuseUntil.ContainsKey(ActionId.ConsumablePortableWaypoint), "it has no reuse");
            }
            finally
            {
                PersonalWaypoints.Worker(harness.BootcampMap, long.MaxValue);
                EntityClassManager.Instance.LoadedEntityClasses.Remove(PersonalWaypoints.WaypointClass);
            }
        }

        #endregion

        #region The packets

        [TestMethod]
        public void EnteredWaypointListsTheWormholesAsTheClientReadsThem()
        {
            var list = new Dictionary<uint, MapWaypointInfoList>();

            Read(new EnteredWaypointPacket(1220, 1220, list, WaypointType.Waypoint, 10,
                new[] { new TempWormhole(0x1_0000_0005UL, new Vector3(1, 2, 3), "Sarah Morgan") }), r =>
            {
                Assert.AreEqual(6, r.ReadTuple());
                Assert.AreEqual(1220, r.ReadInt());
                Assert.AreEqual(1220, r.ReadInt());
                Assert.AreEqual(0, r.ReadList());

                // for (wormholeId, pos, ownername) in tempWormholes
                Assert.AreEqual(1, r.ReadList());
                Assert.AreEqual(3, r.ReadTuple());
                Assert.AreEqual(0x1_0000_0005UL, r.ReadULong());
                Assert.AreEqual(3, r.ReadTuple());
                CollectionAssert.AreEqual(new[] { 1.0, 2.0, 3.0 }, new[] { r.ReadDouble(), r.ReadDouble(), r.ReadDouble() });
                Assert.AreEqual("Sarah Morgan", r.ReadUnicodeString());

                Assert.AreEqual(1, r.ReadInt(), "the client's LOCALWAYPOINT");
                Assert.AreEqual(10, r.ReadInt());
            });

            // None is what it was: "if tempWormholes is not None".
            foreach (var none in new IReadOnlyList<TempWormhole>[] { null, Array.Empty<TempWormhole>() })
                Read(new EnteredWaypointPacket(1220, 1220, list, WaypointType.Waypoint, 10, none), r =>
                {
                    Assert.AreEqual(6, r.ReadTuple());
                    r.ReadInt();
                    r.ReadInt();
                    Assert.AreEqual(0, r.ReadList());
                    r.ReadNoneStruct();
                    Assert.AreEqual(1, r.ReadInt());
                });
        }

        [TestMethod]
        public void ReturnToWormholeReadsTheIdAsAnIntOrALong()
        {
            Assert.AreEqual(GameOpcode.ReturnToWormhole, new ReturnToWormholePacket().Opcode);
            Assert.AreEqual(686, (int)GameOpcode.ReturnToWormhole);

            Assert.AreEqual(77UL, Sent(w => w.WriteInt(77)).WormholeId);
            Assert.AreEqual(0x1_0000_0005UL, Sent(w => w.WriteULong(0x1_0000_0005UL)).WormholeId);
        }

        [TestMethod]
        public void TheWindowsNameAndTheWormholesIdsAreNoWaypointOfTheWorld()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var teleporters = harness.WorldContext.Set<TeleporterEntry>().AsNoTracking().ToList();

            // The window greys out the row whose id is the current one's.
            var row = teleporters.Single(teleporter => teleporter.Id == PersonalWaypoints.WindowNameId);
            Assert.AreEqual("Portable Waypoint", row.Description);
            Assert.AreEqual(0u, row.MapContextId, "a name with no place");

            // And keeps waypoints and wormholes in one list by id: a wormhole's is its entity id,
            // and those begin where the waypoints' have long ended.
            Assert.IsLessThan(1000u, teleporters.Where(teleporter => teleporter.MapContextId != 0).Max(teleporter => teleporter.Id));
            Assert.IsGreaterThanOrEqualTo(1000UL, EntityManager.Instance.GetEntityId);
        }

        #endregion

        #region The medical vendors

        private const string Before = "20261109000000_Use_client_waypoint_ids";
        private const string Migration = "20261110000000_Stock_personal_waypoints";

        private static readonly string Medical = string.Join(", ", PersonalWaypointStock.MedicalPackages);
        private static readonly string Items = string.Join(", ", PersonalWaypointStock.Items);

        [TestMethod]
        public void TheMigrationStocksEveryMedicalVendorAndTakesItOffAgain()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                migrator.Migrate(Before);

                var before = Rows(context, "select id, item_template_id from vendor_item order by rowid");
                var vendors = Rows(context, $"select id from vendor where package_id in ({Medical}) order by id");
                Assert.AreEqual(182, vendors.Count);
                Assert.AreEqual(0, Rows(context, $"select id from vendor_item where item_template_id in ({Items})").Count, "nobody sold one");

                migrator.Migrate();

                // Each of the three, once, at each medical vendor and nowhere else, after what it sold.
                var stocked = Rows(context, $"select id, item_template_id from vendor_item where item_template_id in ({Items}) order by id, item_template_id");
                CollectionAssert.AreEqual(
                    vendors.SelectMany(vendor => PersonalWaypointStock.Items.Select(item => $"{vendor}|{item}")).ToArray(), stocked);
                CollectionAssert.AreEqual(before, Rows(context, $"select id, item_template_id from vendor_item where item_template_id not in ({Items}) order by rowid"));

                // What they cost is the templates' own.
                CollectionAssert.AreEqual(new[] { "118781|1000", "118782|2500", "118783|5000" },
                    Rows(context, $"select id, buy_price from itemtemplate where id in ({Items}) order by id"));

                // Run again, as the script for a MySQL server may be: nothing is doubled.
                var after = Rows(context, "select id, item_template_id from vendor_item order by rowid");

                foreach (var statement in PersonalWaypointStock.Up)
                    context.Database.ExecuteSqlRaw(statement);

                CollectionAssert.AreEqual(after, Rows(context, "select id, item_template_id from vendor_item order by rowid"));

                migrator.Migrate(Before);
                CollectionAssert.AreEqual(before, Rows(context, "select id, item_template_id from vendor_item order by rowid"));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheMigrationIsTheSameStatementsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            foreach (var statement in PersonalWaypointStock.Up)
                StringAssert.Contains(up, statement);

            foreach (var statement in PersonalWaypointStock.Down)
                StringAssert.Contains(down, statement);

            StringAssert.Contains(up, $"'{Migration}'");
        }

        #endregion

        #region Helpers

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static void Drain(Client client) => WorldTestContext.Drain(client);

        /// <summary>The UsableInfo the client was sent with the object: how it is for them.</summary>
        private static UsableInfoPacket Created(Client client, DynamicObject obj) =>
            Packets(client).OfType<CreatePhysicalEntityPacket>().Single(created => created.EntityId == obj.EntityId)
                .EntityData.OfType<UsableInfoPacket>().Single();

        private static DynamicObject[] Placed(MapChannel map) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.DynamicObjectType == DynamicObjectType.PersonalWaypoint)
                .Distinct()
                .ToArray();

        private static List<string> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static void Read(ServerPythonPacket packet, Action<PythonReader> read)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                packet.Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            read(reader);
        }

        private static ReturnToWormholePacket Sent(Action<PythonWriter> id)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                id(writer);
            }

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new ReturnToWormholePacket();
            packet.Read(reader);

            return packet;
        }

        /// <summary>
        /// The fixture map with two waypoints - 10 at the origin and 20 two hundred metres off -
        /// the object's class, and the window's waypoint list taken from the manager under test.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private readonly Func<Client, Dictionary<uint, MapWaypointInfoList>> _network = PersonalWaypoints.Network;

            internal WorldTestContext World { get; } = new WorldTestContext();
            internal DynamicObjectManager Manager { get; }
            internal DynamicObject Near { get; }
            internal DynamicObject Far { get; }

            internal Fixture()
            {
                World.AddClass(PersonalWaypoints.WaypointClass);
                Manager = WaypointTravelTests.CreateManager(World);
                Near = WaypointTravelTests.AddWaypoint(Manager, World.Map, 10, Vector3.Zero);
                Far = WaypointTravelTests.AddWaypoint(Manager, World.Map, 20, new Vector3(200, 5, 0));
                Near.RuntimeMapChannel = World.Map;
                Far.RuntimeMapChannel = World.Map;
                PersonalWaypoints.Network = client => Manager.CreateListOfWaypoints(client, WaypointType.Waypoint);
            }

            /// <summary>A player in the world, with nothing waiting to be read.</summary>
            internal Client Player(float x, float z)
            {
                var client = World.CreateClient(x, z);

                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);

                return client;
            }

            internal void Gain(Client client, params uint[] waypoints)
            {
                foreach (var id in waypoints)
                    client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, id, (byte)WaypointType.Waypoint));
            }

            internal void Move(Client client, Vector3 position)
            {
                client.SetWorldPosition(position, client.Player.Rotation);
                CellManager.Instance.UpdateVisibility(client);
            }

            internal DynamicObject Deploy(Client owner, uint level) =>
                PersonalWaypoints.Deploy(World.Map, owner.Player, level, null, Start);

            internal DynamicObject[] Placed() => PersonalWaypointTests.Placed(World.Map);

            public void Dispose()
            {
                // Whatever is still out is taken away with the map.
                foreach (var client in World.Map.ClientList.ToList())
                    World.Map.ClientList.Remove(client);

                PersonalWaypoints.Worker(World.Map, Start);
                PersonalWaypoints.Network = _network;
                World.Dispose();
            }
        }

        #endregion
    }
}
