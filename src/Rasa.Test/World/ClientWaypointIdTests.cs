using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context;
    using Rasa.Context.Char;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    // Use_client_waypoint_ids (ClientWaypointIds): the waypoints and hospitals the client had no
    // name for, under the ids it names them by, in the world database and in what each character
    // has gained.
    [TestClass]
    [DoNotParallelize]
    public class ClientWaypointIdTests
    {
        private const string WorldBefore = "20261108000000_Add_targets_of_opportunity";
        private const string WorldMigration = "20261109000000_Use_client_waypoint_ids";
        private const string CharBefore = "20261109000000_Add_pvp_records";
        private const string CharMigration = "20261110000000_Use_client_waypoint_ids";

        private static List<string> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static void InTemporaryDatabase(Type contextType, Action<RasaDbContextBase, IMigrator> test)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(contextType, Path.Combine(directory, "database"));
                test(context, context.GetService<IMigrator>());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        private const string Teleporters =
            "select id, class_id, type, description, map_context_id from teleporter where id in (75, 107, 135, 137, 145, 150, 153, 375, 415, 418, 534, 541, 575, 576, 582, 583, 589, 590, 607, 613, 622, 624) order by id";

        [TestMethod]
        public void TheWorldMigrationPutsEachPlaceUnderTheIdTheClientNamesItByAndBack()
        {
            InTemporaryDatabase(typeof(SqliteWorldContext), (context, migrator) =>
            {
                migrator.Migrate(WorldBefore);
                var before = Rows(context, Teleporters);
                var positions = Rows(context, "select pos_x, pos_y, pos_z, rotation from teleporter where id in (150, 145, 534, 575, 582, 589, 590, 607, 624) order by id");
                Assert.AreEqual(22, before.Count);

                migrator.Migrate();

                CollectionAssert.AreEqual(new[]
                {
                    "75|25651|2|Waypoint: Irendas Colony|1764",
                    "107|25651|2|Waypoint: Staging Point (AFS Preparation Camp)|1244",
                    "135|25651|2|Waypoint Paludos|1454",
                    "137|25651|2|Waypoint: Stalker Woods (High Point Retreat)|1454",
                    "145|0|5|Hospital: Plains Post Medical Tent|1761",
                    "150|25651|2|Waypoint: Plains Post|1761",
                    "153|0|5|Hospital: Irendas Colony|1764",
                    "375|29648|2|Waypoint: Tampeii Settlement|2028",
                    "415|25651|2|Waypoint: Viands Village (Rendezvous Point)|1244",
                    "418|25651|2|Portable Waypoint: Velon Hollow|2136",
                    "541|29648|1|Local Teleporter: Ashoka Lower|1734",
                    "576|0|1|Local Teleporter: Larai Outpost|1911",
                    "613|0|1|Local Teleporter: Ashoka Upper|1734"
                }, Rows(context, Teleporters));

                // Each keeps the place it had: 153 is where 145 was, 75 where 150 was, and so on.
                CollectionAssert.AreEqual(positions,
                    Rows(context, "select pos_x, pos_y, pos_z, rotation from teleporter where id in (75, 153, 107, 375, 137, 150, 145, 418, 415) order by case id when 153 then 1 when 75 then 2 when 107 then 3 when 375 then 4 when 137 then 5 when 150 then 6 when 145 then 7 when 418 then 8 else 9 end"));

                // Everything that can be gained has a name in the client, and every hospital is
                // announced under one.
                foreach (var id in Rows(context, "select id from teleporter where type in (2, 3, 4) and map_context_id <> 0").Select(uint.Parse))
                    Assert.IsTrue(HospitalGraveyards.ClientNames(id), $"waypoint {id}");

                foreach (var id in Rows(context, "select id from teleporter where type = 5 and map_context_id <> 0").Select(uint.Parse))
                    Assert.IsTrue(HospitalGraveyards.ClientNames(HospitalGraveyards.GainedAs(id)), $"hospital {id}");

                // The markers follow, and none stands for a row that is gone.
                CollectionAssert.AreEqual(new[]
                {
                    "133182640965832|1244|415",
                    "133182640965834|1244|107",
                    "134084584059679|1454|137",
                    "134084584059680|1454|135",
                    "134419591471832|2028|375",
                    "134419591472214|2136|418",
                    "134419591472343|1764|75",
                    "134419591472344|1764|153",
                    "134419591485481|1761|150",
                    "134419591486751|1761|145"
                }, Rows(context, "select marker_entity_id, map_context_id, object_id from map_marker where object_kind = 1 and object_id in (75, 107, 135, 137, 145, 150, 153, 375, 415, 418) order by marker_entity_id"));
                Assert.AreEqual(0, Rows(context, "select object_id from map_marker where object_kind = 1 and object_id not in (select id from teleporter)").Count);

                // Run again, as the script for a MySQL server may be: nothing moves.
                var after = Rows(context, "select * from teleporter order by id");
                var markers = Rows(context, "select * from map_marker order by marker_entity_id, map_context_id");

                foreach (var statement in ClientWaypointIds.WorldUp)
                    context.Database.ExecuteSqlRaw(statement);

                CollectionAssert.AreEqual(after, Rows(context, "select * from teleporter order by id"));
                CollectionAssert.AreEqual(markers, Rows(context, "select * from map_marker order by marker_entity_id, map_context_id"));

                migrator.Migrate(WorldBefore);
                CollectionAssert.AreEqual(before, Rows(context, Teleporters));

                migrator.Migrate();
                CollectionAssert.AreEqual(after, Rows(context, "select * from teleporter order by id"));
            });
        }

        [TestMethod]
        public void TheHospitalsThatMovedAreNamedInTheHospitalWindowToo()
        {
            Assert.AreEqual(61u, HospitalGraveyards.ByTeleporter[145], "Plains Post Medical Tent, on Incline");
            Assert.AreEqual(89u, HospitalGraveyards.ByTeleporter[153], "Irendas Colony Hospital");
            Assert.IsFalse(HospitalGraveyards.ByTeleporter.ContainsKey(590));
            Assert.AreEqual(145u, HospitalGraveyards.GainedAs(145));
            Assert.AreEqual(153u, HospitalGraveyards.GainedAs(153));
        }

        [TestMethod]
        public void TheCharacterMigrationMovesWhatEachCharacterHasGained()
        {
            InTemporaryDatabase(typeof(SqliteCharContext), (context, migrator) =>
            {
                migrator.Migrate(CharBefore);
                context.Database.ExecuteSqlRaw("pragma foreign_keys = off;");

                // 1 has the old Irendas (150) and the old Plains Post (589) and its hospitals; 2 has
                // both Tampeii rows, Exodus Point and Paludos; 3 has the rest, and one nothing
                // touches (57).
                foreach (var (character, waypoint, type) in new (int, int, int)[]
                {
                    (1, 150, 2), (1, 589, 2), (1, 145, 5), (1, 590, 5),
                    (2, 575, 2), (2, 622, 2), (2, 583, 2), (2, 135, 2),
                    (3, 582, 2), (3, 607, 2), (3, 534, 2), (3, 624, 2), (3, 541, 2), (3, 576, 2), (3, 613, 5), (3, 583, 2), (3, 57, 2)
                })
                    context.Database.ExecuteSql($"insert into character_teleporter (character_id, waypointId, waypoint_type) values ({character}, {waypoint}, {type});");

                migrator.Migrate();

                CollectionAssert.AreEqual(new[]
                {
                    "1|75|2", "1|145|5", "1|150|2", "1|153|5",
                    "2|135|2", "2|375|2",
                    "3|57|2", "3|107|2", "3|135|2", "3|137|2", "3|415|2", "3|418|2"
                }, Rows(context, "select character_id, waypointId, waypoint_type from character_teleporter order by character_id, waypointId"));

                migrator.Migrate(CharBefore);

                CollectionAssert.AreEqual(new[]
                {
                    "1|145|5", "1|150|2", "1|589|2", "1|590|5",
                    "2|135|2", "2|575|2",
                    "3|57|2", "3|135|2", "3|534|2", "3|582|2", "3|607|2", "3|624|2"
                }, Rows(context, "select character_id, waypointId, waypoint_type from character_teleporter order by character_id, waypointId"));
            });
        }

        [TestMethod]
        [DataRow(typeof(MySqlWorldContext), WorldBefore, WorldMigration)]
        [DataRow(typeof(MySqlCharContext), CharBefore, CharMigration)]
        public void TheMigrationsAreTheSameStatementsOnMySql(Type contextType, string before, string migration)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(before, migration);
            var down = migrator.GenerateScript(migration, before);
            var world = contextType == typeof(MySqlWorldContext);

            foreach (var statement in world ? ClientWaypointIds.WorldUp : ClientWaypointIds.CharUp)
                StringAssert.Contains(up, statement);

            foreach (var statement in world ? ClientWaypointIds.WorldDown : ClientWaypointIds.CharDown)
                StringAssert.Contains(down, statement);

            StringAssert.Contains(up, $"'{migration}'");
        }
    }
}
