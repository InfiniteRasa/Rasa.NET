using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// The way back up from the Few logos in Howling Maw (Few_shaft_teleporter, FewShaftTeleporter):
    /// the room is at the foot of the Dead Zone Power Station's mine shaft, which is gone down ring
    /// by ring and has no way back up; a pad behind the logos takes a player up to the base floor,
    /// and nothing takes anyone down.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FewShaftTeleporterTests
    {
        private const string Migration = "20270927000000_Few_shaft_teleporter";
        private const string Previous = "20270926000000_Landing_zone_mech_missiles";

        private static readonly Vector3 FewLogos = new(-482f, 123.5f, -503f);
        private static readonly Vector3 DeadZoneWaypoint = new(-517.3f, 201f, -354.2f);

        /// <summary>The shaft's floor before the cave mouth, where the way down ends.</summary>
        private static readonly Vector3 ShaftFloor = new(-487f, 124f, -432f);

        /// <summary>The middle of the shaft, across the rings (x -507 to -467, z -446 to -406).</summary>
        private static readonly Vector2 ShaftMiddle = new(-487f, -426f);

        [TestMethod]
        public void ThePadAndItsLandingGoInLinkedOneWayAndComeOutAgain()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var linksBefore = Links(database);
            Assert.IsFalse(Teleporters(database).ContainsKey(FewShaftTeleporter.Pad), "988 free");
            Assert.IsFalse(Teleporters(database).ContainsKey(FewShaftTeleporter.Landing), "989 free");

            migrator.Migrate(Migration);
            var teleporters = Teleporters(database);
            var pad = teleporters[FewShaftTeleporter.Pad];
            var landing = teleporters[FewShaftTeleporter.Landing];

            Assert.AreEqual(7895u, pad.ClassId, "a human teleporter, shown");
            Assert.AreEqual(0u, landing.ClassId, "a landing no client is shown");
            foreach (var row in new[] { pad, landing })
            {
                Assert.AreEqual((byte)1, row.Type, row.Description);
                Assert.AreEqual(FewShaftTeleporter.MapContextId, row.MapContextId, row.Description);
            }

            var links = Links(database);
            Assert.HasCount(linksBefore.Count + 1, links);
            CollectionAssert.Contains(links, (FewShaftTeleporter.Pad, FewShaftTeleporter.Landing), "the pad goes up");
            Assert.IsFalse(links.Any(link => link.From == FewShaftTeleporter.Landing), "nothing comes down");

            migrator.Migrate(Previous);
            Assert.IsFalse(Teleporters(database).Keys.Any(id => id is FewShaftTeleporter.Pad or FewShaftTeleporter.Landing), "Down takes the rows out");
            CollectionAssert.AreEquivalent(linksBefore, Links(database), "and the link");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesARowAGmPutOnItsId()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var once = (Teleporters(database).Count, Links(database).Count);
            foreach (var sql in FewShaftTeleporter.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            Assert.AreEqual(once, (Teleporters(database).Count, Links(database).Count), "twice");

            // Someone else's row on the pad's id: left alone, and no link made from it.
            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) VALUES ({FewShaftTeleporter.Pad}, 0, 2, 'GM', 1, 2, 3, 0, 1220)");
            migrator.Migrate(Migration);
            Assert.AreEqual("GM", Teleporters(database)[FewShaftTeleporter.Pad].Description);
            Assert.IsFalse(Links(database).Any(link => link.From == FewShaftTeleporter.Pad));
            migrator.Migrate(Previous);
            Assert.AreEqual("GM", Teleporters(database)[FewShaftTeleporter.Pad].Description, "Down leaves it");
        }

        [TestMethod]
        public void OnMySqlTheRowsAndTheLinkGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in FewShaftTeleporter.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in FewShaftTeleporter.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>
        /// The reason for it: the room and the shaft's floor are one closed region of the navmesh,
        /// apart from the base. The pad is on the room's floor, behind the logos and off the way
        /// in; the landing is on the base floor, clear of the shaft, and walks to the waypoint.
        /// </summary>
        [TestMethod]
        public void ThePadIsBehindTheLogosAndTheLandingOnTheBaseFloor()
        {
            var nav = HowlingMawNavMesh();
            var pad = Position(FewShaftTeleporter.Pad);
            var landing = Position(FewShaftTeleporter.Landing);

            Assert.IsTrue(nav.Reachable(ShaftFloor, FewLogos) == true, "the shaft's floor does not reach the logos");
            Assert.IsTrue(nav.Reachable(FewLogos, DeadZoneWaypoint) == false, "the logos is joined to the base on foot");

            // The pad: on the room's floor, reached from the logos, and not on the way to it.
            Assert.IsTrue(nav.IsOnMesh(pad), "the pad is off the floor");
            Assert.AreEqual(nav.GroundHeight(pad).Value, pad.Y, 0.35f, "the pad's height");
            Assert.IsTrue(nav.Reachable(FewLogos, pad) == true, "no floor from the logos to the pad");
            Assert.IsTrue(nav.DistanceToWall(pad, 6f) > 3f, "the pad against a wall");
            var wayIn = nav.FindPath(ShaftFloor, FewLogos, out var complete);
            Assert.IsTrue(complete);
            // The route's corners, and the straight runs between them: none within 2.5 m of the pad.
            var padAt = new Vector2(pad.X, pad.Z);
            for (var i = 1; i < wayIn.Count; i++)
                Assert.IsTrue(DistanceToSegment(padAt, new Vector2(wayIn[i - 1].X, wayIn[i - 1].Z), new Vector2(wayIn[i].X, wayIn[i].Z)) > 2.5f,
                    "the way from the shaft to the logos passes over the pad");
            Assert.IsTrue(pad.Z < FewLogos.Z - 8f, "the pad is not behind the logos");

            // The landing: on the base floor, walking to the waypoint, clear of the shaft and facing away from it.
            Assert.IsTrue(nav.IsOnMesh(landing), "the landing is off the floor");
            Assert.AreEqual(nav.GroundHeight(landing).Value, landing.Y, 0.35f, "the landing's height");
            Assert.IsTrue(nav.Reachable(landing, DeadZoneWaypoint) == true, "no floor from the landing to the waypoint");
            Assert.IsTrue(nav.DistanceToWall(landing, 8f) > 3f, "the landing against a wall");
            Assert.IsTrue(MathF.Abs(landing.Z - (-406f)) > 6f && Vector2.Distance(new Vector2(landing.X, landing.Z), ShaftMiddle) > 25f, "the landing at the shaft's rim");
            var rotation = (float)FewShaftTeleporter.Rows.Single(row => row.Id == FewShaftTeleporter.Landing).Rotation;
            var facing = new Vector2(-MathF.Sin(rotation), -MathF.Cos(rotation));
            Assert.IsTrue(Vector2.Dot(facing, Vector2.Normalize(ShaftMiddle - new Vector2(landing.X, landing.Z))) < -0.9f, "the landing faces the shaft");
        }

        /// <summary>Onto the pad: up to the landing. Onto the landing: nowhere.</summary>
        [TestMethod]
        public void ThePadTakesThePlayerUpAndTheLandingTakesNobodyDown()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            long now = 1000;
            var manager = WaypointTravelTests.CreateManager(world, clock: () => now);

            // The pad where the player stands, at the world's origin; the landing where it is from it.
            var origin = Row(FewShaftTeleporter.Pad);
            var pad = Place(manager, world.Map, origin, origin);
            var landing = Place(manager, world.Map, Row(FewShaftTeleporter.Landing), origin);
            manager.LoadLocalTeleporterLinks(new[] { new LocalTeleporterLinkEntry { FromTeleporterId = FewShaftTeleporter.Pad, ToTeleporterId = FewShaftTeleporter.Landing } });

            manager.DynamicObjectProximityWorker(world.Map, pad, 0);
            Assert.IsNotNull(client.PendingTransfer, "the pad takes nobody");
            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);

            Assert.IsLessThan(0.01f, Vector3.Distance(landing.Position + new Vector3(0, 1, 0), client.Player.Position), "not on the landing");
            Assert.AreEqual(Math.PI, client.Player.Rotation, 1e-4, "facing as the landing does");

            // Someone standing on the landing: it takes them nowhere, however long they stand there.
            var other = world.CreateClient();
            other.Player.PlaceAt(landing.Position + new Vector3(0, 1, 0));
            CellManager.Instance.AddToWorld(other);
            WorldTestContext.Drain(other);
            manager.DynamicObjectProximityWorker(world.Map, landing, 0);
            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            Assert.IsNull(other.PendingTransfer, "the landing takes them somewhere");
            Assert.IsLessThan(0.01f, Vector3.Distance(landing.Position + new Vector3(0, 1, 0), other.Player.Position));
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.LengthSquared() < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }

        private static FewShaftTeleporter.Row RowOf(uint id) => FewShaftTeleporter.Rows.Single(row => row.Id == id);

        private static Vector3 Position(uint id)
        {
            var row = RowOf(id);
            return new Vector3((float)row.X, (float)row.Y, (float)row.Z);
        }

        private static TeleporterEntry Row(uint id)
        {
            var row = RowOf(id);
            return new TeleporterEntry
            {
                Id = row.Id, ClassId = row.ClassId, Type = 1, Description = row.Description,
                PosX = row.X, PosY = row.Y, PosZ = row.Z, Rotation = row.Rotation, MapContextId = FewShaftTeleporter.MapContextId
            };
        }

        private static DynamicObject Place(DynamicObjectManager manager, MapChannel map, TeleporterEntry row, TeleporterEntry origin)
        {
            var obj = new DynamicObject
            {
                Position = new Vector3((float)(row.PosX - origin.PosX), (float)(row.PosY - origin.PosY), (float)(row.PosZ - origin.PosZ)),
                Rotation = row.Rotation,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                DynamicObjectType = DynamicObjectType.LocalTeleporter,
                ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter)
            };
            manager.Teleporters.Add(row.Id, obj);
            map.Teleporters.Add(row.Id, obj);
            return obj;
        }

        private static Dictionary<uint, TeleporterEntry> Teleporters(OrdinaryLootTestDatabase database) =>
            database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);

        private static List<(uint From, uint To)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList()
                .Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList();

        private static NavMeshQuery HowlingMawNavMesh()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "navmesh not found");
            return TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), "adv_foreas_howlingmaw1"));
        }
    }
}
