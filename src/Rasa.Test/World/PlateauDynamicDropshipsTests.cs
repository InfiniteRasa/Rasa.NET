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
        /// <summary>How far, across the ground, a Bane drop lands from each of its point's turrets at least (ESTIMATE).</summary>
        private const float DropClearOfTurrets = 30f;

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
                    foreach (var turret in ControlPointTurrets.Spots.Where(s => s.Point == assault.PointId))
                    {
                        var clear = new Vector2(assault.Drops[i].Position.X - (float)turret.X, assault.Drops[i].Position.Z - (float)turret.Z).Length();
                        Assert.IsTrue(clear >= DropClearOfTurrets, $"point {assault.PointId} drop {i + 1} lands {clear:0} m from the turret at ({turret.X:0}, {turret.Z:0}): troops set down by a turret's mount stall against it");
                    }
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
            // Live run 2026-11-05 (Research Point): waves 3 min apart died one by one against the
            // garrison. Every zone's: a wave lands while the last still fights (ESTIMATE).
            foreach (var assault in DynamicDropshipsContent.Assaults)
                Assert.IsTrue(assault.WaveSeconds <= 90, $"map {assault.MapId} point {assault.PointId}: waves {assault.WaveSeconds} s apart");
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
            _now += northeast.WaveSeconds * 1000L;
            Tick();
            _now += northeast.WaveSeconds * 1000L;
            Tick();
            Deliver();
            Assert.AreEqual(6, Assaults(Content.NortheastAfs).Length);
            var troops = Troops(Content.NortheastAfs);
            Assert.AreEqual(1, troops.Count(c => c.DbId == Content.ThraxCommander));
            Assert.IsTrue(troops.Length is >= 28 and <= 33, $"{troops.Length} Bane in the first assault (8-9, 9-11, 11-13 with the commander)");
            Assert.IsFalse(troops.Any(c => c.DbId == Content.ShieldDrone), "no Shield Drones: the AFS cannot get under their shield");
            Assert.IsTrue(troops.All(c => c.Level is >= 28 and <= 33), "2-4 levels over the zone's Bane, the commander 5");
            Assert.AreEqual(0, Relief(Content.NortheastAfs).Length);
            _now += northeast.ReliefSeconds * 1000L - 2L * northeast.WaveSeconds * 1000;
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
            Assert.AreEqual(4, squad.Count(c => c.DbId == Content.Rifleman), "4 riflemen");
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.MachineGunner), "and a machine gunner (ESTIMATE, one over retail's 3-4)");
            Assert.IsTrue(Assaults(Content.NortheastAfs).Length == 0 && Assaults(point.Id).Length == 0, "no Bane assault at the same time");
            // The squad stands at the point, the Bane garrison not down (its pools have yet to set
            // anyone down, so they stand): still the Bane's.
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => point.BanePools.Contains(p.DbId)).ToList());
            foreach (var trooper in squad) trooper.Position = Walk(point.Position, "point");
            _now += 30000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "no claim while the Bane garrison stands");
            // What is left goes after its time, also when its pool's living count has drifted to 0.
            var left = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.IsTrue(left.Length > 0);
            ship.SpawnPool.AliveCreatures = 0;
            _now += southeast.SortieLifetimeSeconds * 1000L;
            Tick();
            Assert.IsFalse(left.Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "what is left goes after its time");
        }

        private void Kill(Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(_world.Map, creature, null);
        }

        private Creature[] BaneGarrison(ControlPoints.Point point) => EntityManager.Instance.Creatures.Values
            .Where(c => c.SpawnPool != null && point.BanePools.Contains(c.SpawnPool.DbId) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();

        /// <summary>
        /// Live run 2026-11-05 (Northwest, Bane): the squad stood on the point with the Bane garrison
        /// down for three minutes and made no claim, the mortars came back under it at full health,
        /// and the mech, its riflemen dead, stood 77 m out. Now the Bane garrison a sortie has cleared
        /// stays down while the sortie runs and a trooper that strays past its leash comes back. The
        /// squad does not take the point ("a squad cannot take the point", Ten Ton Hammer); a player
        /// can, while the garrison is down.
        /// </summary>
        [TestMethod]
        public void AnAfsSquadClearsABanePointForAPlayerToTakeAndTheGarrisonDoesNotComeBackUnderIt()
        {
            var clock = ControlPoints.Instance.Now;
            ControlPoints.Instance.Now = () => _now;
            try
            {
                var point = Point(Content.SoutheastBane);
                _world.SpawnWorld(point.BanePools.ToArray());
                Deliver();
                var garrison = BaneGarrison(point);
                Assert.IsTrue(garrison.Length > 0, "the Bane garrison stands");
                Assert.IsTrue(AfsSorties.TryStart(_world.Map, point));
                Deliver();
                var squad = AfsSorties.Squad(_world.Map, point).ToArray();
                var mech = squad.Single(c => c.DbId == Content.Mech);

                // A rifleman far out in a fight comes back to the point.
                var rifleman = squad.First(c => c.DbId == Content.Rifleman);
                rifleman.Position = Walk(point.Position + new Vector3(140, 0, 0), "far out");
                Assert.IsTrue(new Vector2(rifleman.Position.X - point.Position.X, rifleman.Position.Z - point.Position.Z).Length() > 100, "the far spot is far");
                rifleman.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
                rifleman.Controller.ActionFighting.TargetEntityId = garrison[0].EntityId;
                Tick();
                Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, rifleman.Controller.CurrentAction, "the strayed rifleman gives up its fight");
                Assert.IsTrue(Vector3.Distance(rifleman.Controller.ActionWander.WanderDestination, Walk(point.Position, "point")) < 1f, "and makes for the point");

                // The garrison falls; its return time passes while the sortie runs: it does not come back.
                foreach (var creature in garrison) Kill(creature);
                Tick();
                _now += ControlPoints.Instance.ReturnMs(_world.Map, point) + 1000;
                Tick();
                for (var i = 0; i < 3; i++) { SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 600000); Deliver(); Tick(); }
                Assert.AreEqual(0, BaneGarrison(point).Length, "no Bane garrison back while the sortie runs");

                // The squad stands on the point for longer than a capture takes: still the Bane's.
                foreach (var trooper in AfsSorties.Squad(_world.Map, point)) trooper.Position = Walk(point.Position, "point");
                Assert.IsTrue(CreatureManager.IsLivingOnMap(_world.Map, mech), "the mech stands");
                for (var i = 0; i < 3; i++) { _now += 30000; Tick(); }
                Assert.AreEqual(ControlPoints.Bane, point.Owner, "a squad cannot take the point");
                Assert.IsFalse(AfsSorties.Squad(_world.Map, point).Any(c => c.OnClaimDuty), "nobody of the squad claims it");

                // A player can, the garrison being down.
                Assert.IsTrue(ControlPoints.Instance.MayCapture(_world.Map, point, _world.Client), "the point is the player's to capture");
                Assert.IsTrue(ControlPoints.Instance.Captured(_world.Map, _world.Client, point), "the player's capture");
                Assert.AreEqual(ControlPoints.Afs, point.Owner);
            }
            finally
            {
                ControlPoints.Instance.Now = clock;
            }
        }

        /// <summary>Live run 2026-11-05: ".cp 13 sortie" with an automatic sortie running answered "no sortie running".</summary>
        [TestMethod]
        public void AGmStartWhileOneRunsSaysItIsUnderWay()
        {
            var southeast = Point(Content.SoutheastBane);
            var northeast = Point(Content.NortheastAfs);
            Assert.AreEqual($"AFS sortie against {southeast.Name} started.", AfsSorties.GmStart(_world.Map, southeast));
            Assert.AreEqual($"An AFS sortie against {southeast.Name} is already under way.", AfsSorties.GmStart(_world.Map, southeast));
            Assert.AreEqual($"{northeast.Name} assault started.", LandingZoneAssaults.GmStart(_world.Map, northeast));
            Assert.AreEqual($"A Bane assault on {northeast.Name} is already under way.", LandingZoneAssaults.GmStart(_world.Map, northeast));
        }

        [TestMethod]
        public void AnAssaultUnderWayIsTakenOffWhenAssaultsAreTurnedOff()
        {
            var point = Point(Content.NortheastAfs);
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var pools = Assaults(point.Id);
            var troops = Troops(point.Id);
            Assert.IsTrue(pools.Length > 0 && troops.Length > 0);
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
            Tick();
            Assert.AreEqual(0, Assaults(point.Id).Length, "its pools taken off the map");
            Assert.IsFalse(troops.Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "and its Bane");
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = true;
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point), "a new one may start when they are back on");
        }

        [TestMethod]
        public void AGmStartOnAnAssaultOrSortieThatIsDueReportsItStarted()
        {
            var northeast = Point(Content.NortheastAfs);
            var southeast = Point(Content.SoutheastBane);
            Tick();
            _now += Math.Max(Content.Assaults.Single(a => a.PointId == northeast.Id).FirstDelaySeconds,
                Content.Assaults.Single(a => a.PointId == southeast.Id).SortieFirstDelaySeconds) * 1000L;
            // Due, and not started yet: the worker TryStart runs first starts it, and that is the one asked for.
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, northeast), "the assault that was due");
            Assert.IsTrue(Assaults(northeast.Id).Length > 0);
            Assert.IsFalse(LandingZoneAssaults.TryStart(_world.Map, northeast), "one is running");
            Assert.IsTrue(AfsSorties.TryStart(_world.Map, southeast), "the sortie that was due");
            Assert.AreEqual(1, DynamicObjectManager.Instance.Dropships.Values.Count(s => s.SpawnPool?.AssaultPointId == southeast.Id));
            Assert.IsFalse(AfsSorties.TryStart(_world.Map, southeast), "one is running");
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
            // The first wave's ships: Thrax Soldiers 3-4 and 3; x2: 6-8 and 6.
            Assert.IsTrue(first is >= 6 and <= 7, $"{first} Thrax Soldiers in the first wave");
            Assert.IsTrue(second is >= 12 and <= 14, $"{second}: twice after one held off");
            Assert.IsTrue(third is >= 12 and <= 14, $"{third}: and no more after two");
            Assert.AreEqual(3, LandingZoneAssaults.Repelled(_world.Map, point));
            for (var i = 3; i < 7; i++) Soldiers(i);
            Assert.IsTrue(Soldiers(7) is >= 12 and <= 14, "at most twice the troops (MaxEscalation 1): 6-8 and 6");

            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            Tick();
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            Assert.AreEqual(0, LandingZoneAssaults.Repelled(_world.Map, point), "a point that changed hands starts again");
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        private Creature[] Garrison(ControlPoints.Point point) => EntityManager.Instance.Creatures.Values
            .Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) && ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) == null &&
                CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();

        /// <summary>
        /// The AFS turrets are in the garrison (ControlPointTurrets, link kind 2). Troops that went for
        /// the nearest turret, on a mount off the navmesh, stalled there, leashed and came back and never
        /// reached the point: a wave takes on the AFS troops first, and its home is the point.
        /// </summary>
        [TestMethod]
        public void AWaveTakesOnTheGarrisonBeforeTheTurretsAndKeepsThePointAsItsHome()
        {
            var point = Point(Content.NortheastAfs);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var turrets = Garrison(point).Where(Emplacements.Is).ToArray();
            Assert.IsTrue(turrets.Length > 0, "the point's AFS turrets stand");
            Assert.IsTrue(Garrison(point).Any(c => !Emplacements.Is(c)), "the point's AFS troops stand");
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            Tick();
            var troops = Troops(point.Id);
            Assert.IsTrue(troops.Length > 0);
            var onTurrets = troops.Where(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                turrets.Any(t => t.EntityId == c.Controller.ActionFighting.TargetEntityId)).ToArray();
            Assert.AreEqual(0, onTurrets.Length, $"{onTurrets.Length} of {troops.Length} troops on a turret while the AFS troops stand");
            Assert.IsTrue(troops.All(c => Flat(c.HomePos.Position, point.Position) < 15f), "an assault troop's home is its point (or its spot about it), not its drop");
        }

        /// <summary>
        /// While an assault runs on a planned point its garrison stays down: no AFS soldier set down
        /// again on the pool's respawn time, no turret repaired; after it, the garrison comes back.
        /// </summary>
        [TestMethod]
        public void NoAfsGarrisonComesBackAndNoTurretIsRepairedWhileAnAssaultRuns()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var garrison = Garrison(point);
            Assert.IsTrue(garrison.Any(Emplacements.Is) && garrison.Any(c => !Emplacements.Is(c)), "turrets and troops stand");
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            foreach (var creature in garrison) Kill(creature);
            Assert.IsTrue(garrison.Where(Emplacements.Is).All(AlternateMesh.KeepsWreck), "the turrets are wrecks on their mounts");
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(_world.Map, point, ControlPoints.Afs));

            // Every respawn time of the garrison (5 and 10 min) passes while the assault runs.
            for (var i = 0; i < 4; i++) { SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 300000); Deliver(); }
            Assert.AreEqual(0, Garrison(point).Length, "no AFS soldier set down again, no turret repaired");
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(_world.Map, point, ControlPoints.Afs), "no AFS ship on its way");

            // Over (its Bane gone): the garrison comes back, the turrets repaired where they stand.
            ControlPoints.TakeOffLiving(_world.Map, Assaults(point.Id).ToList());
            _now += plan.LifetimeSeconds * 1000L;
            Tick();
            Assert.AreEqual(0, Assaults(point.Id).Length, "the assault is over");
            Assert.AreEqual(ControlPoints.Afs, point.Owner);
            for (var i = 0; i < 4; i++) { SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 300000); Deliver(); }
            var back = Garrison(point);
            Assert.IsTrue(back.Any(c => !Emplacements.Is(c)), "the AFS troops are back after the assault");
            Assert.IsTrue(garrison.Where(Emplacements.Is).All(t => back.Contains(t)), "the turrets are repaired after the assault");
        }

        /// <summary>The plan's relief is the one help in an assault, and only for a garrison that still holds.</summary>
        [TestMethod]
        public void TheReliefShipComesOnlyWhileTheGarrisonHolds()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            foreach (var creature in Garrison(point)) Kill(creature);
            _now += plan.ReliefSeconds * 1000L;
            Tick();
            Assert.AreEqual(0, Relief(point.Id).Length, "no relief ship for a garrison that has fallen");
            _now += 60000;
            Tick();
            Assert.AreEqual(0, Relief(point.Id).Length, "nor later in the same assault");
        }

        /// <summary>
        /// With the garrison down the Bane make for the point, the claimant first, and give up a fight
        /// with anything that is not a player; if the claimant dies the next one claims; the assault
        /// does not end as held off while the garrison is down and a claimant stands; the claim done,
        /// the point is the Bane's.
        /// </summary>
        [TestMethod]
        public void WithTheGarrisonDownTheBaneGoToThePointAndTheNextClaimantTakesIt()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            _now += 2L * plan.WaveSeconds * 1000;
            Tick();
            Deliver();
            var vendor = EntityManager.Instance.Creatures.Values.FirstOrDefault(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) != null && CreatureManager.IsLivingOnMap(_world.Map, c));
            Assert.IsNotNull(vendor, "a vendor of the point stands (a service pool, no garrison)");
            foreach (var creature in Garrison(point)) Kill(creature);
            var troops = Troops(point.Id);
            var commander = troops.Single(c => c.DbId == Content.ThraxCommander);
            var soldier = troops.First(c => c.DbId == Content.ThraxSoldier && Flat(c.Position, point.Position) > LandingZoneAssaults.RallyReach);
            // A soldier out there in a fight with something that is no garrison.
            soldier.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            soldier.Controller.ActionFighting.TargetEntityId = vendor.EntityId;
            Tick();
            Assert.IsTrue(commander.OnClaimDuty, "the commander goes for the point");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, soldier.Controller.CurrentAction, "the soldier gives up the vendor and makes for the point");
            Assert.IsTrue(Flat(soldier.Controller.ActionWander.WanderDestination, point.Position) < LandingZoneAssaults.RallyReach, "to the point");

            // Its time runs out with the commander still on the way: not held off.
            _now = 1000 + plan.LifetimeSeconds * 1000L + 1000;
            Tick();
            Assert.IsTrue(Assaults(point.Id).Length > 0, "the assault goes on while the garrison is down and a claimant stands");
            Assert.AreEqual(0, LandingZoneAssaults.Repelled(_world.Map, point));

            // The commander dies: the next claims.
            Kill(commander);
            Tick();
            var next = Troops(point.Id).SingleOrDefault(c => c.OnClaimDuty);
            Assert.IsNotNull(next, "the next claimant is chosen");
            next.Position = Walk(point.Position, "point");
            Tick();
            Assert.AreSame(next, point.Object.UsedBy, "the claim begins");
            _now += Math.Max(1, ControlPoints.Instance.Config.LandingZoneAssaultCaptureSeconds) * 1000L;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "the claim done, the point is the Bane's");
        }

        /// <summary>
        /// A planned point's assault troops take on the garrison, players and whoever fights them:
        /// not a vendor or a mission's NPC standing by. They do not seek one out (Creature.SeeksOnly),
        /// and one in a fight with one gives it up.
        /// </summary>
        [TestMethod]
        public void AnAssaultTroopLeavesAnNpcThatIsNotTheGarrisonAlone()
        {
            var point = Point(Content.NortheastAfs);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var troop = Troops(point.Id).First();
            var guard = Garrison(point).First(c => !Emplacements.Is(c));
            var bystander = CreatureManager.Instance.CreateCreature(Content.Rifleman, null);
            CreatureManager.Instance.SetLocation(bystander, Walk(troop.Position + new Vector3(3, 0, 0), "by the troop"), 0, Content.MapId);
            CellManager.Instance.AddToWorld(_world.Map, bystander);
            Assert.IsNotNull(troop.SeeksOnly);
            Assert.IsFalse(troop.SeeksOnly(bystander), "an AFS soldier of no garrison is not sought out");
            Assert.IsTrue(troop.SeeksOnly(guard), "the garrison is");
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, bystander.EntityId));
            Tick();
            Assert.IsFalse(troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId == bystander.EntityId,
                "the troop gives up the NPC");
            // One that fights the troop is fought back.
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, bystander.EntityId));
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(bystander, troop.EntityId));
            Tick();
            Assert.AreEqual(bystander.EntityId, troop.Controller.ActionFighting.TargetEntityId, "it fights back one that fights it");
        }

        /// <summary>
        /// An AFS sortie trooper takes on the point's Bane garrison and whoever fights it, not an NPC
        /// near the point; and in a fight it is leashed a little past its target, not only past the
        /// farthest garrison post (live run 2026-11-05: troopers 110 m out at Bane Assertion Camp,
        /// whose farthest post is 107 m out, "fighting" a quest NPC 44 m from the point).
        /// </summary>
        [TestMethod]
        public void ASortieTrooperLeavesAnNpcAloneAndIsLeashedPastItsTarget()
        {
            var point = Point(Content.SoutheastBane);
            _world.SpawnWorld(point.BanePools.ToArray());
            Deliver();
            Assert.IsTrue(AfsSorties.TryStart(_world.Map, point));
            Deliver();
            var trooper = AfsSorties.Squad(_world.Map, point).First(c => c.DbId == Content.Rifleman);
            var garrison = BaneGarrison(point).Where(c => !Emplacements.Is(c)).ToArray();
            Assert.IsTrue(garrison.Length > 0);
            var bystander = CreatureManager.Instance.CreateCreature(Content.ThraxSoldier, null);
            CreatureManager.Instance.SetLocation(bystander, Walk(trooper.Position + new Vector3(3, 0, 0), "by the trooper"), 0, Content.MapId);
            CellManager.Instance.AddToWorld(_world.Map, bystander);
            Assert.IsFalse(trooper.SeeksOnly(bystander), "a Bane of no garrison is not sought out");
            Assert.IsTrue(trooper.SeeksOnly(garrison[0]), "the garrison is");
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(trooper, bystander.EntityId));
            Tick();
            Assert.IsFalse(trooper.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && trooper.Controller.ActionFighting.TargetEntityId == bystander.EntityId,
                "the trooper gives up the NPC");

            // Leashed past its target: far out, in a fight with the garrison's nearest, inside the farthest post's leash.
            var nearest = garrison.OrderBy(c => Flat(c.Position, point.Position)).First();
            var farthest = ControlPoints.Instance.PoolsOfSide(_world.Map, point, ControlPoints.Bane).Max(p => Flat(p.Position, point.Position));
            var spot = Walk(point.Position + Vector3.Normalize(nearest.Position - point.Position with { Y = nearest.Position.Y }) * (Flat(nearest.Position, point.Position) + AfsSorties.LeashBeyondGarrison + 8), "out past");
            var from = Flat(spot, point.Position);
            Assert.IsTrue(from > Flat(nearest.Position, point.Position) + AfsSorties.LeashBeyondGarrison && from <= farthest + AfsSorties.LeashBeyondGarrison,
                $"the spot {from:0} m out: past its target's leash ({Flat(nearest.Position, point.Position) + AfsSorties.LeashBeyondGarrison:0} m), inside the farthest post's ({farthest + AfsSorties.LeashBeyondGarrison:0} m)");
            trooper.Position = spot;
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(trooper, nearest.EntityId));
            Tick();
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, trooper.Controller.CurrentAction, "the trooper gives up a fight that drew it past its leash");
        }

        /// <summary>
        /// Live run 2026-11-05: with nobody of the Bane garrison alive and none set down, the sortie's
        /// "cleared" and "standing again" flapped every 10-20 s. A pool's count of somebody on the way
        /// that the sortie's hold keeps from coming does not stand the garrison up.
        /// </summary>
        [TestMethod]
        public void ASortiesClearedGarrisonStaysClearedWhateverItsPoolsCountsSay()
        {
            var point = Point(Content.SoutheastBane);
            _world.SpawnWorld(point.BanePools.ToArray());
            Deliver();
            Assert.IsTrue(AfsSorties.TryStart(_world.Map, point));
            Deliver();
            foreach (var creature in BaneGarrison(point)) Kill(creature);
            Tick();
            Assert.IsTrue(AfsSorties.Cleared(_world.Map, point), "cleared");
            var pool = _world.Map.SpawnPools.First(p => point.BanePools.Contains(p.DbId));
            pool.QueuedCreatures = 1;
            pool.AliveCreatures = 1;
            Tick();
            Assert.IsTrue(AfsSorties.Cleared(_world.Map, point), "still cleared: nobody of it alive, no ship of it on the way");
        }

        /// <summary>
        /// The map's workers run only while a player is on the map: an assault and a sortie whose
        /// time ran out with nobody there end on the first pass after, and the next comes after its
        /// first delay, not the moment a player is back (live run 2026-11-05: a sortie over at
        /// +11641 s).
        /// </summary>
        [TestMethod]
        public void AnAssaultAndASortieLongOverEndOnTheNextPassAndTheNextWaitsItsFirstDelay()
        {
            var northeast = Point(Content.NortheastAfs);
            var southeast = Point(Content.SoutheastBane);
            var assault = Content.Assaults.Single(a => a.PointId == northeast.Id);
            var sortie = Content.Assaults.Single(a => a.PointId == southeast.Id);
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, northeast));
            Assert.IsTrue(AfsSorties.TryStart(_world.Map, southeast));
            Deliver();
            var troops = Troops(northeast.Id);
            var squad = AfsSorties.Squad(_world.Map, southeast).ToArray();
            Assert.IsTrue(troops.Length > 0 && squad.Length > 0);
            _now += 3 * 3600 * 1000L;
            Tick();
            Assert.IsFalse(LandingZoneAssaults.IsRunning(_world.Map, northeast), "the assault is over on the first pass");
            Assert.IsFalse(AfsSorties.IsRunning(_world.Map, southeast), "and the sortie");
            Assert.IsFalse(troops.Concat(squad).Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "their troops gone");
            _now += 1000;
            Tick();
            Assert.IsFalse(LandingZoneAssaults.IsRunning(_world.Map, northeast), "no assault the moment after");
            Assert.IsFalse(AfsSorties.IsRunning(_world.Map, southeast), "no sortie the moment after");
            _now += Math.Max(assault.FirstDelaySeconds, sortie.SortieFirstDelaySeconds) * 1000L;
            Tick();
            Assert.IsTrue(LandingZoneAssaults.IsRunning(_world.Map, northeast), "the next assault after its first delay");
            Assert.IsTrue(AfsSorties.IsRunning(_world.Map, southeast), "the next sortie after its first delay");
        }

        /// <summary>
        /// Live run 2026-11-05: Bane stood "fighting" at full health 22-58 m from the point for 3-4
        /// minutes. A troop whose target loses no health for NoHarmMs gives it up and goes for
        /// another of the garrison; it leaves that one alone for ShunMs.
        /// </summary>
        [TestMethod]
        public void AnAssaultTroopGivesUpAFightThatGetsItNowhere()
        {
            var point = Point(Content.NortheastAfs);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var troop = Troops(point.Id).First();
            var guards = Garrison(point).Where(c => !Emplacements.Is(c)).ToArray();
            Assert.IsTrue(guards.Length > 1);
            var guard = guards[0];
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, guard.EntityId));
            Tick();
            _now += LandingZoneAssaults.NoHarmMs - 1000;
            Tick();
            Assert.AreEqual(guard.EntityId, troop.Controller.ActionFighting.TargetEntityId, "it keeps at it for a while");
            _now += 2000;
            Tick();
            Assert.IsFalse(troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId == guard.EntityId,
                "no harm done in NoHarmMs: it gives the guard up");
            // A hit on the target is progress.
            var other = troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting ? EntityManager.Instance.GetCreature(troop.Controller.ActionFighting.TargetEntityId) : guards[1];
            Assert.AreNotEqual(guard, other, "another of the garrison");
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, other.EntityId));
            Tick();
            _now += LandingZoneAssaults.NoHarmMs - 1000;
            other.Attributes[Attributes.Health].Current -= 1;
            Tick();
            _now += 2000;
            Tick();
            Assert.AreEqual(other.EntityId, troop.Controller.ActionFighting.TargetEntityId, "a target losing health is kept");
        }

        /// <summary>
        /// The relief comes only to a garrison holding more than half of what stood when the
        /// assault began (balance, ESTIMATE: live runs 2026-11-05, every natural assault was held off).
        /// </summary>
        [TestMethod]
        public void TheReliefShipComesOnlyWhileMoreThanHalfTheGarrisonStands()
        {
            foreach (var (killedShare, relieved) in new[] { (0.25, true), (0.55, false) })
            {
                var point = Point(Content.NortheastAfs);
                var plan = Content.Assaults.Single(a => a.PointId == point.Id);
                _world.SpawnWorld(point.AfsPools.ToArray());
                Deliver();
                var garrison = Garrison(point);
                Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
                var started = _now;
                foreach (var creature in garrison.Take((int)Math.Ceiling(garrison.Length * killedShare))) Kill(creature);
                _now = started + plan.ReliefSeconds * 1000L;
                Tick();
                Assert.AreEqual(relieved ? 1 : 0, Relief(point.Id).Length, $"{garrison.Length - (int)Math.Ceiling(garrison.Length * killedShare)} of {garrison.Length} stand");
                ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
                Tick();
                ControlPoints.Instance.Config.ControlPointAssaultEnabled = true;
                _now += 60000;
            }
        }

        /// <summary>
        /// Once the relief has landed the breach order holds a troop to it (LandingZoneBreach.Order);
        /// a fight with one of it that gets nowhere is still given up (FightWatch runs first).
        /// </summary>
        [TestMethod]
        public void AFightWithTheReliefThatGetsNowhereIsGivenUpToo()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            _now += plan.ReliefSeconds * 1000L;
            Tick();
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(point.Id).Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.IsTrue(relief.Length > 1, "the relief has landed");
            var troop = Troops(point.Id).First();
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, relief[0].EntityId));
            Tick();
            _now += LandingZoneAssaults.NoHarmMs + 1000;
            Tick();
            Assert.IsFalse(troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId == relief[0].EntityId,
                "no harm done in NoHarmMs: the relief soldier is given up");
        }

        /// <summary>A troop still closing on its target is getting somewhere: its walk from the drop does not count as a stall.</summary>
        [TestMethod]
        public void ATroopStillClosingOnItsTargetKeepsAtIt()
        {
            var point = Point(Content.NortheastAfs);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var troop = Troops(point.Id).First();
            var guard = Garrison(point).First(c => !Emplacements.Is(c));
            var away = Vector3.Normalize(new Vector3(troop.Position.X - guard.Position.X, 0, troop.Position.Z - guard.Position.Z));
            troop.Position = guard.Position + away * 60;
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, guard.EntityId));
            Tick();
            // 40 s walking in, 2 m nearer every 5 s.
            for (var i = 1; i <= 8; i++)
            {
                _now += 5000;
                troop.Position = guard.Position + away * (60 - 2 * i);
                Tick();
                Assert.AreEqual(guard.EntityId, troop.Controller.ActionFighting.TargetEntityId, $"still closing at {60 - 2 * i} m");
            }
            // Then standing where it is, no harm done.
            _now += LandingZoneAssaults.NoHarmMs + 1000;
            Tick();
            Assert.IsFalse(troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId == guard.EntityId,
                "no nearer and no harm done in NoHarmMs: given up");
        }

        /// <summary>A sortie whose whole squad is dead is over: the Bane garrison's hold goes with it.</summary>
        [TestMethod]
        public void ASortieWhoseSquadIsDeadIsOver()
        {
            var point = Point(Content.SoutheastBane);
            Assert.IsTrue(AfsSorties.TryStart(_world.Map, point));
            Tick();
            Assert.IsTrue(AfsSorties.IsRunning(_world.Map, point), "its ship on the way");
            Deliver();
            var squad = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.IsTrue(squad.Length > 0);
            foreach (var trooper in squad) Kill(trooper);
            Tick();
            Assert.IsFalse(AfsSorties.IsRunning(_world.Map, point), "over");
            Assert.IsFalse(AfsSorties.HoldsGarrison(_world.Map, point), "the Bane garrison may come back");
        }

        /// <summary>
        /// The relief of an assault that is over, held off or not, goes with it: left at the point it
        /// was no defender of the next (live run 2026-11-05: AFS riflemen, machine gunners and mechs
        /// at full health by the point while the Bane claimed it).
        /// </summary>
        [TestMethod]
        public void AnAssaultThatIsOverTakesItsReliefOff()
        {
            var point = Point(Content.NortheastAfs);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            _now += plan.ReliefSeconds * 1000L;
            Tick();
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(point.Id).Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.IsTrue(relief.Length > 0, "the relief has landed");
            // One of it in none of the map's cells, where ControlPoints.TakeOffLiving looks.
            foreach (var cell in _world.Map.MapCellInfo.Cells.Values) cell.CreatureList.Remove(relief[0]);
            ControlPoints.TakeOffLiving(_world.Map, Assaults(point.Id).ToList());
            _now += plan.LifetimeSeconds * 1000L;
            Tick();
            Assert.IsFalse(LandingZoneAssaults.IsRunning(_world.Map, point), "held off");
            Assert.IsFalse(relief.Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "its relief is taken off with it");
        }

        /// <summary>
        /// A troop on one of the garrison that has just died, or on a turret's wreck, is taken off it;
        /// that is no fight with an NPC standing by (live run 2026-11-05: 42 "not the garrison" lines
        /// for the Bane on Retread Outpost's fallen turret).
        /// </summary>
        [TestMethod]
        public void ATroopOnAFallenDefenderIsTakenOffItAndThatIsNoOffPlanFight()
        {
            var point = Point(Content.NortheastAfs);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, point));
            Deliver();
            var troop = Troops(point.Id).First();
            var guard = Garrison(point).First(c => !Emplacements.Is(c));
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, guard.EntityId));
            Kill(guard);
            troop.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            troop.Controller.ActionFighting.TargetEntityId = guard.EntityId;
            Assert.IsFalse(LandingZoneAssaults.FightsOffPlan(troop, Garrison(point)), "a fallen defender is no NPC standing by");
            Assert.IsTrue(LandingZoneAssaults.FightsTheDown(_world.Map, troop));
            Tick();
            Assert.IsFalse(troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId == guard.EntityId,
                "taken off the fallen defender");
        }

        /// <summary>A sortie on a point a player takes is over, and its squad goes with it.</summary>
        [TestMethod]
        public void ASortieOnAPointAPlayerTakesTakesItsSquadOff()
        {
            var clock = ControlPoints.Instance.Now;
            ControlPoints.Instance.Now = () => _now;
            try
            {
                var point = Point(Content.SoutheastBane);
                _world.SpawnWorld(point.BanePools.ToArray());
                Deliver();
                Assert.IsTrue(AfsSorties.TryStart(_world.Map, point));
                Deliver();
                var squad = AfsSorties.Squad(_world.Map, point).ToArray();
                Assert.IsTrue(squad.Length > 0);
                // One of it in none of the map's cells, where ControlPoints.TakeOffLiving looks.
                foreach (var cell in _world.Map.MapCellInfo.Cells.Values) cell.CreatureList.Remove(squad[0]);
                foreach (var creature in BaneGarrison(point)) Kill(creature);
                Tick();
                Assert.IsTrue(ControlPoints.Instance.Captured(_world.Map, _world.Client, point), "the player's capture");
                Tick();
                Assert.IsFalse(AfsSorties.IsRunning(_world.Map, point), "the sortie is over");
                Assert.IsFalse(squad.Any(c => CreatureManager.IsLivingOnMap(_world.Map, c)), "its squad is taken off");
            }
            finally
            {
                ControlPoints.Instance.Now = clock;
            }
        }
    }
}
