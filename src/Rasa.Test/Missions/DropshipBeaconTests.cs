using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class DropshipBeaconTests
    {
        private const uint BeaconTemplate = 130285;     // Dropship Extraction Beacon: 511 level 1

        [TestMethod]
        public void TheBeaconPutsDownAShipThatFliesInAndIsKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (item, manager) = Prepare(harness);

            Deploy(harness, manager, item);

            var ship = Ships(harness).Single();
            Assert.AreEqual(UseObjectState.WhState2, ship.StateId, "flown in: the birth has played");
            Assert.IsFalse(ship.IsEnabled, "not in service until it has settled");
            Assert.IsLessThan(0.5f, Vector3.Distance(new Vector3(harness.Client.Player.Position.X, 0, harness.Client.Player.Position.Z),
                new Vector3(ship.Position.X, 0, ship.Position.Z)));

            using var unit = harness.Context.CreateChar();
            Assert.IsNotNull(unit.Items.GetItem(item.Id), "the beacon is kept");
            Assert.IsTrue(harness.Client.Player.ActionReuseUntil.ContainsKey(ActionId.AccountrewardPortal), "the hour's reuse has started");
        }

        [TestMethod]
        public void OnceSettledTheOwnerInReachGetsTheDropshipWindow()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (item, manager) = Prepare(harness);
            var deployed = Environment.TickCount64;
            Deploy(harness, manager, item);
            var ship = Ships(harness).Single();
            harness.Drain();

            DropshipBeacons.Worker(harness.BootcampMap, deployed + 1000);
            Assert.IsEmpty(harness.Drain().OfType<EnteredWaypointPacket>().ToArray(), "still flying in");
            Assert.IsFalse(DropshipBeacons.IsNearUsable(harness.Client, harness.BootcampMap));

            DropshipBeacons.Worker(harness.BootcampMap, deployed + 9000);

            Assert.AreEqual(UseObjectState.WhState0, ship.StateId);
            Assert.IsTrue(ship.IsEnabled);
            var window = harness.Drain().OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(WaypointType.Dropship, window.WaypointTypeId);
            Assert.AreEqual(DropshipBeacons.WindowNameId, window.CurrentWaypointId);
            Assert.AreEqual(harness.BootcampMap.MapInfo.MapContextId, window.CurrentMapId, "the map's row, which the window marks current");
            Assert.IsTrue(DropshipBeacons.IsNearUsable(harness.Client, harness.BootcampMap));

            // Once: standing there does not open it again.
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 9500);
            Assert.IsEmpty(harness.Drain().OfType<EnteredWaypointPacket>().ToArray());

            // Walking off closes it.
            harness.MovePlayerTo(ship.Position + new Vector3(20, 0, 0));
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 10000);
            Assert.HasCount(1, harness.Drain().OfType<ExitedWaypointPacket>().ToArray());
            Assert.IsFalse(DropshipBeacons.IsNearUsable(harness.Client, harness.BootcampMap));
        }

        [TestMethod]
        public void SomeoneOutsideTheSquadCannotUseIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (item, manager) = Prepare(harness);
            var deployed = Environment.TickCount64;
            Deploy(harness, manager, item);
            var ship = Ships(harness).Single();
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 9000);

            // The deployer is someone else, in no squad with this player.
            var stranger = new Manifestation { PartyId = 0 };
            SetOwner(harness, ship, stranger);

            Assert.IsFalse(DropshipBeacons.MayUse(harness.Client, ship));

            stranger.PartyId = 7;
            harness.Client.Player.PartyId = 7;
            Assert.IsTrue(DropshipBeacons.MayUse(harness.Client, ship), "a squad mate may");
        }

        [TestMethod]
        public void OnlyTheSquadIsToldTheShipIsInService()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (item, manager) = Prepare(harness);
            var deployed = Environment.TickCount64;
            Deploy(harness, manager, item);
            var ship = Ships(harness).Single();
            harness.Drain();

            // The deployer, once it has settled.
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 9000);
            Assert.IsTrue(harness.Drain().OfType<SetUsablePacket>().Single().IsEnabled);
            Assert.IsTrue(DropshipBeacons.ShowTo(harness.Client, ship), "met again: enabled in its UsableInfo");

            // Someone else's ship: told it is not, and so a client meeting it.
            var stranger = new Manifestation { PartyId = 0 };
            SetOwner(harness, ship, stranger);
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 9500);
            Assert.IsFalse(harness.Drain().OfType<SetUsablePacket>().Single().IsEnabled);
            Assert.IsFalse(DropshipBeacons.ShowTo(harness.Client, ship));

            // Joining the deployer's squad while it is out.
            stranger.PartyId = 7;
            harness.Client.Player.PartyId = 7;
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 10000);
            Assert.IsTrue(harness.Drain().OfType<SetUsablePacket>().Single().IsEnabled);

            // Nothing more while nothing changes.
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 10500);
            Assert.IsEmpty(harness.Drain().OfType<SetUsablePacket>().ToArray());
        }

        [TestMethod]
        public void NotInServiceForAnyoneBeforeItSettles()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (item, manager) = Prepare(harness);
            var deployed = Environment.TickCount64;
            Deploy(harness, manager, item);
            var ship = Ships(harness).Single();
            harness.Drain();

            DropshipBeacons.Worker(harness.BootcampMap, deployed + 1000);

            Assert.IsEmpty(harness.Drain().OfType<SetUsablePacket>().ToArray());
            Assert.IsFalse(DropshipBeacons.ShowTo(harness.Client, ship));
        }

        [TestMethod]
        public void AfterFiveMinutesTheShipLeavesAndIsTakenAway()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (item, manager) = Prepare(harness);
            var deployed = Environment.TickCount64;
            Deploy(harness, manager, item);
            var ship = Ships(harness).Single();
            DropshipBeacons.Worker(harness.BootcampMap, deployed + 9000);
            harness.Drain();

            DropshipBeacons.Worker(harness.BootcampMap, deployed + 8000 + 300_000 + 100);

            Assert.IsFalse(ship.IsEnabled);
            Assert.AreEqual(UseObjectState.WhState1, ship.StateId);
            var packets = harness.Drain();
            Assert.HasCount(1, packets.OfType<ExitedWaypointPacket>().ToArray(), "the open window is closed");
            Assert.IsFalse(packets.OfType<SetUsablePacket>().Single().IsEnabled, "out of service for those told otherwise");
            Assert.IsTrue(packets.OfType<UsePacket>().Any(use => use.CurState == UseObjectState.WhState1), "sent away: 205 to 214");
            Assert.HasCount(1, Ships(harness).ToArray(), "kept while it goes");

            DropshipBeacons.Worker(harness.BootcampMap, deployed + 8000 + 300_000 + 100 + DropshipBeacons.DepartureMs);
            Assert.IsEmpty(Ships(harness).ToArray());
        }

        private static (Item Item, AbilityManager Manager) Prepare(BootcampRuntimeTestHarness.Harness harness)
        {
            LoadClass(harness, (uint)DropshipBeacons.ShipClass);
            var item = ToyTests.Grant(harness, BeaconTemplate);
            var manager = ToyTests.CreateManager(harness, 511, 1, BeaconTemplate);
            harness.Drain();
            return (item, manager);
        }

        private static void Deploy(BootcampRuntimeTestHarness.Harness harness, AbilityManager manager, Item item)
        {
            manager.RequestPerformAbility(harness.Client, ToyTests.Request(511, 1, item.EntityId));
            ToyTests.Land(harness, manager, harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.AccountrewardPortal));
        }

        private static DynamicObject[] Ships(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.DynamicObjectType == DynamicObjectType.DropshipBeacon)
                .Distinct()
                .ToArray();

        private static void SetOwner(BootcampRuntimeTestHarness.Harness harness, DynamicObject ship, Manifestation owner)
        {
            var table = typeof(DropshipBeacons).GetField("Beacons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
            var tryGet = table.GetType().GetMethod("TryGetValue");
            var args = new object[] { harness.BootcampMap, null };
            Assert.IsTrue((bool)tryGet.Invoke(table, args));
            foreach (var beacon in (System.Collections.IEnumerable)args[1])
            {
                var shipField = beacon.GetType().GetField("Ship");
                if (shipField.GetValue(beacon) == ship)
                    beacon.GetType().GetField("Owner").SetValue(beacon, owner);
            }
        }

        private static void LoadClass(BootcampRuntimeTestHarness.Harness harness, uint classId)
        {
            var entry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == classId);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId] = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                entry.ClassCollisionRole, entry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0);
        }
    }
}
