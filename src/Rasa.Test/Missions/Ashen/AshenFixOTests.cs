using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = MarshesClientFloorTests;
    using O = AshenFixOV1;
    using M = AshenFixMV1;
    using Run = AshenPetrovichsRunV1;
    using Outpost = AshenAvernusOutpostTests;

    /// <summary>
    /// Ashen fix job O (AshenFixOV1, migration 20271110002110): 1535's first objective counts our pools 790700 and 790701 on the
    /// graveyard's main floor instead of upstream 531049 and 531060, whose creatures stand on navmesh islands inside the client's
    /// rock (real client 2026-11-13: unseen, unreachable, "You cannot see your target from here").
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixOTests
    {
        private const string FixO = "20271110002110_Ashen_fix_o";

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>
        /// How far over the client's floor a navmesh draw may be (SpawnPoolManager.SpawnPoint: the mesh is anything an agent
        /// steps onto, 0.9 m; 790701's draw at (961.1, 274.75, -204.9) is 0.89 m over it) and a little.
        /// </summary>
        private const float NavOverFloor = 0.95f;

        /// <summary>The navmesh regions below this are islands (531049's is 22 m2, 531060's 8.8 m2; the main floor 43,686 m2).</summary>
        private const float IslandArea = 1000;

        /// <summary>
        /// The upstream island pools as they are (fix O leaves them): every seeded draw of each on a region joined to nothing and
        /// unreachable from Petrovich; the regression.
        /// </summary>
        [TestMethod]
        public void TheUpstreamPoolsStandOnIslands()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = Outpost.IntoOutpost(harness);
            var nav = copy.NavMesh;
            var rows = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.Id == O.WestIsland || row.Id == O.EastIsland).OrderBy(row => row.Id).ToList();
            Assert.AreEqual("531049:821.3,289,-240.5,20,531015/2-3,531016/3-4,531017/0-2,1200; 531060:974.5,274.1,-230.4,13.7,531016/2-4,0/0-0,0/0-0,1200",
                string.Join("; ", rows.Select(row => FormattableString.Invariant(
                    $"{row.Id}:{row.PosX:0.###},{row.PosY:0.###},{row.PosZ:0.###},{row.Radius:0.###},{row.Creature1Id}/{row.Creature1MinCount}-{row.Creature1MaxCount},{row.Creature2Id}/{row.Creature2MinCount}-{row.Creature2MaxCount},{row.Creature3Id}/{row.Creature3MinCount}-{row.Creature3MaxCount},{row.RespawnTime}"))),
                "the upstream rows untouched");
            var ground = AshenSpawnReach.Ground(copy, new[] { O.WestIsland, O.EastIsland });
            foreach (var (pool, points) in ground)
            {
                var draws = points.Skip(1).ToList();
                Assert.HasCount(AshenSpawnReach.Draws, draws, $"{pool}'s draws");
                Assert.IsTrue(draws.All(draw => (nav.RegionArea(draw) ?? 0) < IslandArea), $"{pool}: every draw on an island");
                Assert.IsTrue(draws.All(draw => nav.Reachable(V(M.PetrovichAt), draw) == false), $"{pool}: no draw reached from Petrovich");
            }
        }

        /// <summary>
        /// Fix O's pools stand their creatures where a player sees and reaches them: 1500 seeded draws of each (AshenSpawnReach)
        /// on the main floor (a region of IslandArea and more, reached from Petrovich); every tenth on the client's floor (at most
        /// NavOverFloor under it) with room for a body, seen (a player's eye 1.6 m to the creature's middle 1.0 m, clear of the client's collision) from the walk to
        /// it within 25 m, and walked to on the client floor (fix J's rules, AshenFixBTests.ClientFloorWalk, the pads put in as fix
        /// M2's AddPads) from the walk's nearest point: 790700 from 1535's walk to the tunnel (it crosses the pool's floor), 790701
        /// from Petrovich by the navmesh's way east. The knee-high thing at (839.9, 288.11, -244.5) that a navmesh walk from
        /// Petrovich met (the probe) is beside the leg, not on it: the leg passes fix J's rules at 0.5 m half width (fix M's test)
        /// and its line keeps 0.5 m and more from it here. A rock at (810.9, -254.9), knee-high from the north, stopped walks to the
        /// draws of a first place (812, -252) r 6 south of it; 790700's draws stay north of it.
        /// </summary>
        [TestMethod]
        public void TheNewPoolsStandOnTheFloorSeenAndReached()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, AshenAvernusOutpostNpcsV1.Outpost)), meshes, 1);
            AshenFixMTests.AddPads(harness, geometry, meshes);
            var copy = Outpost.IntoOutpost(harness);
            var nav = copy.NavMesh;
            var petrovich = V(M.PetrovichAt);
            var leg = Outpost.Legs["the graveyard to the tunnel"];
            var knee = new Vector2(839.9f, -244.5f);
            var legNear = AshenSpawnReach.Samples(leg, 0.1f).Min(point => Vector2.Distance(new(point.X, point.Z), knee));
            Assert.IsGreaterThan(0.5f, legNear, FormattableString.Invariant($"the leg {legNear:0.00} m from the knee-high thing"));

            var eastPath = nav.FindPath(nav.Nearest(petrovich) ?? petrovich, V(O.EastAt), out var eastComplete);
            Assert.IsTrue(eastComplete && eastPath is { Count: > 0 }, "a navmesh walk from Petrovich to 790701");
            Assert.IsNull(AshenFixBTests.ClientFloorWalk(nav, geometry, petrovich, V(O.EastAt)), "the client floor walk from Petrovich to 790701");
            var walks = new Dictionary<uint, List<Vector3>>
            {
                [O.WestPool] = AshenSpawnReach.Samples(leg, 1),
                [O.EastPool] = AshenSpawnReach.Samples(eastPath.Prepend(nav.Nearest(petrovich) ?? petrovich).ToList(), 1)
            };
            var ground = AshenSpawnReach.Ground(copy, new[] { O.WestPool, O.EastPool });
            var failures = new List<string>();
            var report = new List<string>();
            foreach (var (pool, points) in ground)
            {
                var draws = points.Skip(1).ToList();
                Assert.HasCount(AshenSpawnReach.Draws, draws, $"{pool}'s draws");
                var centre = points[0];
                var near = F.Near(geometry, new List<Vector3> { centre }, 50)[0];
                var island = draws.Count(draw => (nav.RegionArea(draw) ?? 0) < IslandArea || nav.Reachable(petrovich, draw) != true);
                var sampled = draws.Where((_, k) => k % 10 == 0).ToList();
                int off = 0, unseen = 0, unwalked = 0;
                foreach (var draw in sampled)
                {
                    if (F.FloorAt(near, draw, draw.Y - NavOverFloor, draw.Y + 0.6f) is not { } y
                        || !F.Clear(near, draw with { Y = y + 0.05f }, draw with { Y = y + 1.8f }))
                        off++;
                    var from = walks[pool].Where(point => Vector3.Distance(point, draw) <= 25).ToList();
                    if (!from.Any(point => F.Clear(near, (nav.Nearest(point) ?? point) + new Vector3(0, 1.6f, 0), draw + Vector3.UnitY)))
                        unseen++;
                    var start = walks[pool].OrderBy(point => Vector3.Distance(point, draw)).First();
                    if (AshenFixBTests.ClientFloorWalk(nav, geometry, start, draw) is { } why)
                    {
                        unwalked++;
                        if (unwalked <= 3)
                            failures.Add(FormattableString.Invariant($"{pool}: ({draw.X:0.0}, {draw.Y:0.00}, {draw.Z:0.0}) not walked to from ({start.X:0.0}, {start.Z:0.0}): {why}"));
                    }
                }
                var lo = draws.Aggregate(Vector3.Min);
                var hi = draws.Aggregate(Vector3.Max);
                var farthest = draws.Max(d => Vector2.Distance(new(d.X, d.Z), new(centre.X, centre.Z)));
                report.Add(FormattableString.Invariant($"{pool}: islands {island}, off the floor {off}/{sampled.Count}, unseen {unseen}, not walked {unwalked}; box ({lo.X:0.0}, {lo.Z:0.0})-({hi.X:0.0}, {hi.Z:0.0}), farthest {farthest:0.00} m"));
                // The farthest draw from the pool's point (across the ground), inside its marker: 790700's 8 m (its circle + 4 m);
                // 790701's whole polygons reach 14.0 m (draws near (961, -205)), inside fix P's 18 m (fix O's 10 m did not hold it).
                var marker = pool == O.WestPool ? O.WestRadius + Run.PoolMarkerPadding : AshenFixPV1.EastMarkerNow;
                Assert.IsLessThanOrEqualTo((float)marker, farthest, FormattableString.Invariant($"{pool}: farthest draw {farthest:0.00} m within its {marker} m marker"));
                if (pool == O.EastPool)
                    Assert.AreEqual(14.0f, farthest, 0.1f, FormattableString.Invariant($"{pool}: farthest draw {farthest:0.00} m"));
                if (island > 0) failures.Add($"{pool}: {island} draws on an island or not reached from Petrovich");
                if (off > 0) failures.Add($"{pool}: {off} sampled draws off the client floor");
                if (unseen > 0) failures.Add($"{pool}: {unseen} sampled draws unseen from the walk");
            }
            Console.WriteLine(string.Join("\n", report));
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The rule break fix O makes (docs/ashen-playtest.md): its pools stand beside the upstream islands they stand in for, far
        /// less than the 60 m between hostile pools (the draws, AshenSpawnReach; 790700 from 531049's, 790701 from 531060's and 531050's): the islands' creatures cannot step off their islands (no path; TheUpstreamPoolsStandOnIslands), and
        /// 531050 is the objective's own. Every other hostile pool's draws keep AggroRange and more from fix O's.
        /// </summary>
        [TestMethod]
        public void TheNewPoolsBreakThePoolRuleOnlyBesideTheIslandsAndTheirOwn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = Outpost.IntoOutpost(harness);
            var hostile = Outpost.HostilePools(harness).Select(entry => entry.Id).ToList();
            CollectionAssert.IsSubsetOf(new[] { O.WestPool, O.EastPool }, hostile, "fix O's pools hostile");
            var ground = AshenSpawnReach.Ground(copy, hostile);
            var allowed = new Dictionary<uint, uint[]>
            {
                [O.WestPool] = new[] { O.WestIsland },
                [O.EastPool] = new[] { O.EastIsland, AshenPetrovichsRunV1.CavePools[1].Pool }
            };
            var report = new List<string>();
            var failures = new List<string>();
            foreach (var pool in new[] { O.WestPool, O.EastPool })
            {
                var others = ground.Where(kv => kv.Key != O.WestPool && kv.Key != O.EastPool).ToDictionary(kv => kv.Key, kv => kv.Value);
                foreach (var (other, near, _) in AshenSpawnReach.Reach(ground[pool], others).Where(row => row.Near < AshenSpawnReach.Clearance))
                {
                    report.Add(FormattableString.Invariant($"{pool} {near:0.0} m from {other}'s draws"));
                    if (!allowed[pool].Contains(other) && near < AshenSpawnReach.AggroRange)
                        failures.Add(FormattableString.Invariant($"{pool}: {near:0.0} m from {other}'s draws"));
                }
            }
            Console.WriteLine(string.Join("\n", report));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>Fix O's rows: the two pools, objective 1's four triggers and markers on the four pools it counts, its evidence.</summary>
        [TestMethod]
        public void TheRowsAreFixOs()
        {
            // At fix O's own migration: fix P (20271110002120) resizes the two pools after it (AshenFixPTests).
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixO);
            var world = harness.World;
            Assert.AreEqual(Mine(world), Expected, "fix O's rows");
        }

        private const string Expected =
            "pool 790700 814.000,289.120,-248.000 r4 531015/2-3 531016/3-4 531017/0-2 respawn 1200 map 2110; " +
            "pool 790701 972.000,274.080,-212.000 r6 531016/2-4 0/0-0 0/0-0 respawn 1200 map 2110 | " +
            "trigger 1: 790700; trigger 2: 531050; trigger 3: 790701; trigger 4: 531063 | " +
            "marker 1000000001 814.000,289.120,-248.000 r8.000; marker 1000000002 944.700,263.200,-289.700 r24.000; " +
            "marker 1000000003 972.000,274.080,-212.000 r10.000; marker 1000000004 867.500,282.300,-280.700 r11.000 | evidence 1";

        private static string Mine(DbContext db)
        {
            const string revision = AshenMissionDataV1.Revision;
            return string.Join(" | ",
                string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id == O.WestPool || entry.Id == O.EastPool).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"pool {entry.Id} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000} r{entry.Radius:0.###} ") +
                        $"{entry.Creature1Id}/{entry.Creature1MinCount}-{entry.Creature1MaxCount} {entry.Creature2Id}/{entry.Creature2MinCount}-{entry.Creature2MaxCount} " +
                        $"{entry.Creature3Id}/{entry.Creature3MinCount}-{entry.Creature3MaxCount} respawn {entry.RespawnTime} map {entry.MapContextId}")),
                string.Join("; ", db.Set<MissionTriggerEntry>().AsNoTracking().Where(entry => entry.MissionId == Run.MissionId && entry.ContentRevision == revision && entry.ObjectiveId == Run.ClearTheCave)
                    .OrderBy(entry => entry.TriggerId).AsEnumerable().Select(entry => $"trigger {entry.TriggerId}: {entry.SubjectId}")),
                string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => entry.MissionId == Run.MissionId && entry.ContentRevision == revision && entry.ObjectiveId == Run.ClearTheCave)
                    .OrderBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"marker {entry.IndicatorId} {entry.PosX:0.000},{entry.PosY:0.000},{entry.PosZ:0.000} r{entry.Radius:0.000}"))),
                $"evidence {db.Set<MissionEvidenceEntry>().AsNoTracking().Count(entry => entry.SourceUri == O.Source)}");
        }

        /// <summary>
        /// Fix O's migration rolled back and forth, baselined at its own migration: Down gives back set 17's rows (triggers 1 and 3
        /// on 531049 and 531060, their markers on the pools' circles) and no fix O pools or evidence, as a database migrated only to
        /// set 21 (20271110002100) holds them; nothing else changes; a second Down and a second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixORollsBackToSet21()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixO);
            var world = harness.World;
            string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking().Where(entry => entry.Id != O.WestPool && entry.Id != O.EastPool).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionTriggerEntry>().AsNoTracking().Where(entry => !(entry.MissionId == Run.MissionId && entry.ObjectiveId == Run.ClearTheCave))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.TransitionId).ThenBy(entry => entry.TriggerId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}.{entry.ObjectiveId}.{entry.TransitionId}.{entry.TriggerId}:{entry.SubjectId}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking().Where(entry => !(entry.MissionId == Run.MissionId && entry.ObjectiveId == Run.ClearTheCave))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", db.Set<MissionEvidenceEntry>().AsNoTracking().Where(entry => entry.SourceUri != O.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var all = Mine(world);
            var others = Others(world);
            Assert.AreEqual(Expected, all, "fix O's rows at its own migration");

            var before = AshenMissionTests.Previous(world, FixO);
            Assert.AreEqual("20271110002100_Ashen_creature_tints", before, "after set 21");
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            var down = Mine(world);
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
                Assert.AreEqual(Mine(fresh.World), down, "Down: as a database migrated only to set 21");
            Assert.AreEqual(" | trigger 1: 531049; trigger 2: 531050; trigger 3: 531060; trigger 4: 531063 | " +
                "marker 1000000001 821.300,289.000,-240.500 r24.000; marker 1000000002 944.700,263.200,-289.700 r24.000; " +
                "marker 1000000003 974.500,274.100,-230.400 r17.700; marker 1000000004 867.500,282.300,-280.700 r11.000 | evidence 0", down, "set 17's rows");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in O.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(down, Mine(world), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixO);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(world), "Up again");
            foreach (var sql in O.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(world), "a second Up changes nothing");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }
    }
}
