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
    /// The Foreas Base pads to the Awake logos where a player who knew the base places them
    /// (Foreas_hq_logos_pads, ForeasHqLogosPads): on the corridor ring's east side, setting
    /// players down in the logos room's hall.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ForeasHqLogosPadsTests
    {
        private const string Migration = "20271001000000_Foreas_hq_logos_pads";
        private const string Previous = "20270930000000_Skeev_free_when_unreserved";

        /// <summary>As the player gives them: the pad to the logos, where it sets them down, and the logos.</summary>
        private static readonly Vector3 ReportedPad = new(-98.0f, 53.0f, 478.5f);
        private static readonly Vector3 ReportedArrival = new(-118f, 48.2f, 560f);
        private static readonly Vector3 AwakeLogos = new(-134.14f, 48.56f, 534.25f);

        /// <summary>The headquarters' floor, north of the alcove.</summary>
        private static readonly Vector3 Headquarters = new(-42f, 117.2f, 470f);

        /// <summary>The corridor under the headquarters, between the main hall and the south rooms.</summary>
        private static readonly Vector3 CorridorUnderHq = new(-48f, 52.6f, 424f);

        /// <summary>Where the corridor ring meets the way in from the hall.</summary>
        private static readonly Vector3 RingTee = new(-88f, 52.5f, 480f);

        /// <summary>The room east of the main hall, at the lower levels' other end.</summary>
        private static readonly Vector3 EastRoom = new(24f, 48f, 480f);

        [TestMethod]
        public void ThePadsMoveTheLinksStayAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Teleporters(database);
            var links = Links(database);
            foreach (var move in ForeasHqLogosPads.Moves)
                AssertAt(before[move.Id], move.From, "as Foreas_hq_teleporters put it");

            migrator.Migrate(Migration);
            var after = Teleporters(database);
            foreach (var move in ForeasHqLogosPads.Moves)
            {
                AssertAt(after[move.Id], move.To, "moved");
                Assert.AreEqual(before[move.Id].Description, after[move.Id].Description);
            }

            AssertAt(after[ForeasHqTeleporters.Alcove], Place(before[ForeasHqTeleporters.Alcove]), "the alcove's pad stays");
            CollectionAssert.AreEquivalent(links, Links(database), "the links stay");
            Assert.IsLessThan(0.6f, Vector3.Distance(ReportedPad, Position(after[ForeasHqTeleporters.RingNorth])), "the pad where the player has it");
            Assert.IsLessThan(0.3f, Vector3.Distance(ReportedArrival, Position(after[ForeasHqTeleporters.LogosRoom])), "the arrival where the player has it");

            migrator.Migrate(Previous);
            var back = Teleporters(database);
            foreach (var move in ForeasHqLogosPads.Moves)
                AssertAt(back[move.Id], move.From, "Down puts it back");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesAPadAGmMoved()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            foreach (var sql in ForeasHqLogosPads.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var move in ForeasHqLogosPads.Moves)
                AssertAt(Teleporters(database)[move.Id], move.To, "twice");

            // A GM moved the logos room's pad: left where they put it, both ways.
            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE teleporter SET pos_x = -120 WHERE id = {ForeasHqTeleporters.LogosRoom}");
            migrator.Migrate(Migration);
            var moved = Teleporters(database);
            Assert.AreEqual(-120.0, moved[ForeasHqTeleporters.LogosRoom].PosX, 1e-6, "Up moves the GM's pad");
            Assert.AreEqual(534.25, moved[ForeasHqTeleporters.LogosRoom].PosZ, 1e-6);
            AssertAt(moved[ForeasHqTeleporters.RingNorth], ForeasHqLogosPads.Moves.Single(m => m.Id == ForeasHqTeleporters.RingNorth).To, "the rest move");
            migrator.Migrate(Previous);
            Assert.AreEqual(-120.0, Teleporters(database)[ForeasHqTeleporters.LogosRoom].PosX, 1e-6, "Down moves the GM's pad");
        }

        [TestMethod]
        public void OnMySqlThePadsMoveAndMoveBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in ForeasHqLogosPads.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ForeasHqLogosPads.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>
        /// Each pad on its floor, clear of the walls and of the ways through the lower levels, and
        /// each traveller facing on from where they land.
        /// </summary>
        [TestMethod]
        public void EachPadStandsOnItsFloorOffTheWaysThrough()
        {
            var nav = DivideNavMesh();
            var alcove = Final(ForeasHqTeleporters.Alcove);
            var ringSouth = Final(ForeasHqTeleporters.RingSouth);
            var ringEast = Final(ForeasHqTeleporters.RingNorth);
            var logosRoom = Final(ForeasHqTeleporters.LogosRoom);

            foreach (var pad in new[] { alcove, ringSouth, ringEast, logosRoom })
            {
                var at = Position(pad);
                Assert.IsTrue(nav.IsOnMesh(at), $"{pad.Id} off the floor");
                var ground = nav.GroundHeight(at).Value;
                Assert.IsTrue(ground - at.Y is >= 0f and < 0.35f, $"{pad.Id}: the floor {ground - at.Y} m over the row");
                Assert.IsGreaterThan(2f, nav.DistanceToWall(at, 8f) ?? 0f, $"{pad.Id} against a wall");
            }

            Assert.IsTrue(nav.Reachable(Position(ringEast), Position(ringSouth)) == true, "the ring's pads apart");
            Assert.IsTrue(nav.Reachable(Position(ringEast), CorridorUnderHq) == true, "the pad to the logos off the lower levels");
            Assert.IsTrue(nav.Reachable(Position(logosRoom), AwakeLogos) == true, "the logos room's pad apart from the logos");
            Assert.IsGreaterThan(15f, Vector3.Distance(Position(logosRoom), AwakeLogos), "the pad on the logos");

            // Nobody going through the lower levels, or from the hall to the pad up, walks over a pad.
            AssertClear(nav, CorridorUnderHq, EastRoom, Position(ringSouth), Position(ringEast));
            AssertClear(nav, CorridorUnderHq, RingTee, Position(ringSouth), Position(ringEast));
            AssertClear(nav, CorridorUnderHq, Position(ringSouth), Position(ringEast));
            AssertClear(nav, Position(ringSouth), RingTee, Position(ringEast));

            // Facing on: into the headquarters, at the pad to the logos, out to the hall, at the logos.
            AssertFaces(alcove, Headquarters);
            AssertFaces(ringSouth, Position(ringEast));
            AssertFaces(ringEast, RingTee);
            AssertFaces(logosRoom, AwakeLogos);
        }

        /// <summary>Onto each pad as they now are: onto its partner, a metre over it, facing as its row does.</summary>
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

                var origin = Final(link.FromTeleporterId);
                var from = Put(manager, world.Map, origin, origin);
                var to = Put(manager, world.Map, Final(link.ToTeleporterId), origin);

                manager.LoadLocalTeleporterLinks(links.Where(l => l.FromTeleporterId == link.FromTeleporterId));
                manager.DynamicObjectProximityWorker(world.Map, from, 0);
                Assert.IsNotNull(client.PendingTransfer, $"{link.FromTeleporterId} takes nobody");

                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(world.Map);

                Assert.IsLessThan(0.01f, Vector3.Distance(to.Position + new Vector3(0, 1, 0), client.Player.Position), $"{link.FromTeleporterId} onto {link.ToTeleporterId}");
                Assert.AreEqual(Final(link.ToTeleporterId).Rotation, client.Player.Rotation, 1e-4, $"{link.ToTeleporterId}'s facing");
            }
        }

        /// <summary>A row as it stands once both migrations have run.</summary>
        private static TeleporterEntry Final(uint id)
        {
            var row = ForeasHqTeleporters.Rows.Single(r => r.Id == id);
            var move = ForeasHqLogosPads.Moves.SingleOrDefault(m => m.Id == id);
            var place = move?.To ?? new ForeasHqLogosPads.Place(row.X, row.Y, row.Z, row.Rotation);

            return new TeleporterEntry
            {
                Id = id, ClassId = ForeasHqTeleporters.HumanTeleporter, Type = 1, Description = row.Description,
                PosX = place.X, PosY = place.Y, PosZ = place.Z, Rotation = place.Rotation, MapContextId = ForeasHqTeleporters.MapContextId
            };
        }

        private static ForeasHqLogosPads.Place Place(TeleporterEntry row) => new(row.PosX, row.PosY, row.PosZ, row.Rotation);

        private static Vector3 Position(TeleporterEntry row) => new((float)row.PosX, (float)row.PosY, (float)row.PosZ);

        private static void AssertAt(TeleporterEntry row, ForeasHqLogosPads.Place place, string what)
        {
            Assert.AreEqual(place.X, row.PosX, 1e-4, $"{row.Id} x: {what}");
            Assert.AreEqual(place.Y, row.PosY, 1e-4, $"{row.Id} y: {what}");
            Assert.AreEqual(place.Z, row.PosZ, 1e-4, $"{row.Id} z: {what}");
            Assert.AreEqual(place.Rotation, row.Rotation, 1e-4, $"{row.Id} rotation: {what}");
        }

        private static void AssertClear(NavMeshQuery nav, Vector3 start, Vector3 end, params Vector3[] pads)
        {
            var path = nav.FindPath(start, end, out var complete);
            Assert.IsTrue(complete, $"no way from {start} to {end}");

            // The route's corners, and the straight runs between them: none within 2.5 m of a pad
            // other than the ones it starts or ends on.
            foreach (var pad in pads)
            {
                var at = new Vector2(pad.X, pad.Z);
                for (var i = 1; i < path.Count; i++)
                    Assert.IsGreaterThan(2.5f, DistanceToSegment(at, new Vector2(path[i - 1].X, path[i - 1].Z), new Vector2(path[i].X, path[i].Z)),
                        $"the way from {start} to {end} passes over the pad at {pad}");
            }
        }

        private static void AssertFaces(TeleporterEntry row, Vector3 toward)
        {
            var facing = new Vector2(-MathF.Sin((float)row.Rotation), -MathF.Cos((float)row.Rotation));
            var direction = Vector2.Normalize(new Vector2(toward.X - (float)row.PosX, toward.Z - (float)row.PosZ));
            Assert.IsGreaterThan(0.9f, Vector2.Dot(facing, direction), $"{row.Id} does not face {toward}");
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.LengthSquared() < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }

        private static DynamicObject Put(DynamicObjectManager manager, MapChannel map, TeleporterEntry row, TeleporterEntry origin)
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
