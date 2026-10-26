using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The boss border on the target frame is the class's UI_INDICATOR_BOSS flag and nothing else
    /// (targetinfowindow.py, overheadwindow.py). The Bane Hunter Boss, the Arioch Xanx Boss, the
    /// Plateau's Mox Alpha Male, Lt. Casper and Rotting Sal had a boss class without it
    /// (Boss_frame_flags, BossFrameFlags).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BossFrameFlagsTests
    {
        private const string Migration = "20270127000000_Boss_frame_flags";
        private const string Previous = "20270126000000_Wilderness_fulgor_indicator";

        [TestMethod]
        public void EveryBossClassACreatureUsesHasTheBorderAndDownTakesOnlyItsOwnAway()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var classes = BossFrameFlags.Classes.Select(c => c.ClassId).ToArray();

            migrator.Migrate(Previous);
            var before = Rows(database);
            CollectionAssert.AreEquivalent(new uint[] { 10327, 10523, 22977, 21474 }, RowBossClassesWithout(database, before),
                "the boss classes creature rows use that were without the border");
            Assert.IsFalse(classes.Any(id => before.Contains((id, BossFrameFlags.UiIndicatorBoss))));

            migrator.Migrate(Migration);
            var after = Rows(database);
            Assert.IsEmpty(RowBossClassesWithout(database, after));
            CollectionAssert.AreEquivalent(before.Concat(classes.Select(id => (id, BossFrameFlags.UiIndicatorBoss))).ToList(), after.ToList(),
                "the flag on the six and nothing else");

            foreach (var sql in BossFrameFlags.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEquivalent(after.ToList(), Rows(database).ToList(), "once");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before.ToList(), Rows(database).ToList());
        }

        [TestMethod]
        public void OnMySqlTheFlagsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in BossFrameFlags.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in BossFrameFlags.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheyAreShownAsBosses()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            // 81 Bane Hunter Boss, 49 its Invasion Boss, 77 Arioch Xanx Boss, 520053 Mox Alpha
            // Male - Plateau, 520012 Hominis Machina Lt. Casper, 520017 Rotting Sal.
            foreach (var row in new uint[] { 81, 49, 77, 520053, 520012, 520017 })
            {
                var creature = CreatureManager.Instance.LoadedCreatures[row];
                CollectionAssert.Contains(CreatureManager.CreatureFlagsOf(creature), (int)CreatureFlag.UiIndicatorBoss, $"creature {row}");
            }
        }

        /// <summary>
        /// The boss classes (Boss in the name; a Miniboss is a lieutenant) that a creature row
        /// uses and that have not got the border.
        /// </summary>
        private static List<uint> RowBossClassesWithout(OrdinaryLootTestDatabase database, HashSet<(uint ClassId, uint FlagId)> rows)
        {
            var bosses = database.Context.Database.SqlQueryRaw<long>(
                    "SELECT DISTINCT e.id AS Value FROM entityclass e JOIN creature c ON c.class_id = e.id " +
                    "WHERE e.class_name LIKE '%Boss%' AND e.class_name NOT LIKE '%Miniboss%'")
                .AsEnumerable().Select(id => (uint)id).ToList();

            return bosses.Where(id => !rows.Contains((id, BossFrameFlags.UiIndicatorBoss))).ToList();
        }

        private static HashSet<(uint ClassId, uint FlagId)> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>("SELECT class_id || ' ' || flag_id AS Value FROM creature_class_flag").AsEnumerable()
                .Select(row => row.Split(' '))
                .Select(parts => (uint.Parse(parts[0]), uint.Parse(parts[1])))
                .ToHashSet();
    }
}
