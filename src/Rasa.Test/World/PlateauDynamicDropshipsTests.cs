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
using Content = Rasa.Services.Preloader.PlateauDynamicDropshipsContent;

namespace Rasa.Test.World
{
    /// <summary>Valverde Plateau's control points: AFS garrisons by dropship, timed Bane assaults, AFS relief.</summary>
    [TestClass, DoNotParallelize]
    public class PlateauDynamicDropshipsTests
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
                db.ControlPoints.GetLinks().Where(l => l.ControlPointId is >= 12 and <= 15), null);
            ControlPoints.Instance.Place(_world.Map);
        }

        [TestCleanup]
        public void End()
        {
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
            foreach (var id in new uint[] { 12, 13, 14, 15 })
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
            foreach (var assault in Content.Assaults)
            {
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
        public void TheZonesAssaultsAreSpreadOutNotAllAtOnce()
        {
            var afs = Content.Assaults.Where(a => a.PointId is Content.NortheastAfs or Content.NorthwestAfs).ToArray();
            Assert.AreEqual(afs[0].PeriodSeconds, afs[1].PeriodSeconds);
            var gap = Math.Abs(afs[0].FirstDelaySeconds - afs[1].FirstDelaySeconds) % afs[0].PeriodSeconds;
            Assert.IsTrue(Math.Min(gap, afs[0].PeriodSeconds - gap) >= afs[0].PeriodSeconds / 3,
                "the two AFS points are attacked at least a third of a period apart");
            foreach (var assault in Content.Assaults)
                Assert.IsTrue(assault.LifetimeSeconds < afs[0].PeriodSeconds / 2, "one assault is over before the next point's starts");
        }

        [TestMethod]
        public void TheAfsPointsHaveAGarrisonThatComesBackByAfsDropship()
        {
            foreach (var id in new[] { Content.NortheastAfs, Content.NorthwestAfs })
            {
                var pools = _world.Map.SpawnPools.Where(p => Point(id).AfsPools.Contains(p.DbId) && Content.Garrison.Any(g => g.Id == p.DbId)).ToArray();
                Assert.AreEqual(7, pools.Length, $"point {id}");
                Assert.AreEqual(12, Content.Garrison.Where(g => g.Point == id).Sum(g => g.Troop.Max), $"point {id}: 8 riflemen, 2 machine gunners, 2 mechs");
                Assert.IsTrue(pools.All(p => p.AnimType == 2 && p.IsGarrison && !p.Suspended), $"point {id}");
            }
            foreach (var id in new[] { Content.SoutheastBane, Content.SouthwestBane })
                Assert.IsTrue(_world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == id)).All(p => p.Suspended),
                    $"point {id}: no AFS garrison while the Bane hold it");
        }

        [TestMethod]
        public void NortheastIsAttackedFirstInThreeWavesOfTwoShipsWithTheCommanderInTheLast()
        {
            var northeast = Content.Assaults.Single(a => a.PointId == Content.NortheastAfs);
            _now += northeast.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(Content.NortheastAfs).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(Content.NortheastAfs).Length);
            Assert.AreEqual(0, Assaults(Content.NorthwestAfs).Length, "Northwest is not attacked at the same time");
            Assert.AreEqual(0, Relief(Content.NortheastAfs).Length, "the AFS relief comes at the end");
            Deliver();
            _now += 180000;
            Tick();
            _now += 180000;
            Tick();
            Deliver();
            Assert.AreEqual(6, Assaults(Content.NortheastAfs).Length);
            var troops = Troops(Content.NortheastAfs);
            Assert.AreEqual(1, troops.Count(c => c.DbId == Content.ThraxCommander));
            Assert.IsTrue(troops.Length is >= 28 and <= 33, $"{troops.Length} Bane in the first assault (8-9, 9-11, 11-13 with the commander)");
            Assert.IsFalse(troops.Any(c => c.DbId == Content.ShieldDrone), "no Shield Drones: the AFS cannot get under their shield");
            Assert.IsTrue(troops.All(c => c.Level is >= 28 and <= 33), "2-4 levels over the zone's Bane, the commander 5");
            Assert.AreEqual(0, Relief(Content.NortheastAfs).Length);
            _now += northeast.ReliefSeconds * 1000L - 360000;
            Tick();
            Assert.AreEqual(1, Relief(Content.NortheastAfs).Length, "the AFS relief ship after the last wave");
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(Content.NortheastAfs).Contains(c.SpawnPool)).ToArray();
            Assert.AreEqual(4, relief.Length);
            Assert.IsTrue(relief.All(c => c.DbId is Content.Rifleman or Content.MachineGunner));
        }

        [TestMethod]
        public void TheCommanderTakesTheNortheastWhenItsDefendersAreDead()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point(Content.NortheastAfs)));
            _now += 360000;
            Tick();
            Deliver();
            var point = Point(Content.NortheastAfs);
            var defenders = _world.Map.SpawnPools.Where(p => point.AfsPools.Contains(p.DbId) || p.IsControlPointReinforcement).ToList();
            ControlPoints.TakeOffLiving(_world.Map, defenders);
            foreach (var pool in defenders)
            {
                pool.Suspended |= pool.IsControlPointReinforcement;
                pool.HasSpawned = true; pool.AliveCreatures = 0; pool.QueuedCreatures = 0; pool.DropshipQueue = 0;
            }
            var commander = Troops(Content.NortheastAfs).Single(c => c.DbId == Content.ThraxCommander);
            commander.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(commander, point.Object.UsedBy);
            _now += 10000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner);
            Assert.AreEqual(0, Assaults(Content.NortheastAfs).Length);
        }

        [TestMethod]
        public void AnAfsSquadAttacksABanePointButCannotTakeIt()
        {
            var point = Point(Content.SoutheastBane);
            var southeast = Content.Assaults.Single(a => a.PointId == point.Id);
            Tick();
            Assert.AreEqual(0, AfsSorties.Squad(_world.Map, point).Count());
            _now += southeast.SortieFirstDelaySeconds * 1000L;
            Tick();
            var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool?.AssaultPointId == point.Id);
            Assert.AreEqual(2, ship.SpawnPool.AnimType, "an AFS ship");
            Deliver();
            var squad = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.Mech));
            Assert.IsTrue(squad.Count(c => c.DbId == Content.Rifleman) is >= 3 and <= 4);
            Assert.IsTrue(Assaults(Content.NortheastAfs).Length == 0 && Assaults(point.Id).Length == 0, "no Bane assault at the same time");
            // The garrison falls, the squad stands at the point: still the Bane's.
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => point.BanePools.Contains(p.DbId)).ToList());
            foreach (var trooper in squad) trooper.Position = Walk(point.Position, "point");
            _now += 30000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "the AI does not take a point");
            // What is left goes after its time, also when its pool's living count has drifted to 0.
            var left = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.IsTrue(left.Length > 0);
            ship.SpawnPool.AliveCreatures = 0;
            _now += southeast.SortieLifetimeSeconds * 1000L;
            Tick();
            Assert.IsFalse(left.Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "what is left goes after its time");
        }

        [TestMethod]
        public void ACapturedBanePointGetsItsAfsGarrisonAndACounterAttack()
        {
            var point = Point(Content.SoutheastBane);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            Assert.IsTrue(_world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == point.Id)).All(p => !p.Suspended));
            var southeast = Content.Assaults.Single(a => a.PointId == Content.SoutheastBane);
            _now += southeast.FirstDelaySeconds * 1000L;
            Tick();
            Assert.AreEqual(2, Assaults(point.Id).Length, "the Bane counter-attack after a delay");
        }

        /// <summary>A claim under way when the assault's time runs out finishes: the point is the Bane's, not held off.</summary>
        [TestMethod]
        public void AClaimUnderWayFinishesWhenTheAssaultsTimeRunsOut()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
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
            _now = 1000 + plan.LifetimeSeconds * 1000L - 5000;
            commander.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(commander, point.Object.UsedBy, "the claim starts 5 s before the assault's time is up");
            _now += 6000;
            Tick();
            Assert.AreEqual(0, LandingZoneAssaults.Repelled(_world.Map, point), "not held off while the claim is under way");
            _now += 10000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner);
        }

        /// <summary>An assault that is over takes its Bane off, also when its pools' living counts have drifted to 0.</summary>
        [TestMethod]
        public void AnAssaultThatIsOverLeavesNoBaneBehindEvenWithItsCountsAtZero()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var troops = Troops(point.Id);
            Assert.IsTrue(troops.Length > 0);
            foreach (var pool in Assaults(point.Id)) pool.AliveCreatures = 0;
            _now += plan.LifetimeSeconds * 1000L;
            Tick();
            Assert.AreEqual(ControlPoints.Afs, point.Owner, "held off");
            Assert.IsFalse(troops.Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "no Bane of it stays at the point");
        }

        /// <summary>
        /// Each assault the AFS hold off makes the next one bigger (EscalationStep); the count starts
        /// again when the point changes hands.
        /// </summary>
        [TestMethod]
        public void EachAssaultHeldOffMakesTheNextBiggerUntilThePointChangesHands()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            int Soldiers(int repelled)
            {
                Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
                Deliver();
                Assert.AreEqual(repelled, LandingZoneAssaults.Repelled(_world.Map, point));
                var soldiers = Troops(point.Id).Count(c => c.DbId == Content.ThraxSoldier);
                _now += plan.LifetimeSeconds * 1000L;
                Tick();
                Assert.AreEqual(0, Assaults(point.Id).Length, "over");
                Assert.AreEqual(ControlPoints.Afs, point.Owner, "held off");
                return soldiers;
            }
            var first = Soldiers(0);
            var second = Soldiers(1);
            var third = Soldiers(2);
            // The first wave's ships: Thrax Soldiers 3-4 and 3; x1.25: 4-5 and 4; x1.5: 5-6 and 5 (rounded).
            Assert.IsTrue(first is >= 6 and <= 7, $"{first} Thrax Soldiers in the first wave");
            Assert.IsTrue(second is >= 8 and <= 9, $"{second}: 25% more after one held off");
            Assert.IsTrue(third is >= 10 and <= 11, $"{third}: 50% more after two");
            Assert.AreEqual(3, LandingZoneAssaults.Repelled(_world.Map, point));
            for (var i = 3; i < 7; i++) Soldiers(i);
            Assert.IsTrue(Soldiers(7) is >= 12 and <= 14, "at most twice the troops (MaxEscalation 4): 6-7 and 6");

            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            Tick();
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            Assert.AreEqual(0, LandingZoneAssaults.Repelled(_world.Map, point), "a point that changed hands starts again");
        }
    }
}
