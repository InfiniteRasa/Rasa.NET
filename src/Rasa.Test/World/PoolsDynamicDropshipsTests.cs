using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;
using Content = Rasa.Services.Preloader.PoolsDynamicDropshipsContent;

namespace Rasa.Test.World
{
    /// <summary>Valverde Pools' control points: AFS garrisons by dropship, timed Bane assaults, AFS relief and sorties.</summary>
    [TestClass, DoNotParallelize]
    public class PoolsDynamicDropshipsTests
    {
        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _config;
        private Func<long> _clock;
        private long _now;

        private static ControlPoints.Point Point(uint id) => ControlPoints.Instance.ById(id);

        [TestInitialize]
        public void Start()
        {
            _config = ControlPoints.Instance.Config;
            _clock = LandingZoneAssaults.Now;
            _now = 1000;
            LandingZoneAssaults.Now = () => _now;
            ControlPoints.Instance.Config = new ControlPointConfig();
            _world = WildernessRuntimeTestHarness.Create(mapContextId: Content.MapId);
            using var db = _world.CreateWorld();
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.MapContextId == Content.MapId),
                db.ControlPoints.GetLinks().Where(l => l.ControlPointId is >= 7 and <= 9), null);
            ControlPoints.Instance.Place(_world.Map);
        }

        [TestCleanup]
        public void End()
        {
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
            foreach (var id in new uint[] { 7, 8, 9 })
                LandingZoneAssaults.Worker(_world.Map, Point(id));
            ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
            _world.Dispose();
            ControlPoints.Instance.Config = _config;
            LandingZoneAssaults.Now = _clock;
        }

        private SpawnPool[] Assaults(uint point) => _world.Map.SpawnPools.Where(p => p.IsControlPointAssault && p.AssaultPointId == point).ToArray();
        private SpawnPool[] Relief(uint point) => _world.Map.SpawnPools.Where(p => p.IsControlPointReinforcement && p.AssaultPointId == point).ToArray();
        private Creature[] Troops(uint point) => EntityManager.Instance.Creatures.Values
            .Where(c => Assaults(point).Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        private void Deliver() { for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000); }
        private void Tick() => ControlPoints.Instance.Worker(_world.Map);

        private Vector3 Walk(Vector3 at, string what)
        {
            var ground = NavMeshManager.NearestWalkable(_world.Map, at);
            Assert.IsNotNull(ground, $"{what}: no ground");
            return ground.Value;
        }

        private void Reaches(Vector3 from, uint point, string what)
        {
            var path = NavMeshManager.FindPathBounded(_world.Map, Walk(from, what), Walk(Point(point).Position, $"point {point}"), out var complete);
            Assert.IsTrue(complete && path?.Count > 0, $"{what} must reach point {point}");
        }

        [TestMethod]
        public void EveryDropPadReliefPadAndGarrisonPostStandsOnGroundThatReachesItsPoint()
        {
            Assert.AreEqual(3, Content.Assaults.Length);
            foreach (var assault in Content.Assaults)
            {
                Assert.AreEqual(Content.MapId, assault.MapId, $"point {assault.PointId}");
                Assert.AreEqual(2, assault.Drops.Length, $"point {assault.PointId}: two Bane ships a wave");
                for (var i = 0; i < assault.Drops.Length; i++)
                {
                    Reaches(assault.Drops[i].Position, assault.PointId, $"point {assault.PointId} drop {i + 1}");
                    var distance = Vector3.Distance(assault.Drops[i].Position, Point(assault.PointId).Position);
                    Assert.IsTrue(distance is > 45 and < 100, $"point {assault.PointId} drop {i + 1} lands outside the walls, {distance:0} m");
                }
                Reaches(assault.ReliefPad.Position, assault.PointId, $"point {assault.PointId} relief pad");
                Reaches(assault.ReliefPost, assault.PointId, $"point {assault.PointId} relief post");
            }
            foreach (var pool in Content.Garrison)
            {
                Assert.IsTrue(Vector3.Distance(Walk(pool.At, $"pool {pool.Id}"), pool.At) < 1.5f, $"pool {pool.Id} stands on the ground");
                Reaches(pool.At, pool.Point, $"pool {pool.Id}");
            }
        }

        [TestMethod]
        public void EveryPlanUsesThePoolsOwnAfsTroops()
        {
            var afs = new[] { Content.Rifleman, Content.MachineGunner, Content.Mech };
            foreach (var assault in Content.Assaults)
            {
                Assert.IsTrue(assault.Relief.Concat(assault.Sortie).All(t => afs.Contains(t.Creature)), $"point {assault.PointId}");
                Assert.AreSame(assault, DynamicDropshipsContent.Assaults.Single(a => a.MapId == Content.MapId && a.PointId == assault.PointId));
            }
            foreach (var id in afs.Concat(new[] { Content.ThraxSoldier, Content.ThraxGrenadier, Content.ThraxTechnician, Content.Caretaker, Content.ThraxCommander }))
                Assert.IsTrue(_world.World.CreatureEntries.Any(c => c.Id == id), $"creature {id}");
        }

        [TestMethod]
        public void RetreadOutpostHasAGarrisonThatComesBackByAfsDropship()
        {
            var id = Content.RetreadOutpost;
            var pools = _world.Map.SpawnPools.Where(p => Point(id).AfsPools.Contains(p.DbId) && Content.Garrison.Any(g => g.Id == p.DbId)).ToArray();
            Assert.AreEqual(10, pools.Length);
            Assert.AreEqual(16, Content.Garrison.Where(g => g.Point == id).Sum(g => g.Troop.Max), "10 riflemen, 2 machine gunners, 4 mechs");
            Assert.IsTrue(pools.All(p => p.AnimType == 2 && p.IsGarrison && !p.Suspended));
            foreach (var bane in new[] { Content.BaneCommCenter, Content.BaneGuardStation })
                Assert.IsTrue(_world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == bane)).All(p => p.Suspended),
                    $"point {bane}: no AFS garrison while the Bane hold it");
        }

        [TestMethod]
        public void RetreadOutpostIsAttackedInThreeWavesOfTwoShipsWithTheCommanderInTheLast()
        {
            var retread = Content.Assaults.Single(a => a.PointId == Content.RetreadOutpost);
            _now += retread.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(Content.RetreadOutpost).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(Content.RetreadOutpost).Length);
            Deliver();
            _now += 180000;
            Tick();
            _now += 180000;
            Tick();
            Deliver();
            Assert.AreEqual(6, Assaults(Content.RetreadOutpost).Length);
            var troops = Troops(Content.RetreadOutpost);
            Assert.AreEqual(1, troops.Count(c => c.DbId == Content.ThraxCommander));
            Assert.IsTrue(troops.Length is >= 28 and <= 33, $"{troops.Length} Bane in the first assault");
            Assert.IsTrue(troops.All(c => c.Level is >= 33 and <= 36), "2-4 levels over the zone's Bane, the commander 5");
            _now += retread.ReliefSeconds * 1000L - 360000;
            Tick();
            Assert.AreEqual(1, Relief(Content.RetreadOutpost).Length, "the AFS relief ship after the last wave");
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(Content.RetreadOutpost).Contains(c.SpawnPool)).ToArray();
            Assert.AreEqual(4, relief.Length);
            Assert.IsTrue(relief.All(c => c.DbId is Content.Rifleman or Content.MachineGunner));
        }

        [TestMethod]
        public void TheCommanderTakesRetreadOutpostWhenItsDefendersAreDead()
        {
            var point = Point(Content.RetreadOutpost);
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            _now += 360000;
            Tick();
            Deliver();
            var defenders = _world.Map.SpawnPools.Where(p => point.AfsPools.Contains(p.DbId) || p.IsControlPointReinforcement).ToList();
            ControlPoints.TakeOffLiving(_world.Map, defenders);
            foreach (var pool in defenders)
            {
                pool.Suspended |= pool.IsControlPointReinforcement;
                pool.HasSpawned = true; pool.AliveCreatures = 0; pool.QueuedCreatures = 0; pool.DropshipQueue = 0;
            }
            var commander = Troops(point.Id).Single(c => c.DbId == Content.ThraxCommander);
            commander.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(commander, point.Object.UsedBy);
            _now += 10000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner);
            Assert.AreEqual(0, Assaults(point.Id).Length);
        }

        [TestMethod]
        public void AnAfsSquadAttacksBaneCommCenterButCannotTakeIt()
        {
            var point = Point(Content.BaneCommCenter);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            Tick();
            _now += plan.SortieFirstDelaySeconds * 1000L;
            Tick();
            var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool?.AssaultPointId == point.Id);
            Assert.AreEqual(2, ship.SpawnPool.AnimType, "an AFS ship");
            Deliver();
            var squad = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.Mech));
            Assert.IsTrue(squad.Count(c => c.DbId == Content.Rifleman) is >= 3 and <= 4);
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => point.BanePools.Contains(p.DbId)).ToList());
            foreach (var trooper in squad) trooper.Position = Walk(point.Position, "point");
            _now += 30000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "the AI does not take a point");
        }

        [TestMethod]
        public void ACapturedBanePointGetsItsAfsGarrisonAndACounterAttack()
        {
            foreach (var id in new[] { Content.BaneCommCenter, Content.BaneGuardStation })
            {
                var point = Point(id);
                ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
                Tick();
                var garrison = _world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == id)).ToArray();
                Assert.AreEqual(10, garrison.Length, $"point {id}");
                Assert.IsTrue(garrison.All(p => !p.Suspended), $"point {id}");
            }
            var plan = Content.Assaults.Single(a => a.PointId == Content.BaneCommCenter);
            _now += plan.FirstDelaySeconds * 1000L;
            Tick();
            Assert.AreEqual(2, Assaults(Content.BaneCommCenter).Length, "the Bane counter-attack after a delay");
            Assert.AreEqual(2, Assaults(Content.BaneGuardStation).Length);
        }

        [TestMethod]
        public void EachAssaultHeldOffMakesTheNextBigger()
        {
            var point = Point(Content.RetreadOutpost);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            int Soldiers(int repelled)
            {
                Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
                Deliver();
                Assert.AreEqual(repelled, LandingZoneAssaults.Repelled(_world.Map, point));
                var soldiers = Troops(point.Id).Count(c => c.DbId == Content.ThraxSoldier);
                _now += plan.LifetimeSeconds * 1000L;
                Tick();
                Assert.AreEqual(ControlPoints.Afs, point.Owner, "held off");
                return soldiers;
            }
            Assert.IsTrue(Soldiers(0) is >= 6 and <= 7);
            Assert.IsTrue(Soldiers(1) is >= 8 and <= 9, "25% more after one held off");
            Assert.IsTrue(Soldiers(2) is >= 10 and <= 11, "50% more after two");
        }
    }
}
