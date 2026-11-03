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

    /// <summary>Torcastra Prison's prison (Torcastra_prison_region, TorcastraPrisonRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TorcastraPrisonRegionTests
    {
        private const string Migration = "20270701000000_Torcastra_prison_region";
        private const string Previous = "20270630000000_Eloh_space_shrines";

        private const int Sky = 1, Env = 2, Name = 16;

        /// <summary>Torcastra Prison's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Prison = new()
        {
            [2] = (1, 31), [1] = (2, 23), [3] = (3, 0),
        };

        [TestMethod]
        public void ThePrisonBehindTheDoorHasTheBaneInteriorSky()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var examination = new Vector3(-144f, 99.7f, -548f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), examination, Sky, underground: false), "the prison had the canyon's sky");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(1u, Shown(after, examination, Sky, underground: false), "Prisoner Examination");
            Assert.AreEqual(1u, Shown(after, examination, Env, underground: false), "with the tunnel environment map");
            Assert.AreEqual(1u, Shown(after, new Vector3(-16f, 101.6f, -516f), Name, underground: false), "the Security Block");
            Assert.AreEqual(1u, Shown(after, new Vector3(-80f, 101.7f, -395f), Sky, underground: true), "just inside the door");
            Assert.AreEqual(1u, Shown(after, new Vector3(40f, 99.7f, -660f), Sky, underground: false), "the far control room");
            Assert.AreEqual(0u, Shown(after, new Vector3(-80f, 101.4f, -365f), Sky, underground: false), "not the yard outside the door");
            Assert.AreEqual(0u, Shown(after, new Vector3(-94f, 96.9f, -271f), Sky, underground: false), "nor the field medic by the gate");
            Assert.AreEqual(0u, Shown(after, new Vector3(-304f, 13.9f, 288f), Sky, underground: true), "nor the way in");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(TorcastraPrisonRegion.Volumes.Length, once.Except(before).Count(), "the new row");

            foreach (var sql in TorcastraPrisonRegion.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in TorcastraPrisonRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TorcastraPrisonRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Prison.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Prison[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TorcastraPrisonRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
