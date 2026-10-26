using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>Torden Mires' interiors (Torden_mires_regions, TordenMiresRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TordenMiresRegionsTests
    {
        private const string Migration = "20270621000000_Torden_mires_regions";
        private const string Previous = "20270620000000_Ariochs_lair_region";

        private const int Sky = 1, Ambient = 4, Name = 16;
        private const uint Fort = TordenMiresRegions.FortHarounRegion, Blunder = TordenMiresRegions.BaileysBlunderRegion,
            BaneInterior = TordenMiresRegions.BaneInteriorRegion, Hospital = TordenMiresRegions.HospitalRegion, Command = TordenMiresRegions.CommandRegion;

        /// <summary>Torden Mires' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Mires = new()
        {
            [1] = (1, 13), [10] = (2, 29), [12] = (3, 28), [17] = (4, 20), [13] = (5, 16), [15] = (6, 28), [16] = (7, 28), [14] = (8, 28), [5] = (9, 1),
            [21] = (10, 15), [22] = (11, 15), [4] = (12, 31), [23] = (13, 24), [9] = (14, 4), [8] = (15, 23), [3] = (16, 1), [6] = (17, 1), [18] = (18, 31),
            [20] = (19, 16), [19] = (20, 16), [24] = (21, 0),
        };

        [TestMethod]
        public void TheTunnelIsBaileysBlunder()
        {
            var (before, after) = Migrated();
            var waypoint = new Vector3(-591f, 216.6f, 192f);

            Assert.AreNotEqual(Blunder, Shown(before, waypoint, Sky, underground: true), "the tunnel had no volume");
            Assert.AreEqual(Blunder, Shown(after, waypoint, Sky, underground: true), "at its waypoint");
            Assert.AreEqual(Blunder, Shown(after, waypoint, Name, underground: true), "by name");
            Assert.AreEqual(Blunder, Shown(after, new Vector3(-568f, 216.6f, 0f), Sky, underground: true), "in from the south entrance");
            Assert.AreEqual(Blunder, Shown(after, new Vector3(-580f, 206.9f, 65f), Sky, underground: true), "in the room");
            Assert.AreEqual(Blunder, Shown(after, new Vector3(-700f, 240.6f, 420f), Sky, underground: true), "in the hall");
            Assert.AreEqual(Blunder, Shown(after, new Vector3(-690f, 248.6f, 470f), Sky, underground: true), "up the north ramps");
            Assert.AreNotEqual(Blunder, Shown(after, new Vector3(-715f, 256.1f, 532f), Sky, underground: false), "Tharvox stands outside");
            Assert.AreNotEqual(Blunder, Shown(after, new Vector3(-540f, 211.5f, -95f), Sky, underground: true), "and the pit before the south entrance stays out");
        }

        [TestMethod]
        public void TheChunnelWorksAreTheBaneInterior()
        {
            var (before, after) = Migrated();
            var room = new Vector3(-76f, 262.8f, 960f);

            Assert.AreNotEqual(BaneInterior, Shown(before, room, Sky, underground: true), "the chunnel had no volume");
            Assert.AreEqual(BaneInterior, Shown(after, room, Sky, underground: true), "in the room");
            Assert.AreEqual(BaneInterior, Shown(after, new Vector3(-200f, 263.8f, 880f), Sky, underground: true), "along the chunnel");
            Assert.AreEqual(BaneInterior, Shown(after, new Vector3(10f, 271.8f, 960f), Sky, underground: true), "at the overlook");
            Assert.AreNotEqual(BaneInterior, Shown(after, new Vector3(15f, 257.4f, 905f), Sky, underground: false), "not outside its east entrance");
        }

        [TestMethod]
        public void FortHarounIsItsHallAndCorridors()
        {
            var (before, after) = Migrated();
            var waypoint = new Vector3(621f, 224.3f, 332f);

            Assert.AreNotEqual(Fort, Shown(before, waypoint, Sky, underground: true), "the fort had no volume");
            Assert.AreEqual(Fort, Shown(after, waypoint, Sky, underground: true), "in the hall");
            Assert.AreEqual(Fort, Shown(after, new Vector3(688f, 224.8f, 384f), Sky, underground: true), "at the Medical Bay");
            Assert.AreEqual(Fort, Shown(after, new Vector3(770f, 224.7f, 370f), Sky, underground: true), "at the east gate");
            Assert.AreNotEqual(Fort, Shown(after, new Vector3(540f, 221.6f, 400f), Sky, underground: false), "not outside the west gate");
            Assert.AreNotEqual(Fort, Shown(after, new Vector3(630f, 251.4f, 522f), Sky, underground: false), "nor on the landing pads");
        }

        [TestMethod]
        public void BaylorBaseHasItsHospitalAndHeadquarters()
        {
            var (before, after) = Migrated();
            var ward = new Vector3(-790f, 228.1f, -410f);
            var deck = new Vector3(-766f, 256.2f, -393f);

            Assert.AreNotEqual(Hospital, Shown(before, ward, Ambient, underground: true), "the hospital had no volume");
            Assert.AreEqual(Hospital, Shown(after, ward, Sky, underground: true), "in the hospital");
            Assert.AreEqual(Hospital, Shown(after, ward, Ambient, underground: true), "with its ambience");
            Assert.AreEqual(Command, Shown(after, deck, Sky, underground: false), "on the headquarters' deck");
            Assert.AreEqual(Command, Shown(after, deck, Ambient, underground: false), "with its ambience");
            Assert.AreEqual(0u, Shown(after, new Vector3(-707f, 214f, -472f), Sky, underground: false), "and the base's yard keeps the map's sky");
        }

        private static (List<MapRegion> Before, List<MapRegion> After) Migrated()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Volumes(database);
            migrator.Migrate(Migration);
            return (before, Volumes(database));
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
            Assert.AreEqual(TordenMiresRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in TordenMiresRegions.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in TordenMiresRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TordenMiresRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Mires.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Mires[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TordenMiresRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
