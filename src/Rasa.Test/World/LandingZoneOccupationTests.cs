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
    public class LandingZoneOccupationTests
    {
        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _old;
        private ControlPoints.Point Point => ControlPoints.Instance.ById(4);
        private SpawnPool[] Pools => _world.Map.SpawnPools.Where(LandingZoneOccupation.IsGroup).ToArray();
        private Creature[] Living => EntityManager.Instance.Creatures.Values.Where(c => Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        [TestInitialize]
        public void Start()
        {
            _old = ControlPoints.Instance.Config;
            ControlPoints.Instance.Config = new ControlPointConfig { LandingZoneAssaultEnabled = false };
            _world = WildernessRuntimeTestHarness.Create();
            _world.Map.SpawnPools.RemoveAll(p => !LandingZoneOccupation.IsGroup(p));
            using var db = _world.CreateWorld();
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.Id == 4), db.ControlPoints.GetLinks().Where(p => p.ControlPointId == 4), null);
            ControlPoints.Instance.Place(_world.Map);
        }
        [TestCleanup]
        public void End()
        {
            if (_world != null)
            {
                ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
                ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
                _world.Dispose();
            }
            ControlPoints.Instance.Config = _old;
        }
        private void Worker(long ms = 0) => SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, ms);
        private void Ships() { for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000); }
        private void Deploy()
        {
            Assert.IsTrue(ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null));
            Worker(300000); Ships();
        }
        private void Kill(Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(_world.Map, creature, null);
        }
        [TestMethod]
        public void FourShipsLaunchOnlyAfterFiveMinutesAndDeliverLevelElevenOccupationGroups()
        {
            Assert.AreEqual(4, Pools.Length);
            _world.MoveTo(Point.Position);
            _world.Drain();
            Worker(600000);
            Assert.AreEqual(0, Living.Length, "Bane never occupy an AFS-owned point.");
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Assert.IsFalse(ControlPoints.Instance.MayCapture(_world.Map, Point), "Pending occupation counts as an inbound garrison.");
            Worker(299999);
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => Pools.Contains(s.SpawnPool)));
            Worker(1);
            var ships = DynamicObjectManager.Instance.Dropships.Values.Where(s => Pools.Contains(s.SpawnPool)).ToArray();
            Assert.AreEqual(4, ships.Length);
            Assert.IsTrue(ships.All(s => s.TargetCategory == TargetCategory.Hostile && s.Arrival?.Kind == SpawnPoolArrivalEntry.KindDropship));
            CollectionAssert.AreEquivalent(Pools.Select(p => p.Position).ToArray(), ships.Select(s => s.Position).ToArray());
            Assert.AreEqual(0, Living.Length);
            Ships();
            foreach (var pool in Pools)
            {
                Assert.IsTrue(pool.IsGarrison && Point.BanePools.Contains(pool.DbId));
                Assert.AreEqual(300000L, pool.RespawnTime);
                Assert.IsFalse(ControlPoints.Instance.HoldsBack(pool), "Occupation squads respawn independently.");
                var group = Living.Where(c => c.SpawnPool == pool).ToArray();
                Assert.IsTrue(group.Count(c => c.DbId == LandingZoneAssaultContent.Private) is >= 4 and <= 6);
                Assert.AreEqual(1, group.Count(c => c.DbId == LandingZoneAssaultContent.Sergeant));
                Assert.AreEqual(pool.DbId == LandingZoneOccupationContent.Left ? 1 : 0, group.Count(c => c.DbId == LandingZoneAssaultContent.Boss));
                Assert.IsTrue(group.All(c => c.Level == 11 && c.TargetCategory == TargetCategory.Hostile && c.Actions.Count > 0));
                Assert.AreEqual(0, pool.QueuedCreatures + pool.DropshipQueue);
            }
            Assert.AreEqual(1, Living.Count(c => c.DbId == LandingZoneAssaultContent.Boss));
            Assert.AreEqual(Living.Length, _world.Drain().OfType<PreTeleportPacket>().Count(p => p.TeleportType == TeleportType.BaneTeleporter), "Every soldier receives native Bane beam-down FX.");
        }
        [TestMethod]
        [DataRow(690101u)]
        [DataRow(690102u)]
        [DataRow(690103u)]
        [DataRow(690104u)]
        public void OnlyTheWipedSquadReturnsFiveMinutesAfterItsLastDeath(uint id)
        {
            Deploy();
            var group = Living.Where(c => c.SpawnPool.DbId == id).ToArray();
            var others = Living.Except(group).Select(c => c.EntityId).ToArray();
            foreach (var creature in group.Skip(1)) Kill(creature);
            Worker(600000);
            Assert.AreEqual(1, Living.Count(c => c.SpawnPool.DbId == id));
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => Pools.Contains(s.SpawnPool)));
            Kill(group[0]);
            Worker(299999);
            var pool = Pools.Single(p => p.DbId == id);
            Assert.AreEqual(0, pool.QueuedCreatures + pool.DropshipQueue);
            Worker(1);
            Assert.AreEqual(1, DynamicObjectManager.Instance.Dropships.Values.Count(s => Pools.Contains(s.SpawnPool)));
            Assert.IsTrue(pool.QueuedCreatures >= 5);
            Ships();
            Assert.IsTrue(Living.Count(c => c.SpawnPool == pool) >= 5);
            Assert.IsTrue(others.All(entity => Living.Any(c => c.EntityId == entity)));
        }
        [TestMethod]
        public void ElevatedTroopsReachAllThreeRampartsAndEveryPatrolLegIsWalkable()
        {
            Deploy();
            foreach (var post in LandingZoneOccupationContent.Posts)
            {
                var floor = _world.Map.NavMesh.Nearest(post);
                Assert.IsTrue(floor.HasValue && Vector3.Distance(floor.Value, post) < 1, $"Ship post {post} has floor {floor}.");
            }
            foreach (var creature in Living)
            {
                Assert.IsTrue(creature.Patrol?.Count >= 2);
                _world.Map.NavMesh.FindPath(creature.Position, creature.Patrol[0].Position, out var complete);
                Assert.IsTrue(complete, $"Walk-out for {creature.SpawnPool.DbId} from {creature.Position} to {creature.Patrol[0].Position}");
                foreach (var step in creature.Patrol)
                {
                    var next = creature.Patrol[(creature.Patrol.ToList().IndexOf(step) + 1) % creature.Patrol.Count];
                    var walk = _world.Map.NavMesh.WalkTo(step.Position, next.Position);
                    Assert.IsTrue(walk.HasValue && Vector3.Distance(walk.Value, next.Position) < 1, $"{creature.SpawnPool.DbId}: {step.Position} to {next.Position}");
                    if (creature.SpawnPool.DbId == LandingZoneOccupationContent.Ramparts) Assert.IsTrue(step.Position.Y > 170);
                }
                Assert.IsTrue(creature.FightFromPatrol && creature.AggroRange == 35);
            }
            Assert.AreEqual(3, Living.Where(c => c.SpawnPool.DbId == LandingZoneOccupationContent.Ramparts).Select(c => c.Patrol[0].Position).Distinct().Count());
        }
        [TestMethod]
        public void PlayersMustDefeatTheOccupationAndRecaptureCancelsAllReplacements()
        {
            Deploy();
            Assert.IsFalse(ControlPoints.Instance.MayCapture(_world.Map, Point));
            var leader = Living.Single(c => c.DbId == LandingZoneAssaultContent.Boss);
            foreach (var creature in Living.Where(c => c != leader)) Kill(creature);
            Assert.IsFalse(ControlPoints.Instance.MayCapture(_world.Map, Point), "The leader also holds the point.");
            Kill(leader);
            Assert.IsTrue(ControlPoints.Instance.MayCapture(_world.Map, Point));
            Assert.IsTrue(ControlPoints.Instance.Captured(_world.Map, _world.Client, Point));
            Assert.IsTrue(Point.HeldByAfs);
            Worker(600000); Ships();
            Assert.AreEqual(0, Living.Length);
            Assert.IsTrue(Pools.All(p => p.Suspended));
        }
        [TestMethod]
        public void RetakingThePointBeforeBeamDownCancelsShipsAndASecondTakeoverWaitsAgain()
        {
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Worker(300000);
            Assert.AreEqual(4, DynamicObjectManager.Instance.Dropships.Values.Count(s => Pools.Contains(s.SpawnPool)));
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => Pools.Contains(s.SpawnPool)));
            Assert.IsTrue(Pools.All(p => p.QueuedCreatures == 0 && p.DropshipQueue == 0));
            Ships(); Worker(600000);
            Assert.AreEqual(0, Living.Length);
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Worker(299999);
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => Pools.Contains(s.SpawnPool)));
            Worker(1); Ships();
            Assert.IsTrue(Living.Length >= 21);
        }
        [TestMethod]
        public void ExistingBaneTurretsRemainPartOfTheDefendersRequiredForPlayerRecapture()
        {
            SpawnPoolManager.Instance.InitializeMapChannel(_world.Map);
            _world.Map.SpawnPools.RemoveAll(p => !LandingZoneOccupation.IsGroup(p) &&
                !(Point.BanePools.Contains(p.DbId) && p.SpawnSlot.Any(s => s.CreatureId == 551001)));
            ControlPoints.Instance.Place(_world.Map);
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            Worker();
            var turrets = EntityManager.Instance.Creatures.Values.Where(c =>
                c.DbId == 551001 && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
            Assert.AreEqual(6, turrets.Length);
            Worker(300000); Ships();
            foreach (var creature in Living) Kill(creature);
            Assert.IsFalse(ControlPoints.Instance.MayCapture(_world.Map, Point), "The existing Bane turrets also defend the control point.");
            foreach (var turret in turrets) Kill(turret);
            Assert.IsTrue(ControlPoints.Instance.MayCapture(_world.Map, Point));
            Assert.IsTrue(ControlPoints.Instance.Captured(_world.Map, _world.Client, Point));
        }

        [TestMethod]
        public void AShorterTurretRegroupTimerCannotAdvanceTheIndependentOccupationTimers()
        {
            SpawnPoolManager.Instance.InitializeMapChannel(_world.Map);
            _world.Map.SpawnPools.RemoveAll(p => !LandingZoneOccupation.IsGroup(p) &&
                !(Point.BanePools.Contains(p.DbId) && p.SpawnSlot.Any(s => s.CreatureId == 551001)));
            ControlPoints.Instance.Place(_world.Map);
            Deploy();
            foreach (var pool in _world.Map.SpawnPools.Where(p => !LandingZoneOccupation.IsGroup(p))) pool.RespawnTime = 1000;
            foreach (var creature in EntityManager.Instance.Creatures.Values.Where(c =>
                _world.Map.SpawnPools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray()) Kill(creature);
            var original = ControlPoints.Instance.Now;
            long now = 1000;
            try
            {
                ControlPoints.Instance.Now = () => now;
                ControlPoints.Instance.Worker(_world.Map);
                now += 1000;
                ControlPoints.Instance.Worker(_world.Map);
                Assert.IsTrue(Pools.All(p => p.UpdateTimer == 0), "Only the coordinated turret pools may be advanced by their regroup deadline.");
                Worker(1000);
                Assert.IsTrue(Pools.All(p => p.QueuedCreatures == 0 && p.DropshipQueue == 0));
            }
            finally { ControlPoints.Instance.Now = original; }
        }

        [TestMethod]
        public void ExistingCustomLootBindingsSurviveUpgradeAndRollback()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            const string previous = "20270915000000_Combat_technician_entity_classes";
            migrator.Migrate(previous);
            db.Context.Database.ExecuteSqlRaw("INSERT INTO creature_loot_binding (map_context_id,creature_id,profile_key) VALUES (1220,620002,'wilderness_v1_thrax_5_9');");
            migrator.Migrate("20270917000000_Landing_zone_bane_occupation");
            Assert.AreEqual("wilderness_v1_thrax_5_9", db.Context.CreatureLootBindingEntries.AsNoTracking().Single(b => b.MapContextId == 1220 && b.CreatureId == 620002).ProfileKey);
            migrator.Migrate(previous);
            Assert.AreEqual("wilderness_v1_thrax_5_9", db.Context.CreatureLootBindingEntries.AsNoTracking().Single(b => b.MapContextId == 1220 && b.CreatureId == 620002).ProfileKey);
            Assert.IsFalse(db.Context.CreatureLootBindingEntries.Any(b => b.MapContextId == 1220 && (b.CreatureId == 620003 || b.CreatureId == 620006)));
        }

        [TestMethod]
        public void WorldUpgradeRollbackAndMySqlScriptsContainOnlyTheFourNewOccupationPools()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            const string previous = "20270915000000_Combat_technician_entity_classes";
            const string migration = "20270917000000_Landing_zone_bane_occupation";
            migrator.Migrate(previous);
            var before = db.Context.SpawnPoolEntries.AsNoTracking().Select(p => p.Id).ToArray();
            migrator.Migrate(migration);
            var after = db.Context.SpawnPoolEntries.AsNoTracking().Select(p => p.Id).ToArray();
            CollectionAssert.AreEquivalent(new[] { 690101u,690102u,690103u,690104u }, after.Except(before).ToArray());
            migrator.Migrate(previous);
            CollectionAssert.AreEquivalent(before, db.Context.SpawnPoolEntries.AsNoTracking().Select(p => p.Id).ToArray());
            using var mysql = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var sql = mysql.GetService<IMigrator>();
            foreach (var statement in LandingZoneOccupationContent.Up()) StringAssert.Contains(sql.GenerateScript(previous, migration), statement);
            foreach (var statement in LandingZoneOccupationContent.Down()) StringAssert.Contains(sql.GenerateScript(migration, previous), statement);
        }
    }
}
