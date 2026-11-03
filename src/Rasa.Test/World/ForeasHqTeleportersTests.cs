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
    /// The pads under Foreas Base on Concordia Divide (Foreas_hq_teleporters, ForeasHqTeleporters):
    /// the headquarters' alcove and the lower levels, and the lower levels and the Awake logos
    /// room, each pair linked both ways.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ForeasHqTeleportersTests
    {
        private const string Migration = "20270929000000_Foreas_hq_teleporters";
        private const string Previous = "20270928000000_Minos_warnet_nests_hung";

        /// <summary>The headquarters' floor, north of the alcove, open to the base's yard.</summary>
        private static readonly Vector3 Headquarters = new(-42f, 117.2f, 470f);

        /// <summary>The corridor under the headquarters, between the main hall and the south rooms.</summary>
        private static readonly Vector3 CorridorUnderHq = new(-48f, 52.6f, 424f);

        /// <summary>Where the corridor ring at the lower levels' west end meets the way in from the hall.</summary>
        private static readonly Vector3 RingTee = new(-88f, 52.5f, 480f);

        /// <summary>The room east of the main hall, at the lower levels' other end.</summary>
        private static readonly Vector3 EastRoom = new(24f, 48f, 480f);

        /// <summary>The Awake logos' dispenser.</summary>
        private static readonly Vector3 AwakeLogos = new(-134.14f, 48.56f, 534.25f);

        [TestMethod]
        public void ThePadsGoInLinkedInPairsBothWaysAndComeOutAgain()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Teleporters(database);
            var linksBefore = Links(database);
            Assert.IsFalse(before.Keys.Any(InRange), "948 to 951 free");
            Assert.IsFalse(before.Values.Any(row => row.Type == 1 && row.MapContextId == ForeasHqTeleporters.MapContextId && Under(row)),
                "no local teleporter under Foreas Base before");

            migrator.Migrate(Migration);
            var teleporters = Teleporters(database);
            var links = Links(database);

            foreach (var expected in ForeasHqTeleporters.Rows)
            {
                var row = teleporters[expected.Id];
                Assert.AreEqual(7895u, row.ClassId, $"{row.Id}: a human teleporter, shown");
                Assert.AreEqual((byte)1, row.Type, $"{row.Id}: local");
                Assert.AreEqual(ForeasHqTeleporters.MapContextId, row.MapContextId, $"{row.Id}: Concordia Divide");
                Assert.AreEqual(expected.X, row.PosX, 1e-4, $"{row.Id} x");
                Assert.AreEqual(expected.Y, row.PosY, 1e-4, $"{row.Id} y");
                Assert.AreEqual(expected.Z, row.PosZ, 1e-4, $"{row.Id} z");
            }

            Assert.HasCount(linksBefore.Count + 4, links);
            CollectionAssert.IsSubsetOf(new[]
            {
                (ForeasHqTeleporters.Alcove, ForeasHqTeleporters.RingSouth), (ForeasHqTeleporters.RingSouth, ForeasHqTeleporters.Alcove),
                (ForeasHqTeleporters.RingNorth, ForeasHqTeleporters.LogosRoom), (ForeasHqTeleporters.LogosRoom, ForeasHqTeleporters.RingNorth),
            }, links);

            migrator.Migrate(Previous);
            Assert.IsFalse(Teleporters(database).Keys.Any(InRange), "Down takes the rows out");
            CollectionAssert.AreEquivalent(linksBefore, Links(database), "and the links");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesARowAGmPutOnAnId()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var once = (Teleporters(database).Count, Links(database).Count);
            foreach (var sql in ForeasHqTeleporters.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            Assert.AreEqual(once, (Teleporters(database).Count, Links(database).Count), "twice");

            // Someone else's row on the logos room pad's id: left alone, and no link made to or from it.
            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) VALUES ({ForeasHqTeleporters.LogosRoom}, 0, 2, 'GM', 1, 2, 3, 0, 1220)");
            migrator.Migrate(Migration);
            Assert.AreEqual("GM", Teleporters(database)[ForeasHqTeleporters.LogosRoom].Description);
            var links = Links(database);
            Assert.IsFalse(links.Any(link => link.From == ForeasHqTeleporters.LogosRoom || link.To == ForeasHqTeleporters.LogosRoom), "linked to the GM's row");
            CollectionAssert.Contains(links, (ForeasHqTeleporters.Alcove, ForeasHqTeleporters.RingSouth), "the other pair goes in");
            migrator.Migrate(Previous);
            Assert.AreEqual("GM", Teleporters(database)[ForeasHqTeleporters.LogosRoom].Description, "Down leaves it");
        }

        [TestMethod]
        public void OnMySqlTheRowsAndTheLinksGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in ForeasHqTeleporters.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ForeasHqTeleporters.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>
        /// The reason for them: no floor joins the headquarters to the lower levels, or the lower
        /// levels to the logos room. Each pad stands on its floor, clear of the walls and of the
        /// ways through, and each traveller faces on from where they land.
        /// </summary>
        [TestMethod]
        public void EachPadStandsOnItsFloorOffTheWaysThrough()
        {
            var nav = DivideNavMesh();
            var alcove = Position(ForeasHqTeleporters.Alcove);
            var ringSouth = Position(ForeasHqTeleporters.RingSouth);
            var ringNorth = Position(ForeasHqTeleporters.RingNorth);
            var logosRoom = Position(ForeasHqTeleporters.LogosRoom);

            Assert.IsTrue(nav.Reachable(Headquarters, CorridorUnderHq) == false, "the headquarters joined to the lower levels on foot");
            Assert.IsTrue(nav.Reachable(CorridorUnderHq, AwakeLogos) == false, "the lower levels joined to the logos room on foot");

            foreach (var pad in new[] { alcove, ringSouth, ringNorth, logosRoom })
            {
                Assert.IsTrue(nav.IsOnMesh(pad), $"{pad} off the floor");
                var ground = nav.GroundHeight(pad).Value;
                Assert.IsTrue(ground - pad.Y is >= 0f and < 0.35f, $"{pad}: the floor {ground - pad.Y} m over the row");
                Assert.IsGreaterThan(3f, nav.DistanceToWall(pad, 8f) ?? 0f, $"{pad} against a wall");
            }

            Assert.IsTrue(nav.Reachable(alcove, Headquarters) == true, "the alcove off the headquarters' floor");
            Assert.IsTrue(nav.Reachable(ringSouth, CorridorUnderHq) == true, "the ring's south pad off the lower levels");
            Assert.IsTrue(nav.Reachable(ringNorth, ringSouth) == true, "the ring's pads apart");
            Assert.IsTrue(nav.Reachable(logosRoom, AwakeLogos) == true, "the logos room's pad apart from the logos");
            Assert.IsGreaterThan(15f, Vector3.Distance(logosRoom, AwakeLogos), "the pad on the logos");

            // Nobody going through the lower levels, or round the ring to the other pad, walks over a pad.
            AssertClear(nav, CorridorUnderHq, EastRoom, ringSouth, ringNorth);
            AssertClear(nav, CorridorUnderHq, RingTee, ringSouth, ringNorth);
            AssertClear(nav, RingTee, ringNorth, ringSouth);
            AssertClear(nav, RingTee, ringSouth, ringNorth);

            // Facing on: into the headquarters, along the ring to the other pad, at the logos.
            AssertFaces(ForeasHqTeleporters.Alcove, Headquarters);
            AssertFaces(ForeasHqTeleporters.RingSouth, ringNorth);
            AssertFaces(ForeasHqTeleporters.RingNorth, ringSouth);
            AssertFaces(ForeasHqTeleporters.LogosRoom, AwakeLogos);
        }

        /// <summary>Onto each pad: onto its partner, a metre over it, facing as its row does.</summary>
        [TestMethod]
        public void SteppingOntoEachPadTakesThePlayerToItsPartner()
        {
            var links = ForeasHqTeleporters.Links
                .Select(link => new LocalTeleporterLinkEntry { FromTeleporterId = link.From, ToTeleporterId = link.To }).ToList();

            foreach (var link in links)
            {
                using var world = new WorldTestContext();
                var client = world.CreateClient();
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);
                long now = 1000;
                var manager = WaypointTravelTests.CreateManager(world, clock: () => now);

                // The pad the player stands on at the world's origin, its partner where it is from it.
                var origin = Row(link.FromTeleporterId);
                var from = Place(manager, world.Map, origin, origin);
                var to = Place(manager, world.Map, Row(link.ToTeleporterId), origin);

                manager.LoadLocalTeleporterLinks(links.Where(l => l.FromTeleporterId == link.FromTeleporterId));
                manager.DynamicObjectProximityWorker(world.Map, from, 0);
                Assert.IsNotNull(client.PendingTransfer, $"{link.FromTeleporterId} takes nobody");

                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(world.Map);

                Assert.IsLessThan(0.01f, Vector3.Distance(to.Position + new Vector3(0, 1, 0), client.Player.Position), $"{link.FromTeleporterId} onto {link.ToTeleporterId}");
                Assert.AreEqual(Row(link.ToTeleporterId).Rotation, client.Player.Rotation, 1e-4, $"{link.ToTeleporterId}'s facing");
            }
        }

        private static bool InRange(uint id) => id is >= ForeasHqTeleporters.FirstTeleporter and <= ForeasHqTeleporters.LastTeleporter;

        /// <summary>Under the base: below the yard, round the headquarters.</summary>
        private static bool Under(TeleporterEntry row) =>
            row.PosY < 100 && row.PosX is > -160 and < 60 && row.PosZ is > 360 and < 600;

        private static void AssertClear(NavMeshQuery nav, Vector3 start, Vector3 end, params Vector3[] pads)
        {
            var path = nav.FindPath(start, end, out var complete);
            Assert.IsTrue(complete, $"no way from {start} to {end}");

            // The route's corners, and the straight runs between them: none within 2.5 m of a pad.
            foreach (var pad in pads)
            {
                var at = new Vector2(pad.X, pad.Z);
                for (var i = 1; i < path.Count; i++)
                    Assert.IsGreaterThan(2.5f, DistanceToSegment(at, new Vector2(path[i - 1].X, path[i - 1].Z), new Vector2(path[i].X, path[i].Z)),
                        $"the way from {start} to {end} passes over the pad at {pad}");
            }
        }

        private static void AssertFaces(uint id, Vector3 toward)
        {
            var row = RowOf(id);
            var facing = new Vector2(-MathF.Sin((float)row.Rotation), -MathF.Cos((float)row.Rotation));
            var direction = Vector2.Normalize(new Vector2(toward.X - (float)row.X, toward.Z - (float)row.Z));
            Assert.IsGreaterThan(0.9f, Vector2.Dot(facing, direction), $"{id} does not face {toward}");
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.LengthSquared() < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }

        private static ForeasHqTeleporters.Row RowOf(uint id) => ForeasHqTeleporters.Rows.Single(row => row.Id == id);

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
                Id = row.Id, ClassId = ForeasHqTeleporters.HumanTeleporter, Type = 1, Description = row.Description,
                PosX = row.X, PosY = row.Y, PosZ = row.Z, Rotation = row.Rotation, MapContextId = ForeasHqTeleporters.MapContextId
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

        private static NavMeshQuery DivideNavMesh()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "navmesh not found");
            return TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), "adv_foreas_concordia_divide"));
        }
    }
}
