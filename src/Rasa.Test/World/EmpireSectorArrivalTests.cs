using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Managers;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class EmpireSectorArrivalTests
    {
        private const string Previous="20270915000000_Combat_technician_entity_classes";
        private const string Migration="20270916000000_Empire_sector_entrance";

        [TestMethod]
        public void ForwardRepairMovesOnlyTheArrivalAndItsReturnTriggerAndRollbackRestoresThem()
        {
            using var db=new OrdinaryLootTestDatabase();var migrator=db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);var before=Snapshot(db);
            migrator.Migrate(Migration);var after=Snapshot(db);
            CollectionAssert.AreEqual(new[]{16U,132U},before.Keys.Where(id=>before[id]!=after[id]).OrderBy(id=>id).ToArray());
            var rows=db.Context.MapLinkEntries.AsNoTracking().ToDictionary(l=>l.Id);
            Assert.AreEqual((-282.701,185.0,-678.6459),(rows[16].DestPosX,rows[16].DestPosY,rows[16].DestPosZ));
            Assert.AreEqual((-282.701,185.0,-678.6459),(rows[132].PosX,rows[132].PosY,rows[132].PosZ));
            Assert.AreEqual((-141.2292,232.8332,-619.4121),(rows[16].PosX,rows[16].PosY,rows[16].PosZ));
            Assert.AreEqual((-141.2292,232.8332,-619.4121),(rows[132].DestPosX,rows[132].DestPosY,rows[132].DestPosZ));
            Assert.AreEqual(6.0,rows[132].Radius);Assert.AreEqual(-2.8579,rows[16].DestRotation);
            foreach(var sql in EmpireSectorEntrance.Up())db.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(after.Values.ToArray(),Snapshot(db).Values.ToArray(),"Reapplication cannot move it twice.");
            migrator.Migrate(Previous);CollectionAssert.AreEqual(before.Values.ToArray(),Snapshot(db).Values.ToArray());
        }

        [TestMethod]
        [DataRow("adv_earth_unitedstates_manhattan_01")]
        [DataRow("adv_earth_unitedstates_manhattan_01_shared")]
        public void RepairedArrivalStandsOnTheLowerPlatformAndReachesTheBaseCorridor(string map)
        {
            using var db=new OrdinaryLootTestDatabase();db.Context.GetService<IMigrator>().Migrate(Migration);
            var link=MapLink.FromEntry(db.Context.MapLinkEntries.AsNoTracking().Single(l=>l.Id==16));
            var dir=new DirectoryInfo(AppContext.BaseDirectory);
            while(dir!=null && !File.Exists(Path.Combine(dir.FullName,"Rasa.NET.sln")))dir=dir.Parent;
            var q=TestNavMeshes.Query(Path.Combine(dir.FullName,"navmesh",map+".nav"));
            var landing=link.DestPosition;var floor=q.Nearest(landing);
            Assert.IsTrue(floor.HasValue);Assert.IsTrue(Vector3.Distance(floor.Value,landing)<0.1f);
            // The AFS platform's native origin is Y=177; its collision deck is at local Y=8.
            Assert.AreEqual(185f,landing.Y,0.001f);
            var corridor=new Vector3(-271.6f,186.2f,-551.15f);
            Assert.AreEqual(true,q.Reachable(landing,corridor),"Players must be able to walk from the entry into the lower base.");
            Assert.AreNotEqual(true,q.Reachable(new Vector3(-285.7884f,208.9081f,-706.0602f),corridor),"The old surface arrival is on a different walkable region.");
        }

        [TestMethod]
        public void ArrivalSeedsTheCorrectedExitAndDoesNotImmediatelyBounceBack()
        {
            using var db=new OrdinaryLootTestDatabase();db.Context.GetService<IMigrator>().Migrate(Migration);
            var arrival=MapLink.FromEntry(db.Context.MapLinkEntries.AsNoTracking().Single(l=>l.Id==16));
            var exit=MapLink.FromEntry(db.Context.MapLinkEntries.AsNoTracking().Single(l=>l.Id==132));
            Assert.IsTrue(MapLinkManager.Contains(exit,arrival.DestPosition));
            using var world=new WorldTestContext();
            world.Map.MapInfo=new MapInfo(2327,"adv_earth_unitedstates_manhattan_01",122,0);
            var client=PlayerDeathTests.Player(world,arrival.DestPosition.X,arrival.DestPosition.Z);
            client.SetWorldPosition(arrival.DestPosition,arrival.DestRotation);
            CellManager.Instance.AddToWorld(world.Map,exit);
            MapLinkManager.Instance.PlayerEnteredMap(client);
            Assert.IsTrue(client.Player.InsideMapLinks.Contains(132));
            MapLinkManager.Instance.Worker(world.Map);
            Assert.IsNull(client.PendingTransfer);
            client.SetWorldPosition(arrival.DestPosition+new Vector3(15,0,0),0);
            CellManager.Instance.UpdateVisibility(client);
            MapLinkManager.Instance.Worker(world.Map);
            Assert.IsFalse(client.Player.InsideMapLinks.Contains(132),"Stepping away rearms the normal return trigger.");
        }

        [TestMethod]
        [DataRow("arrival")]
        [DataRow("exit")]
        [DataRow("already-fixed")]
        [DataRow("rerouted")]
        public void CustomLinksAndPreexistingManualCorrectionsSurviveBothDirections(string change)
        {
            using var db=new OrdinaryLootTestDatabase();var migrator=db.Context.GetService<IMigrator>();migrator.Migrate(Previous);
            var sql=change switch {
                "arrival"=>"UPDATE map_link SET dest_pos_y=190 WHERE id=16;",
                "exit"=>"UPDATE map_link SET pos_x=pos_x+2 WHERE id=132;",
                "already-fixed"=>"UPDATE map_link SET dest_pos_x=-282.701,dest_pos_y=185,dest_pos_z=-678.6459 WHERE id=16;",
                _=>"UPDATE map_link SET dest_map_context_id=2375 WHERE id=16;" };
            Assert.AreEqual(1,db.Context.Database.ExecuteSqlRaw(sql));var before=Snapshot(db);
            migrator.Migrate(Migration);var id=change=="exit"?132U:16U;Assert.AreEqual(before[id],Snapshot(db)[id]);
            migrator.Migrate(Previous);CollectionAssert.AreEqual(before.Values.ToArray(),Snapshot(db).Values.ToArray());
        }

        [TestMethod]
        public void MySqlGeneratesTheSameGuardedLinkRepairAndRollback()
        {
            using var db=PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext),"unused");
            var migrator=db.GetService<IMigrator>();var up=migrator.GenerateScript(Previous,Migration);var down=migrator.GenerateScript(Migration,Previous);
            foreach(var sql in EmpireSectorEntrance.Up())StringAssert.Contains(up,sql);
            foreach(var sql in EmpireSectorEntrance.Down())StringAssert.Contains(down,sql);
        }
        private static System.Collections.Generic.Dictionary<uint,string> Snapshot(OrdinaryLootTestDatabase db)=>db.Context.MapLinkEntries.AsNoTracking().OrderBy(l=>l.Id).ToDictionary(l=>l.Id,l=>
            FormattableString.Invariant($"{l.MapContextId} {l.PosX:R} {l.PosY:R} {l.PosZ:R} {l.Radius:R} {l.DestMapContextId} {l.DestPosX:R} {l.DestPosY:R} {l.DestPosZ:R} {l.DestRotation:R} {l.Kind} {l.Enabled} {l.Comment}"));
    }
}