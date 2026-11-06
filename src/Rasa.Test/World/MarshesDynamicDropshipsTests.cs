using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions;
using Rasa.Test.Missions.Wilderness;
using Content = Rasa.Services.Preloader.MarshesDynamicDropshipsContent;

namespace Rasa.Test.World
{
    /// <summary>Valverde Marshes' control points: AFS garrisons by dropship, timed Bane assaults, AFS relief and sorties.</summary>
    [TestClass, DoNotParallelize]
    public class MarshesDynamicDropshipsTests
    {
        private const string Migration = "20271105000200_Marshes_dynamic_dropships";

        /// <summary>How far, across the ground, a Bane drop lands from each of its point's turrets at least (ESTIMATE).</summary>
        private const float DropClearOfTurrets = 30f;

        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _config;
        private Func<long> _clock;
        private long _now;

        private static ControlPoints.Point Point(uint id) => ControlPoints.Instance.ById(id);
        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

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
                db.ControlPoints.GetLinks().Where(l => l.ControlPointId is Content.ResearchPoint or Content.BaneAssertionCamp), null);
            ControlPoints.Instance.Place(_world.Map);
        }

        [TestCleanup]
        public void End()
        {
            ControlPoints.Instance.Config.ControlPointAssaultEnabled = false;
            foreach (var id in new[] { Content.ResearchPoint, Content.BaneAssertionCamp })
            {
                LandingZoneAssaults.Worker(_world.Map, Point(id));
                AfsSorties.Worker(_world.Map, Point(id));
            }
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
        public void TheMarshesHaveTwoControlPointsResearchPointAfsAndBaneAssertionCampBane()
        {
            Assert.AreEqual(2, ControlPoints.Instance.Points.Count(p => p.MapContextId == Content.MapId), "Falcon Hold is not a control point");
            Assert.AreEqual(ControlPoints.Afs, Point(Content.ResearchPoint).Owner);
            Assert.AreEqual(ControlPoints.Bane, Point(Content.BaneAssertionCamp).Owner);
            Assert.AreEqual(2, Content.Assaults.Length);
            Assert.IsTrue(Content.Assaults.Single(a => a.PointId == Content.BaneAssertionCamp).Sortie.Length > 0, "Bane Assertion Camp (CP 11) has a sortie plan");
        }

        [TestMethod]
        public void EveryDropPadReliefPadAndGarrisonPostStandsOnGroundThatReachesItsPoint()
        {
            foreach (var assault in Content.Assaults)
            {
                Assert.AreEqual(Content.MapId, assault.MapId, $"point {assault.PointId}");
                Assert.AreEqual(2, assault.Drops.Length, $"point {assault.PointId}: two Bane ships a wave");
                for (var i = 0; i < assault.Drops.Length; i++)
                {
                    Assert.IsTrue(Vector3.Distance(Walk(assault.Drops[i].Position, "drop"), assault.Drops[i].Position) < 2f, $"point {assault.PointId} drop {i + 1} on the ground");
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
                Assert.IsTrue(Vector3.Distance(pool.At, Point(pool.Point).Position) < 60, $"pool {pool.Id} inside its point (the garrison posts are at most about 52 m out)");
            }
        }

        [TestMethod]
        public void EveryIdIsInTheMarshesBlockAndEveryPlanUsesTheMarshesOwnAfsTroops()
        {
            var afs = new[] { Content.Rifleman, Content.MachineGunner, Content.Mech };
            Assert.IsTrue(afs.Concat(Content.Garrison.Select(g => g.Id)).All(id => id is >= Content.IdMin and <= Content.IdMax));
            Assert.AreEqual(Content.Garrison.Length, Content.Garrison.Select(g => g.Id).Distinct().Count());
            Assert.IsFalse(Content.Garrison.Any(g => PoolsDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id) || PlateauDynamicDropshipsContent.Garrison.Any(p => p.Id == g.Id)));
            foreach (var assault in Content.Assaults)
            {
                Assert.IsTrue(assault.Relief.Concat(assault.Sortie).All(t => afs.Contains(t.Creature)), $"point {assault.PointId}");
                Assert.AreSame(assault, DynamicDropshipsContent.Assaults.Single(a => a.MapId == Content.MapId && a.PointId == assault.PointId));
            }
            foreach (var id in afs.Concat(new[] { Content.ThraxSoldier, Content.ThraxGrenadier, Content.ThraxTechnician, Content.Caretaker, Content.ThraxCommander }))
                Assert.IsTrue(_world.World.CreatureEntries.Any(c => c.Id == id), $"creature {id}");
        }

        [TestMethod]
        public void TheMarshesAfsTroopsAreThePoolsTimesTheMiresOverTheMarshesThrax()
        {
            var creatures = _world.World.CreatureEntries.AsNoTracking().ToDictionary(c => c.Id);
            var actions = _world.World.CreatureActionEntries.AsNoTracking().ToDictionary(a => a.Id);
            foreach (var (marshes, pools) in new[] { (Content.Rifleman, PoolsDynamicDropshipsContent.Rifleman),
                (Content.MachineGunner, PoolsDynamicDropshipsContent.MachineGunner), (Content.Mech, PoolsDynamicDropshipsContent.Mech) })
            {
                Assert.AreEqual((uint)Math.Round(creatures[pools].MaxHitPoints * 1.3), creatures[marshes].MaxHitPoints, $"creature {marshes} health");
                Assert.AreEqual(creatures[pools].Level + 3, creatures[marshes].Level, $"creature {marshes} level");
                Assert.AreEqual(1u, creatures[marshes].Faction, "AFS");
                Assert.AreEqual((uint)Math.Round(actions[pools].MinDamage * 1.3), actions[marshes].MinDamage, $"action {marshes}");
                Assert.AreEqual((uint)Math.Round(actions[pools].MaxDamage * 1.3), actions[marshes].MaxDamage, $"action {marshes}");
            }
        }

        [TestMethod]
        public void ResearchPointHasAGarrisonThatComesBackByAfsDropship()
        {
            var id = Content.ResearchPoint;
            var pools = _world.Map.SpawnPools.Where(p => Point(id).AfsPools.Contains(p.DbId) && Content.Garrison.Any(g => g.Id == p.DbId)).ToArray();
            Assert.AreEqual(7, pools.Length);
            Assert.AreEqual(8, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.Rifleman).Sum(g => g.Troop.Max));
            Assert.AreEqual(2, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.MachineGunner).Sum(g => g.Troop.Max));
            Assert.AreEqual(2, Content.Garrison.Where(g => g.Point == id && g.Troop.Creature == Content.Mech).Sum(g => g.Troop.Max));
            Assert.IsTrue(pools.All(p => p.AnimType == 2 && p.IsGarrison && !p.Suspended));
            Assert.AreEqual(16, Content.Garrison.Where(g => g.Point == Content.BaneAssertionCamp).Sum(g => g.Troop.Max), "10 riflemen, 2 machine gunners, 4 mechs");
            Assert.IsTrue(_world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == Content.BaneAssertionCamp)).All(p => p.Suspended),
                "no AFS garrison at Bane Assertion Camp while the Bane hold it");
        }

        [TestMethod]
        public void ResearchPointIsAttackedInThreeWavesOfTwoShipsWithTheCommanderInTheLast()
        {
            var research = Content.Assaults.Single(a => a.PointId == Content.ResearchPoint);
            Assert.AreEqual(1800, research.PeriodSeconds, "the Pools' period: Research Point is seldom AFS");
            _now += research.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(Content.ResearchPoint).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(Content.ResearchPoint).Length);
            Deliver();
            _now += research.WaveSeconds * 1000L;
            Tick();
            _now += research.WaveSeconds * 1000L;
            Tick();
            Deliver();
            Assert.AreEqual(6, Assaults(Content.ResearchPoint).Length);
            var troops = Troops(Content.ResearchPoint);
            Assert.AreEqual(1, troops.Count(c => c.DbId == Content.ThraxCommander));
            Assert.IsTrue(troops.Length is >= 28 and <= 33, $"{troops.Length} Bane in the first assault");
            Assert.IsTrue(troops.All(c => c.Level is >= 36 and <= 39), "2-4 levels over the zone's Bane, the commander 5-6");
            _now += research.ReliefSeconds * 1000L - 2L * research.WaveSeconds * 1000;
            Tick();
            Assert.AreEqual(1, Relief(Content.ResearchPoint).Length, "the AFS relief ship after the last wave");
            Deliver();
            var relief = EntityManager.Instance.Creatures.Values.Where(c => Relief(Content.ResearchPoint).Contains(c.SpawnPool)).ToArray();
            Assert.AreEqual(4, relief.Length);
            Assert.IsTrue(relief.All(c => c.DbId is Content.Rifleman or Content.MachineGunner));
        }

        [TestMethod]
        public void TheCommanderTakesResearchPointWhenItsDefendersAreDeadAndThenAfsSquadsCome()
        {
            var point = Point(Content.ResearchPoint);
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

            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _now += plan.SortieFirstDelaySeconds * 1000L;
            Tick();
            Assert.AreEqual(1, DynamicObjectManager.Instance.Dropships.Values.Count(s => s.SpawnPool?.AssaultPointId == point.Id && s.SpawnPool.AnimType == 2),
                "an AFS sortie ship on the fallen Research Point");
        }

        [TestMethod]
        public void AWaveTakesOnTheGarrisonBeforeTheTurretsAndKeepsThePointAsItsHome()
        {
            // The turrets are in the AFS garrison (ControlPointTurrets, link kind 2), and troops of the
            // north drop went for the nearest of them, on a mount off the navmesh, stalled, leashed and
            // came back, and never reached the point. Research Point's turret layout, as the Plateau
            // test has it for Northeast.
            var point = Point(Content.ResearchPoint);
            _world.SpawnWorld(point.AfsPools.ToArray());
            Deliver();
            var turrets = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                Emplacements.Is(c) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.IsTrue(turrets.Length > 0, "the point's AFS turrets stand");
            Assert.IsTrue(EntityManager.Instance.Creatures.Values.Any(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                !Emplacements.Is(c) && CreatureManager.IsLivingOnMap(_world.Map, c)), "the point's AFS troops stand");
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

        private Creature[] Garrison(ControlPoints.Point point) => EntityManager.Instance.Creatures.Values
            .Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) && ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) == null &&
                CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();

        private void Kill(Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(_world.Map, creature, null);
        }

        /// <summary>
        /// Live run 2026-11-05: the GM-killed riflemen and machine gunners came back by dropship at the
        /// third wave, a turret was repaired to full health, and the mechs' ships ended a "garrison
        /// down" after 14 s. While an assault runs the garrison stays down; after it, it comes back.
        /// </summary>
        [TestMethod]
        public void NoAfsGarrisonComesBackAndNoTurretIsRepairedWhileAnAssaultRuns()
        {
            var point = Point(Content.ResearchPoint);
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
            var point = Point(Content.ResearchPoint);
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
        /// Live run 2026-11-05: the garrison down, the Commander, a Technician and 2-3 Soldiers stood
        /// 50-73 m out and the point was never claimed. Now the Bane make for the point, the claimant
        /// first; if it dies the next one claims; the assault does not end as held off while the
        /// garrison is down and a claimant stands; the claim done, the point is the Bane's.
        /// </summary>
        [TestMethod]
        public void WithTheGarrisonDownTheBaneGoToThePointAndTheNextClaimantTakesIt()
        {
            var point = Point(Content.ResearchPoint);
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
            // A soldier out there in a fight with something that is no garrison (set as the live run had them: "fighting").
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

        [TestMethod]
        public void AnAfsSquadAttacksBaneAssertionCampButCannotTakeIt()
        {
            var point = Point(Content.BaneAssertionCamp);
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            Tick();
            _now += plan.SortieFirstDelaySeconds * 1000L;
            Tick();
            var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool?.AssaultPointId == point.Id);
            Assert.AreEqual(2, ship.SpawnPool.AnimType, "an AFS ship");
            Deliver();
            var squad = AfsSorties.Squad(_world.Map, point).ToArray();
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.Mech));
            Assert.AreEqual(4, squad.Count(c => c.DbId == Content.Rifleman), "4 riflemen");
            Assert.AreEqual(1, squad.Count(c => c.DbId == Content.MachineGunner), "and a machine gunner (ESTIMATE, one over retail's 3-4)");
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => point.BanePools.Contains(p.DbId)).ToList());
            foreach (var trooper in squad) trooper.Position = Walk(point.Position, "point");
            _now += 30000;
            Tick();
            Assert.AreEqual(ControlPoints.Bane, point.Owner, "no claim while the Bane garrison stands (its pools have yet to set anyone down)");
        }

        [TestMethod]
        public void ACapturedBaneAssertionCampGetsItsAfsGarrisonAndACounterAttack()
        {
            var point = Point(Content.BaneAssertionCamp);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            Tick();
            var garrison = _world.Map.SpawnPools.Where(p => Content.Garrison.Any(g => g.Id == p.DbId && g.Point == point.Id)).ToArray();
            Assert.AreEqual(10, garrison.Length);
            Assert.IsTrue(garrison.All(p => !p.Suspended && p.AnimType == 2));
            var plan = Content.Assaults.Single(a => a.PointId == point.Id);
            _now += plan.FirstDelaySeconds * 1000L - 1;
            Tick();
            Assert.AreEqual(0, Assaults(point.Id).Length);
            _now++;
            Tick();
            Assert.AreEqual(2, Assaults(point.Id).Length, "the Bane counter-attack after a delay");
        }

        [TestMethod]
        public void EachAssaultHeldOffMakesTheNextBigger()
        {
            var point = Point(Content.ResearchPoint);
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
            Assert.IsTrue(Soldiers(1) is >= 12 and <= 14, "twice after one held off (MaxEscalation 1)");
            Assert.IsTrue(Soldiers(2) is >= 12 and <= 14, "and no more after two");
        }

        [TestMethod]
        public void TheMigrationAddsTheRowsAndItsDownTakesThemOut()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var database = Path.Combine(directory, "world");
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());
                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                void Rows(int creatures, int pools, string when)
                {
                    Assert.AreEqual(creatures, world.CreatureEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"creatures {when}");
                    Assert.AreEqual(creatures, world.CreatureActionEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"actions {when}");
                    Assert.AreEqual(pools, world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= Content.IdMin && r.Id <= Content.IdMax), $"pools {when}");
                    Assert.AreEqual(pools, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax), $"links {when}");
                }
                Rows(3, 17, "after");
                Assert.AreEqual(7, world.ControlPointLinkEntries.AsNoTracking().Count(r => r.ControlPointId == Content.ResearchPoint && r.Kind == 2 && r.ObjectId >= Content.IdMin && r.ObjectId <= Content.IdMax));
                world.GetService<IMigrator>().Migrate(DynamicDropshipsMigrations.Previous(world, Migration));
                Rows(0, 0, "down");
                Assert.AreEqual(30, world.SpawnPoolEntries.AsNoTracking().Count(r => r.Id >= 692101 && r.Id <= 692199), "the Pools' rows stay");
                world.GetService<IMigrator>().Migrate(Migration);
                Rows(3, 17, "up again");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }
    }
}
