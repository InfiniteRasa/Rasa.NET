using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;
using Rasa.Test.TestSupport;

namespace Rasa.Test.Missions.Ashen
{
    using W = AshenFixWV1;

    /// <summary>
    /// Ashen fix job W (AshenFixWV1, migration 20271110002200): Wendy (790376), the Atta Hive Queen (790664) and Enslaver Xahn
    /// (790666) attack with rows their client skeletons animate (790243 470/1, 790244 174/25, 790245 1/241), and 1499's scenarios
    /// 6 and 7 have names that fit MySQL's 64 characters. CreatureAttackAnimationTests checks every creature's attacks against
    /// the client; this class pins the rows, the guards and the rollback. [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixWTests
    {
        private const string FixW = "20271110002200_Ashen_fix_w";
        private const string FixV2 = "20271110002190_Ashen_fix_v2";

        private static uint[] Actions(CreatureEntry row) =>
            new[] { row.Action1, row.Action2, row.Action3, row.Action4, row.Action5, row.Action6, row.Action7, row.Action8 };

        private static string Bosses(WorldContext world)
        {
            var creatures = world.CreatureEntries.AsNoTracking().Where(row => row.Id == W.Wendy || row.Id == W.Queen || row.Id == W.Xahn)
                .OrderBy(row => row.Id).ToList();
            var rows = world.CreatureActionEntries.AsNoTracking().Count(row => row.Id >= W.WendySpit && row.Id <= W.XahnWeapon);
            return string.Join("; ", creatures.Select(row => $"{row.Id}: {string.Join(" ", Actions(row))}")) + $" | rows {rows}";
        }

        private static string Names(WorldContext world) => string.Join(" | ", world.Set<MissionScenarioEntry>().AsNoTracking()
            .Where(row => row.MissionId == AshenIntoTheLionsDenV1.MissionId && row.ContentRevision == AshenMissionDataV1.Revision &&
                          (row.ScenarioId == AshenIntoTheLionsDenV1.PitSaved || row.ScenarioId == AshenIntoTheLionsDenV1.CellsOpen))
            .OrderBy(row => row.ScenarioId).AsEnumerable().Select(row => $"{row.Name} / {row.Comment}"));

        private static string Now => $"{W.Wendy}: 55048 {W.WendySpit} 0 0 0 0 0 0; {W.Queen}: {W.QueenClaw} 54016 0 0 0 0 0 0; " +
                                     $"{W.Xahn}: {W.XahnWeapon} 0 0 0 0 0 0 0 | rows 3";
        private const string Before = "790376: 55048 55049 0 0 0 0 0 0; 790664: 54014 54015 54016 0 0 0 0 0; 790666: 34 57008 61003 61004 68003 68004 0 0 | rows 0";
        private static string Short => $"{W.PitSavedNow} / {W.PitSavedNow} | {W.CellsOpenNow} / {W.CellsOpenNow}";
        private static string Long => $"{W.PitSavedBefore} / {W.PitSavedBefore} | {W.CellsOpenBefore} / {W.CellsOpenBefore}";

        /// <summary>
        /// The three bosses' actions and the three new rows, written out (the numbers ESTIMATE, copied from 55049, 54014 and row 34);
        /// each slot's action plays only families the boss's skeleton animates (client actionArguments and animationFamilyData);
        /// 1499's two names fit 64 characters.
        /// </summary>
        [TestMethod]
        public void TheBossesAttackWithRowsTheirModelsAnimate()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            Assert.AreEqual(Now, Bosses(world));
            Assert.AreEqual("790376: 55048 790243 0 0 0 0 0 0; 790664: 790244 54016 0 0 0 0 0 0; 790666: 790245 0 0 0 0 0 0 0 | rows 3", Bosses(world),
                "the actions, written out");

            var rows = world.CreatureActionEntries.AsNoTracking().Where(row => row.Id >= W.WendySpit && row.Id <= W.XahnWeapon)
                .OrderBy(row => row.Id).AsEnumerable()
                .Select(row => System.FormattableString.Invariant(
                    $"{row.Id} '{row.Description}' {row.ActionId}/{row.ActionArgId} {row.RangeMin}-{row.RangeMax} m cd {row.Cooldown} windup {row.Windup} {row.MinDamage}-{row.MaxDamage} type {row.DamageType}"))
                .ToList();
            CollectionAssert.AreEqual(new[]
            {
                "790243 'Wendy Ashen Desert granitour_v2_spit 470/1' 470/1 1-20 m cd 4000 windup 466 332-550 type 2",
                "790244 'Atta Hive Queen claw 174/25' 174/25 0.5-4 m cd 1000 windup 0 1037-2075 type 1",
                "790245 'Enslaver Xahn weapon 1/241' 1/241 0.5-30 m cd 800 windup 0 15-25 type 1",
            }, rows, "the three rows (the numbers ESTIMATE: 55049's, 54014's, row 34's)");

            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var actions = world.CreatureActionEntries.AsNoTracking().ToDictionary(row => row.Id);
            var meshes = world.EntityClassEntries.AsNoTracking().ToDictionary(row => row.Id, row => row.MeshId);
            var levels = world.ActionLevelEntries.AsNoTracking().ToList().GroupBy(row => (row.ActionId, row.Level))
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var (boss, cls, mesh, skeleton) in new[] { (W.Wendy, 25620u, 39866u, 14024u), (W.Queen, 24087u, 17409u, 13916u), (W.Xahn, 11134u, 28518u, 13989u) })
            {
                Assert.AreEqual((cls, mesh, skeleton), (creatures[boss].ClassId, meshes[creatures[boss].ClassId], ClientAnimationSkeletons.MeshSkeleton[mesh]), $"{boss}'s model");
                var families = new List<uint>();
                foreach (var slot in Actions(creatures[boss]).Where(id => id != 0))
                {
                    var level = levels[(actions[slot].ActionId, actions[slot].ActionArgId)];
                    foreach (var family in new[] { level.WindupAnimFamilyId, level.RecoveryAnimFamilyId })
                        if (family is uint id)
                            families.Add(id);
                }
                Assert.IsNotEmpty(families, $"{boss} plays an animation");
                Assert.IsTrue(families.All(ClientAnimationSkeletons.SkeletonFamilies[skeleton].Contains),
                    $"{boss}: families {string.Join(" ", families)} on skeleton {skeleton}");
            }

