using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.World
{
    public partial class LandingZoneAssaultTests
    {
        private SpawnPool[] ReliefPools => _world.Map.SpawnPools.Where(p => p.IsControlPointReinforcement).ToArray();
        private Creature[] Relief => EntityManager.Instance.Creatures.Values.Where(c => ReliefPools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        private void DeliverRelief()
        {
            LandingZoneGarrison.Worker(_world.Map, 0); Deliver();
            LandingZoneGarrison.Worker(_world.Map, 20000); Deliver();
        }
        [TestMethod]
        public void AssaultRequestsExactlyTwoMixedFivePersonSquadsOnlyOnce()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Assert.AreEqual(2, ReliefPools.Length);
            foreach (var pool in ReliefPools)
            {
                Assert.AreEqual(MissionSpawnGroupPolicy.ScenarioControlled, pool.SpawnPolicy);
                Assert.AreEqual(SpawnPoolManager.ModeScripted, pool.Mode);
                Assert.AreEqual(5, pool.QueuedCreatures);
                Assert.AreEqual(3, pool.SpawnSlot.Single(slot => slot.CreatureId == LandingZoneGarrisonContent.Rifleman).CountMin);
                Assert.AreEqual(1, pool.SpawnSlot.Single(slot => slot.CreatureId == LandingZoneReinforcementContent.Pistol).CountMin);
                Assert.IsTrue(pool.SpawnSlot.Any(slot => slot.CreatureId is LandingZoneReinforcementContent.Shotgun or LandingZoneReinforcementContent.MachineGun));
            }
            DeliverRelief();
            Assert.AreEqual(10, Relief.Length);
            Assert.AreEqual(6, Relief.Count(c => c.DbId == LandingZoneGarrisonContent.Rifleman));
            Assert.AreEqual(2, Relief.Count(c => c.DbId == LandingZoneReinforcementContent.Pistol));
            Assert.IsTrue(Relief.All(c => c.Level == 11 && c.TargetCategory == TargetCategory.Friendly && c.Actions.Count > 0));
            var ids = Relief.Select(c => c.EntityId).ToArray();
            _now += 360000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(2, ReliefPools.Length);
            CollectionAssert.AreEquivalent(ids, Relief.Select(c => c.EntityId).ToArray());
        }
        [TestMethod]
        public void ReliefGunshipsShareThePadAndTheInfantryRunToReachableOutsidePosts()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            LandingZoneGarrison.Worker(_world.Map, 0);
            var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool?.IsControlPointReinforcement == true);
            Assert.AreEqual(TargetCategory.Friendly, ship.TargetCategory);
            Assert.AreEqual(LandingZoneGarrisonContent.Gunship, ship.Position);
            Deliver();
            Assert.AreEqual(5, Relief.Length);
            LandingZoneGarrison.Worker(_world.Map, 19999);
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => s.SpawnPool?.IsControlPointReinforcement == true));
            LandingZoneGarrison.Worker(_world.Map, 1); Deliver();
            Assert.AreEqual(10, Relief.Length);
            ControlPoints.TakeOffLiving(_world.Map, Pools.ToList());
            foreach (var soldier in Relief)
            {
                Assert.AreEqual(soldier.RunSpeed, soldier.WalkSpeed);
                Assert.IsTrue(soldier.HomePos.Position.Z > -30, "Defensive posts must be outside the field.");
                var path = NavMeshManager.FindPathBounded(_world.Map, soldier.Position, soldier.HomePos.Position, out var complete);
                Assert.IsTrue(complete && path?.Count > 0, $"No route from gunship to {soldier.HomePos.Position}");
            }
            for (var i = 0; i < 900; i++) _world.Tick(100);
            Assert.IsTrue(Relief.All(c => c.Position.Z > -30), string.Join("; ", Relief.Select(c => c.Position)));
        }
        [TestMethod]
        public void AReliefSquadThatDiesNeverRespawnsDuringTheAssault()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            DeliverRelief();
            var pools = ReliefPools;
            foreach (var soldier in Relief)
            {
                soldier.Attributes[Attributes.Health].Current = 0;
                CreatureManager.Instance.HandleCreatureKill(_world.Map, soldier, null);
            }
            _world.Map.SpawnPools.RemoveAll(p => !p.IsControlPointReinforcement && !p.IsControlPointAssault);
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 3600000);
            LandingZoneGarrison.Worker(_world.Map, 3600000); Deliver();
            Assert.AreEqual(0, Relief.Length);
            Assert.IsTrue(pools.All(p => p.QueuedCreatures == 0 && p.DropshipQueue == 0));
            _now += 360000; LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(2, ReliefPools.Length);
            Assert.AreEqual(0, Relief.Length);
        }
        [TestMethod]
        public void LosingThePointCancelsWaitingAndInboundReliefWithoutLateDelivery()
        {
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            LandingZoneGarrison.Worker(_world.Map, 0);
            var pools = ReliefPools;
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            LandingZoneAssaults.Worker(_world.Map, Point);
            LandingZoneGarrison.Worker(_world.Map, 120000); Deliver();
            Assert.AreEqual(0, ReliefPools.Length);
            Assert.AreEqual(0, Relief.Length);
            Assert.IsTrue(pools.All(p => p.QueuedCreatures == 0 && p.DropshipQueue == 0));
            Assert.IsFalse(DynamicObjectManager.Instance.Dropships.Values.Any(s => pools.Contains(s.SpawnPool)));
        }
        [TestMethod]
        public void RemainingReliefDefendersPreventBossCapture()
        {
            WaveThree(); DeliverRelief();
            Assert.AreEqual(10, Relief.Length);
            ControlPoints.TakeOffLiving(_world.Map, _world.Map.SpawnPools.Where(p => Point.AfsPools.Contains(p.DbId)).ToList());
            foreach (var pool in _world.Map.SpawnPools.Where(p => Point.AfsPools.Contains(p.DbId)))
            { pool.HasSpawned = true; pool.AliveCreatures = 0; pool.QueuedCreatures = 0; pool.DropshipQueue = 0; }
            var boss = Troops.Single(c => c.DbId == LandingZoneAssaultContent.Boss);
            boss.Position = NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsNull(Point.Object.UsedBy);
            Assert.IsTrue(Point.HeldByAfs);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, boss.Controller.CurrentAction);
            boss.Controller.ActionFighting.Opened = true;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.IsTrue(boss.Controller.ActionFighting.Opened, "Living relief must keep the boss fighting rather than reset its combat for a claim.");
        }
    }
}
