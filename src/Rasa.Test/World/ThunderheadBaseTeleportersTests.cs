extern alias RasaGame;

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

    /// <summary>Thunderhead Base's local teleporters, linked as its signs say (Thunderhead_base_teleporters, ThunderheadBaseTeleporters).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ThunderheadBaseTeleportersTests
    {
        private const string Migration = "20270123000000_Thunderhead_base_teleporters";
        private const string Previous = "20270122000000_Bane_mortars";

        /// <summary>The map's ten pads in the base, from adv_arieki_ligo_thunderhead.map, and the row on each.</summary>
        private static readonly (uint Row, double X, double Y, double Z)[] MapPads =
        {
            (542, 564.7672729492188, 342.0835266113281, -432.13189697265625),
            (543, 575.7902221679688, 342.0835266113281, -420.98663330078125),
            (544, 637.0, 338.9996337890625, -507.0),
            (545, 651.0, 338.9996337890625, -493.0),
            (547, 633.1351318359375, 376.4999694824219, -605.8204956054688),
            (548, 643.75, 376.4999694824219, -595.25),
            (980, 640.3850708007812, 382.9999694824219, -509.5959777832031),
            (981, 653.4601440429688, 382.9999694824219, -496.35986328125),
            (982, 659.4149169921875, 382.9999694824219, -490.4725036621094),
            (983, 634.4654541015625, 382.9999694824219, -515.3790893554688),
        };

        [TestMethod]
        public void TheBasesTenPadsAreLinkedInFivePairsBothWays()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var linksBefore = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList();
            Assert.IsFalse(database.Context.TeleporterEntries.AsNoTracking().Any(row => row.Id >= 980 && row.Id <= 983), "980 to 983 free");

            migrator.Migrate(Migration);
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList();
            Assert.HasCount(linksBefore.Count + 10, links);

            // Every pad in the base has its row on it, a local teleporter of the base's class.
            foreach (var pad in MapPads)
            {
                var row = teleporters[pad.Row];
                Assert.AreEqual((byte)1, row.Type, $"{pad.Row}");
                Assert.AreEqual(ThunderheadBaseTeleporters.MapContextId, row.MapContextId, $"{pad.Row}");
                Assert.AreEqual(ThunderheadBaseTeleporters.TeleporterClass, row.ClassId, $"{pad.Row}");
                Assert.IsLessThan(0.15, Math.Sqrt(Math.Pow(row.PosX - pad.X, 2) + Math.Pow(row.PosZ - pad.Z, 2)), $"{pad.Row} on its pad");
                Assert.AreEqual(ThunderheadBaseTeleporters.PadHeight, row.PosY - pad.Y, 0.01, $"{pad.Row} as high over its pad as the others");
            }

            var mine = links.Where(link => MapPads.Any(pad => pad.Row == link.FromTeleporterId)).ToDictionary(link => link.FromTeleporterId, link => link.ToTeleporterId);
            Assert.HasCount(10, mine, "one link from each pad");
            foreach (var (from, to) in mine)
                Assert.AreEqual(from, mine[to], $"{from} -> {to} goes both ways");

            // As the signs: the Frontlines (by the dropship) to the Hospital and the HQ, the
            // Hospital to the HQ, and the lifts up to the HQ's floor.
            Assert.AreEqual(547u, mine[542]);
            Assert.AreEqual(ThunderheadBaseTeleporters.HqToFrontlines, mine[543]);
            Assert.AreEqual(ThunderheadBaseTeleporters.HqToHospital, mine[548]);
            Assert.AreEqual(ThunderheadBaseTeleporters.HqLiftWest, mine[544]);
            Assert.AreEqual(ThunderheadBaseTeleporters.HqLiftEast, mine[545]);

            // A lift goes straight up the 44 m to the pad across from it.
            foreach (var (ground, upper) in new[] { (544u, ThunderheadBaseTeleporters.HqLiftWest), (545u, ThunderheadBaseTeleporters.HqLiftEast) })
            {
                var a = teleporters[ground];
                var b = teleporters[upper];
                Assert.IsLessThan(5.0, Math.Sqrt(Math.Pow(a.PosX - b.PosX, 2) + Math.Pow(a.PosZ - b.PosZ, 2)), $"{ground} under {upper}");
                Assert.AreEqual(44.0, b.PosY - a.PosY, 0.5, $"{ground} to {upper}");
            }

            // The base's other marker row, and the rest of Thunderhead's, still go nowhere.
            foreach (var unlinked in new uint[] { 546, 549, 550, 576 })
                Assert.IsFalse(links.Any(link => link.FromTeleporterId == unlinked || link.ToTeleporterId == unlinked), $"{unlinked}");

            migrator.Migrate(Previous);
            Assert.IsFalse(database.Context.TeleporterEntries.AsNoTracking().Any(row => row.Id >= 980 && row.Id <= 983), "Down takes the rows out");
            CollectionAssert.AreEquivalent(
                linksBefore.Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList(),
                database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList().Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList(),
                "and the links");
        }

        /// <summary>
        /// Over the navmesh, its floor within half a metre under the row or a metre over it: one lands a
        /// metre over the row and is within 2 m of it standing. The base's six rows have the floor
        /// 0.16 m under to 0.10 m over them, the HQ floor's four 0.19 to 0.28 m over.
        /// </summary>
        [TestMethod]
        public void EveryPadStandsOnTheBasesWalkableGround()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var query = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_arieki_ligo_thunderhead"));
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);

            foreach (var pad in MapPads)
            {
                var row = teleporters[pad.Row];
                var position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                var ground = query.Nearest(position);

                Assert.IsTrue(ground.HasValue, $"{pad.Row} off the navmesh");
                Assert.IsLessThan(0.1f, Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(ground.Value.X, ground.Value.Z)), $"{pad.Row} over walkable ground");
                Assert.IsTrue(ground.Value.Y - position.Y is >= -0.5f and < 1, $"{pad.Row}: the ground is {ground.Value.Y - position.Y} m above the row");
            }
        }

        /// <summary>The server keeps every link the migration made, and each takes a player to the pad its row names.</summary>
        [TestMethod]
        public void SteppingOntoEachPadTakesThePlayerToTheOtherEnd()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList()
                .Where(link => MapPads.Any(pad => pad.Row == link.FromTeleporterId)).ToList();
            Assert.HasCount(10, links);

            foreach (var link in links)
            {
                using var world = new WorldTestContext();
                var client = world.CreateClient();
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);
                long now = 1000;
                var manager = WaypointTravelTests.CreateManager(world, clock: () => now);

                // The pad the player stands on at the world's origin, the others where they are from it.
                var origin = teleporters[link.FromTeleporterId];
                DynamicObject from = null;
                foreach (var pad in MapPads)
                {
                    var row = teleporters[pad.Row];
                    var added = Pad(manager, world.Map, row, origin);
                    if (row.Id == link.FromTeleporterId)
                        from = added;
                }

                manager.LoadLocalTeleporterLinks(links);
                manager.DynamicObjectProximityWorker(world.Map, from, 0);
                Assert.IsNotNull(client.PendingTransfer, $"{link.FromTeleporterId} takes them");

                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(world.Map);

                var to = teleporters[link.ToTeleporterId];
                var expected = Offset(to, origin) + new Vector3(0, 1, 0);
                Assert.IsLessThan(0.01f, Vector3.Distance(expected, client.Player.Position), $"{link.FromTeleporterId} onto {link.ToTeleporterId}");
            }
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesALinkAGmMade()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES (542, 548)");

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.Contains("542 548", once, "the GM's link stands");
            Assert.DoesNotContain("542 547", once);
            Assert.Contains("547 542", once, "the rest go in");

            foreach (var sql in ThunderheadBaseTeleporters.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            Assert.Contains("542 548", Snapshot(database), "Down leaves the GM's link");
        }

        [TestMethod]
        public void OnMySqlTheRowsAndLinksGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in ThunderheadBaseTeleporters.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ThunderheadBaseTeleporters.Down())
                StringAssert.Contains(down, sql);
        }

        private static Vector3 Offset(TeleporterEntry row, TeleporterEntry origin) =>
            new((float)(row.PosX - origin.PosX), (float)(row.PosY - origin.PosY), (float)(row.PosZ - origin.PosZ));

        private static DynamicObject Pad(DynamicObjectManager manager, MapChannel map, TeleporterEntry row, TeleporterEntry origin)
        {
            var pad = new DynamicObject
            {
                Position = Offset(row, origin),
                Rotation = row.Rotation,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                DynamicObjectType = DynamicObjectType.LocalTeleporter,
                ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter)
            };
            manager.Teleporters.Add(row.Id, pad);
            map.Teleporters.Add(row.Id, pad);
            return pad;
        }

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>("SELECT from_teleporter_id || ' ' || to_teleporter_id AS Value FROM local_teleporter_link")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
