using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // A creature that attacks with WEAPON_GROUNDTARGET fires Weapon_Creature_Bane_Mortar_Launcher,
    // and the client has nothing to fire it from unless the launcher is in the creature's weapon
    // slot (Wilderness_mortar_launchers): the Wilderness mortars hit with no shell and no blast.
    [TestClass]
    public class MortarLauncherTests
    {
        private const string Migration = "20261218000000_Wilderness_mortar_launchers";
        private const string Previous = "20261217000000_Fithik_bites";

        [TestMethod]
        public void EveryCreatureThatFiresAMortarHasTheLauncherInItsWeaponSlotOnce()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var mortars = Query(database,
                "SELECT DISTINCT c.id AS Value FROM creature c JOIN creature_action a ON a.action_id = 411 AND " +
                "a.id IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8) ORDER BY c.id");

            CollectionAssert.IsSubsetOf(WildernessMortarLaunchers.Mortars.Select(id => (long)id).ToArray(), mortars);
            foreach (var id in mortars)
                CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher },
                    Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13"), $"creature {id}");
        }

        [TestMethod]
        public void DownTakesTheLauncherAwayAndAWeaponAlreadyThereIsKept()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = WildernessMortarLaunchers.Mortars[0];
            var second = WildernessMortarLaunchers.Mortars[1];

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Slot(database, first).Length);

            // One given a weapon by hand keeps it.
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({second}, 13, 3782, 7)");

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher }, Slot(database, first));
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, second));

            database.Context.Database.ExecuteSqlRaw(WildernessMortarLaunchers.Up);
            CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher }, Slot(database, first), "guarded");

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Slot(database, first).Length);
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, second), "not the launcher: left");
        }

        // A World that ran it as 20261217000000_Wilderness_mortar_launchers, the timestamp
        // Fithik_bites has: it runs again under its own, adds nothing, and the old row goes.
        [TestMethod]
        public void AWorldThatRanItUnderTheOldIdEndsWithOneLauncherAndOnlyTheNewHistoryRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = WildernessMortarLaunchers.Mortars[0];

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw(WildernessMortarLaunchers.Up);
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{WildernessMortarLaunchers.OldId}', '9.0.20')");

            CollectionAssert.Contains(database.Context.Database.GetPendingMigrations().ToList(), Migration);

            migrator.Migrate();

            CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher }, Slot(database, first), "one launcher, not two");
            var history = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(history, Migration);
            CollectionAssert.DoesNotContain(history, WildernessMortarLaunchers.OldId);
        }

        // Six pairs older than this shared a timestamp before it, and are run on every World
        // already, several with table changes that are not guarded, so they keep their ids. So do
        // two later pairs, each of two migrations that touch nothing of the other's, run on Worlds
        // since: Alia_Das_cellar_link and Weapon_template_rows (20261230000000), Add_npc_barks and
        // Alia_das_npcs (20261231000000); Add_npc_barks creates a table, so it cannot run twice.
        // And five at 20270109000000, run on Worlds since: Bootcamp_cave_in_landmark,
        // Bootcamp_fort_medic, Bootcamp_front_line, Capture_the_flag_promotion_xp and
        // Youngblood_fort_squad. Of them only the cave-in's and the squad's touch the same rows -
        // each wrote the Proving Grounds' scene bindings whole - and the one run second undid the
        // other; Bootcamp_cave_in_with_fort_squad (20270110000000) writes the two together after.
        // And two at 20270111000000, run on Worlds since: Proving_grounds_front_squads and
        // Targets_of_opportunity_givers, made the same morning on trees without each other. Both
        // write creature, its action, appearance and stat rows and spawnpool, but no row the other
        // writes: the squads are 400301-400306, the officers and bosses 510300-510305,
        // 520015-520073 and 540002. Proving_grounds_front_squads was there first and sorts
        // first, so every World ran them in the same order.
        // And two pairs made the same day on trees without each other, run on Worlds since: at
        // 20270114000000 Bootcamp_van_valkenberg (creature 510209's creature_appearance and
        // npc_greeting rows) and Creature_flags (creature_class_flag), at 20270115000000
        // Add_mission_reward_item_modules (the module columns of mission_reward_item) and
        // Named_npc_barks (one npc_bark row), with Bootcamp_caldera_sky (two map_region rows) a
        // third there; and at 20270116000000 Region_sky_circles (map_region rows) and
        // Wilderness_reward_modules (mission_reward_item and mission_evidence rows); and at
        // 20270117000000 Cellar_teleporter_pads (teleporter rows) and Unplaced_named_npcs
        // (creature, creature_appearance and npc_bark rows of 400401-400406), with
        // Capture_the_flag_promotion_level (one mission_reward_definition row) a third there; and
        // at 20270120000000 Bootcamp_cave_in_any_route (mission 1994's transition, trigger and
        // scenario rows) and Lightning_fields_strikes (world_usable rows). None touches a table of
        // another's at its timestamp, so the order EF runs them in changes nothing.
        // A new pair is a mistake: EF runs both, ordered by the rest of the id, whatever was meant.
        [TestMethod]
        public void NoNewMigrationSharesATimestamp()
        {
            var known = new[] { "20260927000000", "20260930000000", "20261012000000", "20261104000000", "20261111000000", "20261203000000",
                "20261230000000", "20261231000000", "20270109000000", "20270111000000", "20270114000000", "20270115000000", "20270116000000", "20270117000000", "20270120000000" };

            using var database = new OrdinaryLootTestDatabase();
            var shared = database.Context.Database.GetMigrations()
                .GroupBy(id => id.Substring(0, 14)).Where(stamp => stamp.Count() > 1).Select(stamp => stamp.Key).ToList();

            CollectionAssert.AreEquivalent(known, shared, string.Join(", ", shared));
        }

        // The same for every database and both providers, read off the migrations' attributes. The
        // character database has two pairs, run on every Char since and touching nothing of each
        // other's: Add_character_boss_kill (a new table) and Use_client_waypoint_ids
        // (character_teleporter rows) at 20261110000000, Family_name_unique (an index on account)
        // and Index_character_and_clan_inventory (two indexes on the inventory tables) at
        // 20261215000000. And since the schemas were consolidated (2026-09-26) a migration made for
        // one provider is made for the other under the same id: a twin left out is a database the
        // other provider never gets.
        [TestMethod]
        public void EveryDatabaseKeepsItsTimestampsAndBothProvidersHaveEveryMigration()
        {
            var known = new System.Collections.Generic.Dictionary<string, string[]>
            {
                ["World"] = new[] { "20260927000000", "20260930000000", "20261012000000", "20261104000000", "20261111000000", "20261203000000",
                    "20261230000000", "20261231000000", "20270109000000", "20270111000000", "20270114000000", "20270115000000", "20270116000000", "20270117000000", "20270120000000" },
                ["Char"] = new[] { "20261110000000", "20261215000000" },
                ["Auth"] = new string[0]
            };
            var migrations = typeof(Rasa.Context.World.SqliteWorldContext).Assembly.GetTypes()
                .Where(type => typeof(Microsoft.EntityFrameworkCore.Migrations.Migration).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => (
                    id: type.GetCustomAttributes(typeof(MigrationAttribute), false).Cast<MigrationAttribute>().SingleOrDefault()?.Id,
                    context: type.GetCustomAttributes(typeof(DbContextAttribute), false).Cast<DbContextAttribute>().SingleOrDefault()?.ContextType.Name,
                    name: type.FullName))
                .ToList();

            CollectionAssert.AreEqual(new string[0], migrations.Where(m => m.id == null || m.context == null).Select(m => m.name).ToArray(),
                "a migration without its [Migration] or [DbContext] is never run");

            foreach (var database in known.Keys)
            {
                var mySql = migrations.Where(m => m.context == $"MySql{database}Context").Select(m => m.id).ToList();
                var sqlite = migrations.Where(m => m.context == $"Sqlite{database}Context").Select(m => m.id).ToList();

                foreach (var (provider, ids) in new[] { ("MySql", mySql), ("Sqlite", sqlite) })
                {
                    var shared = ids.GroupBy(id => id.Substring(0, 14)).Where(stamp => stamp.Count() > 1).Select(stamp => stamp.Key).ToList();
                    CollectionAssert.AreEquivalent(known[database], shared, $"{provider}{database}: " + string.Join(", ", shared));
                }

                var since = mySql.Concat(sqlite).Where(id => string.CompareOrdinal(id, "20260927") >= 0).Distinct()
                    .Where(id => !mySql.Contains(id) || !sqlite.Contains(id)).ToList();
                CollectionAssert.AreEqual(new string[0], since, $"{database}: on one provider only: " + string.Join(", ", since));
            }
        }

        [TestMethod]
        public void TheMigrationForgetsTheOldIdOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);

            StringAssert.Contains(up, WildernessMortarLaunchers.Up);
            StringAssert.Contains(up, "DELETE FROM __EFMigrationsHistory WHERE MigrationId = '20261217000000_Wilderness_mortar_launchers';");
            StringAssert.Contains(up, $"'{Migration}'");
        }

        private static long[] Slot(OrdinaryLootTestDatabase database, uint id) =>
            Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13");

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
