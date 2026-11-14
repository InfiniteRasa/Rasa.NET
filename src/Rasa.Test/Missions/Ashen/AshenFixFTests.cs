using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static Rasa.Test.Missions.Marshes.MarshesPaludosTests;
    using static Rasa.Test.Missions.Plateau.PlateauCampResistanceTests;
    using D = AshenDesignerHidesNpcsV1;
    using Jewelry = AshenDesignerJewelryV1;
    using X = AshenFixFV1;

    /// <summary>
    /// Ashen fix job F: set 10's bosses' damage immunities written down (to-fix row 20), their pools' respawn of 300 s (row 18),
    /// 1287's own Ashen Atta Soldier pool out of the Mauna Barrens camp's call for help (row 22), and the reach of the creatures
    /// those rows met (the Atta Workers' warcry, the Xanx at White Oasis's panel 4: row 21) as the pool spacing rule reads it.
    ///
    /// Why these tests: a boss class that takes nothing from a damage type is a fight a player loses with the wrong weapon
    /// (Sally to Electric, row 20); a boss back in 50 s comes again into the carapace farm (row 18); a carapace pool within a
    /// Worker's warcry pulls the camp (row 22); a code change to the aggro, chase, wander or warcry reach makes the spacing rule
    /// (NOTES-ashen-triage.md, fix F) wrong; a Down that does not put set 10's rows back breaks a rollback.
    ///
    /// The place's ground, respawns and walk are checked with set 10's pools (AshenDesignerHidesTests.Places), its distance to
    /// the other set 10 pools by AshenFixETests.TheOrdersPlacesKeepClearOfOtherHostilePools.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixFTests
    {
        private const string FixF = "20271110001310_Ashen_fix_f";

        /// <summary>
        /// The reach the spacing rule reads (code and World DB, as of fix F): a creature notices a player within
        /// Creature.AggroRange (18 m) and strolls within its pool's radius, or WanderZoneFallbackRadius (10 m) round its spawn
        /// point for a pool of radius 0; it follows a fight MaxChaseDistance (60 m) from home; an Atta harvester's warcry calls
        /// CALL_FOR_HELP_NUM_CREATURES (2) hostile creatures within RADIUS_AROUND_SOURCE (80 m).
        /// </summary>
        internal const float AggroRange = 18, FallbackWander = 10, ChaseDistance = 60, WarcryRadius = 80;
        internal const int WarcryCalls = 2;

        /// <summary>A pool of radius 0 notices a player up to this far from its point (its strolls and the aggro).</summary>
        internal const float PointNotice = FallbackWander + AggroRange;

        /// <summary>Fix F's rule: a pool's point this far (2D, less the other's radius) from every other hostile pool (two notice reaches and 4 m), as set 10's 60 m.</summary>
        internal const float PoolGap = 60;

        /// <summary>And this far from every pool of a creature with the warcry: the cry's reach and both pools' strolls.</summary>
        internal const float WarcryGap = WarcryRadius + 2 * FallbackWander;

        private static float Flat(double ax, double az, double bx, double bz) => Vector2.Distance(new((float)ax, (float)az), new((float)bx, (float)bz));

        /// <summary>
        /// Row 20: set 10's creatures take nothing from these damage types (their classes' IMMUNE_ flags, World DB
        /// creature_class_flag, read by CreatureImmunities): Sally 22627 Fire and Electrical, Molly 10331 Virulent and Laser,
        /// Trudy 10317 Electrical, Wendy 25620 Fire; the Lasher 7477 Virulent and Laser, the Beam Manta 6441 Electrical, the
        /// Granitour 10164 Fire; and 1287's droppers, set 2's Ashen Atta Soldier (7081) Fire and Electrical, the Worker (7078)
        /// none; set 12's ridge Lashers are set 10's row. Every other damage type (Physical, Ice, EMP, Sonic among them) lands.
        /// docs/ashen-playtest.md lists them.
        /// </summary>
        [TestMethod]
        public void TheSetTenCreaturesTakeNothingFromTheirClassesTypes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var types = new[] { DamageType.Physical, DamageType.Fire, DamageType.Ice, DamageType.Virulent, DamageType.EMP, DamageType.Laser, DamageType.Sonic, DamageType.Electrical };
            var expected = new (uint Creature, uint Class, DamageType[] Immune)[]
            {
                (D.Sally, 22627, new[] { DamageType.Fire, DamageType.Electrical }),
                (D.Molly, 10331, new[] { DamageType.Virulent, DamageType.Laser }),
                (D.Trudy, 10317, new[] { DamageType.Electrical }),
                (D.Wendy, 25620, new[] { DamageType.Fire }),
                (D.Lasher, 7477, new[] { DamageType.Virulent, DamageType.Laser }),
                (D.Manta, 6441, new[] { DamageType.Electrical }),
                (D.Granitour, 10164, new[] { DamageType.Fire }),
                (AshenMaunaBarrensNpcsV1.AttaSoldier, 7081, new[] { DamageType.Fire, DamageType.Electrical }),
                (AshenMaunaBarrensNpcsV1.AttaWorker, 7078, Array.Empty<DamageType>())
            };
            var failures = new List<string>();
            foreach (var (id, cls, immune) in expected)
            {
                Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature), $"{id} loaded");
                Assert.AreEqual(cls, (uint)creature.EntityClass, $"{id}'s class");
                var actual = types.Where(type => CreatureImmunities.IsImmune(creature, type)).ToArray();
                if (!actual.SequenceEqual(immune))
                    failures.Add($"{id} (class {cls}) immune to [{string.Join(", ", actual)}], not [{string.Join(", ", immune)}]");
                if (CreatureImmunities.IsImmune(creature, DamageType.Physical))
                    failures.Add($"{id} immune to Physical");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));

            // The classes stay (row 20, coordinator 2026-11-10: switch a reconstructed class to an immune-free one of the same kind
            // where the client has one): every client creature class of each kind (entityclass, World DB) carries the same
            // immunities, so there is none to switch to. The Atta Harvester and Queen classes take everything, but they are
            // other kinds than a Soldier (Sally is "the Ashen Atta Soldier called Sally").
            var classes = harness.World.EntityClassEntries.AsNoTracking().Where(row => row.ClassName.StartsWith("Creature_")).AsEnumerable().ToList();
            var flags = harness.World.CreatureClassFlagEntries.AsNoTracking().AsEnumerable().GroupBy(row => row.ClassId).ToDictionary(group => group.Key, group => group.Select(row => (uint)row.FlagId).ToHashSet());
            string Immunities(uint cls) => string.Join(",", CreatureImmunities.Flags.Where(pair => types.Contains(pair.Key) && flags.TryGetValue(cls, out var set) && set.Contains((uint)pair.Value)).Select(pair => pair.Key));
            foreach (var (kind, expectedKind, count) in new[] { ("Creature_Atta_Soldier_", "Fire,Electrical", 5), ("Creature_Lasher", "Virulent,Laser", 2),
                ("Creature_Beam_Manta", "Electrical", 2), ("Creature_Granitour_", "Fire", 3) })
            {
                var ofKind = classes.Where(row => row.ClassName.StartsWith(kind)).ToList();
                Assert.HasCount(count, ofKind, $"the client's {kind} classes");
                foreach (var row in ofKind)
                    Assert.AreEqual(expectedKind, Immunities(row.Id), $"{row.ClassName} ({row.Id}): no immune-free class of the kind");
            }
        }

        /// <summary>
        /// Row 18: set 10's four boss pools respawn in 3000 tenths (300 s), set 10's 500 before; the World DB rows and the loaded
        /// pools (milliseconds) alike. Their other columns are set 10's.
        /// </summary>
        [TestMethod]
        public void TheBossesComeBackAfterFiveMinutes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            Assert.HasCount(4, X.BossPools, "Sally, Molly, Trudy, Wendy");
            Assert.AreEqual((500U, 3000U), (X.BossRespawnBefore, X.BossRespawnTenths), "set 10's 50 s, fix F's 300 s");
            foreach (var (pool, boss, _) in X.BossPools)
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((boss, (byte)1, (byte)1, 0.0, X.BossRespawnTenths), (row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount, row.Radius, row.RespawnTime), $"pool {pool}");
                Assert.AreEqual(300_000L, desert.SpawnPools.Single(loaded => loaded.DbId == pool).RespawnTime, $"pool {pool} loaded: 300 s");
            }
        }

        /// <summary>
        /// Row 22: 1287's own pool (790378) holds Ashen Atta Soldiers (three; one since fix G) (790043, set 2's row: no warcry), radius 0, set 2's
        /// 120 s, at fix F's place, 1287's marker 1/6 (r20) on it. It stands PoolGap (60 m) and more from every other hostile
        /// Ashen pool (2D, less its radius), WarcryGap (100 m) from every pool of a creature with the Atta harvester warcry (477),
        /// and 85 m from every NPC (set 10's). Each of the camp's four Atta pools (set 2) stands within a Worker's warcry
        /// (the fix's reason). A Soldier of the pool drops 1287's carapace (74, as set 10's other Soldiers) and counts for 1242.
        /// </summary>
        [TestMethod]
        public void TheCarapacePoolStandsOutOfTheCampsCallForHelp()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var world = harness.World;
            var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == X.AttaPool);
            // Three as fix F wrote it; one since fix G (AshenFixGTests).
            Assert.AreEqual((AshenMissionDataV1.MapContextId, X.AttaSoldier, (byte)AshenFixGV1.AttaCount, (byte)AshenFixGV1.AttaCount, 0U, 0.0, 1200U, (byte)0),
                (row.MapContextId, row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount, row.Creature2Id, row.Radius, row.RespawnTime, row.Mode), "the pool");
            Assert.AreEqual((X.AttaAt.X, X.AttaAt.Y, X.AttaAt.Z), (row.PosX, row.PosY, row.PosZ), "at fix F's place");
            var marker = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == Jewelry.MissionId &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.MarkerObjective && entry.IndicatorId == X.MarkerIndicator);
            Assert.AreEqual((X.AttaAt.X, X.AttaAt.Y, X.AttaAt.Z, 20.0), (marker.PosX, marker.PosY, marker.PosZ, marker.Radius), "1287's marker on it");

            var criers = WarcryCreatures(harness);
            CollectionAssert.AreEquivalent(new[] { AshenMaunaBarrensNpcsV1.AttaWorker }, criers.Where(id => id >= 790000).ToArray(), "set 2's Worker is the Ashen warcry creature");
            Assert.IsFalse(criers.Contains(X.AttaSoldier), "the Soldier has no warcry");
            var pools = AshenWhiteOasisTests.HostilePools(harness);
            var criersPools = pools.Where(pool => Creatures(pool).Any(criers.Contains)).ToList();
            Assert.IsGreaterThanOrEqualTo(2, criersPools.Count, "the camp's Worker pools");
            var failures = new List<string>();
            foreach (var other in pools.Where(pool => pool.Id != X.AttaPool))
            {
                var gap = Flat(row.PosX, row.PosZ, other.PosX, other.PosZ) - (float)other.Radius;
                if (gap < PoolGap)
                    failures.Add(FormattableString.Invariant($"hostile pool {other.Id} {gap:0.0} m"));
                if (criersPools.Contains(other) && gap < WarcryGap)
                    failures.Add(FormattableString.Invariant($"warcry pool {other.Id} {gap:0.0} m"));
            }
            var npcs = world.SpawnPoolEntries.AsNoTracking().Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId).AsEnumerable()
                .Where(entry => Creatures(entry).Any(id => CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.Npc != null)).ToList();
            Assert.IsGreaterThan(10, npcs.Count, "the Ashen NPCs are read");
            if (npcs.Min(npc => Flat(row.PosX, row.PosZ, npc.PosX, npc.PosZ)) is var nearest && nearest < 85)
                failures.Add(FormattableString.Invariant($"an NPC {nearest:0.0} m"));
            Assert.IsEmpty(failures, string.Join("; ", failures));
            // The camp: each of set 2's four Atta pools lies within a Worker's warcry of a Worker pool (or is one).
            foreach (var (pool, _, at) in AshenMaunaBarrensNpcsV1.AttaPools.Where(pool => Jewelry.Droppers.Contains(pool.Creature)))
                Assert.IsTrue(criersPools.Any(crier => Flat(at.X, at.Z, crier.PosX, crier.PosZ) < WarcryRadius), $"camp pool {pool} within a warcry (the fix's reason)");

            // A Soldier of the pool drops 1287's carapace (the 75 % of set 10, 74 drops).
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.ShadowsEdgeWaypoint);
            var ida = Npc(desert, D.IdaHayner);
            Spawn(desert, X.AttaPool);
            Accept(harness, ida, Jewelry.MissionId);
            percentile = 74;
            var loot = AshenScavengerTests.Kill(harness, desert, X.AttaSoldier, new[] { X.AttaPool });
            Assert.IsTrue(Holds(loot, Jewelry.ShinyTemplate), "a carapace off the new pool's Soldier");
            percentile = 75;
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, X.AttaSoldier, new[] { X.AttaPool }), Jewelry.ShinyTemplate), "75 misses");
        }

        /// <summary>
        /// Row 23: 1356's three pumps are the V02 pump 21007 in the World DB's scene and in the world (set 11's V01 10190 kept its
        /// intact model after its destroy path in the real client); 21007 is an InertDestroyable (augmentation 41) as 10190, of
        /// the same model bounds; the rest of the scene is set 11's (only the class differs from set 11's JSON). Used, a pump
        /// goes 110 -> 185 -> 186 -> 2 (DestroyableStates), as before.
        /// </summary>
        [TestMethod]
        public void TheFuelPumpsAreTheV02Pump()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var stored = harness.World.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == AshenGasShortagesV1.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
            Assert.AreEqual(X.SceneNow(), stored, "the World DB holds fix F's scene");
            Assert.AreEqual(X.SceneBefore(), X.SceneNow().Replace($"\"templateId\":{X.PumpClass}", $"\"templateId\":{X.PumpClassBefore}"), "only the pump class differs");
            Assert.AreEqual((10190U, 21007U), (X.PumpClassBefore, X.PumpClass), "V01 to V02");
            var v02 = EntityClassManager.Instance.GetClassInfo((EntityClasses)X.PumpClass);
            Assert.IsTrue(v02.Augmentations.Contains(AugmentationType.InertDestroyable), "21007 an InertDestroyable");
            var scene = System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(stored, Rasa.Missions.Content.MissionContentCodec.Options);
            for (var number = 1; number <= AshenGasShortagesV1.Pumps; number++)
                Assert.AreEqual(X.PumpClass, scene.Actors[AshenGasShortagesV1.PumpRole(number)].TemplateId, $"pump {number}");
        }

        /// <summary>
        /// The reach the spacing rule reads, pinned to the code and the World DB (to-fix rows 21 and 22): Creature.AggroRange 18 m,
        /// BehaviorManager.WanderZoneFallbackRadius 10 m and MaxChaseDistance 60 m; the Atta harvester warcry (477) 80 m and two
        /// creatures (action_property 1 and 129); CreatureBuffs.WarcryRearmMs a minute. Row 21: White Oasis's panel 4 (1295,
        /// 1302) stands outside the Xanx pool 540076's notice reach (its radius 15 + 18 m, with 5 m to spare), and set 11's
        /// way there by the west of Maji Field keeps out of it; the straight walk from panel 3 to panel 4 passes through it
        /// (the pull the tester met twice; ESTIMATE of the walk). So panel 4 stays (a Xanx pulled there follows up to 60 m from
        /// home and webs from 40 m: 115 m, beyond any place by harvester 4).
        /// </summary>
        [TestMethod]
        public void TheReachTheSpacingRuleReadsIsTheCodes()
        {
            Assert.AreEqual(AggroRange, new Creature().AggroRange, "Creature.AggroRange");
            Assert.AreEqual((FallbackWander, ChaseDistance), (BehaviorManager.WanderZoneFallbackRadius, BehaviorManager.MaxChaseDistance), "wander and chase");
            Assert.AreEqual(60_000L, CreatureBuffs.WarcryRearmMs, "a warcry a minute");
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var properties = world.ActionPropertyEntries.AsNoTracking().Where(entry => entry.ActionId == (uint)CreatureBuffs.HarvesterWarcry && entry.Level == 1)
                .AsEnumerable().ToDictionary(entry => entry.PropertyId, entry => entry.Value);
            Assert.AreEqual(((int)WarcryRadius, WarcryCalls), (properties[(uint)AbilityProperty.RadiusAroundSource], properties[(uint)AbilityProperty.CallForHelpNumCreatures]), "the warcry's reach and calls");

            var xanx = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == 540076);
            Assert.AreEqual((15.0, 540020U), (xanx.Radius, xanx.Creature1Id), "upstream's Xanx pool");
            var web = world.CreatureActionEntries.AsNoTracking().Single(entry => entry.Id == 55030);
            Assert.AreEqual((238U, 40.0), (web.ActionId, web.RangeMax), "the Xanx web, 40 m");
            var notice = (float)xanx.Radius + AggroRange;
            var panels = AshenAnalyzingHarvestersV1.PanelsAt;
            var panel4 = new Vector2(panels[3].X, panels[3].Z);
            var point = new Vector2((float)xanx.PosX, (float)xanx.PosZ);
            Assert.IsGreaterThan(notice + 5, Vector2.Distance(panel4, point), "panel 4 outside the Xanx's notice reach");
            var west = AshenDesignerHidesTests.WestOfMaji.Select(at => new Vector2(at.X, at.Z)).Append(panel4).ToArray();
            for (var leg = 0; leg + 1 < west.Length; leg++)
                Assert.IsGreaterThan(notice + 5, ToSegment(point, west[leg], west[leg + 1]), $"set 11's way, leg {leg + 1}, outside the notice reach");
            Assert.IsLessThan(notice, ToSegment(point, new Vector2(panels[2].X, panels[2].Z), panel4), "the straight walk from panel 3 passes through it");
            Assert.IsLessThan((float)(xanx.Radius + ChaseDistance + web.RangeMax), Vector2.Distance(panel4, point), "a pulled Xanx reaches panel 4");
        }

        /// <summary>
        /// Ashen_fix_f rolls back: Down puts set 10's boss respawns back (500, exact), takes out the new pool, its marker and fix
        /// F's evidence rows; Up again, fix F's are back. A second Up (its SQL run again) changes nothing; a boss pool holding
        /// another respawn is not touched by Up or Down. Nothing else changes ("others" from a database migrated only up to fix F).
        /// </summary>
        [TestMethod]
        public void FixFRollsBackWithItsMigration()
        {
            // At its migration (a later set's rows, set 14's on, are not this one's "nothing else"), as the set 12 and 13 tests;
            // fix G (later too) is left out of "others" below as well, so the comparison holds at any later head.
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixF);
            var world = harness.World;
            uint Respawn(uint pool) => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool).RespawnTime;
            bool HasPool() => world.SpawnPoolEntries.AsNoTracking().Any(entry => entry.Id == X.AttaPool);
            bool HasMarker() => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.MissionId == Jewelry.MissionId &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.MarkerObjective && entry.IndicatorId == X.MarkerIndicator);
            int Evidence() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.SourceUri == X.Source);
            string Scene() => world.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == AshenGasShortagesV1.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
            var bossPools = X.BossPools.Select(pool => pool.Pool).ToArray();
            // Fix G (sorts after fix F) changes the Atta pools' counts, set 13's NPC pools and 1281's / 1282's markers: left out.
            var laterPools = AshenFixGV1.AttaPools.Select(pool => pool.Pool).Append(AshenIndraPassNpcsV1.Odesha).Append(AshenIndraPassNpcsV1.Endala).ToArray();
            var laterMarkers = new[] { AshenGodOfStormsV1.MissionId, AshenBondsOfBloodV1.MissionId };
            string Others(Rasa.Context.World.SqliteWorldContext db) => string.Join("; ", db.SpawnPoolEntries.AsNoTracking()
                    .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != X.AttaPool && !laterPools.Contains(entry.Id)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Creature1Id},{entry.Creature1MaxCount},{(bossPools.Contains(entry.Id) ? 0 : entry.RespawnTime)}")))
                + " | " + string.Join("; ", db.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Where(entry => !(entry.MissionId == Jewelry.MissionId && entry.IndicatorId == X.MarkerIndicator) && !laterMarkers.Contains(entry.MissionId))
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.MissionEvidenceEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.SourceUri != X.Source && entry.SourceUri != AshenFixGV1.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.EvidenceId).AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.EvidenceId}"));
            void Fixed(string when)
            {
                foreach (var pool in bossPools)
                    Assert.AreEqual(X.BossRespawnTenths, Respawn(pool), $"{when}: boss pool {pool} 3000");
                Assert.IsTrue(HasPool() && HasMarker(), $"{when}: the carapace pool and its marker");
                Assert.AreEqual(X.Evidences.Length, Evidence(), $"{when}: fix F's evidence rows");
                Assert.AreEqual(X.SceneNow(), Scene(), $"{when}: 1356's scene with the V02 pumps");
            }
            string others;
            using (var atFixF = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixF))
                others = Others(atFixF.World);
            Fixed("at its migration");
            Assert.AreEqual(others, Others(world), "a database migrated only to fix F holds the same other rows");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixF));
            world.ChangeTracker.Clear();
            foreach (var pool in bossPools)
                Assert.AreEqual(X.BossRespawnBefore, Respawn(pool), $"boss pool {pool}: set 10's 500, exact");
            Assert.IsFalse(HasPool() || HasMarker(), "no carapace pool, no marker");
            Assert.AreEqual(0, Evidence(), "no fix F evidence");
            Assert.AreEqual(X.SceneBefore(), Scene(), "set 11's scene, exact");
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.Previous(world, FixF)))
                Assert.AreEqual(fresh.World.Set<MissionSceneBindingEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == AshenGasShortagesV1.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings, Scene(),
                    "the Down's scene is a database's migrated only to before fix F");
            Assert.AreEqual(others, Others(world), "nothing else changes");

            world.GetService<IMigrator>().Migrate(FixF);
            world.ChangeTracker.Clear();
            Fixed("Up again");
            // A second Up: the respawn UPDATEs match nothing now, the guarded pool INSERT adds nothing.
            foreach (var sql in X.UpSql())
                world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            world.ChangeTracker.Clear();
            Fixed("a second Up");
            // The guard: a boss pool holding neither 500 nor 3000 is left alone both ways.
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET respown_time = 777 WHERE id = {D.SallyPool};");
            foreach (var sql in X.DownSql().Concat(X.UpSql()).Where(sql => sql.StartsWith("UPDATE spawnpool")))
                world.Database.ExecuteSqlRaw(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(777U, Respawn(D.SallyPool), "a pool at another respawn is not touched");
            Assert.AreEqual(X.BossRespawnTenths, Respawn(D.MollyPool), "the others: Down then Up");
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET respown_time = {X.BossRespawnTenths} WHERE id = {D.SallyPool};");
            // A scene holding neither set 11's JSON nor fix F's is left alone both ways (the JSON's braces doubled for ExecuteSqlRaw).
            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            Run($"UPDATE mission_scene_binding SET bindings = '{{}}' WHERE mission_id = {AshenGasShortagesV1.MissionId} AND content_revision = '{AshenMissionDataV1.Revision}';");
            foreach (var sql in X.DownSql().Concat(X.UpSql()).Where(sql => sql.StartsWith("UPDATE mission_scene_binding")))
                Run(sql);
            Assert.AreEqual("{}", Scene(), "a scene holding another value is not touched");
            Run($"UPDATE mission_scene_binding SET bindings = '{X.SceneNow()}' WHERE mission_id = {AshenGasShortagesV1.MissionId} AND content_revision = '{AshenMissionDataV1.Revision}';");
            world.ChangeTracker.Clear();
            Fixed("restored");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }

        private static uint[] Creatures(SpawnPoolEntry pool) =>
            new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id, pool.Creature5Id, pool.Creature6Id }.Where(id => id != 0).ToArray();

        /// <summary>The World DB's creatures with the Atta harvester warcry (477) among their actions.</summary>
        internal static HashSet<uint> WarcryCreatures(WildernessRuntimeTestHarness harness)
        {
            var cries = harness.World.CreatureActionEntries.AsNoTracking().Where(entry => entry.ActionId == (uint)CreatureBuffs.HarvesterWarcry).Select(entry => entry.Id).ToHashSet();
            return harness.World.CreatureEntries.AsNoTracking().AsEnumerable()
                .Where(row => new[] { row.Action1, row.Action2, row.Action3, row.Action4, row.Action5, row.Action6, row.Action7, row.Action8 }.Any(cries.Contains))
                .Select(row => row.Id).ToHashSet();
        }

        private static float ToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var k = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0, 1);
            return Vector2.Distance(p, a + ab * k);
        }
    }
}
