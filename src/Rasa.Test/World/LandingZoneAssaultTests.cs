using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public partial class LandingZoneAssaultTests
    {
        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _config;
        private Func<long> _clock;
        private long _now;
        private ControlPoints.Point Point => ControlPoints.Instance.ById(4);
        [TestInitialize]
        public void Start()
        {
            _config = ControlPoints.Instance.Config;
            _clock = LandingZoneAssaults.Now;
            _now = 1000;
            LandingZoneAssaults.Now = () => _now;
            ControlPoints.Instance.Config = new ControlPointConfig();
            _world = WildernessRuntimeTestHarness.Create();
            using var db = _world.CreateWorld();
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.Id == 4), db.ControlPoints.GetLinks().Where(p => p.ControlPointId == 4), null);
            ControlPoints.Instance.Place(_world.Map);
        }
        [TestCleanup]
        public void End()
        {
            ControlPoints.Instance.Config.LandingZoneAssaultEnabled = false;
            LandingZoneAssaults.Worker(_world.Map, Point);
            ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
            _world.Dispose();
            ControlPoints.Instance.Config = _config;
            LandingZoneAssaults.Now = _clock;
        }
        private SpawnPool[] Pools => _world.Map.SpawnPools.Where(p => p.IsControlPointAssault).ToArray();
        private Creature[] Troops => EntityManager.Instance.Creatures.Values.Where(c => Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        private void Deliver() { for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000); }
        private void WaveThree()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Deliver();
            _now += 360000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Deliver();
        }
        [TestMethod]
        public void HourlyScheduleAndFixedWaveOffsetsAllowSurvivorsToOverlap()
        {
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 3599999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Pools.Length);
            _now++;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
            Assert.IsFalse(LandingZoneAssaults.TryStart(_world.Map, Point));
            Deliver();
            var survivors = Troops.Select(c => c.EntityId).ToArray();
            _now += 179999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
            _now++;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(12, Pools.Length);
            _now += 180000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(18, Pools.Length);
            Deliver();
            Assert.IsTrue(survivors.All(id => Troops.Any(c => c.EntityId == id)));
            Assert.AreEqual(1, Troops.Count(c => c.DbId == LandingZoneAssaultContent.Boss));
            Assert.IsTrue(Troops.All(c => c.Level == 11));
            Assert.IsTrue(Troops.All(c => Rasa.Game.Missions.World.CreatureGameplayRules.RewardsKills(c)), "world attackers award normal kill rewards");
            _now += 540000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Pools.Length);
            Assert.AreEqual(0, Troops.Length);
            _now += 2700000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
        }
        [TestMethod]
        public void AllSixPadsDeliverTheAuthoredGroupForEveryWave()
        {
            WaveThree();
            for (var wave = 0; wave < 3; wave++)
                for (var i = 0; i < 6; i++)
                {
                    var pool = Pools[wave * 6 + i];
                    Assert.AreEqual(LandingZoneAssaultContent.Drops[i].Position, pool.Position);
                    Assert.AreEqual(LandingZoneAssaultContent.Drops[i].Rotation, pool.Rotation);
                    var group = Troops.Where(c => c.SpawnPool == pool).ToArray();
                    var infantry = wave == 0 ? LandingZoneAssaultContent.Trainee : LandingZoneAssaultContent.Private;
                    Assert.IsTrue(group.Count(c => c.DbId == infantry) is >= 3 and <= 5);
                    Assert.AreEqual(1, group.Count(c => c.DbId == (wave == 0 ? LandingZoneAssaultContent.Private : LandingZoneAssaultContent.Sergeant)));
                    Assert.IsTrue(group.Count(c => c.DbId == LandingZoneAssaultContent.Hunter) == 1 ^ group.Count(c => c.DbId == LandingZoneAssaultContent.Linker) == 2);
                    Assert.AreEqual(0, pool.DropshipQueue);
                    Assert.AreEqual(0, pool.QueuedCreatures);
                    Assert.IsTrue(group.All(c => c.Attributes[Attributes.Health].CurrentMax > 0 && c.Actions.Any()));
                    var ground = NavMeshManager.NearestWalkable(_world.Map, pool.Position);
                    var target = NavMeshManager.NearestWalkable(_world.Map, Point.Position);
                    Assert.IsNotNull(ground, $"Pad {i + 1}");
                    Assert.IsNotNull(target);
                    var path = NavMeshManager.FindPathBounded(_world.Map, ground.Value, target.Value, out var complete);
                    Assert.IsTrue(complete && path?.Count > 0, $"Pad {i + 1} must reach the outpost");
                }
        }
        [TestMethod]
        public void OwnershipLossCancelsPendingDropshipsAndRecaptureRestartsTheHour()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Pools.Length);
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => s.SpawnPool?.IsControlPointAssault == true));
            Assert.IsFalse(LandingZoneAssaults.TryStart(_world.Map, Point));
            _now += 7200000;
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Pools.Length);
            _now += 3600000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
        }
        private void ClearGarrison()
        {
            LandingZoneGarrison.CancelReinforcements(_world.Map, ReliefPools);
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => Point.AfsPools.Contains(p.DbId) || p.IsControlPointReinforcement).ToList());
            foreach (var pool in _world.Map.SpawnPools.Where(p => Point.AfsPools.Contains(p.DbId) || p.IsControlPointReinforcement))
            { pool.Suspended |= pool.IsControlPointReinforcement; pool.HasSpawned = true; pool.AliveCreatures = 0; pool.QueuedCreatures = 0; pool.DropshipQueue = 0; }
        }
        [TestMethod]
        public void BossMustReachPointAndCaptureOnlyAfterAllDefendersFall()
        {
            WaveThree();
            var boss = Troops.Single(c => c.DbId == LandingZoneAssaultContent.Boss);
            boss.Position = NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsNull(Point.Object.UsedBy, "unspawned defenders are still expected");
            ClearGarrison();
            ControlPoints.Instance.Worker(_world.Map);
            Assert.IsTrue(Point.HeldByAfs, "ordinary garrison loss cannot bypass the boss");
            Assert.AreSame(boss, Point.Object.UsedBy);
            _now += 9999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsTrue(Point.HeldByAfs);
            _now++;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(ControlPoints.Bane, Point.Owner);
            Assert.AreEqual(0, Pools.Length);
        }
        [TestMethod]
        public void DamageInterruptsCaptureAndABossKilledDuringCaptureCannotTakeThePoint()
        {
            WaveThree(); ClearGarrison();
            var boss = Troops.Single(c => c.DbId == LandingZoneAssaultContent.Boss);
            boss.Position = NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreSame(boss, Point.Object.UsedBy);
            boss.Attributes[Attributes.Health].Current--;
            _now += 9999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsNull(Point.Object.UsedBy);
            LandingZoneAssaults.Worker(_world.Map, Point);
            boss.Attributes[Attributes.Health].Current = 0;
            boss.State = CharacterState.Dead;
            _now += 10000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsTrue(Point.HeldByAfs);
            Assert.IsNull(Point.Object.UsedBy);
        }
        [TestMethod]
        public void TroopsUseCombatAiAgainstActualGarrisonAndNeverRespawnFromTheirPools()
        {
            _world.SpawnWorld(Point.AfsPools.ToArray());
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Deliver();
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsTrue(Troops.Any(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting));
            var troop = Troops.First(c => c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting);
            var guard = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && Point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(_world.Map, c.SpawnPool) == null && CreatureManager.IsLivingOnMap(_world.Map, c))
                .OrderBy(c => System.Numerics.Vector3.DistanceSquared(c.Position, troop.Position)).First();
            troop.Position = guard.Position + new System.Numerics.Vector3(3, 0, 0);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(troop, guard.EntityId));
            var before = guard.Attributes[Attributes.Health].Current + guard.Attributes[Attributes.Armor].Current;
            for (var i = 0; i < 80; i++) { _world.Tick(250); MissileManager.Instance.DoWork(_world.Map, 250); }
            Assert.IsTrue(guard.Attributes[Attributes.Health].Current + guard.Attributes[Attributes.Armor].Current < before, "assault actions must inflict real damage");
            var pool = troop.SpawnPool;
            ControlPoints.TakeOffLiving(_world.Map, new System.Collections.Generic.List<SpawnPool> { pool });
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 3600000);
            Assert.IsFalse(Troops.Any(c => c.SpawnPool == pool), "scripted assault pools cannot replenish killed attackers");
        }
        [TestMethod]
        public void BossCannotCaptureFromDropPadAndMovingAwayInterruptsTheClaim()
        {
            WaveThree(); ClearGarrison();
            var boss = Troops.Single(c => c.DbId == LandingZoneAssaultContent.Boss);
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsNull(Point.Object.UsedBy);
            boss.Position = NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreSame(boss, Point.Object.UsedBy);
            boss.Position = LandingZoneAssaultContent.Drops[0].Position;
            _now += 10000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsTrue(Point.HeldByAfs);
            Assert.IsNull(Point.Object.UsedBy);
        }
        [TestMethod]
        public void DisabledAssaultsDoNotInterceptOtherPointsCaptureRules()
        {
            Assert.IsTrue(LandingZoneAssaults.ControlsCapture(_world.Map, Point));
            Assert.IsFalse(LandingZoneAssaults.ControlsCapture(_world.Map, new ControlPoints.Point { Id = 5 }));
            ControlPoints.Instance.Config.LandingZoneAssaultEnabled = false;
            Assert.IsFalse(LandingZoneAssaults.ControlsCapture(_world.Map, Point));
            Assert.IsFalse(LandingZoneAssaults.TryStart(_world.Map, Point));
        }
    }
}
