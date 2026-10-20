using System;
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
    /// Lightning strikes across Torden Plains' Lightning Fields (LightningFieldsStrikes,
    /// Lightning_fields_strikes): 50 RandomExplosionAriekiLightning round control point 23, kept
    /// off its compound and the road west, each on a cycle of its own.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LightningFieldsStrikeTests
    {
        private const string Before = "20270119000000_Index_creature_appearance";
        private const string Migration = "20270120000000_Lightning_fields_strikes";

        private const int Count = 50;

        [TestMethod]
        public void FiftyStrikesFillTheFieldAndKeepOffTheControlPoint()
        {
            var strikes = LightningFieldsStrikes.Strikes;

            Assert.HasCount(Count, strikes);
            CollectionAssert.AreEqual(Enumerable.Range((int)LightningFieldsStrikes.FirstId, Count).Select(id => (uint)id).ToList(), strikes.Select(s => s.Id).ToList());
            Assert.AreEqual(LightningFieldsStrikes.LastId, strikes[^1].Id);

            foreach (var s in strikes)
            {
                Assert.IsLessThanOrEqualTo(LightningFieldsStrikes.FieldRadius,
                    Math.Sqrt(Square(s.X - LightningFieldsStrikes.FieldX) + Square(s.Z - LightningFieldsStrikes.FieldZ)), $"{s.Id} in the Lightning Fields' circle");
                Assert.IsFalse(s.X is >= -182 and <= -75 && s.Z is >= 456 and <= 571, $"{s.Id} in the control point's compound");
                Assert.IsGreaterThanOrEqualTo(-178, s.X, $"{s.Id} on the road to Mt. Hellas");
                Assert.IsTrue(s.Y is > 415 and < 450, $"{s.Id} on the field's ground, {s.Y}");
            }

            // The waypoint, the hospital and the control point itself well clear of every strike.
            foreach (var (place, x, z) in new[] { ("control point 23", -135.449, 516.976), ("its waypoint", -114.4, 544.3), ("its hospital", -145.8, 487.4) })
                Assert.IsGreaterThan(35.0, strikes.Min(s => Math.Sqrt(Square(s.X - x) + Square(s.Z - z))), place);

            // Spread out: none within 28 m of another, and some on each side of the field.
            for (var i = 0; i < strikes.Length; i++)
                for (var j = i + 1; j < strikes.Length; j++)
                    Assert.IsGreaterThanOrEqualTo(28.0, Math.Sqrt(Square(strikes[i].X - strikes[j].X) + Square(strikes[i].Z - strikes[j].Z)), $"{strikes[i].Id} and {strikes[j].Id}");

            Assert.IsTrue(strikes.Any(s => s.X > 80) && strikes.Any(s => s.X < -150) && strikes.Any(s => s.Z > 620) && strikes.Any(s => s.Z < 380));
        }

        [TestMethod]
        public void EachStrikeKeepsATimeOfItsOwn()
        {
            var strikes = LightningFieldsStrikes.Strikes;

            Assert.IsTrue(strikes.All(s => s.MinSeconds is >= 20 and <= 45), "the shortest wait, 20-45 s");
            Assert.IsTrue(strikes.All(s => s.MaxSeconds - s.MinSeconds is >= 15 and <= 40), "the longest, 15-40 s more");
            Assert.IsGreaterThan(15, strikes.Select(s => s.MinSeconds).Distinct().Count());
            Assert.AreEqual(Count, strikes.Select(s => (s.MinSeconds, s.MaxSeconds)).Distinct().Count());

            // About one a second somewhere on the field.
            var perSecond = strikes.Sum(s => 2.0 / (s.MinSeconds + s.MaxSeconds));
            Assert.IsTrue(perSecond is > 0.8 and < 1.5, $"{perSecond:0.00} a second");
        }

        [TestMethod]
        public void TheMigratedWorldPlacesThemAsElectricalLightningStrikes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            try
            {
                using var world = harness.CreateWorld();
                var rows = world.WorldUsables.Get().Where(row => row.Id is >= LightningFieldsStrikes.FirstId and <= LightningFieldsStrikes.LastId).ToList();
                Assert.HasCount(Count, rows);

                var first = rows.Single(row => row.Id == LightningFieldsStrikes.FirstId);
                var strike = LightningFieldsStrikes.Strikes[0];
                Assert.AreEqual((1764u, LightningFieldsStrikes.LightningClass, strike.X, strike.Y, strike.Z, strike.MinSeconds, strike.MaxSeconds, 250u, 750u, 5.0),
                    (first.MapContextId, first.ClassId, first.PosX, first.PosY, first.PosZ, first.IntervalMinSeconds, first.IntervalMaxSeconds,
                     first.DamageMin, first.DamageMax, first.Radius));
                StringAssert.Contains(first.Comment, "Lightning Fields");

                Assert.AreEqual(Count, PlacedUsables.Load(rows), "every row kept: a random explosion");

                foreach (var placement in PlacedUsables.All)
                {
                    Assert.AreEqual((PlacedUsables.Kind.Explosion, UseObjectState.ReState0, PlacedUsables.Lightning, false),
                        (placement.Kind, placement.State, placement.ActionArg, placement.Usable), placement.ToString());
                    Assert.AreEqual(DamageType.Electrical, PlacedUsables.DamageTypeOf(placement.ActionArg));
                    Assert.AreEqual((250u, 750u, 5f), (placement.DamageMin, placement.DamageMax, placement.Radius));
                    Assert.IsTrue(placement.IntervalMinMs >= 20_000 && placement.IntervalMaxMs > placement.IntervalMinMs);
                }
            }
            finally
            {
                PlacedUsables.Load(null);
                PlacedUsables.Reset();
            }
        }

        [TestMethod]
        public void TheUpRunsAgainOverItsRowsAndTheDownTakesThemOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            Assert.AreEqual(0L, Strikes(database));
            var others = Others(database);

            migrator.Migrate(Migration);
            Assert.AreEqual((long)Count, Strikes(database));
            Assert.AreEqual(others, Others(database), "no other row touched");

            // A World that has them (a GM's copy, a half-run Up) gets each once.
            migrator.Migrate(Before);
            database.Context.Database.ExecuteSqlRaw(
                "INSERT INTO world_usable (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, state, usable, item_template_id, item_class_id, item_count, " +
                "reset_seconds, interval_min_seconds, interval_max_seconds, damage_min, damage_max, radius, action_arg, comment) " +
                $"VALUES ({LightningFieldsStrikes.FirstId}, 1764, 8002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 'old');");
            migrator.Migrate(Migration);
            Assert.AreEqual((long)Count, Strikes(database));

            migrator.Migrate(Before);
            Assert.AreEqual(0L, Strikes(database));
            Assert.AreEqual(others, Others(database));
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);
            var ids = string.Join(", ", LightningFieldsStrikes.Strikes.Select(s => s.Id));

            StringAssert.Contains(up, $"delete from world_usable where id in ({ids});");
            StringAssert.Contains(up, "INSERT INTO `world_usable` (`id`, `map_context_id`, `class_id`, `pos_x`, `pos_y`, `pos_z`, `rotation`, `state`, `usable`");
            StringAssert.Contains(up, "Lightning strike, Lightning Fields");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, $"delete from world_usable where id in ({ids});");
        }

        private static double Square(double value) => value * value;

        private static long Strikes(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<long>(
                $"SELECT COUNT(*) AS Value FROM world_usable WHERE id BETWEEN {LightningFieldsStrikes.FirstId} AND {LightningFieldsStrikes.LastId}").AsEnumerable().Single();

        private static long Others(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<long>(
                $"SELECT COUNT(*) AS Value FROM world_usable WHERE id NOT BETWEEN {LightningFieldsStrikes.FirstId} AND {LightningFieldsStrikes.LastId}").AsEnumerable().Single();
    }
}
