using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Crucible
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions.Crucible;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Ligo Crucible set 14, the Crucible creature tints (Crucible_creature_tints, CrucibleCreatureTintsV1): the 52 rows of
    /// the plan (tools/mission-crossref/mob-tints-plan-crucible.csv) written as listed, each on the creature row of its class
    /// and level, kept apart from the Valverde rows, given to the creatures as they spawn, and taken back by the migration's
    /// rollback. The mechanism (creature_tint, TintAtSpawn, BodyAttributes) is CreatureTintRowTests'. Up and Down twice:
    /// CrucibleGuardTests.EachCrucibleMigrationRunsUpTwiceAndDownTwice (every _Crucible_ migration) and the edited-row case here.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleCreatureTintsTests
    {
        private const string Data = "20271108002100_Crucible_creature_tints";
        private const string IncurablesWardWings = "20271108002000_Crucible_incurables_ward_wings";

        /// <summary>(10, 200, 30), alpha 255, as Color.Hue packs it: A B G R.</summary>
        private const uint Green = 0xFF1EC80A;

        private const uint ThraxRed = 0xFF00086Eu, OfficerWhite = 0xFFFFFFFFu, EmberOrange = 0xFF002882u, SkeevGreen = 0xFF143C00u;

        [TestMethod]
        public void TheCrucibleTintsAreWrittenAsListed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = Rows(harness.World);
            var tints = CrucibleCreatureTintsV1.Rows;

            // Pinned from the plan (mob-tints-plan-crucible.csv, 2026-11-08): 52 creatures (32 of the Crucible maps, 20 new rows
            // of sets 1-13), 36 of them stock (7 of them on models with no Hue shader); 7 officers white (255,255,255), 6 Thrax red (110,8,0), Ember (130,40,0), Skeev twice (0,60,20).
            Assert.HasCount(52, tints);
            Assert.AreEqual(52, tints.Select(row => row.CreatureId).Distinct().Count());
            Assert.AreEqual(20, tints.Count(row => row.CreatureId >= 770000));
            Assert.AreEqual(36, tints.Count(row => row.Hue == null));
            Assert.AreEqual(7, tints.Count(row => row.Hue == OfficerWhite));
            Assert.AreEqual(6, tints.Count(row => row.Hue == ThraxRed));
            Assert.AreEqual(1, tints.Count(row => row.Hue == EmberOrange));
            Assert.AreEqual(2, tints.Count(row => row.Hue == SkeevGreen));
            Assert.AreEqual(EmberOrange, CrucibleCreatureTintsV1.Rgb(130, 40, 0), "(130, 40, 0), alpha 255: A B G R");
            Assert.AreEqual(SkeevGreen, CrucibleCreatureTintsV1.Rgb(0, 60, 20), "(0, 60, 20), alpha 255: A B G R");

            Assert.AreEqual(OfficerWhite, rows[520013].Hue, "Executioner Derge, officer white-grey");
            Assert.AreEqual(OfficerWhite, rows[520014].Hue, "Thrax Commander L47, officer white-grey");
            Assert.AreEqual(ThraxRed, rows[531018].Hue, "Thrax Soldier Mark IX, Thrax red");
            Assert.AreEqual(ThraxRed, rows[531019].Hue, "Thrax Grenadier L42, Thrax red");
            Assert.AreEqual(ThraxRed, rows[531020].Hue, "Thrax Technician Major, Thrax red");
            Assert.AreEqual(ThraxRed, rows[678013].Hue, "Rebel Converted Thrax, the family red");
            Assert.AreEqual(ThraxRed, rows[678016].Hue, "Rebel Pacified Thrax, the family red");
            Assert.AreEqual(EmberOrange, rows[678007].Hue, "Ember, orange-red");
            foreach (var officer in new uint[] { 770023, 770031, 770032, 770033, 770042 })
                Assert.AreEqual(OfficerWhite, rows[officer].Hue, $"creature {officer}: Molitor, Krubb, Mungrel, Sedge, Ugoretz: officer white-grey");
            Assert.AreEqual(ThraxRed, rows[770036].Hue, "the Junkyard Thrax Technician, the 531020 family red");
            Assert.AreEqual(SkeevGreen, rows[770061].Hue, "Skeev (talks), dark green");
            Assert.AreEqual(SkeevGreen, rows[770062].Hue, "Skeev (fights), dark green");
            foreach (var stock in new uint[] { 531021, 531022, 540030, 551013, 592014, 531023, 678002, 678003, 678004, 678005, 678006,
                         678010, 678011, 678012, 678014, 678015, 678020, 678021, 678022, 678023, 678024, 678032, 678030, 678031,
                         770005, 770007, 770008, 770015, 770038, 770041, 770014, 770022, 770047, 770048, 770049, 770050 })
                Assert.IsNull(rows[stock].Hue, $"creature {stock}: stock");

            foreach (var row in tints)
            {
                Assert.IsTrue(rows.ContainsKey(row.CreatureId), $"creature {row.CreatureId} has its tint row");
                Assert.AreEqual(row.Hue, rows[row.CreatureId].Hue, $"creature {row.CreatureId} hue");
                Assert.AreEqual(row.Hue, rows[row.CreatureId].Hue2, $"creature {row.CreatureId} hue2");
            }
            Assert.HasCount(ValverdeCreatureTints.Rows.Count + tints.Count, rows, "the Valverde rows and the Crucible rows, no other");
        }

        [TestMethod]
        public void EachTintIsOnTheCreatureRowOfItsClassAndLevelAndKeepsItsSources()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ids = CrucibleCreatureTintsV1.Rows.Select(row => row.CreatureId).ToList();
            var creatures = harness.World.CreatureEntries.AsNoTracking().Where(row => ids.Contains(row.Id)).ToDictionary(row => row.Id);
            var valverde = ValverdeCreatureTints.Rows.Select(row => row.CreatureId).ToHashSet();

            foreach (var row in CrucibleCreatureTintsV1.Rows)
            {
                Assert.IsTrue(creatures.TryGetValue(row.CreatureId, out var creature), $"{row.Name} ({row.CreatureId}) is a creature row");
                Assert.AreEqual(row.ClassId, creature.ClassId, $"{row.Name} ({row.CreatureId}): the class");
                Assert.AreEqual(row.Level, creature.Level, $"{row.Name} ({row.CreatureId}): the level");
                Assert.AreNotEqual(1u, creature.Faction, $"{row.Name} ({row.CreatureId}) is not FRIENDLY (a FRIENDLY one has no tint already)");
                Assert.IsFalse(valverde.Contains(row.CreatureId), $"{row.Name} ({row.CreatureId}) has no Valverde row");
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.Why), $"{row.Name}: its sources");
                Assert.IsTrue(row.Estimate.StartsWith("ESTIMATE: ") || row.Estimate == "no: a tint has no effect",
                    $"{row.Name}: what is guessed ({row.Estimate})");
            }
            Assert.AreEqual(7, CrucibleCreatureTintsV1.Rows.Count(row => !row.Estimate.StartsWith("ESTIMATE")),
                "only the models with no Hue shader are not guessed: the turret, the mortar, the Shield Drone and the 4 Incurables");
        }

        /// <summary>
        /// Every new Crucible creature row of sets 1-13 (770000 up to CrucibleHybridArcNpcsV1.NextFreeId) that is not FRIENDLY has a
        /// tint row or is left out with its reason (CrucibleCreatureTintsV1.LeftOut); a FRIENDLY one has neither (no tint already).
        /// </summary>
        [TestMethod]
        public void EachNewCrucibleCreatureHasATintRowOrIsLeftOutWithItsReason()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var tinted = CrucibleCreatureTintsV1.Rows.Select(row => row.CreatureId).ToHashSet();
            var leftOut = CrucibleCreatureTintsV1.LeftOut;
            var creatures = harness.World.CreatureEntries.AsNoTracking()
                .Where(row => row.Id >= 770000 && row.Id < CrucibleHybridArcNpcsV1.NextFreeId).ToList();

            Assert.IsNotEmpty(creatures, "the new Crucible creature rows");
            foreach (var creature in creatures)
            {
                if (creature.Faction == 1)
                {
                    Assert.IsFalse(tinted.Contains(creature.Id) || leftOut.ContainsKey(creature.Id), $"{creature.Id} is FRIENDLY: no row needed");
                    continue;
                }
                Assert.IsTrue(tinted.Contains(creature.Id) ^ leftOut.ContainsKey(creature.Id),
                    $"{creature.Id} ({creature.Comment}): a tint row or a reason to leave it out, not both");
            }
            CollectionAssert.AreEquivalent(new uint[] { 770034, 770035 }, leftOut.Keys.ToList(), "Kalamit and Lamontix: no image");
            Assert.IsTrue(leftOut.Values.All(reason => !string.IsNullOrWhiteSpace(reason)));
            Assert.IsTrue(leftOut.Keys.All(id => creatures.Any(creature => creature.Id == id)), "each left-out id is a creature row");
        }

        [TestMethod]
        public void TheCrucibleCreaturesSpawnWithTheirTints()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var templates = CreatureManager.Instance.LoadedCreatures;

            Assert.AreEqual(((uint?)ThraxRed, (uint?)ThraxRed), templates[531018].RowTint);
            Assert.AreEqual(((uint?)OfficerWhite, (uint?)OfficerWhite), templates[520014].RowTint);
            Assert.AreEqual(((uint?)null, (uint?)null), templates[678002].RowTint, "Rebel Leader Salyard: stock");

            var soldier = CreatureManager.Instance.CreateCreature(531018, null);
            Assert.AreEqual(ThraxRed, soldier.Hue.Hue);
            Assert.AreEqual(ThraxRed, soldier.Hue2.Hue);

            var pacified = CreatureManager.Instance.CreateCreature(678016, null);
            Assert.AreEqual(ThraxRed, pacified.Hue.Hue, "a neutral (faction 3) Thrax has its row's red too");

            var salyard = CreatureManager.Instance.CreateCreature(678002, null);
            Assert.IsNull(salyard.Hue, "a neutral human NPC: stock colours, no random tint on the skin");
            Assert.IsNull(salyard.Hue2);
        }

        [TestMethod]
        public void TheMigrationFollowsSetElevenBAndItsRollbackTakesOnlyItsRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            var all = Rows(world);

            Assert.AreEqual(IncurablesWardWings, Previous(world, Data), "set 14 follows set 11b");

            migrator.Migrate(IncurablesWardWings);
            var before = Rows(world);
            Assert.HasCount(ValverdeCreatureTints.Rows.Count, before, "the Valverde rows stay");
            Assert.IsFalse(CrucibleCreatureTintsV1.Rows.Any(row => before.ContainsKey(row.CreatureId)), "no Crucible row is left");

            // Up again (the second Up is in UpAndDownAreEachSafeTwiceAndDownLeavesAnEditedRow).
            migrator.Migrate(Data);
            CollectionAssert.AreEquivalent(all.ToList(), Rows(world).ToList());
        }

        [TestMethod]
        public void UpAndDownAreEachSafeTwiceAndDownLeavesAnEditedRow()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            var before = Rows(world);

            // Up again: nothing more.
            Run(world, CrucibleCreatureTintsV1.Up());
            CollectionAssert.AreEquivalent(before.ToList(), Rows(world).ToList());

            // A GM recolours a tinted and a stock row; Down takes only the rows still as Up wrote them, twice.
            world.Database.ExecuteSqlRaw($"UPDATE {CreatureTintEntry.TableName} SET hue = {Green} WHERE id = 531018;");
            world.Database.ExecuteSqlRaw($"UPDATE {CreatureTintEntry.TableName} SET hue = {Green} WHERE id = 678002;");

            migrator.Migrate(Previous(world, Data));
            Run(world, CrucibleCreatureTintsV1.Down());

            var left = Rows(world);
            Assert.HasCount(ValverdeCreatureTints.Rows.Count + 2, left);
            Assert.AreEqual(Green, left[531018].Hue);
            Assert.AreEqual(Green, left[678002].Hue);

            // Up again keeps the GM's rows as they are and puts back the rest.
            migrator.Migrate(Data);
            Run(world, CrucibleCreatureTintsV1.Up());

            var after = Rows(world);
            Assert.HasCount(before.Count, after);
            Assert.AreEqual(Green, after[531018].Hue);
            Assert.AreEqual(Green, after[678002].Hue);
            Assert.AreEqual(before[678007], after[678007]);
        }

        /// <summary>The migration directly before <paramref name="migration"/>, read from the assembly.</summary>
        private static string Previous(DbContext context, string migration)
        {
            var ids = context.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, "the migration is there, after another");
            return ids[index - 1];
        }

        private static Dictionary<uint, (uint? Hue, uint? Hue2)> Rows(DbContext world) =>
            world.Set<CreatureTintEntry>().AsNoTracking().ToList().ToDictionary(row => row.Id, row => (row.Hue, row.Hue2));

        private static void Run(DbContext world, IEnumerable<string> statements)
        {
            foreach (var sql in statements)
                world.Database.ExecuteSqlRaw(sql);
        }
    }
}
