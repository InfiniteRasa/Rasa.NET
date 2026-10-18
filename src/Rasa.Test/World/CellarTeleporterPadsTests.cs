using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    /// <summary>The CELLAR Arena's pads on its links to the battlefields (Cellar_teleporter_pads, CellarTeleporterPads).</summary>
    [TestClass]
    [DoNotParallelize]
    public class CellarTeleporterPadsTests
    {
        private const string Migration = "20270117000000_Cellar_teleporter_pads";
        private const string Previous = "20270116000000_Wilderness_reward_modules";

        [TestMethod]
        public void EachLinkOutOfTheHubHasAPadOnIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            foreach (var pad in CellarTeleporterPads.Pads)
            {
                Assert.AreEqual($"7895 1 20000009 {F(pad.X)} {F(pad.Y)} {F(pad.Z)}", Strings(database,
                    $"SELECT class_id || ' ' || type || ' ' || map_context_id || ' ' || printf('%.4f %.4f %.4f', pos_x, pos_y, pos_z) AS Value FROM teleporter WHERE id = {pad.Id}").Single(), pad.Description);

                // On the link's own point, and the link goes where the pad says.
                Assert.AreEqual($"20000009 {F(pad.X)} {F(pad.Y)} {F(pad.Z)} 0 1", Strings(database,
                    $"SELECT map_context_id || ' ' || printf('%.4f %.4f %.4f', pos_x, pos_y, pos_z) || ' ' || kind || ' ' || enabled AS Value FROM map_link WHERE id = {pad.LinkId}").Single(), pad.Description);

                Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id = {pad.Id} OR to_teleporter_id = {pad.Id}"),
                    "the pad itself takes nobody anywhere");
            }

            // Every link out of the hub has one now, the Alia Das one included (991).
            var hubLinks = Longs(database, "SELECT id AS Value FROM map_link WHERE map_context_id = 20000009 AND kind = 0");
            CollectionAssert.AreEquivalent(new long[] { 135, 136, 137, 138, 139, 140 }, hubLinks);
            Assert.AreEqual(6L, Count(database, "SELECT COUNT(*) AS Value FROM teleporter WHERE map_context_id = 20000009 AND class_id = 7895"));
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownTakesThemOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(before.Count + 5, once.Count);

            foreach (var sql in CellarTeleporterPads.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in CellarTeleporterPads.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in CellarTeleporterPads.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheHumanTeleporterRunsInItsFirstStateAndTheOthersInTheirSecond()
        {
            // 7895: its blue field is its own mesh (55); 56 is arch_hum_teleporter_disabled.geo.
            Assert.AreEqual(UseObjectState.TsState0, DynamicObjectManager.TeleporterRunningState((EntityClasses)7895));

            // The waypoint pads, wormholes, personal wormholes and the Eloh bridge waypoint carry their effect in 56.
            foreach (var other in new uint[] { 25651, 25408, 28474, 21768, 29648 })
                Assert.AreEqual(UseObjectState.TsState1, DynamicObjectManager.TeleporterRunningState((EntityClasses)other), $"class {other}");
        }

        private static string F(double value) => value.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static List<long> Longs(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().ToList();

        private static List<string> Strings(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            Strings(database, "SELECT id || ' ' || class_id || ' ' || type || ' ' || map_context_id || ' ' || description AS Value FROM teleporter WHERE map_context_id = 20000009")
                .OrderBy(s => s, System.StringComparer.Ordinal).ToList();
    }
}
