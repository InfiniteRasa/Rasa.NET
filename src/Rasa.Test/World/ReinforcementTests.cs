using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;
    using Rasa.Structures.World;


    // How fresh troops come into the world (Reinforcements): the arrival point of no table's a pool with
    // no row of its own is given - whose ship and where it comes down, or a beam-in - what a drop is made
    // of, who beams in, and where a raid's ship comes down round a point within walls and before its gates.
    [TestClass]
    [DoNotParallelize]
    public class ReinforcementTests
    {
        private const EntityClasses ThraxClass = (EntityClasses)3762;       // Bane_Thrax_Soldier_Pistol
        private const EntityClasses GrenadierClass = (EntityClasses)9354;   // Bane_Thrax_Grenadier
        private const EntityClasses FithikClass = (EntityClasses)4313;      // Bane_Fithik_Winged
        private const EntityClasses SoldierClass = (EntityClasses)29423;    // Redshirt_Human_Soldier_Light_Male
        private const EntityClasses ForeanClass = (EntityClasses)6043;      // Redshirt_Forean_Spearman

        private const uint ThraxId = 4_100_000_001;
        private const uint GrenadierId = 4_100_000_002;
        private const uint FithikId = 4_100_000_003;
        private const uint BossId = 4_100_000_004;
        private const uint SoldierId = 4_100_000_005;
        private const uint ForeanId = 4_100_000_006;

        private readonly List<EntityClasses> _classes = new List<EntityClasses>();
        private readonly List<uint> _creatures = new List<uint>();
        private ReinforcementConfig _config;

        [TestInitialize]
        public void Start()
        {
            _config = Reinforcements.Config;
            Reinforcements.Config = new ReinforcementConfig();

            Class(ThraxClass, "Bane_Thrax_Soldier_Pistol");
            Class(GrenadierClass, "Bane_Thrax_Grenadier");
            Class(FithikClass, "Bane_Fithik_Winged");
            Class(SoldierClass, "Redshirt_Human_Soldier_Light_Male");
            Class(ForeanClass, "Redshirt_Forean_Spearman");

            Template(ThraxId, ThraxClass, TargetCategory.Hostile);
            Template(GrenadierId, GrenadierClass, TargetCategory.Hostile);
            Template(FithikId, FithikClass, TargetCategory.Hostile);
            Template(BossId, ThraxClass, TargetCategory.Hostile).ExtraFlags.Add(CreatureFlag.Powerlevel6Boss);
        }

        [TestCleanup]
        public void Restore()
        {
            Reinforcements.Config = _config;

            foreach (var id in _classes)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);

            foreach (var id in _creatures)
                CreatureManager.Instance.LoadedCreatures.Remove(id);

            _classes.Clear();
            _creatures.Clear();
        }

        [TestMethod]
        public void BaneTroopsInTheOpenWorldComeByBaneShip()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world);
            pool.HasSpawned = true;

            var arrival = Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId, ThraxId, GrenadierId));

            Assert.IsNotNull(arrival, "a camp of Thrax on the Wilderness restocks by ship");
            Assert.AreEqual(SpawnPoolArrivalEntry.KindDropship, arrival.Kind, "the Bane's");
            Assert.AreEqual(TargetCategory.Hostile, Reinforcements.SideOf(arrival));
            Assert.IsTrue(Vector3.Distance(arrival.Position, pool.Position) <= Reinforcements.MinLandingRadius + 0.01f, "on the pool's ground");
        }

        [TestMethod]
        public void TheWorldsFirstStockingIsPlainUnlessTheGarrisonIsToBeFlownIn()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world);

            Assert.IsNull(Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId)), "not spawned yet: the world starts stocked");

            pool.ShipNextArrival = true;
            Assert.AreEqual(SpawnPoolArrivalEntry.KindDropship, Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId))?.Kind, "a garrison that has just taken its point over is flown in");

            pool.Arrivals.Add(new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship, PosX = 1, PosY = 2, PosZ = 3, Comment = "pad" });
            pool.HasSpawned = true;
            Assert.IsNull(Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId)), "a pool with arrival rows has its own way in (BaneArrivals)");
            Assert.AreSame(pool.Arrivals[0], BaneArrivals.Pick(pool));
        }

        [TestMethod]
        public void NoShipForBeastsMissionsOtherMapsAnimatedPoolsOrWithTheSettingOff()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world);
            pool.HasSpawned = true;

            Assert.IsNull(Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId, FithikId)), "the Bane's beasts are not flown in, nor beamed in");

            pool.ScenarioKey = "scene";
            Assert.IsNull(Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId)), "a mission stages its own spawns");
            pool.ScenarioKey = null;

            pool.AnimType = 1;
            Assert.IsNull(Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId)), "a pool its data animates has its own ship (SpawnPoolManager)");
            pool.AnimType = 0;

            Reinforcements.Config.BaneDropships = false;
            Assert.AreEqual(SpawnPoolArrivalEntry.KindBeamIn, Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId))?.Kind, "ships off: troops beam in");
            Reinforcements.Config.BeamIn = false;
            Assert.IsNull(Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId)), "and with that off too, as before");
            Reinforcements.Config.BaneDropships = true;
            Reinforcements.Config.BeamIn = true;

            var bootcamp = new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 0, 0), ClientList = new List<Client>() };
            Assert.AreEqual(SpawnPoolArrivalEntry.KindBeamIn, Reinforcements.ArrivalFor(bootcamp, pool, Of(ThraxId))?.Kind, "no ships over the Bootcamp: they beam in");

            Reinforcements.Config.ShipMaps = new List<uint> { 1985 };
            Assert.AreEqual(SpawnPoolArrivalEntry.KindDropship, Reinforcements.ArrivalFor(bootcamp, pool, Of(ThraxId))?.Kind, "the setting names the maps");
            Assert.AreEqual(SpawnPoolArrivalEntry.KindBeamIn, Reinforcements.ArrivalFor(world.Map, pool, Of(ThraxId))?.Kind, "and only those");
        }

        [TestMethod]
        public void AnAfsGarrisonOfSoldiersComesByAfsShip()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world);
            pool.HasSpawned = true;
            Template(SoldierId, SoldierClass, TargetCategory.Friendly);

            Assert.AreNotEqual(SpawnPoolArrivalEntry.KindAfsDropship, Reinforcements.ArrivalFor(world.Map, pool, Of(SoldierId))?.Kind, "not a control point's garrison");

            pool.IsGarrison = true;
            var arrival = Reinforcements.ArrivalFor(world.Map, pool, Of(SoldierId, SoldierId));

            Assert.AreEqual(SpawnPoolArrivalEntry.KindAfsDropship, arrival?.Kind);
            Assert.AreEqual(TargetCategory.Friendly, Reinforcements.SideOf(arrival), "the AFS's");

            Reinforcements.Config.AfsDropships = false;
            Assert.AreNotEqual(SpawnPoolArrivalEntry.KindAfsDropship, Reinforcements.ArrivalFor(world.Map, pool, Of(SoldierId))?.Kind, "off");
        }

        [TestMethod]
        public void ACampThatHasStockedTheWorldRestocksByShipAndComesOutUnderIt()
        {
            using var world = new WorldTestContext();
            var manager = new SpawnPoolManager(null);
            var pool = NewPool(world);
            pool.HasSpawned = true;
            pool.UpdateTimer = pool.RespawnTime;
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            try
            {
                manager.SpawnPoolWorker(world.Map, 1000);

                var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool == pool);
                Assert.AreEqual(TargetCategory.Hostile, ship.TargetCategory, "the Bane's ship");
                Assert.AreEqual(EntityClasses.UsableCrSpawnerBaneDropshipV01, ship.EntityClassId);
                Assert.IsNotNull(ship.Arrival, "it comes down on a spot of its own, and they come out under it");
                Assert.AreEqual(ship.Arrival.Position, ship.Position);
                Assert.AreEqual(1, pool.DropshipQueue);
                Assert.IsTrue(pool.QueuedCreatures >= 2, "the pool's troops are on it");
            }
            finally
            {
                foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s => s.SpawnPool == pool).ToArray())
                {
                    CellManager.Instance.RemoveFromWorld(world.Map, ship);
                    DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                }
            }
        }

        [TestMethod]
        public void AGarrisonUnderSiegeIsNotRestocked()
        {
            using var world = new WorldTestContext();
            var manager = new SpawnPoolManager(null);
            var pool = NewPool(world);
            pool.HasSpawned = true;
            pool.UpdateTimer = pool.RespawnTime;
            pool.Besieged = true;
            manager.LoadedSpawnPools.Add(pool.DbId, pool);

            manager.SpawnPoolWorker(world.Map, 1000);

            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => s.SpawnPool == pool), "no ship");
            Assert.AreEqual(0, pool.QueuedCreatures + pool.AliveCreatures, "nobody");
        }

        [TestMethod]
        public void BeamingInIsForTroopsOnly()
        {
            Assert.IsTrue(Reinforcements.BeamsIn(Of(ThraxId, GrenadierId)));
            Assert.IsFalse(Reinforcements.BeamsIn(Of(ThraxId, FithikId)), "a beast among them");
            Assert.IsFalse(Reinforcements.BeamsIn(new List<Creature>()));

            Reinforcements.Config.BeamIn = false;
            Assert.IsFalse(Reinforcements.BeamsIn(Of(ThraxId)), "off");
        }

        [TestMethod]
        public void TheAfsAlliesBeamInAsItsSoldiersDo()
        {
            var forean = Template(ForeanId, ForeanClass, TargetCategory.Friendly);
            Template(SoldierId, SoldierClass, TargetCategory.Friendly);

            Assert.IsTrue(Reinforcements.IsAlly(forean));
            Assert.IsTrue(Reinforcements.BeamsIn(Of(ForeanId)), "a Forean warrior: his class has the Thrax's CREATURE_BIRTH");
            Assert.IsTrue(Reinforcements.BeamsIn(Of(SoldierId, ForeanId)), "soldiers and Foreans together");
            Assert.IsFalse(Reinforcements.BeamsIn(Of(ForeanId, FithikId)), "a beast among them");
            Assert.IsFalse(Reinforcements.IsAfsTroop(forean), "an AFS ship still brings only the AFS's soldiers");
            Assert.IsFalse(Reinforcements.IsAlly(CreatureManager.Instance.LoadedCreatures[ThraxId]), "nor are the Bane anybody's allies");
        }

        [TestMethod]
        public void ARaidsShipsComeDownOutsideTheWallsOfAPointWithinThem()
        {
            // A fort 80 m across round the origin, walls 10 m high, a gate in the south wall.
            var vertices = new List<float>();
            var indices = new List<int>();

            void Wall(float x0, float z0, float x1, float z1)
            {
                var first = vertices.Count / 3;
                vertices.AddRange(new[] { x0, 0f, z0, x1, 0f, z1, x1, 10f, z1, x0, 10f, z0 });
                indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            Wall(40, -40, 40, 40);
            Wall(-40, -40, -40, 40);
            Wall(-40, 40, 40, 40);
            Wall(-40, -40, -5, -40);
            Wall(5, -40, 40, -40);

            var fort = new MapChannel { MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 0, 0), Cover = CoverMesh.Build(vertices, indices) };
            var open = new MapChannel { MapInfo = new MapInfo(1220, "adv_foreas_concordia_wilderness", 0, 0), Cover = CoverMesh.Build(new List<float>(), new List<int>()) };

            Assert.IsTrue(Reinforcements.Enclosed(fort, Vector3.Zero), "the point stands within the fort's walls");
            Assert.IsFalse(Reinforcements.OutsideWalls(fort, Vector3.Zero, new Vector3(20, 0, 0)), "not in its courtyard");
            Assert.IsFalse(Reinforcements.OutsideWalls(fort, Vector3.Zero, new Vector3(0, 0, -30)), "nor just inside its gate");
            Assert.IsTrue(Reinforcements.OutsideWalls(fort, Vector3.Zero, new Vector3(70, 0, 0)), "outside its east wall");
            Assert.IsTrue(Reinforcements.OutsideWalls(fort, Vector3.Zero, new Vector3(-60, 0, 45)), "outside a corner");
            Assert.IsFalse(Reinforcements.Enclosed(open, Vector3.Zero), "a point in the open");
            Assert.IsTrue(Reinforcements.OutsideWalls(open, Vector3.Zero, new Vector3(20, 0, 0)), "anywhere round it");
            Assert.IsTrue(Reinforcements.OutsideWalls(new MapChannel(), Vector3.Zero, new Vector3(20, 0, 0)), "a map with no cover mesh: as before");

            for (var bearing = 0.0; bearing < Math.PI * 2; bearing += 0.7)
            {
                var spot = Reinforcements.RaidLanding(fort, Vector3.Zero, 40f, 75f, bearing);

                Assert.IsNotNull(spot, $"bearing {bearing:0.0}: somewhere outside");
                Assert.IsTrue(Reinforcements.OutsideWalls(fort, Vector3.Zero, spot.Value), $"bearing {bearing:0.0}: {spot} is outside the walls");
                Assert.IsTrue(new Vector2(spot.Value.X, spot.Value.Z).Length() <= Reinforcements.RaidLandingFurthest + 0.01f, "within 120 m");
            }
        }

        [TestMethod]
        public void AShipComesDownStraightOutFromAGateAndTheNextBesideIt()
        {
            using var world = new WorldTestContext();
            var point = new Vector3(0, 0, 0);
            var gate = new Vector3(0, 0, -40);
            var ships = new List<Dropship>();

            Assert.AreEqual(new Vector3(0, 0, -66), Reinforcements.BeforeGate(point, gate, Vector3.UnitZ, 26f), "26 m out, on the side away from the point, whichever way the sheet faces");
            Assert.AreEqual(new Vector3(0, 0, -66), Reinforcements.BeforeGate(point, gate, -Vector3.UnitZ, 26f));
            Assert.AreEqual(new Vector3(-7, 0, -66), Reinforcements.BeforeGate(point, gate, -Vector3.UnitZ, 26f, -7f), "and to the side of the line");

            try
            {
                var first = Reinforcements.GateLanding(world.Map, point, gate, Vector3.UnitZ);

                Assert.AreEqual(new Vector3(0, 0, -66), first, "the first ship straight before the gate");

                var ship = new Dropship(TargetCategory.Hostile, DropshipType.Spawner, NewPool(world)) { Position = first.Value, RuntimeMapChannel = world.Map };
                DynamicObjectManager.Instance.Dropships.Add(ship.EntityId, ship);
                ships.Add(ship);

                var second = Reinforcements.GateLanding(world.Map, point, gate, Vector3.UnitZ);

                Assert.IsNotNull(second, "the second has room");
                Assert.IsTrue(Math.Abs(second.Value.X) >= Reinforcements.HullRadius * 2, $"beside the first, not in it: {second}");
                Assert.IsTrue(second.Value.Z < gate.Z, "and before the gate too");
                Assert.IsTrue(second.Value.Length() <= Reinforcements.RaidLandingFurthest, "near the point");
            }
            finally
            {
                foreach (var ship in ships)
                {
                    DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                    EntityManager.Instance.FreeEntity(ship.EntityId);
                }
            }
        }

        [TestMethod]
        public void BeforeTheWildernessGatesThereIsRoomForARaidsShip()
        {
            using var world = new WorldTestContext();
            var directory = Path.Combine(RepositoryRoot(), "navmesh");
            var coverPath = Path.Combine(directory, "adv_foreas_concordia_wilderness.cover");
            var navPath = NavMeshFile.PathFor(directory, "adv_foreas_concordia_wilderness");

            if (!File.Exists(coverPath) || !File.Exists(navPath))
                Assert.Inconclusive("The Wilderness's navmesh and cover are not in navmesh/.");

            world.Map.Cover = CoverMesh.Read(coverPath);
            world.Map.NavMesh = TestNavMeshes.Query(navPath);

            // control_point 4 and its outer fence gate; control_point 3 and its one entrance (our fork's
            // control_point_field rows 10 and 6, measured on the map).
            var gates = new[]
            {
                (Name: "the Landing Zone's fence gate", Point: new Vector3(197.7f, 162.3f, -54.1f), Class: "humgate", At: new Vector3(198f, 163.05f, -33.55f), Yaw: -3.142f),
                (Name: "Imperial Valley's entrance", Point: new Vector3(-357.1f, 174.6f, -528.6f), Class: "baneminor", At: new Vector3(-342.93f, 173.66f, -496.36f), Yaw: 0.314f)
            };

            foreach (var gate in gates)
            {
                var opening = new OutpostWalls.Gate { Class = ForceFields.ClassOf(gate.Class), Position = gate.At, Yaw = gate.Yaw };
                var spot = Reinforcements.GateLanding(world.Map, gate.Point, opening.Middle, opening.Through);

                Assert.IsNotNull(spot, $"{gate.Name}: a ship has room before it");

                var outFromGate = new Vector2(spot.Value.X - opening.Middle.X, spot.Value.Z - opening.Middle.Z).Length();
                var fromPoint = new Vector2(spot.Value.X - gate.Point.X, spot.Value.Z - gate.Point.Z).Length();
                var gateFromPoint = new Vector2(opening.Middle.X - gate.Point.X, opening.Middle.Z - gate.Point.Z).Length();

                Console.WriteLine($"{gate.Name}: ship down at ({spot.Value.X:0.#}, {spot.Value.Y:0.#}, {spot.Value.Z:0.#}), {outFromGate:0.#} m out from the gate, {fromPoint:0.#} m from the point (the gate {gateFromPoint:0.#} m)");

                Assert.IsTrue(outFromGate >= 15f && outFromGate <= 62f, $"{gate.Name}: {outFromGate:0.#} m out from the gate");
                Assert.IsTrue(fromPoint > gateFromPoint + 10f, $"{gate.Name}: the gate stands between the ship and the point");
                Assert.IsTrue(Reinforcements.Reaches(world.Map, spot.Value, NavMeshManager.SnapToGround(world.Map, gate.Point)), $"{gate.Name}: a way on foot to the point");
            }

            // And round the Landing Zone with no gate known: outside its fort, within 120 m, with a way to it.
            var lz = gates[0].Point;

            for (var bearing = 0.0; bearing < Math.PI * 2; bearing += 1.3)
            {
                var spot = Reinforcements.RaidLanding(world.Map, lz, BaneRaids.LandingMin, BaneRaids.LandingMax, bearing);

                Assert.IsNotNull(spot, $"bearing {bearing:0.0}: somewhere round the Landing Zone");
                Console.WriteLine($"round the Landing Zone from bearing {bearing:0.0}: ({spot.Value.X:0.#}, {spot.Value.Y:0.#}, {spot.Value.Z:0.#}), {Vector3.Distance(spot.Value, lz):0} m out");
                Assert.IsTrue(Vector3.Distance(spot.Value, lz) <= Reinforcements.RaidLandingFurthest + 5f, "within 120 m");
                Assert.IsTrue(Reinforcements.OutsideWalls(world.Map, lz, spot.Value), "outside its walls");
            }
        }

        [TestMethod]
        public void ADropIsTheCommonestRankAndFileOfTheNearestPools()
        {
            using var world = new WorldTestContext();

            world.Map.SpawnPools.Add(NewPool(world, 1, new Vector3(30, 0, 0), (GrenadierId, 1, 2), (ThraxId, 2, 3), (BossId, 1, 1)));
            world.Map.SpawnPools.Add(NewPool(world, 2, new Vector3(60, 0, 0), (ThraxId, 3, 4), (FithikId, 5, 5)));
            world.Map.SpawnPools.Add(NewPool(world, 3, new Vector3(2000, 0, 0), (GrenadierId, 9, 9)));

            var troops = Reinforcements.TroopsNear(world.Map, Vector3.Zero, 500f);

            CollectionAssert.AreEqual(new List<uint> { ThraxId, GrenadierId }, troops, "Thrax first (7 of them to 2), no boss, no beast, nothing out of reach");
            CollectionAssert.AreEqual(new List<uint> { ThraxId }, Reinforcements.TroopsNear(world.Map, Vector3.Zero, 500f, kinds: 1));
            Assert.AreEqual(0, Reinforcements.TroopsNear(world.Map, new Vector3(-5000, 0, 0), 500f).Count);
        }

        [TestMethod]
        public void ATransientPoolIsNoTablesAndScripted()
        {
            using var world = new WorldTestContext();
            var pool = Reinforcements.TransientPool(world.Map, new Vector3(1, 2, 3), 0, 6f, new[] { new SpawnPoolSlot(ThraxId, 0, 0) });

            Assert.IsTrue(Reinforcements.IsTransient(pool));
            Assert.IsTrue(pool.DbId >= Reinforcements.FirstTransientPoolId);
            Assert.AreEqual(SpawnPoolManager.ModeScripted, pool.Mode, "never restocks on its own");
            Assert.AreSame(world.Map, pool.RuntimeMapChannel);
            Assert.IsFalse(world.Map.SpawnPools.Contains(pool), "on no map's list");
            Assert.IsFalse(Reinforcements.IsTransient(NewPool(world)));
        }

        [TestMethod]
        public void BothSidesReserveHullRoomUntilTheirShipLeaves()
        {
            using var world = new WorldTestContext();
            var ships = DynamicObjectManager.Instance.Dropships;

            foreach (var side in new[] { TargetCategory.Friendly, TargetCategory.Hostile })
            {
                var ship = new Dropship(side, DropshipType.Spawner, NewPool(world)) { Position = Vector3.Zero, RuntimeMapChannel = world.Map };
                ships.Add(ship.EntityId, ship);

                try
                {
                    Assert.IsFalse(Reinforcements.IsOpenSky(world.Map, new Vector3(20, 0, 0)), "hulls cannot overlap even when they belong to opposite sides");
                    Assert.IsTrue(Reinforcements.IsOpenSky(world.Map, new Vector3(40, 0, 0)), "a clear neighbouring landing is available");

                    using var other = new WorldTestContext();
                    Assert.IsTrue(Reinforcements.IsOpenSky(other.Map, Vector3.Zero), "another channel has its own sky");
                }
                finally
                {
                    ships.Remove(ship.EntityId);
                    EntityManager.Instance.FreeEntity(ship.EntityId);
                }

                Assert.IsTrue(Reinforcements.IsOpenSky(world.Map, Vector3.Zero), "a ship that has left gives its room back");
            }
        }

        [TestMethod]
        public void HullClearanceRejectsGeometryBesideTheBeam()
        {
            using var world = new WorldTestContext();
            world.Map.Cover = CoverMesh.Build(
                new List<float> { 10, 6, -20, 10, 6, 20, 10, 14, 20, 10, 14, -20 },
                new List<int> { 0, 1, 2, 0, 2, 3 });

            Assert.IsFalse(Reinforcements.IsOpenSky(world.Map, Vector3.Zero), "a wall through the wing is blocked although the beam itself is clear");
            Assert.IsTrue(Reinforcements.IsOpenSky(world.Map, new Vector3(-30, 0, 0)), "moving the hull away from that wall clears it");
        }

        [TestMethod]
        public void ASpawnerShipStaysInEachStateForItsClip()
        {
            using var world = new WorldTestContext();

            var bane = Fly(world, TargetCategory.Hostile);
            Assert.AreEqual(5750, bane.Built, "the Bane ship's fly-in clip is 5.73 s");
            Assert.IsTrue(Math.Abs(bane.TroopsAt - 7750) <= 500, $"its troops down its beam two seconds into its landing: {bane.TroopsAt}");
            Assert.IsTrue(Math.Abs(bane.GoneAt - 20000) <= 750, $"its landing 8.25 s, its fly-out 6 s: {bane.GoneAt}");

            var afs = Fly(world, TargetCategory.Friendly);
            Assert.AreEqual(5000, afs.Built, "the AFS ship's fly-in is shorter than the five seconds it had: as it was");
            Assert.IsTrue(Math.Abs(afs.GoneAt - 15650) <= 750, $"its fly-out 5.65 s: {afs.GoneAt}");

            Reinforcements.Config.ClipTimings = false;
            var before = Fly(world, TargetCategory.Hostile);
            Assert.AreEqual(5000, before.Built, "off: five seconds a state");
            Assert.IsTrue(Math.Abs(before.TroopsAt - 7000) <= 500 && Math.Abs(before.GoneAt - 15000) <= 750, $"as before: {before.TroopsAt}, {before.GoneAt}");
        }

        /// <summary>A spawner ship with nobody aboard flown through the worker a quarter second at a time: its fly-in, when it set its troops down, when it was gone.</summary>
        private static (long Built, long TroopsAt, long GoneAt) Fly(WorldTestContext world, TargetCategory side)
        {
            var pool = NewPool(world);
            var ship = new Dropship(side, DropshipType.Spawner, pool) { RuntimeMapChannel = world.Map };
            var built = ship.PhaseTimeleft;
            var ships = DynamicObjectManager.Instance.Dropships;
            long t = 0, troopsAt = -1;

            pool.QueuedCreatureList = new List<Creature>();
            pool.DropshipQueue = 1;
            ships.Add(ship.EntityId, ship);

            try
            {
                while (ships.ContainsKey(ship.EntityId) && t < 60_000)
                {
                    DynamicObjectManager.Instance.DropshipsWorker(world.Map, 250);
                    t += 250;

                    if (troopsAt < 0 && ship.Phase >= 3)
                        troopsAt = t;
                }
            }
            finally
            {
                ships.Remove(ship.EntityId);
                EntityManager.Instance.FreeEntity(ship.EntityId);
            }

            return (built, troopsAt, t);
        }

        #region Fixtures

        private void Class(EntityClasses id, string name)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (classes.ContainsKey(id))
                return;

            classes.Add(id, new EntityClass((uint)id, name, 0, 0, new List<AugmentationType> { AugmentationType.Creature }, true));
            _classes.Add(id);
        }

        private Creature Template(uint id, EntityClasses classId, TargetCategory side)
        {
            var template = new Creature { DbId = id, EntityClass = classId, TargetCategory = side, WalkSpeed = 5, RunSpeed = 9 };

            CreatureManager.Instance.LoadedCreatures[id] = template;
            _creatures.Add(id);

            return template;
        }

        private static List<Creature> Of(params uint[] ids) =>
            ids.Select(id => CreatureManager.Instance.LoadedCreatures[id]).ToList();

        private static SpawnPool NewPool(WorldTestContext world) => NewPool(world, 77, new Vector3(10, 0, 10), (ThraxId, 2, 3));

        private static SpawnPool NewPool(WorldTestContext world, uint id, Vector3 at, params (uint Id, short Min, short Max)[] slots) => new SpawnPool
        {
            DbId = id,
            MapContextId = world.Map.MapInfo.MapContextId,
            RuntimeMapChannel = world.Map,
            Position = at,
            Radius = 8,
            RespawnTime = 90_000,
            SpawnSlot = slots.Select(s => new SpawnPoolSlot(s.Id, s.Min, s.Max)).ToList()
        };

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new DirectoryNotFoundException("navmesh");
        }

        #endregion
    }
}
