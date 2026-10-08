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
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    // Place_marked_teleporters (MarkedTeleporters): a waypoint and two hospitals the client's map
    // marks, whose teleporter rows had the id and the name and no usable place.
    [TestClass]
    [DoNotParallelize]
    public class MarkedTeleporterTests
    {
        private const string WorldBefore = "20261118000000_Oneoff_titles";
        private const string WorldMigration = "20261119000000_Place_marked_teleporters";

        private const string Three = "select id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id from teleporter where id in (110, 306, 307) order by id";
        private const string Others = "select * from teleporter where id not in (110, 306, 307) order by id";
        private const string Markers = "select marker_entity_id, map_context_id, marker_type, object_kind, object_id from map_marker order by marker_entity_id, map_context_id";
        private const string Links = "select control_point_id, kind, object_id from control_point_link order by control_point_id, kind, object_id";

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

        private static void InTemporaryDatabase(Action<RasaDbContextBase, IMigrator> test)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                test(context, context.GetService<IMigrator>());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheMigrationGivesEachRowItsPlaceItsMarkerAndItsPointAndBack()
        {
            InTemporaryDatabase((context, migrator) =>
            {
                migrator.Migrate(WorldBefore);

                var three = Rows(context, Three);
                var others = Rows(context, Others);
                var markers = Rows(context, Markers);
                var links = Rows(context, Links);

                // As they were: two names with no place, and a pad forty-three thousand kilometres off.
                CollectionAssert.AreEqual(new[]
                {
                    "110|0|0|Level 01: Control Room|0|0|0|0|0",
                    "306|0|0|Hospital: Drill Resonator (Control Point)|0|0|0|0|0",
                    "307|25651|2|Waypoint: Cuthah Scout Post (Control Point)|-1134.9648|244.8086|-42909375|1.8|2051"
                }, three);

                migrator.Migrate(WorldMigration);

                CollectionAssert.AreEqual(new[]
                {
                    "110|0|5|Hospital: Level 01: Control Room|152.2477|73.57834|128.49892|0|1806",
                    "306|0|5|Hospital: Drill Resonator (Control Point)|36.271484|240.30869|161.86011|0|2047",
                    "307|25651|2|Waypoint: Cuthah Scout Post (Control Point)|-1134.9648|244.8086|-429.09375|1.8|2051"
                }, Rows(context, Three));

                // No other teleporter row is touched; Outpost Aurora's is left as it is.
                CollectionAssert.AreEqual(others, Rows(context, Others));
                Assert.AreEqual("-14538672", Rows(context, "select pos_z from teleporter where id = 86").Single());

                // Each has its marker: the client's id for it, on its map, of its kind.
                CollectionAssert.AreEqual(new[]
                {
                    "134419591468356|2047|3|1|306",
                    "134419591470252|1806|3|1|110",
                    "134419591475383|2051|2|1|307"
                }, Rows(context, Markers).Except(markers).ToArray());
                Assert.AreEqual(markers.Count + 3, Rows(context, Markers).Count);

                // And the two that are a control point's are its: Cuthah Scout Post's waypoint,
                // Drill Resonator's hospital.
                CollectionAssert.AreEqual(new[] { "38|3|306", "39|4|307" }, Rows(context, Links).Except(links).ToArray());
                Assert.AreEqual(links.Count + 2, Rows(context, Links).Count);

                // Run again, as the script for a MySQL server may be: nothing moves.
                var after = Rows(context, "select * from teleporter order by id");
                var markersAfter = Rows(context, "select * from map_marker order by marker_entity_id, map_context_id");
                var linksAfter = Rows(context, Links);

                foreach (var statement in MarkedTeleporters.WorldUp)
                    context.Database.ExecuteSqlRaw(statement);

                CollectionAssert.AreEqual(after, Rows(context, "select * from teleporter order by id"));
                CollectionAssert.AreEqual(markersAfter, Rows(context, "select * from map_marker order by marker_entity_id, map_context_id"));
                CollectionAssert.AreEqual(linksAfter, Rows(context, Links));

                // Down leaves everything as it was, and Up comes to the same again.
                migrator.Migrate(WorldBefore);

                CollectionAssert.AreEqual(three, Rows(context, Three));
                CollectionAssert.AreEqual(others, Rows(context, Others));
                CollectionAssert.AreEqual(markers, Rows(context, Markers));
                CollectionAssert.AreEqual(links, Rows(context, Links));

                migrator.Migrate(WorldMigration);
                CollectionAssert.AreEqual(after, Rows(context, "select * from teleporter order by id"));
            });
        }

        [TestMethod]
        public void EveryMarkerAndEveryLinkStandsForARowOfItsKindOnItsMap()
        {
            InTemporaryDatabase((context, migrator) =>
            {
                migrator.Migrate();

                // A waypoint's marker for a waypoint, a hospital's or a safe zone's for a hospital.
                CollectionAssert.AreEquivalent(new[] { "2|2", "3|5", "19|5" },
                    Rows(context, "select distinct m.marker_type, t.type from map_marker m join teleporter t on t.id = m.object_id where m.object_kind = 1"));

                Assert.AreEqual(0, Rows(context, "select m.object_id from map_marker m left join teleporter t on t.id = m.object_id where m.object_kind = 1 and (t.id is null or t.map_context_id <> m.map_context_id)").Count,
                    "a marker's row is on the marker's map");

                Assert.AreEqual(0, Rows(context, "select l.object_id from control_point_link l join control_point p on p.id = l.control_point_id left join teleporter t on t.id = l.object_id where l.kind in (3, 4) and (t.id is null or t.map_context_id <> p.map_context_id or t.type <> case l.kind when 3 then 5 else 2 end)").Count,
                    "a point's hospital is a hospital and its waypoint a waypoint, on the point's map");

                // The three are where the client's map marks them: the hospitals on their markers,
                // the waypoint 6.7 m from its.
                foreach (var (id, x, y, z, within) in new (int, double, double, double, double)[]
                {
                    (307, -1139.0159, 245.7842, -423.8107, 7.0),
                    (306, 36.2715, 240.3087, 161.8601, 0.01),
                    (110, 152.2477, 73.5783, 128.4989, 0.01)
                })
                {
                    var at = Rows(context, $"select pos_x, pos_y, pos_z from teleporter where id = {id}").Single().Split('|')
                        .Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    var distance = Math.Sqrt(Math.Pow(at[0] - x, 2) + Math.Pow(at[1] - y, 2) + Math.Pow(at[2] - z, 2));

                    Assert.IsTrue(distance <= within, $"teleporter {id} is {distance:0.00} m from its marker");
                }
            });
        }

        [TestMethod]
        public void TheWaypointAndTheHospitalGoWithTheirControlPoints()
        {
            InTemporaryDatabase((context, migrator) =>
            {
                migrator.Migrate();

                var points = context.Set<ControlPointEntry>().AsNoTracking().ToList();
                var links = context.Set<ControlPointLinkEntry>().AsNoTracking().ToList();

                try
                {
                    // As the server starts: both points are the AFS'.
                    ControlPoints.Instance.Load(points, links, null);

                    var scoutPost = ControlPoints.Instance.ById(39);
                    var drill = ControlPoints.Instance.ById(38);

                    Assert.AreEqual("Cuthah Scout Post", scoutPost.Name);
                    CollectionAssert.AreEquivalent(new uint[] { 307 }, scoutPost.Waypoints.ToArray());
                    CollectionAssert.AreEquivalent(new uint[] { 308 }, scoutPost.Hospitals.ToArray());

                    Assert.AreEqual("Drill Resonator", drill.Name);
                    CollectionAssert.AreEquivalent(new uint[] { 305 }, drill.Waypoints.ToArray());
                    CollectionAssert.AreEquivalent(new uint[] { 306 }, drill.Hospitals.ToArray());

                    Assert.IsTrue(scoutPost.HeldByAfs && drill.HeldByAfs);
                    Assert.IsTrue(ControlPoints.Instance.IsOpen(307));
                    Assert.IsTrue(ControlPoints.Instance.IsOpen(306));

                    // With the Bane holding them the waypoint is closed and the hospital lost; the
                    // control room's hospital is no control point's and stays.
                    ControlPoints.Instance.Load(points, links, new Kept(
                        new ControlPointStateEntry { ControlPointId = 38, Owner = ControlPointEntry.OwnerBane, ChangedAt = 1 },
                        new ControlPointStateEntry { ControlPointId = 39, Owner = ControlPointEntry.OwnerBane, ChangedAt = 1 }));

                    Assert.IsFalse(ControlPoints.Instance.IsOpen(307));
                    Assert.IsFalse(ControlPoints.Instance.IsOpen(306));
                    Assert.IsTrue(ControlPoints.Instance.IsOpen(110));
                }
                finally
                {
                    ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
                }
            });
        }

        [TestMethod]
        public void TheHospitalWindowNamesTheDrillResonatorsAndBothAreGainedUnderTheirOwnNames()
        {
            // graveyardlanguage 182, "Hospital: Drill Resonator (Control Point)", and nobody else's.
            Assert.AreEqual(182u, HospitalGraveyards.ByTeleporter[306]);
            Assert.AreEqual(1, HospitalGraveyards.ByTeleporter.Values.Count(graveyard => graveyard == 182));

            // The client has no hospital-window name for the control room: it is given a generic one.
            Assert.IsFalse(HospitalGraveyards.ByTeleporter.ContainsKey(110));

            // "You just gained ...": waypointlanguage has both under their own ids.
            Assert.AreEqual(306u, HospitalGraveyards.GainedAs(306));
            Assert.AreEqual(110u, HospitalGraveyards.GainedAs(110));
            Assert.IsTrue(HospitalGraveyards.ClientNames(307));
        }

        [TestMethod]
        public void BothHospitalsAreOnTheirMapsWithAGraveyardEach()
        {
            InTemporaryDatabase((context, migrator) =>
            {
                migrator.Migrate();

                // The teleporter table's hospitals, as the server reads them once it has loaded.
                var hospitals = Rows(context, "select id, map_context_id, pos_x, pos_y, pos_z, description from teleporter where type = 5 and map_context_id <> 0")
                    .Select(row => row.Split('|'))
                    .Select(row => (Id: uint.Parse(row[0]), MapContextId: uint.Parse(row[1]),
                        Position: new System.Numerics.Vector3(Single(row[2]), Single(row[3]), Single(row[4])), Name: row[5]))
                    .ToList();

                var source = Hospitals.Source;
                var safe = Hospitals.IsSafeZone;

                try
                {
                    Hospitals.Source = () => hospitals;
                    Hospitals.IsSafeZone = id => false;
                    Hospitals.Reset();

                    // Purgas Station: the entrance hall's, and now the control room's, which
                    // the hospital window has no name for and shows under a generic one.
                    var purgas = Hospitals.OnMap(1806);

                    CollectionAssert.AreEqual(new uint[] { 110, 360 }, purgas.Select(h => h.TeleporterId).ToArray());
                    Assert.AreEqual(239u, purgas.Single(h => h.TeleporterId == 360).GraveyardId);
                    CollectionAssert.Contains(HospitalGraveyards.GenericIds, purgas.Single(h => h.TeleporterId == 110).GraveyardId);
                    Assert.AreEqual(new System.Numerics.Vector3(152.2477f, 73.57834f, 128.49892f), purgas.Single(h => h.TeleporterId == 110).Position);

                    // Valverde Descent: Drill Resonator's, under its own name, and not free.
                    var drill = Hospitals.OnMap(2047).Single(h => h.TeleporterId == 306);

                    Assert.AreEqual(182u, drill.GraveyardId);
                    Assert.AreEqual(new System.Numerics.Vector3(36.271484f, 240.30869f, 161.86011f), drill.Position);
                    Assert.IsFalse(drill.IsFree);
                    Assert.AreEqual(Hospitals.OnMap(2047).Count, Hospitals.OnMap(2047).Select(h => h.GraveyardId).Distinct().Count(), "a graveyard id is one hospital on a map");
                }
                finally
                {
                    Hospitals.Source = source;
                    Hospitals.IsSafeZone = safe;
                    Hospitals.Reset();
                }
            });
        }

        private static float Single(string value) => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        [TestMethod]
        public void TheMigrationIsTheSameStatementsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(WorldBefore, WorldMigration);
            var down = migrator.GenerateScript(WorldMigration, WorldBefore);

            foreach (var statement in MarkedTeleporters.WorldUp)
                StringAssert.Contains(up, statement);

            foreach (var statement in MarkedTeleporters.WorldDown)
                StringAssert.Contains(down, statement);

            StringAssert.Contains(up, $"'{WorldMigration}'");
        }

        /// <summary>The owners a server kept from its last run.</summary>
        private sealed class Kept : ControlPoints.IStore
        {
            private readonly List<ControlPointStateEntry> _rows;

            public Kept(params ControlPointStateEntry[] rows) => _rows = rows.ToList();

            public List<ControlPointStateEntry> Load() => _rows;

            public void Save(ControlPointStateEntry state)
            {
            }
        }
    }
}