            Assert.AreEqual(Short, Names(world), "1499's names: the short ones");
            Assert.IsTrue(new[] { W.PitSavedNow, W.CellsOpenNow }.All(name => name.Length <= 64), "64 characters at most");
            Assert.AreEqual(0, world.Set<MissionScenarioEntry>().AsNoTracking().Count(row => row.Name.Length > 64 || row.Comment.Length > 64),
                "no scenario name or comment over 64 characters");
        }

        /// <summary>
        /// Fix W rolled back and forth, baselined at its own migration: Down puts back the bosses' old actions and deletes the three
        /// rows, as a database migrated only to fix V2 holds them; the short names stay (set 20 writes them now). A database that ran
        /// set 20 with the long names (the live one) gets the short ones on Up. Nothing else changes; a second Down and a second Up
        /// change nothing.
        /// </summary>
        [TestMethod]
        public void FixWRollsBackToFixV2()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixW);
            var world = harness.World;
            string Others(WorldContext db) =>
                string.Join("; ", db.CreatureEntries.AsNoTracking().Where(row => row.Id != W.Wendy && row.Id != W.Queen && row.Id != W.Xahn)
                    .OrderBy(row => row.Id).AsEnumerable().Select(row => $"{row.Id}:{string.Join(",", Actions(row))}"))
                + " | " + string.Join("; ", db.CreatureActionEntries.AsNoTracking().Where(row => row.Id < W.WendySpit || row.Id > W.XahnWeapon)
                    .OrderBy(row => row.Id).AsEnumerable().Select(row => $"{row.Id}:{row.ActionId}/{row.ActionArgId}:{row.MinDamage}-{row.MaxDamage}"))
                + " | " + string.Join("; ", db.Set<MissionScenarioEntry>().AsNoTracking().OrderBy(row => row.MissionId).ThenBy(row => row.ContentRevision)
                    .ThenBy(row => row.ScenarioId).AsEnumerable().Select(row => $"{row.MissionId}/{row.ScenarioId}:{row.Name}/{row.Comment}"));
            var others = Others(world);
            Assert.AreEqual(Now, Bosses(world), "at fix W");
            Assert.AreEqual(Short, Names(world), "at fix W");

            var before = AshenMissionTests.Previous(world, FixW);
            Assert.AreEqual(FixV2, before, "after fix V2");
            var migrator = world.GetService<IMigrator>();
            migrator.Migrate(before);
            world.ChangeTracker.Clear();
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
            {
                Assert.AreEqual(Bosses(fresh.World), Bosses(world), "Down: as a database migrated only to fix V2");
                Assert.AreEqual(Others(fresh.World), Others(world), "Down: the rest as at fix V2");
            }
            Assert.AreEqual(Before, Bosses(world), "Down: the old actions, no new rows");
            Assert.AreEqual(Short, Names(world), "Down: the short names stay");
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");

            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in W.DownSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Before, Bosses(world), "a second Down changes nothing");

            // The live database ran set 20 with the long names: Up shortens them.
            Sql($"UPDATE mission_scenario SET name = '{W.PitSavedBefore.Replace("'", "''")}', comment = '{W.PitSavedBefore.Replace("'", "''")}' " +
                $"WHERE mission_id = {AshenIntoTheLionsDenV1.MissionId} AND scenario_id = {AshenIntoTheLionsDenV1.PitSaved};");
            Sql($"UPDATE mission_scenario SET name = '{W.CellsOpenBefore}', comment = '{W.CellsOpenBefore}' " +
                $"WHERE mission_id = {AshenIntoTheLionsDenV1.MissionId} AND scenario_id = {AshenIntoTheLionsDenV1.CellsOpen};");
            world.ChangeTracker.Clear();
            Assert.AreEqual(Long, Names(world), "as the live database");

            migrator.Migrate(FixW);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Bosses(world), "Up again");
            Assert.AreEqual(Short, Names(world), "Up: the short names");
            foreach (var sql in W.UpdateSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(Now, Bosses(world), "a second Up changes nothing");
            Assert.AreEqual(Short, Names(world), "a second Up: the names");
            Assert.AreEqual(others, Others(world), "nothing else changes");
        }

        /// <summary>
        /// The guards: an UPDATE changes a boss only on its exact old actions. A boss already changed by hand (Wendy with another
        /// spit) is left as it is.
        /// </summary>
        [TestMethod]
        public void TheUpdatesChangeOnlyTheExactOldActions()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixV2);
            var world = harness.World;
            void Sql(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            Sql($"UPDATE creature SET action2 = 55048 WHERE id = {W.Wendy};");
            foreach (var sql in W.UpSql())
                Sql(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual($"{W.Wendy}: 55048 55048 0 0 0 0 0 0; {W.Queen}: {W.QueenClaw} 54016 0 0 0 0 0 0; " +
                            $"{W.Xahn}: {W.XahnWeapon} 0 0 0 0 0 0 0 | rows 3", Bosses(world), "Wendy off her old actions: kept");
        }

        /// <summary>The MySQL script holds the same statements.</summary>
        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(FixV2, FixW);
            var down = migrator.GenerateScript(FixW, FixV2);

            foreach (var sql in W.UpSql())
                StringAssert.Contains(up, sql);
            foreach (var sql in W.DownSql())
                StringAssert.Contains(down, sql);
            StringAssert.Contains(up, "UPDATE creature SET action1 = 790245, action2 = 0, action3 = 0, action4 = 0, action5 = 0, action6 = 0, action7 = 0, " +
                                      "action8 = 0 WHERE id = 790666 AND action1 = 34 AND action2 = 57008 AND action3 = 61003 AND action4 = 61004 AND " +
                                      "action5 = 68003 AND action6 = 68004 AND action7 = 0 AND action8 = 0;");
            StringAssert.Contains(up, "AND name = 'Humphries saved: the ledge''s NPCs and switch go, the fighters still standing fall (3 done)'");
            StringAssert.Contains(up, $"'{FixW}'");
        }
    }
}
