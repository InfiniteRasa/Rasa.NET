using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Database
{
    [TestClass, DoNotParallelize]
    public class CombatTechnicianClassTests
    {
        private const string Previous="20270914000000_Afs_turret_mount_heights";
        private const string Migration="20270915000000_Combat_technician_entity_classes";

        [TestMethod]
        public void ForwardMigrationChangesOnlyTheFourCombatClassesAndRollbackRestoresThem()
        {
            using var db=new OrdinaryLootTestDatabase();
            var migrator=db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before=db.Context.CreatureEntries.AsNoTracking().ToDictionary(c=>c.Id);
            var appearance=Appearance(db);
            migrator.Migrate(Migration);
            var after=db.Context.CreatureEntries.AsNoTracking().ToDictionary(c=>c.Id);
            CollectionAssert.AreEquivalent(new uint[] {47,530003,566015,660021},after.Values.Where(c=>c.ClassId!=before[c.Id].ClassId).Select(c=>c.Id).ToArray());
            foreach(var c in after.Values)
            {
                var old=before[c.Id];
                Assert.AreEqual((old.Faction,old.Level,old.MaxHitPoints,old.NameId,old.RunSpeed,old.WalkSpeed,old.Action1,old.Action2,old.Action3,old.Action4),
                    (c.Faction,c.Level,c.MaxHitPoints,c.NameId,c.RunSpeed,c.WalkSpeed,c.Action1,c.Action2,c.Action3,c.Action4),$"creature {c.Id}");
                if(CombatTechnicianClasses.Creatures.Contains(c.Id)) Assert.AreEqual(7043U,c.ClassId);
            }
            CollectionAssert.AreEqual(appearance,Appearance(db));
            foreach(var sql in CombatTechnicianClasses.Up()) db.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(4,db.Context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM combat_technician_class_repair").Single());
            migrator.Migrate(Previous);
            foreach(var c in db.Context.CreatureEntries.AsNoTracking()) Assert.AreEqual(before[c.Id].ClassId,c.ClassId,$"rollback {c.Id}");
        }

        [TestMethod]
        [DataRow("package")]
        [DataRow("greeting")]
        [DataRow("vendor")]
        [DataRow("mission")]
        [DataRow("legacy-mission")]
        [DataRow("friendly")]
        [DataRow("manual-fix")]
        [DataRow("custom-class")]
        public void DialogueBindingsAndCustomClassesArePreserved(string change)
        {
            using var db=new OrdinaryLootTestDatabase();
            var migrator=db.Context.GetService<IMigrator>();migrator.Migrate(Previous);
            var sql=change switch
            {
                "package"=>"INSERT INTO npc_package (id,package_id,comment) VALUES (47,1,'Custom dialogue');",
                "greeting"=>"INSERT INTO npc_greeting (id,greeting_id,important) VALUES (47,1,0);",
                "vendor"=>"INSERT INTO vendor (id,package_id) VALUES (47,1);",
                "mission"=>"UPDATE mission_content_definition SET giver_id=47 WHERE mission_id=508;",
                "legacy-mission"=>"INSERT INTO npc_mission (id,giver_id,reciver_id,level,group_type,category_id,shareable,radio_completeable,comment) VALUES (999998,47,0,1,0,0,0,0,'Custom NPC role');",
                "friendly"=>"UPDATE creature SET faction=1 WHERE id=47;",
                "manual-fix"=>"UPDATE creature SET class_id=7043 WHERE id=47;",
                _=>"UPDATE creature SET class_id=10502 WHERE id=47;"
            };
            Assert.IsTrue(db.Context.Database.ExecuteSqlRaw(sql)>0,"The custom-role fixture must create a binding.");
            var expected=Class(db,47);
            migrator.Migrate(Migration);Assert.AreEqual(expected,Class(db,47));
            Assert.AreEqual(7043U,Class(db,530003),"Other eligible combat definitions still change.");
            migrator.Migrate(Previous);Assert.AreEqual(expected,Class(db,47));
        }

        [TestMethod]
        public void NativeCorpseEntitiesUseTheNonDialogueModelVariant()
        {
            using var h=WildernessRuntimeTestHarness.Create();
            var combat=EntityClassManager.Instance.GetClassInfo((EntityClasses)7043);
            var dialogue=EntityClassManager.Instance.GetClassInfo((EntityClasses)11320);
            Assert.AreEqual(dialogue.MeshId,combat.MeshId);
            CollectionAssert.AreEquivalent(dialogue.Augmentations.Where(a=>a!=AugmentationType.NPC).ToArray(),combat.Augmentations.ToArray());
            Assert.IsFalse(combat.Augmentations.Contains(AugmentationType.NPC));
            Assert.IsTrue(dialogue.Augmentations.Contains(AugmentationType.NPC),"The legitimate dialogue class stays intact.");
            foreach(var id in CombatTechnicianClasses.Creatures)
            {
                var pool=new SpawnPool { MapContextId=1220, RuntimeMapChannel=h.Map, Position=h.Client.Player.Position };
                var creature=h.Creatures.CreateCreature(id,pool);
                Assert.IsNotNull(creature);Assert.IsNull(creature.Npc);
                Assert.AreEqual((EntityClasses)7043,creature.EntityClass);
                Assert.IsNotEmpty(creature.Actions);
                CreatureManager.Instance.SetLocation(creature,pool.Position,0,1220);
                creature.State=CharacterState.Dead;
                creature.Attributes[Attributes.Health].Current=0;
                CellManager.Instance.AddToWorld(h.Map,creature);
                h.Drain();h.Creatures.CreateCreatureOnClient(h.Client,creature);
                var sent=h.Drain();
                Assert.AreEqual((EntityClasses)7043,sent.OfType<CreatePhysicalEntityPacket>().Single(p=>p.EntityId==creature.EntityId).ClassId);
                Assert.IsFalse(sent.OfType<NPCInfoPacket>().Any());
                Assert.IsFalse(sent.OfType<NPCConversationStatusPacket>().Any());
            }
        }

        [TestMethod]
        public void MySqlGeneratesTheGuardedForwardRepairAndRollback()
        {
            using var db=PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext),"unused");
            var migrator=db.GetService<IMigrator>();
            var up=migrator.GenerateScript(Previous,Migration);var down=migrator.GenerateScript(Migration,Previous);
            foreach(var sql in CombatTechnicianClasses.Up()) StringAssert.Contains(up,sql);
            foreach(var sql in CombatTechnicianClasses.Down()) StringAssert.Contains(down,sql);
        }
        private static uint Class(OrdinaryLootTestDatabase db,uint id)=>db.Context.CreatureEntries.AsNoTracking().Single(c=>c.Id==id).ClassId;
        private static string[] Appearance(OrdinaryLootTestDatabase db)=>db.Context.Database.SqlQueryRaw<string>(
            "SELECT id || ' ' || slot_id || ' ' || Class_id || ' ' || color AS Value FROM creature_appearance ORDER BY id,slot_id").ToArray();
    }
}