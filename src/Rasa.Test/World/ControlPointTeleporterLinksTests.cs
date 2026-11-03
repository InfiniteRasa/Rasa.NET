extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// The hospitals and waypoints Add_control_points left off their control points
    /// (Control_point_teleporter_links, ControlPointTeleporterLinks). The Wilderness LZ waypoint
    /// was not the Landing Zone's, so with the Bane holding the point players went on
    /// teleporting in.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ControlPointTeleporterLinksTests
    {
        private const string Migration = "20270923000000_Control_point_teleporter_links";
        private const string Previous = "20270922000000_Control_tower_teleporters";

        [TestMethod]
        public void EachTeleporterGoesToItsPointAndDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Links(database);
            foreach (var link in ControlPointTeleporterLinks.Links)
                Assert.IsFalse(before.Any(l => l.ObjectId == link.TeleporterId && l.Kind is ControlPointLinkEntry.KindHospital or ControlPointLinkEntry.KindWaypoint),
                    $"{link.TeleporterId} on no point");
            Assert.IsFalse(before.Any(l => l.ControlPointId == ControlPointTeleporterLinks.LandingZone && l.Kind == ControlPointLinkEntry.KindWaypoint),
                "the Landing Zone had no waypoint");

            migrator.Migrate(Migration);
            var after = Links(database);
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(t => t.Id);
            var points = database.Context.ControlPointEntries.AsNoTracking().ToDictionary(p => p.Id);

            Assert.HasCount(before.Count + ControlPointTeleporterLinks.Links.Length, after);
            foreach (var link in ControlPointTeleporterLinks.Links)
            {
                Assert.AreEqual((link.ControlPointId, link.Kind), after.Where(l => l.ObjectId == link.TeleporterId).Select(l => (l.ControlPointId, l.Kind)).Single());
                var row = teleporters[link.TeleporterId];
                Assert.AreEqual(link.TeleporterType, row.Type, $"{link.TeleporterId}");
                Assert.AreEqual(points[link.ControlPointId].MapContextId, row.MapContextId, $"{link.TeleporterId} on its point's map");
            }

            // Every hospital and waypoint that stands in the world and is named for a point
            // is that point's.
            var linked = after.Where(l => l.Kind is ControlPointLinkEntry.KindHospital or ControlPointLinkEntry.KindWaypoint)
                .ToDictionary(l => l.ObjectId, l => l.ControlPointId);
            foreach (var row in teleporters.Values.Where(t => t.Type is 2 or 5 && t.MapContextId != 0))
                foreach (var point in points.Values.Where(p => p.MapContextId == row.MapContextId && p.Id < 100 &&
                    row.Description.Contains($"{p.Name} (Control Point)")))
                    Assert.AreEqual(point.Id, linked.GetValueOrDefault(row.Id), $"{row.Id} {row.Description}");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before, Links(database), "Down puts the links back as they were");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesALinkAGmMade()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("INSERT INTO control_point_link (control_point_id, kind, object_id) VALUES (28, 4, 325)");

            migrator.Migrate(Migration);
            var once = Links(database);
            CollectionAssert.Contains(once, (28u, ControlPointLinkEntry.KindWaypoint, 325u), "the GM's link stands");
            CollectionAssert.DoesNotContain(once, (27u, ControlPointLinkEntry.KindWaypoint, 325u));
            CollectionAssert.Contains(once, (ControlPointTeleporterLinks.LandingZone, ControlPointLinkEntry.KindWaypoint, ControlPointTeleporterLinks.WildernessLzWaypoint));

            foreach (var sql in ControlPointTeleporterLinks.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEquivalent(once, Links(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.Contains(Links(database), (28u, ControlPointLinkEntry.KindWaypoint, 325u), "Down leaves the GM's link");
        }

        [TestMethod]
        public void OnMySqlTheLinksGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in ControlPointTeleporterLinks.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ControlPointTeleporterLinks.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>
        /// The report: the Landing Zone's rows as the world has them. With the Bane holding the
        /// point the Wilderness LZ waypoint is not listed and refuses a player who picks it; with
        /// the AFS back it takes them in.
        /// </summary>
        [TestMethod]
        public void WhileTheBaneHoldTheLandingZoneItsWaypointTakesNobodyIn()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var point = database.Context.ControlPointEntries.AsNoTracking().Single(p => p.Id == ControlPointTeleporterLinks.LandingZone);
            var links = database.Context.ControlPointLinkEntries.AsNoTracking().Where(l => l.ControlPointId == point.Id).ToList();
            var row = database.Context.TeleporterEntries.AsNoTracking().Single(t => t.Id == ControlPointTeleporterLinks.WildernessLzWaypoint);

            using var world = new WorldTestContext();
            Assert.AreEqual(point.MapContextId, world.Map.MapInfo.MapContextId);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = WaypointTravelTests.CreateManager(world);
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero);   // the pad they stand on
            var lz = WaypointTravelTests.AddWaypoint(manager, world.Map, row.Id, new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, row.Id, (byte)WaypointType.Waypoint));

            // The point's teleporters are the world's (DynamicObjectManager.Instance), as on the server.
            DynamicObjectManager.Instance.Teleporters[row.Id] = lz;
            try
            {
                ControlPoints.Instance.Load(new[] { point }, links, null);
                var cp = ControlPoints.Instance.ById(point.Id);
                Assert.IsTrue(cp.Waypoints.Contains(row.Id));

                Assert.IsTrue(ControlPoints.Instance.SetOwner(cp, ControlPoints.Bane, null));
                Assert.IsFalse(ControlPoints.Instance.IsOpen(row.Id));
                Assert.IsTrue(((WaypointInfo)lz.ObjectData).Contested);
                Assert.IsFalse(Listed(manager, client, row.Id), "not listed");

                manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = row.Id });
                Assert.IsNull(client.PendingTransfer);
                Assert.AreEqual(Vector3.Zero, client.Player.Position, "nobody teleports in");
                Assert.IsTrue(WorldTestContext.Drain(client).Any(p => p.Message is CallMethodMessage { Packet: TeleportFailedPacket }));

                Assert.IsTrue(ControlPoints.Instance.SetOwner(cp, ControlPoints.Afs, null));
                Assert.IsTrue(ControlPoints.Instance.IsOpen(row.Id));
                Assert.IsTrue(Listed(manager, client, row.Id), "listed again");

                manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = row.Id });
                Assert.AreEqual(ClientState.Teleporting, client.State, "the AFS's again, it takes them");
            }
            finally
            {
                ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
                DynamicObjectManager.Instance.Teleporters.Remove(row.Id);
            }
        }

        private static bool Listed(DynamicObjectManager manager, Client client, uint id) =>
            manager.CreateListOfWaypoints(client, WaypointType.Waypoint).Values.SelectMany(list => list.Waypoints).Any(w => w.WaypointId == id);

        private static List<(uint ControlPointId, byte Kind, uint ObjectId)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.ControlPointLinkEntries.AsNoTracking().ToList()
                .Select(link => (link.ControlPointId, link.Kind, link.ObjectId)).ToList();
    }
}
