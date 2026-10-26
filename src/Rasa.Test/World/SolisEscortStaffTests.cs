using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Test.Database;
using Rasa.Test.Missions;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class SolisEscortStaffTests
    {
        private const string Previous = "20270917000000_Landing_zone_bane_occupation";
        private const string Migration = "20270918000000_Solis_escort_staff";
        [TestMethod]
        public void ForwardMigrationEquipsOnlyThePersonalRangerAndRollbackRemovesItsAddedStaff()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrate = db.Context.GetService<IMigrator>();
            migrate.Migrate(Previous);
            Assert.AreEqual(0, Slot(db));
            migrate.Migrate(Migration);
            Assert.AreEqual(10532, Slot(db));
            Assert.AreEqual(10532, db.Context.Database.SqlQueryRaw<int>("SELECT class_id AS Value FROM creature_appearance WHERE id=37 AND slot_id=13").Single());
            migrate.Migrate(Previous);
            Assert.AreEqual(0, Slot(db));
        }
        [TestMethod]
        [DataRow(10532)]
        [DataRow(7077)]
        public void ExistingWeaponAppearanceSurvivesUpgradeAndRollback(int weapon)
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrate = db.Context.GetService<IMigrator>();
            migrate.Migrate(Previous);
            db.Context.Database.ExecuteSqlRaw($"INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (630001,13,{weapon},1);");
            migrate.Migrate(Migration); Assert.AreEqual(weapon, Slot(db));
            migrate.Migrate(Previous); Assert.AreEqual(weapon, Slot(db));
        }
        [TestMethod]
        public void ActualPersonalEscortHasItsNativeForeanWeaponEquipped()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            h.SpawnWorld(100,64,184,219);
            h.MoveTo(h.Npc(100).Position);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, h.Npc(100).EntityId, 1407));
            var ranger = EntityManager.Instance.Creatures.Values.Single(c => c.SpawnPool?.SceneActorRole == "ranger" &&
                c.SpawnPool.ScenarioMissionId == 1407 && c.SpawnPool.ScenarioOwnerCharacterId == h.Client.Player.Id && CreatureManager.IsLivingOnMap(h.Map,c));
            Assert.AreEqual(10532U, ranger.AppearanceData[(EquipmentData)13].Class);
            Assert.IsTrue(ranger.Actions.Count > 0);
        }
        [TestMethod]
        public void MySqlProducesTheSameGuardedRepair()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext),"unused");
            var migrate = db.GetService<IMigrator>();
            foreach (var sql in SolisEscortStaff.Up()) StringAssert.Contains(migrate.GenerateScript(Previous,Migration),sql);
            foreach (var sql in SolisEscortStaff.Down()) StringAssert.Contains(migrate.GenerateScript(Migration,Previous),sql);
        }
        private static int Slot(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<int>("SELECT class_id AS Value FROM creature_appearance WHERE id=630001 AND slot_id=13").AsEnumerable().SingleOrDefault();
    }
}
