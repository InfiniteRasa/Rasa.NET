using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Ashen
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Services.Preloader.Missions.Ashen;
    using Rasa.Services.Preloader.Missions.Crucible;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Ligo Ashen Desert set 21, the Ashen creature tints (Ashen_creature_tints, AshenCreatureTintsV1): the 50 rows (the 21 of
    /// the Ashen maps, NOTES-mob-skins.md 10.2, and 29 new rows of sets 1-20) written as listed, each on the creature row of its
    /// class and level, kept apart from the Valverde and Crucible rows, given to the creatures as they spawn, and taken back by
    /// the migration's rollback. The mechanism is CreatureTintRowTests'. Up and Down twice: AshenGuardTests.EachAshenMigrationRunsUpTwiceAndDownTwice
    /// (every _Ashen_ migration) and the edited-row case here.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenCreatureTintsTests
    {
        private const string Data = "20271110002100_Ashen_creature_tints";

        /// <summary>(10, 200, 30), alpha 255, as Color.Hue packs it: A B G R.</summary>
        private const uint Green = 0xFF1EC80A;

        private const uint ThraxRed = 0xFF00086Eu, OfficerWhite = 0xFFFFFFFFu;

        private static readonly uint[] Officers = { 520003, 520004, 520005, 520006, 520007, 531012, 790523, 790563 };
        private static readonly uint[] Red = { 531008, 531009, 531010, 790657 };
        private static readonly uint[] NoHueShader = { 531013, 551010, 592011, 790081, 790082, 790083, 790084, 790160, 790161, 790162, 790163 };
        private static readonly uint[] StockWithHue =
        {
            520002, 531011, 531014, 531015, 531016, 531017, 540018, 540019, 540020,
            790043, 790044, 790045, 790646, 790662, 790360, 790664, 790666, 790080, 790121, 790240, 790241, 790362, 790367, 790373,
            790654, 790655, 790656,
        };

        [TestMethod]
        public void TheAshenTintsAreWrittenAsListed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = Rows(harness.World);
            var tints = AshenCreatureTintsV1.Rows;

            // Pinned from the plan (NOTES-mob-skins.md 10.2 and 10.8, 2026-11-12): 50 creatures (21 of the Ashen maps, 29 new rows
            // of sets 1-20), 38 of them stock (11 of them on models with no Hue shader); 8 officers white (255,255,255), 4 Thrax red (110,8,0).
            Assert.HasCount(50, tints);
            Assert.AreEqual(50, tints.Select(row => row.CreatureId).Distinct().Count());
            Assert.AreEqual(29, tints.Count(row => row.CreatureId >= 790000));
            Assert.AreEqual(38, tints.Count(row => row.Hue == null));
            Assert.AreEqual(8, tints.Count(row => row.Hue == OfficerWhite));
            Assert.AreEqual(4, tints.Count(row => row.Hue == ThraxRed));
            Assert.AreEqual(ThraxRed, AshenCreatureTintsV1.Rgb(110, 8, 0), "(110, 8, 0), alpha 255: A B G R");
            Assert.AreEqual(OfficerWhite, AshenCreatureTintsV1.Rgb(255, 255, 255));

            foreach (var officer in Officers)
                Assert.AreEqual(OfficerWhite, rows[officer].Hue, $"creature {officer}: the Overseers, the Commander, Jirex, Kuutk: officer white-grey");
            foreach (var thrax in Red)
                Assert.AreEqual(ThraxRed, rows[thrax].Hue, $"creature {thrax}: Mark VI, Ravager, Technician Lieutenant, Tunneler: Thrax red");
            foreach (var stock in NoHueShader.Concat(StockWithHue))
                Assert.IsNull(rows[stock].Hue, $"creature {stock}: stock");
            Assert.HasCount(50, Officers.Concat(Red).Concat(NoHueShader).Concat(StockWithHue).Distinct().ToList(), "every row pinned once");

            foreach (var row in tints)
            {
                Assert.IsTrue(rows.ContainsKey(row.CreatureId), $"creature {row.CreatureId} has its tint row");
                Assert.AreEqual(row.Hue, rows[row.CreatureId].Hue, $"creature {row.CreatureId} hue");
                Assert.AreEqual(row.Hue, rows[row.CreatureId].Hue2, $"creature {row.CreatureId} hue2");
            }
            Assert.HasCount(ValverdeCreatureTints.Rows.Count + CrucibleCreatureTintsV1.Rows.Count + tints.Count, rows,
                "the Valverde, Crucible and Ashen rows, no other");
        }

        [TestMethod]
        public void EachTintIsOnTheCreatureRowOfItsClassAndLevelAndKeepsItsSources()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ids = AshenCreatureTintsV1.Rows.Select(row => row.CreatureId).ToList();
            var creatures = harness.World.CreatureEntries.AsNoTracking().Where(row => ids.Contains(row.Id)).ToDictionary(row => row.Id);
            var earlier = ValverdeCreatureTints.Rows.Select(row => row.CreatureId)
                .Concat(CrucibleCreatureTintsV1.Rows.Select(row => row.CreatureId)).ToHashSet();

            foreach (var row in AshenCreatureTintsV1.Rows)
            {
                Assert.IsTrue(creatures.TryGetValue(row.CreatureId, out var creature), $"{row.Name} ({row.CreatureId}) is a creature row");
                Assert.AreEqual(row.ClassId, creature.ClassId, $"{row.Name} ({row.CreatureId}): the class");
                Assert.AreEqual(row.Level, creature.Level, $"{row.Name} ({row.CreatureId}): the level");
                Assert.AreNotEqual(1u, creature.Faction, $"{row.Name} ({row.CreatureId}) is not FRIENDLY (a FRIENDLY one has no tint already)");
                Assert.IsFalse(earlier.Contains(row.CreatureId), $"{row.Name} ({row.CreatureId}) has no Valverde or Crucible row");
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.Why), $"{row.Name}: its sources");
                Assert.IsTrue(row.Estimate.StartsWith("ESTIMATE: ") || row.Estimate == "no: a tint has no effect",
                    $"{row.Name}: what is guessed ({row.Estimate})");
            }
            Assert.AreEqual(11, AshenCreatureTintsV1.Rows.Count(row => !row.Estimate.StartsWith("ESTIMATE")),
                "only the models with no Hue shader are not guessed: the Shield Drone, the turret, the mortar and its 8 copies");
            CollectionAssert.AreEquivalent(NoHueShader, AshenCreatureTintsV1.Rows.Where(row => !row.Estimate.StartsWith("ESTIMATE"))
                .Select(row => row.CreatureId).ToList());
        }

        /// <summary>
        /// Every new Ashen creature row of sets 1-20 (790000-799999) that is not FRIENDLY has a tint row or is left out with its
        /// reason (AshenCreatureTintsV1.LeftOut); a FRIENDLY one has neither (no tint already).
        /// </summary>
        [TestMethod]
        public void EachNewAshenCreatureHasATintRowOrIsLeftOutWithItsReason()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var tinted = AshenCreatureTintsV1.Rows.Select(row => row.CreatureId).ToHashSet();
            var leftOut = AshenCreatureTintsV1.LeftOut;
            var creatures = harness.World.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790000 && row.Id < 800000).ToList();

            Assert.IsNotEmpty(creatures, "the new Ashen creature rows");
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
            Assert.AreEqual(33, creatures.Count(creature => creature.Faction != 1), "the hostile 790xxx rows of sets 1-20: 29 tinted, 4 left out");
            CollectionAssert.AreEquivalent(new uint[] { 790365, 790371, 790376, 790602 }, leftOut.Keys.ToList(),
                "Molly, Trudy, Wendy, Overlord Kretas: no image");
            Assert.IsTrue(leftOut.Values.All(reason => !string.IsNullOrWhiteSpace(reason)));
            Assert.IsTrue(leftOut.Keys.All(id => creatures.Any(creature => creature.Id == id && creature.Faction != 1)), "each left-out id is a hostile creature row");
        }

        [TestMethod]
        public void TheAshenCreaturesSpawnWithTheirTints()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var templates = CreatureManager.Instance.LoadedCreatures;

            Assert.AreEqual(((uint?)ThraxRed, (uint?)ThraxRed), templates[531008].RowTint);
            Assert.AreEqual(((uint?)OfficerWhite, (uint?)OfficerWhite), templates[520005].RowTint);
            Assert.AreEqual(((uint?)null, (uint?)null), templates[790240].RowTint, "Nitroglazer: stock");
            Assert.IsNull(templates[790365].RowTint, "Molly: left out, no row");

            var soldier = CreatureManager.Instance.CreateCreature(531008, null);
            Assert.AreEqual(ThraxRed, soldier.Hue.Hue);
            Assert.AreEqual(ThraxRed, soldier.Hue2.Hue);

            var jirex = CreatureManager.Instance.CreateCreature(790523, null);
            Assert.AreEqual(OfficerWhite, jirex.Hue.Hue, "a set 14 Overseer has the officer white");

            var queen = CreatureManager.Instance.CreateCreature(790664, null);
            Assert.IsNull(queen.Hue, "the Atta Hive Queen: stock colours, no random tint");
            Assert.IsNull(queen.Hue2);

            var molly = CreatureManager.Instance.CreateCreature(790365, null);
            Assert.IsNotNull(molly.Hue, "a left-out boss: two random tints, as before");
            Assert.IsNotNull(molly.Hue2);
        }

        [TestMethod]
        public void TheMigrationFollowsSetTwentyAndItsRollbackTakesOnlyItsRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            var all = Rows(world);

            Assert.AreEqual(AshenMissionTests.ConscriptionQueen, Previous(world, Data), "set 21 follows set 20");

            migrator.Migrate(AshenMissionTests.ConscriptionQueen);
            var before = Rows(world);
            Assert.HasCount(ValverdeCreatureTints.Rows.Count + CrucibleCreatureTintsV1.Rows.Count, before, "the Valverde and Crucible rows stay");
            Assert.IsFalse(AshenCreatureTintsV1.Rows.Any(row => before.ContainsKey(row.CreatureId)), "no Ashen row is left");

            migrator.Migrate(Data);
            CollectionAssert.AreEquivalent(all.ToList(), Rows(world).ToList());
            migrator.Migrate();
        }

        [TestMethod]
        public void UpAndDownAreEachSafeTwiceAndDownLeavesAnEditedRow()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            var before = Rows(world);

            // Up again: nothing more.
            Run(world, AshenCreatureTintsV1.Up());
            CollectionAssert.AreEquivalent(before.ToList(), Rows(world).ToList());

            // A GM recolours a tinted and a stock row; Down takes only the rows still as Up wrote them, twice.
            world.Database.ExecuteSqlRaw($"UPDATE {CreatureTintEntry.TableName} SET hue = {Green} WHERE id = 531008;");
            world.Database.ExecuteSqlRaw($"UPDATE {CreatureTintEntry.TableName} SET hue = {Green} WHERE id = 790664;");

            migrator.Migrate(Previous(world, Data));
            Run(world, AshenCreatureTintsV1.Down());

            var left = Rows(world);
            Assert.HasCount(ValverdeCreatureTints.Rows.Count + CrucibleCreatureTintsV1.Rows.Count + 2, left);
            Assert.AreEqual(Green, left[531008].Hue);
            Assert.AreEqual(Green, left[790664].Hue);

            // Up again keeps the GM's rows as they are and puts back the rest.
            migrator.Migrate(Data);
            Run(world, AshenCreatureTintsV1.Up());

            var after = Rows(world);
            Assert.HasCount(before.Count, after);
            Assert.AreEqual(Green, after[531008].Hue);
            Assert.AreEqual(Green, after[790664].Hue);
            Assert.AreEqual(before[520005], after[520005]);
            migrator.Migrate();
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
