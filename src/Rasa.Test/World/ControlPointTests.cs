using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    // The open world's control points (ControlPoints): who holds one and what that does to its
    // garrisons, its object, its hospital and its waypoint; a capture; the Bane taking one back;
    // a Bane garrison coming back together; prestige for its dead; and what is kept.
    [TestClass]
    [DoNotParallelize]
    public class ControlPointTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint PointId = 7;
        private const uint BanePoolA = 9101;
        private const uint BanePoolB = 9102;
        private const uint AfsPool = 9201;
        private const uint HospitalId = 9301;
        private const uint WaypointId = 9302;
        private const uint BossCreatureId = 9401;
        private const uint MinionCreatureId = 9402;
        private const ulong MarkerId = 133079561962676UL;

        private Func<Client, int, bool> _change;
        private Func<uint, bool> _known;
        private Func<long> _now;
        private Func<long> _utcNow;
        private Func<uint, bool> _hospitalOpen;
        private Func<IEnumerable<(uint, uint, Vector3, string)>> _hospitalSource;
        private Func<uint, bool> _hospitalSafe;
        private long _tick;
        private readonly List<uint> _teleporters = new List<uint>();

        [TestInitialize]
        public void Start()
        {
            _change = PvpPrestige.Change;
            _known = ControlPoints.Instance.KnownCreature;
            _now = ControlPoints.Instance.Now;
            _utcNow = ControlPoints.Instance.UtcNow;
            _hospitalOpen = Hospitals.IsOpen;
            _hospitalSource = Hospitals.Source;
            _hospitalSafe = Hospitals.IsSafeZone;
            _tick = 1_000_000;

            ControlPoints.Instance.KnownCreature = id => true;
            ControlPoints.Instance.Now = () => _tick;
            ControlPoints.Instance.UtcNow = () => 1_700_000_000_000;
            PvpPrestige.Change = (client, amount) =>
            {
                client.Player.Credits[CurencyType.Prestige] = Prestige(client) + amount;
                return true;
            };
        }

        [TestCleanup]
        public void Restore()
        {
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            ControlPoints.Instance.KnownCreature = _known;
            ControlPoints.Instance.Now = _now;
            ControlPoints.Instance.UtcNow = _utcNow;
            PvpPrestige.Change = _change;
            Hospitals.IsOpen = _hospitalOpen;
            Hospitals.Source = _hospitalSource;
            Hospitals.IsSafeZone = _hospitalSafe;
            Hospitals.Reset();

            foreach (var id in _teleporters)
                DynamicObjectManager.Instance.Teleporters.Remove(id);

            _teleporters.Clear();
        }

        [TestMethod]
        public void APointIsWithItsDefaultOwnerUnlessAnotherWasKept()
        {
            var store = new MemoryStore();

            ControlPoints.Instance.Load(new[] { Entry(ControlPointEntry.OwnerBane), Entry(ControlPointEntry.OwnerAfs, 8) }, Links(), store);
            Assert.IsFalse(ControlPoints.Instance.ById(PointId).HeldByAfs, "its default");
            Assert.IsTrue(ControlPoints.Instance.ById(8).HeldByAfs);

            store.Rows.Add(new ControlPointStateEntry { ControlPointId = PointId, Owner = ControlPointEntry.OwnerAfs, ChangedAt = 5 });
            store.Rows.Add(new ControlPointStateEntry { ControlPointId = 999, Owner = ControlPointEntry.OwnerAfs, ChangedAt = 5 });

            ControlPoints.Instance.Load(new[] { Entry(ControlPointEntry.OwnerBane) }, Links(), store);
            Assert.IsTrue(ControlPoints.Instance.ById(PointId).HeldByAfs, "as it was kept");
        }

        [TestMethod]
        public void TheHoldersGarrisonRunsAndTheOtherSidesDoesNot()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);
            var bane = Pool(world, BanePoolA);
            var afs = Pool(world, AfsPool);

            Assert.IsTrue(bane.IsGarrison && !bane.Suspended, "the Bane's runs");
            Assert.AreEqual(bane.RespawnTime, bane.UpdateTimer, "at once");
            Assert.IsTrue(afs.IsGarrison && afs.Suspended, "the AFS's does not");
            Assert.AreEqual(UseObjectState.CpointStateFactionBOwned, point.Object.StateId);
            Assert.IsFalse(point.Object.IsEnabled);
            Assert.AreSame(point.Object, world.Map.ControlPoints[PointId]);
            Assert.AreSame(point, ControlPoints.Instance.PointOf(point.Object));
        }

        [TestMethod]
        public void APrivateCopyOfTheMapHasNoControlPoints()
        {
            using var world = new WorldTestContext();
            world.Map.IsPrivateInstance = true;

            try
            {
                var point = Placed(world, ControlPointEntry.OwnerBane);

                Assert.IsNull(point.Object);
                Assert.AreEqual(0, world.Map.ControlPoints.Count);
                Assert.IsFalse(Pool(world, AfsPool).Suspended, "its pools are left as they were");
            }
            finally
            {
                world.Map.IsPrivateInstance = false;
            }
        }

        [TestMethod]
        public void ABanePointIsInServiceOnlyWhileNoneOfItsGarrisonStands()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);

            ControlPoints.Instance.Worker(world.Map);
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(world.Map, point, ControlPoints.Bane), "yet to arrive is on its way");
            Assert.IsFalse(point.Object.IsEnabled);

            Spawned(world, BanePoolA, alive: 3);
            Spawned(world, BanePoolB, alive: 0);
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsFalse(point.Object.IsEnabled, "one pool of two still stands");
            Assert.IsFalse(ControlPoints.Instance.MayCapture(world.Map, point));

            Spawned(world, BanePoolA, alive: 0, queued: 2);
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsFalse(point.Object.IsEnabled, "those on the way count");

            Spawned(world, BanePoolA, alive: 0);
            ControlPoints.Instance.Worker(world.Map);
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(world.Map, point, ControlPoints.Bane));
            Assert.IsTrue(point.Object.IsEnabled);
            Assert.IsTrue(ControlPoints.Instance.MayCapture(world.Map, point));
        }

        [TestMethod]
        public void AGarrisonOfPoolsThatSpawnNothingIsNobodyToKill()
        {
            using var world = new WorldTestContext();
            ControlPoints.Instance.KnownCreature = id => false;
            var point = Placed(world, ControlPointEntry.OwnerBane);

            Assert.AreEqual(ControlPoints.Garrison.None, ControlPoints.Instance.GarrisonOf(world.Map, point, ControlPoints.Bane));

            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(point.Object.IsEnabled);
        }

        [TestMethod]
        public void ACaptureWithTheGarrisonDownGivesThePointToTheAfsAndIsKept()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Placed(world, ControlPointEntry.OwnerBane, store);
            var client = world.CreateClient();
            Teleporter(HospitalId, WaypointType.Hospital);
            Teleporter(WaypointId, WaypointType.Waypoint);

            Spawned(world, BanePoolA, alive: 0);
            Spawned(world, BanePoolB, alive: 0);
            ControlPoints.Instance.Worker(world.Map);
            Drain(client);

            Assert.IsTrue(ControlPoints.Instance.Captured(world.Map, client, point));

            Assert.IsTrue(point.HeldByAfs);
            Assert.AreEqual((PointId, ControlPoints.Afs, 1_700_000_000_000), store.Saved.Single());
            Assert.AreEqual(UseObjectState.CpointStateFactionAOwned, point.Object.StateId);
            Assert.IsFalse(point.Object.IsEnabled, "nothing to capture of one's own");
            Assert.IsTrue(Pool(world, BanePoolA).Suspended && Pool(world, BanePoolB).Suspended);

            var afs = Pool(world, AfsPool);
            Assert.IsFalse(afs.Suspended);
            Assert.IsFalse(afs.HasSpawned);
            Assert.AreEqual(afs.RespawnTime, afs.UpdateTimer, "set down at once");

            Assert.IsTrue(ControlPoints.Instance.IsOpen(HospitalId));
            Assert.IsTrue(ControlPoints.Instance.IsOpen(WaypointId));

            var packets = Drain(client);
            var owned = packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned);
            Assert.AreEqual("AFS", owned.Args["faction"]);
            Assert.AreEqual("Retread Outpost", owned.Args["cpName"]);

            var marker = packets.OfType<UpdateMapMarkerPacket>().Single(m => m.MarkerEntityId == MarkerId);
            CollectionAssert.AreEqual(new byte[] { 0x82, 0x16, 0x01 }, Encode(marker.State), "(FACTION_OWNED, True)");

            Assert.IsFalse(ControlPoints.Instance.Captured(world.Map, client, point), "it is not the Bane's any more");
        }

        [TestMethod]
        public void ACaptureWithTheGarrisonBackOnItsFeetComesToNothing()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Placed(world, ControlPointEntry.OwnerBane, store);
            var client = world.CreateClient();

            Spawned(world, BanePoolA, alive: 0);
            Spawned(world, BanePoolB, alive: 1);

            Assert.IsFalse(ControlPoints.Instance.Captured(world.Map, client, point));
            Assert.IsFalse(point.HeldByAfs);
            Assert.AreEqual(0, store.Saved.Count);
        }

        [TestMethod]
        public void AnAfsPointWhoseGarrisonIsAllDeadAtOnceIsTheBanes()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Placed(world, ControlPointEntry.OwnerAfs, store);
            var client = world.CreateClient();
            Teleporter(HospitalId, WaypointType.Hospital);
            Teleporter(WaypointId, WaypointType.Waypoint);
            ControlPoints.Instance.Place(world.Map);

            Assert.IsTrue(Pool(world, BanePoolA).Suspended, "the Bane's garrison waits");
            Assert.IsFalse(Contested(WaypointId));

            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(point.HeldByAfs, "a garrison yet to arrive has not been killed");

            Spawned(world, AfsPool, alive: 2);
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(point.HeldByAfs);

            Spawned(world, AfsPool, alive: 0);
            Drain(client);
            ControlPoints.Instance.Worker(world.Map);

            Assert.IsFalse(point.HeldByAfs);
            Assert.AreEqual((PointId, ControlPoints.Bane, 1_700_000_000_000), store.Saved.Single());
            Assert.AreEqual(UseObjectState.CpointStateFactionBOwned, point.Object.StateId);
            Assert.IsTrue(Pool(world, AfsPool).Suspended);
            Assert.IsFalse(Pool(world, BanePoolA).Suspended);
            Assert.IsFalse(ControlPoints.Instance.IsOpen(HospitalId));
            Assert.IsTrue(Contested(WaypointId) && Contested(HospitalId));

            var packets = Drain(client);
            Assert.AreEqual("Bane", packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned).Args["faction"]);
            CollectionAssert.AreEqual(new byte[] { 0x82, 0x16, 0x02 },
                Encode(packets.OfType<UpdateMapMarkerPacket>().Single(m => m.MarkerEntityId == MarkerId).State), "(FACTION_OWNED, False)");
        }

        [TestMethod]
        public void ABaneGarrisonComesBackTogetherOnceItHasAllBeenDownLongEnough()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);
            var a = Pool(world, BanePoolA);
            var b = Pool(world, BanePoolB);

            Assert.IsFalse(ControlPoints.Instance.HoldsBack(a), "its first spawn is not held");

            Spawned(world, BanePoolA, alive: 0);
            Spawned(world, BanePoolB, alive: 2);
            a.UpdateTimer = 0;
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(ControlPoints.Instance.HoldsBack(a), "killed, it waits for the rest");
            Assert.AreEqual(90_000, ControlPoints.Instance.ReturnMs(world.Map, point), "the shorter of the two");

            Spawned(world, BanePoolB, alive: 0);
            b.UpdateTimer = 0;
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(ControlPoints.Instance.HoldsBack(a) && ControlPoints.Instance.HoldsBack(b), "all down: the time runs");

            _tick += 89_000;
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(ControlPoints.Instance.HoldsBack(a));
            Assert.IsTrue(point.Object.IsEnabled);

            _tick += 1_000;
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsFalse(ControlPoints.Instance.HoldsBack(a) || ControlPoints.Instance.HoldsBack(b), "let back");
            Assert.AreEqual(a.RespawnTime, a.UpdateTimer);
            Assert.AreEqual(b.RespawnTime, b.UpdateTimer, "the slower pool with it");

            Spawned(world, BanePoolA, alive: 3);
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsFalse(ControlPoints.Instance.HoldsBack(b), "not held while the rest is still arriving");
            Assert.IsFalse(point.Object.IsEnabled);

            Spawned(world, BanePoolB, alive: 0, dropships: 1);
            ControlPoints.Instance.Worker(world.Map);
            Spawned(world, BanePoolB, alive: 0);
            Assert.IsTrue(ControlPoints.Instance.HoldsBack(b), "all of it back: the next to fall waits again");
        }

        [TestMethod]
        public void TheSpawnWorkerLeavesAHeldGarrisonPoolAlone()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerBane);
            var a = Pool(world, BanePoolA);

            Spawned(world, BanePoolA, alive: 0);
            Spawned(world, BanePoolB, alive: 2);
            a.UpdateTimer = 0;

            SpawnPoolManager.Instance.SpawnPoolWorker(world.Map, 500_000);

            Assert.AreEqual(0, a.UpdateTimer, "its respawn time does not run");
            Assert.AreEqual(0, a.AliveCreatures + a.QueuedCreatures);
        }

        [TestMethod]
        public void ALostPointsHospitalIsNotOfferedOrGainedButIsNotForgotten()
        {
            using var world = new WorldTestContext();
            Hospitals.Source = () => new[]
            {
                (HospitalId, MapId, new Vector3(100, 0, 0), "Hospital: Retread Outpost (Control Point)"),
                (9399u, MapId, new Vector3(-400, 0, 0), "Hospital: Far Away")
            };
            Hospitals.IsSafeZone = id => false;
            Hospitals.Reset();

            var point = Placed(world, ControlPointEntry.OwnerAfs);
            var client = world.CreateClient(95, 0);

            Hospitals.Worker(world.Map);
            Assert.IsTrue(Hospitals.Knows(client.Player, HospitalId));
            CollectionAssert.AreEqual(new[] { HospitalId }, Hospitals.AvailableTo(client.Player, MapId).Select(h => h.TeleporterId).ToArray());

            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);

            CollectionAssert.AreEqual(new[] { 9399u }, Hospitals.AvailableTo(client.Player, MapId).Select(h => h.TeleporterId).ToArray(),
                "the nearest the AFS still hold");
            Assert.IsTrue(Hospitals.Knows(client.Player, HospitalId), "kept for when it is retaken");

            var stranger = world.CreateClient(100, 0);
            Hospitals.Worker(world.Map);
            Assert.IsFalse(Hospitals.Knows(stranger.Player, HospitalId), "not gained while the Bane hold it");

            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            CollectionAssert.AreEqual(new[] { HospitalId }, Hospitals.AvailableTo(client.Player, MapId).Select(h => h.TeleporterId).ToArray());
        }

        [TestMethod]
        public void WithEveryHospitalLostTheNearestIsOfferedAllTheSame()
        {
            using var world = new WorldTestContext();
            Hospitals.Source = () => new[] { (HospitalId, MapId, new Vector3(100, 0, 0), "Hospital: Retread Outpost (Control Point)") };
            Hospitals.IsSafeZone = id => false;
            Hospitals.Reset();

            Placed(world, ControlPointEntry.OwnerBane);
            var client = world.CreateClient();

            CollectionAssert.AreEqual(new[] { HospitalId }, Hospitals.AvailableTo(client.Player, MapId).Select(h => h.TeleporterId).ToArray());
        }

        [TestMethod]
        public void OneOfABaneGarrisonIsWorthThirtyPrestigeAndABossAHundred()
        {
            using var world = new WorldTestContext();
            Placed(world, ControlPointEntry.OwnerBane);
            var client = world.CreateClient();
            var minion = new Creature { DbId = MinionCreatureId, SpawnPool = Pool(world, BanePoolA), EntityClass = EntityClasses.HumanBaseMale };
            var boss = new Creature { DbId = BossCreatureId, SpawnPool = Pool(world, BanePoolA), EntityClass = EntityClasses.HumanBaseMale };
            var guard = new Creature { DbId = MinionCreatureId, SpawnPool = Pool(world, AfsPool), EntityClass = EntityClasses.HumanBaseMale };
            var stray = new Creature { DbId = BossCreatureId, EntityClass = EntityClasses.HumanBaseMale };

            Assert.AreEqual(30, ControlPoints.Instance.CreatureKilled(minion, client));
            Assert.AreEqual(30, Prestige(client));

            var told = Drain(client).OfType<ReceivedCreatureKillPrestigePacket>().Single();
            Assert.AreEqual(minion.EntityId, told.EntityId);
            Assert.AreEqual(30, told.Amount);

            Assert.AreEqual(100, ControlPoints.Instance.CreatureKilled(boss, client));
            Assert.AreEqual(130, Prestige(client));
            Assert.AreEqual(100, Drain(client).OfType<ReceivedCreatureKillPrestigePacket>().Single().Amount);

            Assert.AreEqual(0, ControlPoints.Instance.CreatureKilled(guard, client), "the AFS's own");
            Assert.AreEqual(0, ControlPoints.Instance.CreatureKilled(stray, client), "of no control point");
            Assert.AreEqual(130, Prestige(client));

            PvpPrestige.Change = (c, amount) => false;
            Assert.AreEqual(0, ControlPoints.Instance.CreatureKilled(minion, client), "not said if it was not given");
            Assert.AreEqual(0, Drain(client).OfType<ReceivedCreatureKillPrestigePacket>().Count());
        }

        [TestMethod]
        public void TheSeedLinksEveryPointToThingsOfAKindThatIsRead()
        {
            var points = ControlPointSeed.Points.Select(p => (uint)p[0]).ToList();

            Assert.AreEqual(41, points.Count);
            Assert.AreEqual(ControlPointSeed.PointCount, points.Count);
            Assert.AreEqual(points.Count, points.Distinct().Count());
            Assert.AreEqual(ControlPointSeed.LinkCount, ControlPointSeed.Links.Length);

            foreach (var link in ControlPointSeed.Links)
            {
                Assert.IsTrue(points.Contains((uint)link[0]), $"link to control point {link[0]}");
                Assert.IsTrue((byte)link[1] >= ControlPointLinkEntry.KindBanePool && (byte)link[1] <= ControlPointLinkEntry.KindBoss, $"kind {link[1]}");
            }

            Assert.AreEqual(ControlPointSeed.Links.Length,
                ControlPointSeed.Links.Select(l => ((uint)l[0], (byte)l[1], (uint)l[2])).Distinct().Count(), "no link twice");

            // A pool, a hospital or a waypoint belongs to one point: whoever holds that point has it.
            foreach (var kinds in new[] { new[] { ControlPointLinkEntry.KindBanePool, ControlPointLinkEntry.KindAfsPool }, new[] { ControlPointLinkEntry.KindHospital, ControlPointLinkEntry.KindWaypoint } })
            {
                var owned = ControlPointSeed.Links.Where(l => kinds.Contains((byte)l[1])).Select(l => (uint)l[2]).ToList();

                Assert.AreEqual(owned.Count, owned.Distinct().Count());
            }

            var bane = ControlPointSeed.Points.Where(p => (byte)p[9] == ControlPointEntry.OwnerBane).Select(p => (string)p[2]).ToList();
            Assert.AreEqual(6, bane.Count, "the points named for the Bane are theirs to begin with");
            Assert.IsTrue(bane.All(name => name.Contains("Bane")));
        }

        #region Fixture

        private sealed class MemoryStore : ControlPoints.IStore
        {
            public List<ControlPointStateEntry> Rows { get; } = new List<ControlPointStateEntry>();
            public List<(uint, byte, long)> Saved { get; } = new List<(uint, byte, long)>();

            public List<ControlPointStateEntry> Load() => Rows;
            public void Save(uint controlPointId, byte owner, long changedAt) => Saved.Add((controlPointId, owner, changedAt));
        }

        private static ControlPointEntry Entry(byte defaultOwner, uint id = PointId) => new ControlPointEntry
        {
            Id = id,
            MapContextId = MapId,
            Name = "Retread Outpost",
            ClassId = 3814,
            PosX = 10,
            PosY = 0,
            PosZ = 10,
            Rotation = 0,
            MarkerEntityId = id == PointId ? MarkerId : 0,
            DefaultOwner = defaultOwner
        };

        private static List<ControlPointLinkEntry> Links() => new List<ControlPointLinkEntry>
        {
            Link(ControlPointLinkEntry.KindBanePool, BanePoolA),
            Link(ControlPointLinkEntry.KindBanePool, BanePoolB),
            Link(ControlPointLinkEntry.KindAfsPool, AfsPool),
            Link(ControlPointLinkEntry.KindHospital, HospitalId),
            Link(ControlPointLinkEntry.KindWaypoint, WaypointId),
            Link(ControlPointLinkEntry.KindBoss, BossCreatureId)
        };

        private static ControlPointLinkEntry Link(byte kind, uint objectId) =>
            new ControlPointLinkEntry { ControlPointId = PointId, Kind = kind, ObjectId = objectId };

        /// <summary>The point loaded and set down on the world's map, with two Bane pools (90 s and 180 s) and one of the AFS's.</summary>
        private static ControlPoints.Point Placed(WorldTestContext world, byte owner, ControlPoints.IStore store = null)
        {
            world.Map.SpawnPools.Add(NewPool(world, BanePoolA, SpawnPoolManager.ModeControlPoint, 90_000));
            world.Map.SpawnPools.Add(NewPool(world, BanePoolB, SpawnPoolManager.ModeControlPoint, 180_000));
            world.Map.SpawnPools.Add(NewPool(world, AfsPool, SpawnPoolManager.ModeAutomatic, 2_000));

            ControlPoints.Instance.Load(new[] { Entry(owner) }, Links(), store);
            ControlPoints.Instance.Place(world.Map);

            return ControlPoints.Instance.ById(PointId);
        }

        private static SpawnPool NewPool(WorldTestContext world, uint id, short mode, long respawnMs) => new SpawnPool
        {
            DbId = id,
            Mode = mode,
            AnimType = 0,
            MapContextId = MapId,
            RuntimeMapChannel = world.Map,
            Position = new Vector3(12, 0, 12),
            RespawnTime = respawnMs,
            SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(MinionCreatureId, 1, 3) }
        };

        private static SpawnPool Pool(WorldTestContext world, uint id) => world.Map.SpawnPools.Single(p => p.DbId == id);

        /// <summary>The pool as it is after it has spawned, with this many alive and on the way.</summary>
        private static void Spawned(WorldTestContext world, uint id, int alive, int queued = 0, int dropships = 0)
        {
            var pool = Pool(world, id);

            pool.HasSpawned = true;
            pool.AliveCreatures = alive;
            pool.QueuedCreatures = queued;
            pool.DropshipQueue = dropships;
        }

        private void Teleporter(uint id, WaypointType type)
        {
            DynamicObjectManager.Instance.Teleporters[id] = new DynamicObject
            {
                MapContextId = MapId,
                Position = new Vector3(20, 0, 20),
                ObjectData = new WaypointInfo(id, false, type)
            };

            _teleporters.Add(id);
        }

        private static bool Contested(uint id) =>
            ((WaypointInfo)DynamicObjectManager.Instance.Teleporters[id].ObjectData).Contested;

        private static int Prestige(Client client) =>
            client.Player.Credits.TryGetValue(CurencyType.Prestige, out var prestige) ? prestige : 0;

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        private static byte[] Encode(MapMarkerState state)
        {
            using var stream = new System.IO.MemoryStream();
            using var binary = new System.IO.BinaryWriter(stream);
            using var writer = new Rasa.Memory.PythonWriter(binary);
            state.Write(writer);
            return stream.ToArray();
        }

        #endregion
    }
}
