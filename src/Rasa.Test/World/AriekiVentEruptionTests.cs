using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The eruptions of Arieki's geysers and hornitos (AriekiVentEruptions, Arieki_vent_eruptions):
    /// one random explosion at each vent the client's maps draw, but the smoking hornitos and the
    /// small geysers, each going off on a cycle of its own.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AriekiVentEruptionTests
    {
        private const string Before = "20261222000000_Boss_weapons";
        private const string Migration = "20261225000000_Arieki_vent_eruptions";

        private static readonly Dictionary<uint, int> HornitosByMap = new()
        {
            [1759] = 2,   // Torden Mires
            [1761] = 41,  // Torden Incline
            [1764] = 2,   // Torden Plains
            [1977] = 9,   // Magma Caverns
            [1993] = 165, // Burning Steps
            [2028] = 4,   // Torden Abyss
            [2115] = 1    // Fluxite Mines
        };

        [TestMethod]
        public void EveryVentButTheSmokingAndSmallOnesHasAnEruptionOfItsOwn()
        {
            var vents = AriekiVentEruptions.Vents;

            Assert.HasCount(308, vents);
            CollectionAssert.AreEqual(Enumerable.Range((int)AriekiVentEruptions.FirstId, 308).Select(id => (uint)id).ToList(), vents.Select(vent => vent.Id).ToList());
            Assert.AreEqual(308, vents.Select(vent => (vent.Map, vent.Vent)).Distinct().Count(), "one eruption a vent (a .map's entity ids are its own)");

            var geysers = vents.Where(vent => vent.Geyser).ToList();
            Assert.HasCount(84, geysers);
            Assert.IsTrue(geysers.All(vent => vent.Map == 1764 && vent.VentClass.StartsWith("TerraAriekiPlainsGeyserV")), "Torden Plains' large geysers");

            var hornitos = vents.Where(vent => !vent.Geyser).ToList();
            CollectionAssert.AreEquivalent(HornitosByMap.ToList(), hornitos.GroupBy(vent => vent.Map).ToDictionary(group => group.Key, group => group.Count()).ToList());
            Assert.IsTrue(hornitos.All(vent => vent.VentClass.StartsWith("TerraAriekiGenHornitoV") || vent.VentClass.StartsWith("TerraAriekiLigoHornitoV")));

            Assert.IsFalse(vents.Any(vent => vent.VentClass.Contains("Smoking") || vent.VentClass.Contains("Small")));
        }

        [TestMethod]
        public void EachVentKeepsATimeOfItsOwn()
        {
            var vents = AriekiVentEruptions.Vents;

            Assert.IsTrue(vents.All(vent => vent.MinSeconds is >= 30 and <= 60), "the shortest wait, 30-60 s");
            Assert.IsTrue(vents.All(vent => vent.MaxSeconds - vent.MinSeconds is >= 15 and <= 45), "the longest, 15-45 s more");

            // Drawn once per vent: far from all alike.
            Assert.IsGreaterThan(30, vents.Select(vent => vent.MinSeconds).Distinct().Count());
            Assert.IsGreaterThan(200, vents.Select(vent => (vent.MinSeconds, vent.MaxSeconds)).Distinct().Count());

            // On Burning Steps' 165, as many go off in any one second as the waits' spread makes:
            // about three, from vents all over the map.
            var perSecond = vents.Where(vent => vent.Map == 1993).Sum(vent => 2.0 / (vent.MinSeconds + vent.MaxSeconds));
            Assert.IsTrue(perSecond is > 1.5 and < 4.5, $"{perSecond:0.00} a second");
        }

        [TestMethod]
        public void TheMigratedWorldPlacesThemAsGeyserBlastsAndHornitoEruptions()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            try
            {
                using var world = harness.CreateWorld();
                var rows = world.WorldUsables.Get().Where(row => row.Id is >= AriekiVentEruptions.FirstId and <= AriekiVentEruptions.LastId).ToList();
                Assert.HasCount(308, rows);

                var first = rows.Single(row => row.Id == AriekiVentEruptions.FirstId);
                var vent = AriekiVentEruptions.Vents[0];
                Assert.AreEqual((vent.Map, AriekiVentEruptions.HornitoClass, vent.X, vent.Y, vent.Z, vent.MinSeconds, vent.MaxSeconds, 50u, 100u, 6.0),
                    (first.MapContextId, first.ClassId, first.PosX, first.PosY, first.PosZ, first.IntervalMinSeconds, first.IntervalMaxSeconds,
                     first.DamageMin, first.DamageMax, first.Radius));
                StringAssert.Contains(first.Comment, $"0x{vent.Vent:X}");

                Assert.AreEqual(308, PlacedUsables.Load(rows), "every row kept: both classes are random explosions");

                foreach (var placement in PlacedUsables.All)
                {
                    var geyser = placement.ClassId == (EntityClasses)AriekiVentEruptions.GeyserClass;
                    Assert.AreEqual((PlacedUsables.Kind.Explosion, UseObjectState.ReState0, geyser ? PlacedUsables.Geyser : PlacedUsables.Hornito, false),
                        (placement.Kind, placement.State, placement.ActionArg, placement.Usable), placement.ToString());
                    Assert.AreEqual((50u, 100u, 6f), (placement.DamageMin, placement.DamageMax, placement.Radius));
                    Assert.IsTrue(placement.IntervalMinMs >= 30_000 && placement.IntervalMaxMs > placement.IntervalMinMs);
                }
            }
            finally
            {
                PlacedUsables.Load(null);
                PlacedUsables.Reset();
            }
        }

        // A World that ran it as 20261223000000_Arieki_vent_eruptions, the timestamp
        // Random_name_quotes has: it runs again under its own, its rows are put in once, and the
        // old row goes.
        [TestMethod]
        public void AWorldThatRanItUnderTheOldIdEndsWithEachRowOnceAndOnlyTheNewHistoryRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate();
            database.Context.Database.ExecuteSqlRaw(
                $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{Migration}'; " +
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{AriekiVentEruptions.OldId}', '9.0.20');");

            CollectionAssert.Contains(database.Context.Database.GetPendingMigrations().ToList(), Migration);

            migrator.Migrate();

            Assert.AreEqual(308L, database.Context.Database.SqlQueryRaw<long>(
                $"SELECT COUNT(*) AS Value FROM world_usable WHERE id BETWEEN {AriekiVentEruptions.FirstId} AND {AriekiVentEruptions.LastId}").Single());
            var history = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(history, Migration);
            CollectionAssert.DoesNotContain(history, AriekiVentEruptions.OldId);
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "INSERT INTO `world_usable` (`id`, `map_context_id`, `class_id`, `pos_x`, `pos_y`, `pos_z`, `rotation`, `state`, `usable`");
            StringAssert.Contains(up, $"0x{AriekiVentEruptions.Vents[^1].Vent:X}");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, $"delete from world_usable where id in ({string.Join(", ", AriekiVentEruptions.Vents.Select(vent => vent.Id))});");
        }
    }
}
