using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using P = AshenFixPV1;
    using O = AshenFixOV1;

    /// <summary>
    /// Ashen fix job P (AshenFixPV1, migration 20271110002120): fix O's pools 790700 and 790701 sized for one player (real client
    /// 2026-11-13; a deviation, user review). [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixPTests
    {
        private const string FixP = "20271110002120_Ashen_fix_p";

        private const string Now =
            "790700 814.000,289.120,-248.000 r4 531015/1-1 531016/1-2 531017/0-0 respawn 1200 | " +
            "790701 972.000,274.080,-212.000 r6 531016/1-2 0/0-0 0/0-0 respawn 1200 | marker 18 | evidence 1";

        private const string Before =
            "790700 814.000,289.120,-248.000 r4 531015/2-3 531016/3-4 531017/0-2 respawn 1200 | " +
            "790701 972.000,274.080,-212.000 r6 531016/2-4 0/0-0 0/0-0 respawn 1200 | marker 10 | evidence 0";

        private static string Mine(DbContext db) => string.Join(" | ",
            db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id == O.WestPool || entry.Id == O.EastPool).OrderBy(entry => entry.Id).AsEnumerable()
                .Select(entry => FormattableString.Invariant($"{entry.Id} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000} r{entry.Radius:0.###} ") +
                    $"{entry.Creature1Id}/{entry.Creature1MinCount}-{entry.Creature1MaxCount} {entry.Creature2Id}/{entry.Creature2MinCount}-{entry.Creature2MaxCount} " +
                    $"{entry.Creature3Id}/{entry.Creature3MinCount}-{entry.Creature3MaxCount} respawn {entry.RespawnTime}")
                .Append(FormattableString.Invariant($"marker {db.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == P.Run && entry.ContentRevision == AshenMissionDataV1.Revision && entry.IndicatorId == O.EastMarker).Radius:0.###}"))
                .Append($"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == P.Source)}"));

        /// <summary>Fix P's counts on fix O's pools (places, radii and respawn kept), 790701's 18 m marker and its evidence row.</summary>
        [TestMethod]
        public void ThePoolsAreSizedForOnePlayer()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(Now, Mine(harness.World));
        }

        /// <summary>
        /// Fix P's migration rolled back and forth, baselined at its own migration: Down gives back fix O's counts and 10 m marker and no fix P
        /// evidence, as a database migrated only to fix O holds them; no other pool or evidence changes; a second Down and a second
        /// Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixPRollsBackToFixO()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixP);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id != O.WestPool && entry.Id != O.EastPool).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosZ:R},{entry.Creature1Id},{entry.Creature1MinCount},{entry.Creature1MaxCount},{entry.Creature2MaxCount},{entry.Creature3MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != P.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(Now, Mine(world), "at fix P");

            var before = AshenMissionTests.Previous(world, FixP);
            Assert.AreEqual("20271110002110_Ashen_fix_o", before, "after fix O");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Mine(fresh.World), Mine(world), "Down: as a database migrated only to fix O");
            Assert.AreEqual(Before, Mine(world), "fix O's counts");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in P.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Mine(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixP);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "Up again");
            foreach (var sql in P.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
