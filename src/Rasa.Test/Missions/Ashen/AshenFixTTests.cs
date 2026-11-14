using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using T = AshenFixTV1;

    /// <summary>
    /// Ashen fix job T (AshenFixTV1, migration 20271110002170, to-fix row 60): 1498's drop pools 531064 (r 30 -> 12) and 531066
    /// (r 21.3 -> 14) keep their draws on the biolab's floor; markers 1 and 2 round the new circles. [TestCategory]: none, as
    /// PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixTTests
    {
        private const string FixT = "20271110002170_Ashen_fix_t";

        private const string Now = "531064 r12 | 531066 r14 | 1/1000000001 r17 | 2/1000000002 r19 | evidence 1";
        private const string Before = "531064 r30 | 531066 r21.3 | 1/1000000001 r35 | 2/1000000002 r26.3 | evidence 0";

        /// <summary>The lab floor's lowest height (the probe: the floor at 105-105.6; the ramps round it at 96-104).</summary>
        private const float LabFloor = 104.9f;

        /// <summary>The upper ledge west of the lab where the tester's walk to 531066 dead-ended (to-fix row 60).</summary>
        private static readonly Vector3 Ledge = new(-185.9f, 105.6f, -183.6f);

        private static readonly uint[] PoolIds = T.Pools.Select(pool => pool.Pool).ToArray();

        private static string Mine(DbContext db)
        {
            var pools = db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => PoolIds.Contains(entry.Id)).OrderBy(entry => entry.Id).AsEnumerable()
                .Select(entry => FormattableString.Invariant($"{entry.Id} r{entry.Radius:0.###}"));
            var markers = T.Pools.Select(pool =>
            {
                var marker = db.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == T.Blast && entry.ContentRevision == AshenMissionDataV1.Revision &&
                    entry.ObjectiveId == pool.Objective && entry.IndicatorId == pool.Marker);
                return FormattableString.Invariant($"{pool.Objective}/{pool.Marker} r{marker.Radius:0.###}");
            });
            return string.Join(" | ", pools.Concat(markers)) + $" | evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == T.Source)}";
        }

        /// <summary>Fix T's rows: the two pools' radii (places, counts and respawn kept), markers 1 and 2 (places kept), its evidence row.</summary>
        [TestMethod]
        public void TheLabPoolsAndTheirMarkersAreSmaller()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(Now, Mine(harness.World));
            foreach (var (pool, at, marker, objective, _, now) in T.Pools)
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((T.Facility, at.X, at.Z, 1200u), (row.MapContextId, Math.Round(row.PosX, 1), Math.Round(row.PosZ, 1), row.RespawnTime), $"{pool}: its place and respawn");
                var indicator = harness.World.Set<MissionIndicatorEntry>().AsNoTracking().Single(entry => entry.MissionId == T.Blast && entry.ObjectiveId == objective && entry.IndicatorId == marker);
                Assert.AreEqual((at.X, at.Z, now + T.Padding), (Math.Round(indicator.PosX, 1), Math.Round(indicator.PosZ, 1), indicator.Radius), $"marker {marker}: the pool's point, its circle and 5 m");
            }
        }

        /// <summary>
        /// The pools' ground (AshenSpawnReach: 1500 seeded draws each, as SpawnPoolManager.SpawnPoint draws): with fix T's radii every
        /// draw stands on the lab floor (y 104.9 and up), not on the ramps round it, and keeps AshenSpawnReach.Clearance (20 m, 3D)
        /// from the lab's charge (fix M's place, objective 5) and from the west ledge. With the old radii some draws did not (the
        /// probe's 9 % and 11 % on the ramps): the check can fail.
        /// </summary>
        [TestMethod]
        public void EveryDrawStandsOnTheLabFloor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = AshenConscriptionEntryTests.IntoFacility(harness);
            var charge = new Vector3((float)AshenFixMV1.LabChargeAt.X, (float)AshenFixMV1.LabChargeAt.Y, (float)AshenFixMV1.LabChargeAt.Z);
            foreach (var (id, _, _, _, before, now) in T.Pools)
            {
                var pool = copy.SpawnPools.First(entry => entry.DbId == id);
                Assert.AreEqual((float)now, pool.Radius, 0.001f, $"{id}: fix T's radius in the copy");
                var ground = AshenSpawnReach.Ground(copy, new[] { id })[id].Skip(1).ToList();
                Assert.AreEqual(AshenSpawnReach.Draws, ground.Count, $"{id}: every draw on the navmesh");
                var low = ground.Min(point => point.Y);
                var flat = ground.Max(point => Vector2.Distance(new(point.X, point.Z), new(pool.Position.X, pool.Position.Z)));
                var toCharge = ground.Min(point => Vector3.Distance(point, charge));
                var toLedge = ground.Min(point => Vector3.Distance(point, Ledge));
                Console.WriteLine(FormattableString.Invariant($"{id} r {now}: y {low:0.00}..{ground.Max(point => point.Y):0.00}, flat max {flat:0.0}, to the charge {toCharge:0.0}, to the ledge {toLedge:0.0}"));
                Assert.IsGreaterThanOrEqualTo(LabFloor, low, $"{id}: every draw on the lab floor");
                Assert.IsGreaterThanOrEqualTo(AshenSpawnReach.Clearance, toCharge, $"{id}: clear of the lab's charge");
                Assert.IsGreaterThanOrEqualTo(AshenSpawnReach.Clearance, toLedge, $"{id}: clear of the west ledge");

                pool.Radius = (float)before;
                try
                {
                    var old = AshenSpawnReach.Ground(copy, new[] { id })[id].Skip(1).ToList();
                    Console.WriteLine(FormattableString.Invariant($"{id} r {before}: below the floor {old.Count(point => point.Y < LabFloor)} of {old.Count}"));
                    Assert.IsTrue(old.Any(point => point.Y < LabFloor), $"{id}: the old radius drew onto the ramps (the check can fail)");
                }
                finally
                {
                    pool.Radius = (float)now;
                }
            }
        }

        /// <summary>
        /// Fix T's migration rolled back and forth, baselined at its own migration: Down gives back set 19's radii and markers and
        /// no fix T evidence, as a database migrated only to fix S2 holds them; nothing else changes; a second Down and a second Up
        /// change nothing.
        /// </summary>
        [TestMethod]
        public void FixTRollsBackToFixS2()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixT);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant(
                        $"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{(PoolIds.Contains(entry.Id) ? "-" : entry.Radius.ToString("R", System.Globalization.CultureInfo.InvariantCulture))},{entry.RespawnTime},{entry.Creature1Id},{entry.Creature1MaxCount},{entry.Creature2Id},{entry.Creature2MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}/{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},") +
                        (entry.MissionId == T.Blast && T.Pools.Any(pool => pool.Marker == entry.IndicatorId) ? "-" : entry.Radius.ToString("R", System.Globalization.CultureInfo.InvariantCulture))))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != T.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var others = Others(world);
            Assert.AreEqual(Now, Mine(world), "at fix T");

            var before = AshenMissionTests.Previous(world, FixT);
            Assert.AreEqual("20271110002160_Ashen_fix_s2", before, "after fix S2");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Mine(fresh.World), Mine(world), "Down: as a database migrated only to fix S2");
            Assert.AreEqual(Before, Mine(world), "set 19's radii and markers");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in T.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Mine(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixT);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "Up again");
            foreach (var sql in T.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
