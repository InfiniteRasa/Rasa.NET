using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class LandingZoneGarrisonTests
    {
        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _old;
        private ControlPoints.Point Point => ControlPoints.Instance.ById(4);
        private SpawnPool[] Pools => _world.Map.SpawnPools.Where(LandingZoneGarrison.IsGroup).ToArray();
        private Creature[] Living => EntityManager.Instance.Creatures.Values.Where(c => Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        [TestInitialize]
        public void Start()
        {
            _old = ControlPoints.Instance.Config;
            ControlPoints.Instance.Config = new ControlPointConfig { LandingZoneAssaultEnabled = false };
            _world = WildernessRuntimeTestHarness.Create();
            _world.Map.SpawnPools.RemoveAll(p => !LandingZoneGarrison.IsGroup(p));
            using var db = _world.CreateWorld();
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.Id == 4), db.ControlPoints.GetLinks().Where(p => p.ControlPointId == 4), null);
            ControlPoints.Instance.Place(_world.Map);
        }
        [TestCleanup]
        public void End()
        {
            if (_world != null)
            {
                ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
                SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 0);
                ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
                _world.Dispose();
            }
            ControlPoints.Instance.Config = _old;
        }
        private void Worker(long ms = 0) => SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, ms);
        private void Ships() { for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000); }
        private void Deploy()
        {
            Worker(); Worker(700); Ships(); Worker(19300); Ships();
            Assert.AreEqual(8, Living.Length);
        }
        private void Kill(Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(_world.Map, creature, null);
        }
        [TestMethod]
        public void NativePoolsAreLevelElevenCombatDefendersWithExactGroupSizesAndDelays()
        {
            Assert.AreEqual(3, Pools.Length);
            foreach (var pool in Pools)
            {
                Assert.IsTrue(pool.IsGarrison);
                Assert.IsTrue(Point.AfsPools.Contains(pool.DbId));
                Assert.IsNull(ControlPoints.Instance.ServicePointOf(_world.Map, pool));
                Assert.AreEqual(pool.DbId == 680001 ? 600000L : 300000L, pool.RespawnTime);
                Assert.AreEqual((short)(pool.DbId == 680001 ? 2 : 3), pool.SpawnSlot[0].CountMin);
                Assert.AreEqual(pool.SpawnSlot[0].CountMin, pool.SpawnSlot[0].CountMax);
            }
            Deploy();
            Assert.IsTrue(Living.All(c => c.Level == 11 && c.TargetCategory == TargetCategory.Friendly));
            Assert.AreEqual(2, Living.Count(c => c.DbId == LandingZoneGarrisonContent.Mech));
            Assert.AreEqual(6, Living.Count(c => c.DbId == LandingZoneGarrisonContent.Rifleman));
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(_world.Map, Point, ControlPoints.Afs));
            Assert.IsTrue(Living.All(c => c.Actions.Count > 0 && c.Attributes[Attributes.Health].Current > 0));
        }
        [TestMethod]
        public void FirstDeploymentUsesTeleportAndTwoFriendlyGunshipsAtTheAuthoredLocations()
        {
            _world.MoveTo(LandingZoneGarrisonContent.Teleporter + new Vector3(3, 0, 0));
            _world.Drain();
            Worker();
            Assert.AreEqual(0, Living.Length);
            Worker(700);
            Assert.AreEqual(2, Living.Length);
            Assert.AreEqual(2, _world.Drain().OfType<TeleportArrivalPacket>().Count());
            Assert.IsTrue(Living.All(c => Vector3.Distance(c.Position, LandingZoneGarrisonContent.Teleporter) < 5), string.Join("; ", Living.Select(c => c.Position)));
            var first = DynamicObjectManager.Instance.Dropships.Values.Single(s => LandingZoneGarrison.IsGroup(s.SpawnPool));
            Assert.AreEqual(TargetCategory.Friendly, first.TargetCategory);
            Assert.AreEqual(EntityClasses.UsableCrSpawnerHumDropshipV01, first.EntityClassId);
            Assert.AreEqual(LandingZoneGarrisonContent.Gunship, first.Position);
            Assert.AreEqual(LandingZoneGarrisonContent.GunshipRotation, first.Rotation);
            Ships();
            Assert.AreEqual(5, Living.Length);
            Worker(19300);
            Assert.AreEqual(1, DynamicObjectManager.Instance.Dropships.Values.Count(s => LandingZoneGarrison.IsGroup(s.SpawnPool)));
            Ships();
            Assert.AreEqual(8, Living.Length);
            Assert.IsTrue(Pools.All(p => p.QueuedCreatures == 0 && p.DropshipQueue == 0));
        }
        [TestMethod]
        [DataRow(680001u, 600000L)]
        [DataRow(680002u, 300000L)]
        [DataRow(680003u, 300000L)]
        public void OnlyAWipedGroupIsReplacedAndItsDelayStartsAtTheLastDeath(uint id, long delay)
        {
            Deploy();
            var group = Living.Where(c => c.SpawnPool.DbId == id).ToArray();
            var others = Living.Except(group).Select(c => c.EntityId).ToArray();
            foreach (var creature in group.Skip(1)) Kill(creature);
            Worker(1200000);
            Assert.AreEqual(1, Living.Count(c => c.SpawnPool.DbId == id), "no replacements while a member survives");
            Kill(group[0]);
            Worker(delay - 1);
            var pool = Pools.Single(p => p.DbId == id);
            Assert.AreEqual(0, pool.QueuedCreatures);
            Assert.AreEqual(0, pool.DropshipQueue);
            Worker(1);
            Assert.AreEqual(group.Length, pool.QueuedCreatures);
            Worker(700); Ships();
            Assert.AreEqual(group.Length, Living.Count(c => c.SpawnPool.DbId == id));
            Assert.IsTrue(others.All(entity => Living.Any(c => c.EntityId == entity)), "other groups remain unchanged");
        }
        [TestMethod]
        public void EveryPatrolAndWalkOutHasACompleteRouteAndInfantryStayOnElevatedSurfaces()
        {
            Deploy();
            foreach (var creature in Living)
            {
                Assert.IsTrue(creature.Patrol.Count >= 2, $"{creature.DbId}, pool {creature.SpawnPool.DbId}: {string.Join("; ", creature.Patrol.Select(p => p.Position))}");
                _world.Map.NavMesh.FindPath(creature.Position, creature.Patrol[0].Position, out var complete);
                Assert.IsTrue(complete, $"walk-out for {creature.DbId}");
                foreach (var step in creature.Patrol)
                {
                    if (creature.DbId == LandingZoneGarrisonContent.Rifleman) Assert.IsTrue(step.Position.Y > 170, $"catwalk height {step.Position}");
                    var next = creature.Patrol[(creature.Patrol.ToList().IndexOf(step) + 1) % creature.Patrol.Count];
                    var walk = _world.Map.NavMesh.WalkTo(step.Position, next.Position);
                    Assert.IsTrue(walk.HasValue && Vector3.Distance(walk.Value, next.Position) < 1, $"walkable leg {step.Position} to {next.Position}");
                }
            }
            foreach (var pool in Pools.Where(p => p.DbId != 680001))
            {
                var group = Living.Where(c => c.SpawnPool == pool).ToArray();
                Assert.AreEqual(3, group.Select(c => c.Patrol[0].Position).Distinct().Count());
                Assert.IsTrue(group.All(c => c.FightFromPatrol && c.AggroRange == 35));
            }
        }
        [TestMethod]
        public void BothOwnershipStatesAndInboundTakeoverPreventAFSSpawningUntilRecapture()
        {
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Worker(1200000); Worker(1200000); Ships();
            Assert.AreEqual(0, Living.Length);
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
            Worker(); Worker(1);
            Assert.IsTrue(DynamicObjectManager.Instance.Dropships.Values.Any(s => LandingZoneGarrison.IsGroup(s.SpawnPool)));
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Assert.IsNull(CreatureManager.Instance.CreateCreature(LandingZoneGarrisonContent.Rifleman, Pools.Single(p => p.DbId == 680002)));
            Worker(); Ships();
            Assert.AreEqual(0, Living.Length);
            Assert.IsTrue(Pools.All(p => p.QueuedCreatures == 0 && p.DropshipQueue == 0));
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
            Worker(); Worker(20000); Ships(); Worker(20000); Ships();
            Assert.AreEqual(8, Living.Length);
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Assert.AreEqual(0, Living.Length, "living defenders despawn during takeover");
        }
        [TestMethod]
        public void AllEightDefendersActuallyWalkOntoTheirAssignedPatrols()
        {
            Deploy();
            _world.MoveTo(new Vector3(200, 171, -60));
            var reached = new System.Collections.Generic.HashSet<ulong>();
            for (var i = 0; i < 1600; i++)
            {
                _world.Tick(250);
                foreach (var c in Living.Where(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionPatrol)) reached.Add(c.EntityId);
                if (reached.Count == 8) break;
            }
            Assert.AreEqual(8, reached.Count, string.Join("; ", Living.Where(c => !reached.Contains(c.EntityId)).Select(c => $"{c.DbId} at {c.Position}, action {c.Controller.CurrentAction}, goal {c.Patrol[0].Position}")));
        }
        [TestMethod]
        public void CatwalkRiflemenFireAtBaneInRangeAndDoNotChaseOffTheirBeat()
        {
            Deploy();
            var soldier = Living.First(c => c.DbId == LandingZoneGarrisonContent.Rifleman);
            foreach (var other in Living.Where(c => c != soldier)) other.IsScripted = true;
            soldier.Position = new Vector3(200, 171.1178f, -38);
            soldier.ImmuneToAllDamage = true;
            _world.MoveTo(new Vector3(200, 164, -20));
            var bane = CreatureManager.Instance.CreateCreature(LandingZoneAssaultContent.Private, null);
            CreatureManager.Instance.SetLocation(bane, new Vector3(200, 164, -14), 0, 1220);
            CellManager.Instance.AddToWorld(_world.Map, bane);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(soldier, bane.EntityId));
            var before = bane.Attributes[Attributes.Health].Current + bane.Attributes[Attributes.Armor].Current;
            for (var i = 0; i < 40; i++) { _world.Tick(250); MissileManager.Instance.DoWork(_world.Map, 250); }
            Assert.IsTrue(bane.Attributes[Attributes.Health].Current + bane.Attributes[Attributes.Armor].Current < before, "actual rifle damage from catwalk to attackers outside");
            bane.Position = new Vector3(200, 164, 100);
            var from = soldier.Position;
            BehaviorManager.Instance.TrySetActionFighting(soldier, bane.EntityId);
            _world.Tick(250);
            Assert.AreEqual(from, soldier.Position, "no chase step off the catwalk");
        }
        [TestMethod]
        public void PendingReinforcementsAndEveryLastSurvivorCountAsCaptureDefenders()
        {
            Worker();
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(_world.Map, Point, ControlPoints.Afs));
            Worker(700); Ships(); Worker(19300); Ships();
            var all = Living;
            foreach (var guard in all.Skip(1)) Kill(guard);
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(_world.Map, Point, ControlPoints.Afs));
            Kill(all[0]);
            Assert.AreEqual(ControlPoints.Garrison.Down, ControlPoints.Instance.GarrisonOf(_world.Map, Point, ControlPoints.Afs));
            Worker(300000);
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(_world.Map, Point, ControlPoints.Afs));
        }
        [TestMethod]
        public void MySqlMigrationContainsTheSameContentAndARollback()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript("20270904000000_Landing_zone_assault", "20270905000000_Landing_zone_garrison");
            var down = migrator.GenerateScript("20270905000000_Landing_zone_garrison", "20270904000000_Landing_zone_assault");
            foreach (var sql in LandingZoneGarrisonContent.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in LandingZoneGarrisonContent.Down()) StringAssert.Contains(down, sql);
        }
    }
}
