using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using V = AshenFixVV1;
    using Queen = AshenSaveTheQueenV1;

    /// <summary>
    /// Ashen fix job V (AshenFixVV1, migration 20271110002180, to-fix row 64): 1501's Life Support System (26423) on the client's
    /// usabledata chain: it spawns in 10000001 (idle), goes to 10000002 when 3 is done and to 10000003 (the end, no use point in
    /// the client) when 4 is done. The playthrough (AshenConscriptionQueenTests.SaveTheQueenEndsHerSufferingAndTakesTheEggToJawan)
    /// checks the states in play. [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixVTests
    {
        private const string FixV = "20271110002180_Ashen_fix_v";

        private static string Scene(DbContext db) => db.Set<MissionSceneBindingEntry>().AsNoTracking()
            .Single(entry => entry.MissionId == V.Queen && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;

        private static int Evidence(DbContext db) => db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == V.Source);

        /// <summary>
        /// The device's states in fix V's scene: spawn 10000001, 3 done 10000002 (with the use switched on), 4 done 10000003 (with
        /// the use switched off and the queen's talk object gone); the rest of set 20's scene kept.
        /// </summary>
        [TestMethod]
        public void TheLifeSupportSystemFollowsTheClientsChain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(AshenFixV2V1.QueenNow(), Scene(harness.World), "1501's scene: fix V2's, on fix V's");
            Assert.AreEqual(V.QueenNow(), AshenFixV2V1.QueenBefore(), "fix V2 guards on fix V's scene");
            Assert.AreEqual(1, Evidence(harness.World), "fix V's evidence row");
            Assert.AreEqual((10000001u, 10000002u, 10000003u), (V.DeviceIdle, V.DeviceActive, V.DeviceEnd), "usabledata 26423's chain");
            Assert.AreEqual((10000003u, 10000001u), (V.DeviceSpawnBefore, V.DeviceAtRestBefore), "set 20's states");

            var scene = V.QueenScene();
            var before = V.QueenSceneBefore();
            var device = scene.Actors[Queen.DeviceRole];
            Assert.AreEqual((V.DeviceIdle, false), (device.InitialObjectState, device.InitiallyInteractable), "spawns idle, not usable");
            Assert.AreEqual(before.Actors[Queen.DeviceRole] with { InitialObjectState = V.DeviceIdle }, device, "the device otherwise as set 20's");

            var ready = scene.Sequences[Queen.DeviceReady].World;
            Assert.AreEqual(2, ready.Count, "3 done: two steps");
            Assert.AreEqual(before.Sequences[Queen.DeviceReady].World.Single(), ready[0], "3 done: set 20's SetInteraction kept");
            Assert.AreEqual(new TransitionObjectStateIntent(V.ActiveKey, Queen.DeviceRole, V.DeviceActive), ready[1], "3 done: to 10000002");

            var atRest = scene.Sequences[Queen.QueenAtRest].World;
            var atRestBefore = before.Sequences[Queen.QueenAtRest].World;
            Assert.AreEqual(atRestBefore.Count, atRest.Count, "4 done: as many steps");
            Assert.AreEqual(new TransitionObjectStateIntent("life-support-off", Queen.DeviceRole, V.DeviceEnd), atRest[0], "4 done: to 10000003");
            CollectionAssert.AreEqual(atRestBefore.Skip(1).ToList(), atRest.Skip(1).ToList(), "4 done: the use off and the queen's talk object gone, kept");

            foreach (var id in before.Sequences.Keys.Where(id => id != Queen.DeviceReady && id != Queen.QueenAtRest))
                CollectionAssert.AreEqual(before.Sequences[id].World, scene.Sequences[id].World, $"sequence {id} kept");
            foreach (var role in before.Actors.Keys.Where(role => role != Queen.DeviceRole))
                Assert.AreEqual(before.Actors[role], scene.Actors[role], $"{role} kept");
            CollectionAssert.AreEquivalent(before.Actors.Keys.ToList(), scene.Actors.Keys.ToList(), "the same actors");
            CollectionAssert.AreEquivalent(before.Sequences.Keys.ToList(), scene.Sequences.Keys.ToList(), "the same sequences");
        }

        /// <summary>
        /// Fix V's migration rolled back and forth, baselined at its own migration: Down gives back set 20's scene (the device on
        /// 10000003 -> 10000001) and no fix V evidence, as a database migrated only to fix T holds them; nothing else changes; a
        /// second Down and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixVRollsBackToFixT()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixV);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.MissionId != V.Queen)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable().Select(entry => $"{entry.MissionId}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != V.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(V.QueenNow(), Scene(world), "at fix V");
            Assert.AreEqual(1, Evidence(world), "at fix V: its evidence");

            var before = AshenMissionTests.Previous(world, FixV);
            Assert.AreEqual("20271110002170_Ashen_fix_t", before, "after fix T");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Scene(fresh.World), Scene(world), "Down: the scene as at fix T");
            Assert.AreEqual(V.QueenBefore(), Scene(world), "set 20's scene");
            Assert.AreEqual(0, Evidence(world), "Down: no fix V evidence");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in V.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(V.QueenBefore(), Scene(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixV);
            world.ChangeTracker.Clear();
            Assert.AreEqual(V.QueenNow(), Scene(world), "Up again");
            foreach (var sql in V.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(V.QueenNow(), Scene(world), "a second Up changes nothing");
            Assert.AreEqual(1, Evidence(world), "Up: its evidence");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
