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
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// A Bane base's intrusion alarm (BaneAlarms): a security camera that sees a player sounds it
    /// and brings Thrax through the nearest teleporters, again while the camera stands; the
    /// databanks bring the exit wave once; and in a squad's instance what is destroyed stays
    /// destroyed as long as what is killed stays killed.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BaneAlarmTests
    {
        private const uint Wilderness = 1220;
        private const uint ThraxSoldier = 3;        // Bane_Thrax_Soldier
        private const EntityClasses Camera = (EntityClasses)BaneAlarms.CameraClass;
        private const EntityClasses Light = (EntityClasses)BaneAlarms.AlarmLightClass;
        private const EntityClasses Siren = (EntityClasses)BaneAlarms.SirenClass;
        private const EntityClasses Databank = (EntityClasses)10191;     // UsableInertDestBaneSecurityMachineryV01

        private long _now;
        private BaneAlarms.Site _site;
        private readonly List<uint> _creatures = new();
        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();

        [TestInitialize]
        public void Start()
        {
            _now = 7_000_000;
            BaneAlarms.Now = () => _now;
            PlacedDestructibles.Now = () => _now;
            PlacedUsables.Now = () => _now;
            PlacedDestructibles.Roll = (min, max) => 50_000;

            // The test's own site, on the fixture map: camera pools 901-904, exit pools 905-906.
            _site = new BaneAlarms.Site
            {
                MapContextId = Wilderness,
                CameraPools = new uint[] { 901, 902, 903, 904 },
                ExitPools = new uint[] { 905, 906 },
                DatabankClasses = new[] { (uint)Databank }
            };
            ((List<BaneAlarms.Site>)BaneAlarms.Sites).Add(_site);

            AddClass(Camera, "UsableInertDestBaneCameraV01", true, AugmentationType.InertDestroyable);
            AddClass(Databank, "UsableInertDestBaneSecurityMachineryV01", true, AugmentationType.InertDestroyable);
            AddClass(Light, "UsableTwoStateBaneAlarmLightV01", false, AugmentationType.TwoStateSwitch);
            AddClass(Siren, "UsableTwoStateBaneControlPointSirenV01Intrusion", false, AugmentationType.TwoStateSwitch);
        }

        private void AddClass(EntityClasses id, string name, bool targetable, params AugmentationType[] augmentations)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, 1, augmentations.ToList(), targetable);
        }

        [TestCleanup]
        public void Restore()
        {
            ((List<BaneAlarms.Site>)BaneAlarms.Sites).Remove(_site);
            BaneAlarms.Now = () => Environment.TickCount64;
            BaneAlarms.InTheClear = (map, eye, player) => map.Cover == null || Cover.Visible(map.Cover, eye, Cover.SamplePoints(player, eye)) > 0;
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
            PlacedUsables.Load(null);
            PlacedUsables.Reset();

            foreach (var id in _creatures)
                CreatureManager.Instance.LoadedCreatures.Remove(id);

            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            for (var i = _classes.Count - 1; i >= 0; i--)
            {
                if (_classes[i].Had)
                    classes[_classes[i].Id] = _classes[i].Was;
                else
                    classes.Remove(_classes[i].Id);
            }
            _classes.Clear();
        }

        [TestMethod]
        public void ACameraThatSeesAPlayerSoundsTheAlarmAndBringsWavesWhileItStands()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var map = world.Map;
            Define(harness, ThraxSoldier);
            BaneAlarms.Forget(map);

            // The camera at the origin of the base, facing -z (rotation 0: facing (0, 0, -1)); two
            // teleporters beside it, two farther off; alarm light and siren.
            var pools = new[] { Pool(map, 901, new Vector3(100, 2, 90)), Pool(map, 902, new Vector3(108, 2, 92)), Pool(map, 903, new Vector3(140, 2, 60)), Pool(map, 904, new Vector3(160, 2, 60)) };
            PlacedDestructibles.Load(new[] { Destructible(1, Camera, 100, 100, rotation: 0) });
            PlacedDestructibles.Place(map);
            PlacedUsables.Load(new[] { Usable(1, Light, 104, 100), Usable(2, Siren, 96, 100) });
            PlacedUsables.Place(map);
            var camera = PlacedDestructibles.LiveOf(map, 1);
            var light = PlacedUsables.LiveOf(map, 1);
            var siren = PlacedUsables.LiveOf(map, 2);

            try
            {
                // Behind the camera, and out of its range: nothing.
                var intruder = Stand(world, new Vector3(100, 2, 110));
                BaneAlarms.Worker(map);
                Assert.IsFalse(BaneAlarms.Of(map).Sounding, "behind the lens");
                intruder.Player.Position = new Vector3(100, 2, 70);
                _now += BaneAlarms.LookEveryMs;
                BaneAlarms.Worker(map);
                Assert.IsFalse(BaneAlarms.Of(map).Sounding, "30 m off, out of range");

                // Hidden in front of it: nothing. (A GM's invisibility is the cheapest hide.)
                intruder.Player.Position = new Vector3(100, 2, 88);
                BaneAlarms.InTheClear = (_, _, _) => false;
                _now += BaneAlarms.LookEveryMs;
                BaneAlarms.Worker(map);
                Assert.IsFalse(BaneAlarms.Of(map).Sounding, "behind cover");
                BaneAlarms.InTheClear = (_, _, _) => true;

                // In front of it, 12 m off, in the clear: the alarm, and the first wave through the two nearest teleporters.
                _now += BaneAlarms.LookEveryMs;
                BaneAlarms.Worker(map);
                var alarm = BaneAlarms.Of(map);
                Assert.IsTrue(alarm.Sounding);
                Assert.AreSame(intruder.Player, alarm.Intruder);
                Assert.AreEqual(UseObjectState.TsState1, light.State, "the light on");
                Assert.AreEqual(UseObjectState.TsState1, siren.State, "the siren on");
                Assert.AreEqual(2, BaneArrivals.Pending(map), "two teleporters working");
                Assert.AreEqual(2, pools[0].QueuedCreatures);
                Assert.AreEqual(2, pools[1].QueuedCreatures);
                Assert.AreEqual(0, pools[2].QueuedCreatures, "the far ones left alone");
                Assert.AreEqual(2, alarm.Waves);

                // Through: four Thrax, every one of them out for the intruder.
                SpawnPoolManager.Instance.SpawnPoolWorker(map, BaneArrivals.TeleportLeadMs);
                var thrax = Creatures(map).Where(c => c.SpawnPool != null && c.SpawnPool.DbId is 901 or 902).ToList();
                Assert.HasCount(4, thrax);
                Assert.IsTrue(thrax.All(c => c.Hate.Contains(intruder.Player.EntityId)), "they hunt whoever tripped it");
                SpawnPoolManager.Instance.SpawnPoolWorker(map, BaneArrivals.TeleportHoldMs);
                Assert.AreEqual(0, BaneArrivals.Pending(map));

                // Not again until the wave is due; then again, the camera still standing.
                _now += BaneAlarms.WaveEveryMs - 10_000;
                BaneAlarms.Worker(map);
                Assert.AreEqual(0, BaneArrivals.Pending(map));
                _now += 10_000;
                BaneAlarms.Worker(map);
                Assert.AreEqual(2, BaneArrivals.Pending(map), "the next wave");
                Assert.AreEqual(4, alarm.Waves);
                SpawnPoolManager.Instance.SpawnPoolWorker(map, BaneArrivals.TeleportHoldMs);

                // The camera shot out: no more waves, and the lights go off once the hold is up.
                Assert.IsTrue(PlacedDestructibles.Destroy(camera, intruder.Player));
                _now += BaneAlarms.WaveEveryMs;
                BaneAlarms.Worker(map);
                Assert.AreEqual(0, BaneArrivals.Pending(map), "a camera that is down brings nothing");
                Assert.IsTrue(alarm.Sounding, "still sounding: the hold runs from the last wave");
                _now += BaneAlarms.HoldMs;
                BaneAlarms.Worker(map);
                Assert.IsFalse(alarm.Sounding);
                Assert.AreEqual(UseObjectState.TsState0, light.State);
                Assert.AreEqual(UseObjectState.TsState0, siren.State);
            }
            finally
            {
                Clear(map);
            }
        }

        [TestMethod]
        public void TheDatabanksBringTheExitWaveOnceAndSoundTheAlarm()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            using var world = new WorldTestContext();
            var map = world.Map;
            Define(harness, ThraxSoldier);
            BaneAlarms.Forget(map);

            var exits = new[] { Pool(map, 905, new Vector3(200, 2, 0)), Pool(map, 906, new Vector3(220, 2, -40)) };
            Pool(map, 901, new Vector3(100, 2, 90));
            PlacedDestructibles.Load(new[] { Destructible(1, Databank, 150, 20), Destructible(2, Databank, 154, 20) });
            PlacedDestructibles.Place(map);
            PlacedUsables.Load(new[] { Usable(1, Light, 150, 24) });
            PlacedUsables.Place(map);

            try
            {
                var player = Stand(world, new Vector3(150, 2, 10));
                var alarm = BaneAlarms.Of(map);

                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, 1), player.Player));
                Assert.IsTrue(alarm.Sounding);
                Assert.IsTrue(alarm.ExitWaveSent);
                Assert.AreEqual(UseObjectState.TsState1, PlacedUsables.LiveOf(map, 1).State);
                Assert.AreEqual(2, BaneArrivals.Pending(map), "one teleporter of each exit pool");
                Assert.IsTrue(exits.All(pool => pool.QueuedCreatures == 2), "two through each");
                SpawnPoolManager.Instance.SpawnPoolWorker(map, BaneArrivals.TeleportHoldMs);
                Assert.IsTrue(Creatures(map).Where(c => c.SpawnPool != null && c.SpawnPool.DbId is 905 or 906).All(c => c.Hate.Contains(player.Player.EntityId)));

                // The second databank: the alarm kept on, no second wave.
                _now += 60_000;
                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, 2), player.Player));
                Assert.AreEqual(0, BaneArrivals.Pending(map));
                Assert.AreEqual(2, alarm.Waves);
                Assert.AreEqual(_now + BaneAlarms.HoldMs, alarm.QuietAt, "the hold runs from the last thing that sounded it");

                // Nothing is watching: off when the hold is up.
                _now += BaneAlarms.HoldMs;
                BaneAlarms.Worker(map);
                Assert.IsFalse(alarm.Sounding);
                Assert.AreEqual(UseObjectState.TsState0, PlacedUsables.LiveOf(map, 1).State);
            }
            finally
            {
                Clear(map);
            }
        }

        [TestMethod]
        public void InASquadsInstanceWhatIsDestroyedStaysDownUntilTheInstanceRespawns()
        {
            using var world = new WorldTestContext();
            var map = world.Map;

            Assert.AreEqual(_now + 50_000, PlacedDestructibles.RespawnAtFor(map, _now), "an ordinary map: the roll");

            var respawn = 0L;
            map.SquadState = new SquadInstanceState(0, 0, () => 1, () => respawn, (_, _, _) => { }, (_, _) => { }, _ => { });
            Assert.AreEqual(long.MaxValue, PlacedDestructibles.RespawnAtFor(map, _now), "no respawn in the instance: down for good");
            respawn = 1_800_000;
            Assert.AreEqual(_now + 1_800_000, PlacedDestructibles.RespawnAtFor(map, _now), "the instance's own respawn");

            // Through Hit: a destroyed object on the instance is due back by the instance's clock.
            PlacedDestructibles.Load(new[] { Destructible(1, Databank, 150, 20) });
            PlacedDestructibles.Place(map);
            try
            {
                var live = PlacedDestructibles.LiveOf(map, 1);
                PlacedDestructibles.Hit(map, null, live.Object, (int)live.MaxHitPoints);
                Assert.IsTrue(live.IsDown);
                Assert.AreEqual(_now + 1_800_000, live.RespawnAt);
            }
            finally
            {
                map.SquadState = null;
                Clear(map);
            }
        }

        private static SpawnPool Pool(MapChannel map, uint id, Vector3 teleporter)
        {
            var pool = new SpawnPool
            {
                DbId = id,
                MapContextId = map.MapInfo.MapContextId,
                Mode = SpawnPoolManager.ModeScripted,
                AnimType = 0,
                RespawnTime = 900_000,
                Position = teleporter + new Vector3(3, 0, 3),
                Radius = 4,
                RuntimeMapChannel = map,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(ThraxSoldier, 2, 3) }
            };
            pool.Arrivals.Add(new SpawnPoolArrivalEntry { Id = id, PoolId = id, Kind = SpawnPoolArrivalEntry.KindTeleporter, PosX = teleporter.X, PosY = teleporter.Y, PosZ = teleporter.Z, EntityId = 7000 + id });
            map.SpawnPools.Add(pool);
            return pool;
        }

        private static WorldDestructibleEntry Destructible(uint id, EntityClasses classId, float x, float z, double rotation = 0.5) => new()
        {
            Id = id, MapContextId = Wilderness, ClassId = (uint)classId, PosX = x, PosY = 2, PosZ = z, Rotation = rotation, Comment = "fixture"
        };

        private static WorldUsableEntry Usable(uint id, EntityClasses classId, float x, float z) => new()
        {
            Id = id, MapContextId = Wilderness, ClassId = (uint)classId, PosX = x, PosY = 2, PosZ = z, Rotation = 0, State = 0, Usable = 0, Comment = "fixture"
        };

        private static Client Stand(WorldTestContext world, Vector3 at)
        {
            var client = world.CreateClient(at.X, at.Z);
            client.Player.Position = at;
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 2000, 2000, 2000, 0, 0);
            CellManager.Instance.AddToWorld(client);
            return client;
        }

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct();

        /// <summary>A creature of the world database, known to the creature manager as the server has it.</summary>
        private void Define(BootcampRuntimeTestHarness.Harness harness, uint creatureId)
        {
            var entry = harness.WorldContext.Set<Rasa.Structures.World.CreatureEntry>().AsNoTracking().Single(row => row.Id == creatureId);
            var classEntry = harness.WorldContext.Set<Rasa.Structures.World.EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
            var entityClass = new EntityClass(classEntry.Id, classEntry.ClassName, classEntry.MeshId, classEntry.ClassCollisionRole,
                classEntry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), classEntry.TargetFlag != 0);

            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.ClassId] = entityClass;
            CreatureManager.Instance.LoadedCreatures[entry.Id] = new Creature(entry) { AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
            _creatures.Add(entry.Id);
        }

        private static void Clear(MapChannel map)
        {
            foreach (var creature in Creatures(map).ToList())
            {
                foreach (var cell in map.MapCellInfo.Cells.Values)
                    cell.CreatureList.Remove(creature);

                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }

            map.SpawnPools.Clear();
            PlacedDestructibles.Forget(map);
            PlacedUsables.Forget(map);
            BaneAlarms.Forget(map);
        }
    }
}
