using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The way into the CELLAR Arena is where a player stands in front of its sign in the Alia
    /// Das hospital room, and neither a revive there nor an arrival from the arena lands in it
    /// (Alia_Das_cellar_link).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AliaDasCellarLinkTests
    {
        private const string Migration = "20261230000000_Alia_Das_cellar_link";
        private const string Previous = "20261229000000_Three_devils";

        /// <summary>Where the player stood in front of the sign: [.where] map=1220 pos=(749.6406, 294.2109, 382.5547).</summary>
        private static readonly Vector3 Reported = new Vector3(749.6406f, 294.2109f, 382.5547f);

        private static MapLink Link() => new MapLink
        {
            Position = new Vector3((float)AliaDasCellarLink.Link.X, (float)AliaDasCellarLink.Link.Y, (float)AliaDasCellarLink.Link.Z),
            Radius = (float)AliaDasCellarLink.Link.Radius
        };

        private static Vector3 V((double X, double Y, double Z, double) p) => new Vector3((float)p.X, (float)p.Y, (float)p.Z);

        [TestMethod]
        public void AStepInFrontOfTheSignTakesThemAndTheRevivedAndArrivingStandClear()
        {
            var link = Link();
            var flatRadius = link.Radius;

            Assert.IsTrue(MapLinkManager.Contains(link, Reported), "where they stood");
            Assert.IsTrue(MapLinkManager.Contains(link, Reported + new Vector3(0.8f, 0, 0)), "a step in front of it too");

            var hospital = V(AliaDasCellarLink.Hospital);
            var arrival = V(AliaDasCellarLink.Arrival);

            Assert.IsFalse(MapLinkManager.Contains(link, hospital));
            Assert.IsTrue(Flat(hospital, link.Position) - flatRadius >= 5f, "the revived stand well clear");
            Assert.IsFalse(MapLinkManager.Contains(link, arrival));
            Assert.IsTrue(Flat(arrival, link.Position) - flatRadius >= 2f, "the arriving stand clear");

            // Facing: the revived look at the sign (to -x), the arriving into the room (to +x).
            Assert.AreEqual(-1.0, -System.Math.Sin(AliaDasCellarLink.Hospital.Rotation), 0.001);
            Assert.AreEqual(1.0, -System.Math.Sin(AliaDasCellarLink.Arrival.Rotation), 0.001);
        }

        [TestMethod]
        public void AllThreeAreOnTheFloorOfTheRoomAndWithinReachOfEachOther()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = harness.Map.NavMesh;

            foreach (var (name, spot) in new[] { ("link", V((AliaDasCellarLink.Link.X, AliaDasCellarLink.Link.Y, AliaDasCellarLink.Link.Z, 0))), ("hospital", V(AliaDasCellarLink.Hospital)), ("arrival", V(AliaDasCellarLink.Arrival)) })
            {
                var floor = nav.Nearest(spot);
                Assert.IsTrue(floor.HasValue && Flat(floor.Value, spot) < 0.3f && System.Math.Abs(floor.Value.Y - spot.Y) < 0.5f, $"{name} at {spot}: floor {floor}");
                Assert.IsTrue(nav.IsWalkClear(nav.Nearest(V(AliaDasCellarLink.Hospital)).Value, floor.Value), $"{name} in the same room");
            }
        }

        [TestMethod]
        public void DownPutsThemBackAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(AliaDasCellarLink.LinkWas, Link(database));
            Assert.AreEqual(AliaDasCellarLink.ArrivalWas, Arrival(database));
            Assert.AreEqual(AliaDasCellarLink.HospitalWas, Hospital(database));

            migrator.Migrate(Migration);
            Assert.AreEqual(AliaDasCellarLink.Link, Link(database));
            Assert.AreEqual(AliaDasCellarLink.Arrival, Arrival(database));
            Assert.AreEqual(AliaDasCellarLink.Hospital, Hospital(database));

            foreach (var sql in AliaDasCellarLink.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(AliaDasCellarLink.Link, Link(database), "once");

            migrator.Migrate(Previous);
            Assert.AreEqual(AliaDasCellarLink.LinkWas, Link(database));
            Assert.AreEqual(AliaDasCellarLink.ArrivalWas, Arrival(database));
            Assert.AreEqual(AliaDasCellarLink.HospitalWas, Hospital(database));

            // One a GM has moved since is left where it is.
            database.Context.Database.ExecuteSqlRaw("UPDATE teleporter SET pos_x = 760 WHERE id = 103");
            migrator.Migrate(Migration);
            Assert.AreEqual(760.0, Hospital(database).X);
            Assert.AreEqual(AliaDasCellarLink.Link, Link(database));
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        private static (double, double, double, double) Link(OrdinaryLootTestDatabase database) =>
            Row(database, "map_link", AliaDasCellarLink.ArenaLinkId, "pos_x", "pos_y", "pos_z", "radius");

        private static (double, double, double, double) Arrival(OrdinaryLootTestDatabase database) =>
            Row(database, "map_link", AliaDasCellarLink.ReturnLinkId, "dest_pos_x", "dest_pos_y", "dest_pos_z", "dest_rotation");

        private static (double X, double, double, double) Hospital(OrdinaryLootTestDatabase database) =>
            Row(database, "teleporter", AliaDasCellarLink.HospitalId, "pos_x", "pos_y", "pos_z", "rotation");

        private static (double, double, double, double) Row(OrdinaryLootTestDatabase database, string table, uint id, params string[] columns)
        {
            double Get(string column) => database.Context.Database.SqlQueryRaw<double>($"SELECT {column} AS Value FROM {table} WHERE id = {id}").Single();
            return (Get(columns[0]), Get(columns[1]), Get(columns[2]), Get(columns[3]));
        }
    }
}
