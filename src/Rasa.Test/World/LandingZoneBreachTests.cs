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
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class LandingZoneBreachTests
    {
        private WildernessRuntimeTestHarness _world;
        private ControlPointConfig _oldConfig;
        private ForceFields.Field _field;
        private ControlPoints.Point Point => ControlPoints.Instance.ById(4);
        private Creature[] Guards => EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool?.IsGarrison == true && CreatureManager.IsLivingOnMap(_world.Map, c)).ToArray();
        [TestInitialize]
        public void Start()
        {
            _oldConfig = ControlPoints.Instance.Config;
            ControlPoints.Instance.Config = new ControlPointConfig();
            _world = WildernessRuntimeTestHarness.Create();
            _world.Client.Player.GmFlagAlwaysFriendly = true;
            var ids = new uint[] { 106, 107, 551121, 551123, 551125, 680001, 680002 };
            _world.Map.SpawnPools.RemoveAll(p => !ids.Contains(p.DbId));
            using var db = _world.CreateWorld();
            ControlPoints.Instance.Load(db.ControlPoints.GetControlPoints().Where(p => p.Id == 4), db.ControlPoints.GetLinks().Where(p => p.ControlPointId == 4), null);
            ControlPoints.Instance.Place(_world.Map);
            _field = ForceFields.Place(_world.Map, ForceFields.ClassOf("humbase"), ForceFields.Side.A, new Vector3(198, 159, -37), 0, 200);
        }
        [TestCleanup]
        public void End()
        {
            ForceFields.Remove(_field);
            ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 0);
            ControlPoints.Instance.Config.LandingZoneAssaultEnabled = false;
            LandingZoneAssaults.Worker(_world.Map, Point);
            ControlPoints.Instance.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
            _world.Dispose();
            ControlPoints.Instance.Config = _oldConfig;
        }
        [DataTestMethod]
        [DataRow(145.74f,172.4f,-79.1f)]
        [DataRow(254.34f,172.4f,-75.23f)]
        [DataRow(144.7f,172.6f,-127.19f)]
        [DataRow(254.74f,172.4f,-110.69f)]
        public void RearMountedTurretHasReachableClearFiringPositionAndTakesNativeDamage(float x,float y,float z)
        {
            // The location that stalled the original simulations, with real collision cover.
            var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Rasa.NET.sln"))) dir = dir.Parent;
            _world.Map.Cover = Rasa.Navigation.CoverMesh.Read(System.IO.Path.Combine(dir.FullName, "navmesh", "adv_foreas_concordia_wilderness.cover"));
            using var db = _world.CreateWorld();
            var pool = new SpawnPool { DbId = 551131, RuntimeMapChannel = _world.Map, MapContextId = 1220 };
            var turret = CreatureManager.Instance.CreateCreature(550018, pool);
            CreatureManager.Instance.SetLocation(turret, new Vector3(x,y,z),0,1220);
            turret.IsScripted = true;
            CellManager.Instance.AddToWorld(_world.Map,turret);
            var attacker = Attacker(new Vector3(230.44f,163.70f,-100.77f));
            attacker.HomePos.Position = Point.Position;
            if(x>254 && z< -100) Assert.IsFalse(BehaviorManager.Sees(_world.Map.Cover,attacker,turret));
            var at = LandingZoneAssaultTargeting.FiringPosition(_world.Map,attacker,turret);
            Assert.IsNotNull(at,"Must find a real reachable firing position without relocating the mount");
            Assert.IsTrue(_world.Map.NavMesh.Reachable(attacker.Position,at.Value));
            var before=turret.Attributes[Attributes.Health].Current + turret.Attributes[Attributes.Armor].Current;
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(attacker,turret.EntityId));
            for(var i=0;i<1200 && turret.Attributes[Attributes.Health].Current + turret.Attributes[Attributes.Armor].Current>=before;i++)
            { _world.Tick(250); MissileManager.Instance.DoWork(_world.Map,250); }
            Assert.IsTrue(turret.Attributes[Attributes.Health].Current + turret.Attributes[Attributes.Armor].Current<before,"Native movement must reach a shot and inflict damage");
        }
        [TestMethod]
        public void AssaultTroopsIgnoreServicesAndUseTheBattleBoundaryRatherThanTheDropPad()
        {
            var attacker=Attacker(new Vector3(254,164,-110));
            attacker.HomePos.Position=new Vector3(296,171,-5);
            Assert.IsFalse(BehaviorManager.ChasedTooFar(attacker));
            var servicePool=new SpawnPool {DbId=172,MapContextId=1220,RuntimeMapChannel=_world.Map,
                SpawnSlot=new System.Collections.Generic.List<SpawnPoolSlot>{new(97,1,1)}};
            var service=CreatureManager.Instance.CreateCreature(97,servicePool);
            CreatureManager.Instance.SetLocation(service,Point.Position,0,1220);
            CellManager.Instance.AddToWorld(_world.Map,service);
            Assert.IsTrue(LandingZoneAssaultTargeting.IsServiceTarget(attacker,service));
            Assert.IsFalse(BehaviorManager.Instance.TrySetActionFighting(attacker,service.EntityId));
            Assert.IsFalse(Threat.CanFight(attacker,service.EntityId));
            attacker.Position=Point.Position+new Vector3(201,0,0);
            Assert.IsTrue(BehaviorManager.ChasedTooFar(attacker));
        }
        private SpawnPool Pool(uint id) => _world.Map.SpawnPools.Single(p => p.DbId == id);
        private void Deploy()
        {
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 0);
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 700);
            for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000);
            Assert.AreEqual(8, Guards.Length);
        }
        private void Kill(Creature c) { c.Attributes[Attributes.Health].Current = 0; CreatureManager.Instance.HandleCreatureKill(_world.Map, c, null); }
        private void KillPool(uint id) { foreach (var c in Guards.Where(c => c.SpawnPool.DbId == id)) Kill(c); }
        private void ClearFront() { foreach (var id in LandingZoneBreachContent.FrontlinePools) KillPool(id); }
        private Creature Attacker(Vector3? at = null)
        {
            var pool = new SpawnPool { RuntimeMapChannel = _world.Map, MapContextId = 1220, IsControlPointAssault = true, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled };
            var c = CreatureManager.Instance.CreateCreature(LandingZoneAssaultContent.Private, pool);
            CreatureManager.Instance.SetLocation(c, at ?? new Vector3(198, 164, -10), 0, 1220);
            c.ImmuneToAllDamage = true;
            c.HomePos.Position = c.Position;
            CellManager.Instance.AddToWorld(_world.Map, c);
            _world.MoveTo(new Vector3(198, 164, -10));
            return c;
        }
        [TestMethod]
        public void BothMechsAndBothFrontStandardTurretsMustBeDownBeforeFieldDamage()
        {
            Deploy(); var c = Attacker();
            Assert.IsFalse(ForceFields.MayShoot(_field, c));
            var mechs = Guards.Where(g => g.SpawnPool.DbId == 680001).ToArray();
            Kill(mechs[0]);
            Assert.IsFalse(ForceFields.MayShoot(_field, c));
            Kill(mechs[1]); KillPool(551121);
            Assert.IsFalse(ForceFields.MayShoot(_field, c));
            KillPool(551123);
            Assert.IsTrue(ForceFields.MayShoot(_field, c));
            Assert.AreEqual(4, Guards.Length, "rear turret and catwalk infantry need not be dead before breaching");
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(_world.Map, Point, ControlPoints.Afs));
        }
        [TestMethod]
        public void FirstDeploymentAndQueuedReinforcementsBlockTheBreach()
        {
            var c = Attacker();
            Assert.IsFalse(LandingZoneBreach.FrontlineDown(_world.Map));
            Assert.IsFalse(BehaviorManager.Instance.TrySetActionFighting(c, _field.Object.EntityId));
            Deploy(); ClearFront();
            var pool = Pool(680001);
            pool.QueuedCreatures = 2; pool.DropshipQueue = 1;
            Assert.IsFalse(ForceFields.MayShoot(_field, c));
            pool.QueuedCreatures = 0; pool.DropshipQueue = 0;
            Assert.IsTrue(ForceFields.MayShoot(_field, c));
        }
        [TestMethod]
        public void TheDirectorPrioritizesFrontDefendersThenTheFieldThenOtherGuards()
        {
            Deploy(); var c = Attacker();
            Assert.IsTrue(LandingZoneBreach.Order(_world.Map, Point, c, Guards));
            var target = EntityManager.Instance.GetCreature(c.Controller.ActionFighting.TargetEntityId);
            Assert.IsTrue(LandingZoneBreach.IsFrontline(target.SpawnPool));
            ClearFront();
            Assert.IsTrue(LandingZoneBreach.Order(_world.Map, Point, c, Guards));
            Assert.AreEqual(_field.Object.EntityId, c.Controller.ActionFighting.TargetEntityId);
            Assert.IsTrue(Threat.CanFight(c, _field.Object.EntityId));
            ForceFields.Damage(_field, _field.Health, 0);
            Assert.IsFalse(LandingZoneBreach.Order(_world.Map, Point, c, Guards));
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, c.Controller.CurrentAction);
        }
        [TestMethod]
        public void NativeRifleCombatDestroysTheFieldAndMovementCanThenCrossIt()
        {
            Deploy(); ClearFront(); var c = Attacker();
            foreach (var guard in Guards) guard.IsScripted = true;
            Assert.IsTrue(ForceFields.Stops(_world.Map, c, new Vector3(198, 164, -30), new Vector3(198, 164, -45)));
            Assert.IsTrue(LandingZoneBreach.Order(_world.Map, Point, c, Guards));
            for (var i = 0; i < 160 && !_field.IsDestroyed; i++) { _world.Tick(250); MissileManager.Instance.DoWork(_world.Map, 250); }
            Assert.IsTrue(_field.IsDestroyed, $"field health {_field.Health}, action {c.Controller.CurrentAction}, target {c.Controller.ActionFighting.TargetEntityId}");
            Assert.IsFalse(ForceFields.Stops(_world.Map, c, new Vector3(198, 164, -30), new Vector3(198, 164, -45)));
            foreach (var guard in Guards) Kill(guard);
            LandingZoneBreach.Order(_world.Map, Point, c, Guards);
            BehaviorManager.Instance.WalkIn(c, NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value);
            for (var i = 0; i < 240; i++) _world.Tick(250);
            Assert.IsTrue(c.Position.Z < -40, $"attacker must actually enter the base: {c.Position}");
        }
        [TestMethod]
        public void ReturningMechsStopFieldAttacksAndEvenShotsAlreadyInFlightDoNoDamage()
        {
            Deploy(); ClearFront(); var c = Attacker();
            MissileManager.Instance.MissileLaunch(_world.Map, new ActionData(c, ActionId.WeaponAttack, 79, _field.Object.EntityId, 0), 50, landsInMs: 1000);
            Assert.IsTrue(_world.Map.QueuedMissiles.Count > 0);
            Pool(680001).QueuedCreatures = 2;
            MissileManager.Instance.DoWork(_world.Map, 1000);
            Assert.AreEqual(200, _field.Health);
            Assert.IsFalse(Threat.CanFight(c, _field.Object.EntityId));
            Pool(680001).QueuedCreatures = 0;
            ForceFields.TakeHit(new Missile { Source = c, TargetEntityId = _field.Object.EntityId, DamageA = 50 });
            Assert.AreEqual(150, _field.Health);
        }
        [TestMethod]
        public void StandardTurretsTakeRealDamageAndRespawnIndividuallyAfterFiveMinutes()
        {
            Deploy(); var turret = Guards.Single(c => c.SpawnPool.DbId == 551121);
            Assert.AreEqual(2892, turret.Attributes[Attributes.Health].CurrentMax);
            Assert.AreEqual(130, turret.Attributes[Attributes.Armor].CurrentMax);
            var c = Attacker(new Vector3(159, 164, -15));
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(c, turret.EntityId));
            var before = turret.Attributes[Attributes.Health].Current + turret.Attributes[Attributes.Armor].Current;
            for (var i = 0; i < 40; i++) { _world.Tick(250); MissileManager.Instance.DoWork(_world.Map, 250); }
            Assert.IsTrue(turret.Attributes[Attributes.Health].Current + turret.Attributes[Attributes.Armor].Current < before);
            Kill(turret);
            Assert.IsTrue(AlternateMesh.KeepsWreck(turret));
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 299999);
            Assert.AreEqual(0, Pool(551121).AliveCreatures);
            SpawnPoolManager.Instance.SpawnPoolWorker(_world.Map, 1);
            Assert.AreEqual(1, Pool(551121).AliveCreatures);
            Assert.IsTrue(Guards.Any(g => g.SpawnPool.DbId == 551123), "the other front turret need not die");
        }
        [TestMethod]
        public void BothLegacyLightTurretsAreDisabledAndOnlyTheStandardTurretsOccupyTheFrontMounts()
        {
            Deploy();
            foreach (var id in new uint[] { 106, 107 })
            {
                Assert.AreEqual(SpawnPoolManager.ModeScripted, Pool(id).Mode);
                Assert.IsFalse(Point.AfsPools.Contains(id));
                Assert.IsFalse(Guards.Any(c => c.SpawnPool.DbId == id));
            }
            foreach (var id in new uint[] { 551121, 551123 })
                Assert.AreEqual((EntityClasses)4064, Guards.Single(c => c.SpawnPool.DbId == id).EntityClass);
        }
        [TestMethod]
        public void UnrelatedFriendlySealedOrOtherMapFieldsCannotBeBreachTargets()
        {
            Deploy(); ClearFront(); var c = Attacker();
            _field.Sealed = true; Assert.IsFalse(ForceFields.MayShoot(_field, c)); _field.Sealed = false;
            ForceFields.SetSide(_field, ForceFields.Side.B); Assert.IsFalse(ForceFields.MayShoot(_field, c)); ForceFields.SetSide(_field, ForceFields.Side.A);
            _world.Map.IsPrivateInstance = true;
            Assert.IsFalse(ForceFields.MayShoot(_field, c));
            _world.Map.IsPrivateInstance = false;
            Assert.IsTrue(ForceFields.MayShoot(_field, c));
        }
        [TestMethod]
        public void MigrationRetiresOnlyTheLegacyPoolsAndRollbackRestoresThem()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate("20270905000000_Landing_zone_garrison");
            long Count(string sql) { using var cmd = db.Context.Database.GetDbConnection().CreateCommand(); cmd.CommandText = sql; return Convert.ToInt64(cmd.ExecuteScalar()); }
            db.Context.Database.OpenConnection();
            Assert.AreEqual(2L, Count("SELECT COUNT(*) FROM spawnpool WHERE id IN(106,107) AND mode=0"));
            migrator.Migrate("20270906000000_Landing_zone_breach");
            Assert.AreEqual(2L, Count("SELECT COUNT(*) FROM spawnpool WHERE id IN(106,107) AND mode=2"));
            Assert.AreEqual(0L, Count("SELECT COUNT(*) FROM control_point_link WHERE control_point_id=4 AND kind=2 AND object_id IN(106,107)"));
            Assert.AreEqual(2L, Count("SELECT COUNT(*) FROM spawnpool WHERE id IN(551121,551123) AND respown_time=3000 AND creature_1_Id=550018"));
            migrator.Migrate("20270905000000_Landing_zone_garrison");
            Assert.AreEqual(2L, Count("SELECT COUNT(*) FROM spawnpool WHERE id IN(106,107) AND mode=0 AND respown_time=20"));
            Assert.AreEqual(2L, Count("SELECT COUNT(*) FROM control_point_link WHERE control_point_id=4 AND kind=2 AND object_id IN(106,107)"));
        }
        [TestMethod]
        public void BossCannotClaimBehindAnIntactFieldButCanClaimAfterItFalls()
        {
            Deploy(); foreach (var guard in Guards) Kill(guard);
            var old = LandingZoneAssaults.Now;
            var now = 1000000L;
            LandingZoneAssaults.Now = () => now;
            try
            {
                Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
                for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000);
                now += 360000;
                LandingZoneAssaults.Worker(_world.Map, Point);
                for (var i = 0; i < 6; i++) DynamicObjectManager.Instance.DropshipsWorker(_world.Map, 60000);
                var boss = EntityManager.Instance.Creatures.Values.Single(c => c.RuntimeMapChannel == _world.Map && c.DbId == LandingZoneAssaultContent.Boss);
                boss.Position = NavMeshManager.NearestWalkable(_world.Map, Point.Position).Value;
                LandingZoneAssaults.Worker(_world.Map, Point);
                Assert.IsTrue(Point.HeldByAfs);
                Assert.IsNull(Point.Object.UsedBy);
                ForceFields.Damage(_field, _field.Health, 0);
                BehaviorManager.Instance.GiveUp(boss);
                LandingZoneAssaults.Worker(_world.Map, Point);
                Assert.AreSame(boss, Point.Object.UsedBy);
                now += 10000;
                LandingZoneAssaults.Worker(_world.Map, Point);
                Assert.AreEqual(ControlPoints.Bane, Point.Owner);
            }
            finally { LandingZoneAssaults.Now = old; }
        }
        [TestMethod]
        public void MySqlMigrationEmitsTheSameRetirementAndRollbackStatements()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migration = db.GetService<IMigrator>();
            var up = migration.GenerateScript("20270905000000_Landing_zone_garrison", "20270906000000_Landing_zone_breach");
            var down = migration.GenerateScript("20270906000000_Landing_zone_breach", "20270905000000_Landing_zone_garrison");
            foreach (var sql in LandingZoneBreachContent.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in LandingZoneBreachContent.Down()) StringAssert.Contains(down, sql);
        }
    }
}
